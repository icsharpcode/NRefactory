//
// ConvertClosureToMethodGroupAnalyzer.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ConvertClosureToMethodGroupAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ConvertClosureToMethodDiagnosticID, 
			GettextCatalog.GetString("Convert anonymous method to method group"),
			GettextCatalog.GetString("{0}"), 
			DiagnosticAnalyzerCategories.Opportunities, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,//"Anonymous method or lambda expression can be simplified to method group"
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.ConvertClosureToMethodDiagnosticID),
			customTags: DiagnosticCustomTags.Unnecessary
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryConvertClosureToMethodGroup (nodeContext, out diagnostic)) {
						nodeContext.ReportDiagnostic(diagnostic);
					}
				}, 
				new SyntaxKind[] {  SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression, SyntaxKind.AnonymousMethodExpression }
			);
		}

		bool TryConvertClosureToMethodGroup (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			var simpleLambda = nodeContext.Node as SimpleLambdaExpressionSyntax;
			var parenLambda = nodeContext.Node as ParenthesizedLambdaExpressionSyntax;
			var anoMethod = nodeContext.Node as AnonymousMethodExpressionSyntax;
			diagnostic = default(Diagnostic);
			var body = simpleLambda?.Body ?? parenLambda?.Body ?? anoMethod?.Block;
			if (body == null)
				return false;

			var invocation = AnalyzeBody(body);
			if (invocation == null)
				return false;
			if (!IsSimpleTarget (invocation.Expression))
				return false;

			var symbolInfo = nodeContext.SemanticModel.GetSymbolInfo (invocation);
			var method = symbolInfo.Symbol as IMethodSymbol;
			if (method == null)
				return false;

			foreach (var param in method.Parameters) {
				if (param.RefKind == RefKind.Ref || param.RefKind == RefKind.Out || param.IsParams)
					return false;
			}

			IReadOnlyList<ParameterSyntax> lambdaParameters = parenLambda?.ParameterList?.Parameters ?? anoMethod?.ParameterList?.Parameters;
			if (simpleLambda != null)
				lambdaParameters = new [] { simpleLambda.Parameter };
			
			var arguments = invocation.ArgumentList.Arguments;
			if (method.Parameters.Length != arguments.Count || lambdaParameters.Count != arguments.Count)
				return false;
			
			for (int i = 0; i < arguments.Count && i < lambdaParameters.Count; i++) {
//				var arg = UnpackImplicitIdentityOrReferenceConversion(arguments[i]) as LocalResolveResult;
				if (arguments[i].Expression.ToString () != lambdaParameters[i].Identifier.ToString ())
					return false;
			}

			var returnConv = nodeContext.SemanticModel.GetConversion (invocation, nodeContext.CancellationToken);
			if (returnConv.IsExplicit || !(returnConv.IsIdentity || returnConv.IsReference))
				return false;
			
			var validTypes = TypeGuessing.GetValidTypes (nodeContext.SemanticModel, nodeContext.Node, nodeContext.CancellationToken).ToList ();

			// search for method group collisions
			foreach (var t in validTypes) {
				if (t.TypeKind != TypeKind.Delegate)
					continue;
				var invokeMethod = t.GetDelegateInvokeMethod();

				foreach (var otherMethod in GetMethodGroup(method)) {
					if (otherMethod == method)
						continue;
					if (SignatureComparer.HaveSameSignature(otherMethod.GetParameters (), invokeMethod.Parameters))
						return false;
				}
			}

			bool isValidReturnType = false;
			foreach (var t in validTypes) {
				if (t.TypeKind != TypeKind.Delegate)
					continue;
				var invokeMethod = t.GetDelegateInvokeMethod ();
				isValidReturnType = method.ReturnType == invokeMethod.ReturnType || method.ReturnType.GetBaseTypes ().Contains (invokeMethod.ReturnType);
				if (isValidReturnType)
					break;
			}
			if (!isValidReturnType)
				return false;

			if (method.ContainingType.TypeKind == TypeKind.Delegate) {
				if (!validTypes.Contains(method.ContainingType))
					return false;
			}

			diagnostic = Diagnostic.Create (
				descriptor,
				nodeContext.Node.GetLocation (),
				anoMethod != null ? GettextCatalog.GetString("Anonymous method can be simplified to method group") : GettextCatalog.GetString("Lambda expression can be simplified to method group")
			);
			return true;
		}

		internal static IEnumerable<ISymbol> GetMethodGroup(IMethodSymbol symbol)
		{
			var containingType = symbol.ContainingType;
			if (containingType.Kind == SymbolKind.NamedType) {
				foreach (var member in containingType.GetMembers()) {
					if (string.Equals(member.MetadataName, symbol.MetadataName, StringComparison.Ordinal) && member is IMethodSymbol)
						yield return member;
				}
			}
		}

//			static ResolveResult UnpackImplicitIdentityOrReferenceConversion(ResolveResult rr)
//			{
//				var crr = rr as ConversionResolveResult;
//				if (crr != null && crr.Conversion.IsImplicit && (crr.Conversion.IsIdentityConversion || crr.Conversion.IsReferenceConversion))
//					return crr.Input;
//				return rr;
//			}

		static bool IsSimpleTarget(ExpressionSyntax target)
		{
			if (target is IdentifierNameSyntax)
				return true;
			var mref = target as MemberAccessExpressionSyntax;
			if (mref != null)
				return IsSimpleTarget (mref.Expression);
			return false;
		}

		internal static InvocationExpressionSyntax AnalyzeBody(SyntaxNode body)
		{
			var result = body as InvocationExpressionSyntax;
			if (result != null)
				return result;
			var block = body as BlockSyntax;
			if (block != null && block.Statements.Count == 1) {
				var stmt = block.Statements[0] as ExpressionStatementSyntax;
				if (stmt != null)
					result = stmt.Expression as InvocationExpressionSyntax;

				var returnStmt = block.Statements[0] as ReturnStatementSyntax; 
				if (returnStmt != null)
					result = returnStmt.Expression as InvocationExpressionSyntax;
			}
			return result;
		}
	}
}