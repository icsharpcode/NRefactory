// 
// ReplaceWithSingleCallToAnyAnalyzer.cs
//
// Author:
//       Mike Kr¨¹ger <mkrueger@xamarin.com>
// 
// Copyright (c) 2013 Xamarin <http://xamarin.com>
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

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ReplaceWithSingleCallToAnyAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ReplaceWithSingleCallToAnyAnalyzerID, 
			GettextCatalog.GetString("Redundant Where() call with predicate followed by Any()"),
			GettextCatalog.GetString("Replace with single call to 'Any()'"), 
			DiagnosticAnalyzerCategories.PracticesAndImprovements, 
			DiagnosticSeverity.Info, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.ReplaceWithSingleCallToAnyAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryGetDiagnostic(nodeContext, out diagnostic))
						nodeContext.ReportDiagnostic(diagnostic);
				},
				SyntaxKind.InvocationExpression

			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			var anyInvoke = nodeContext.Node as InvocationExpressionSyntax;
			var info = nodeContext.SemanticModel.GetSymbolInfo (anyInvoke);

			IMethodSymbol anyResolve = info.Symbol as IMethodSymbol;
			if (anyResolve == null) {
				anyResolve = info.CandidateSymbols.OfType<IMethodSymbol> ().FirstOrDefault (candidate => HasPredicateVersion (candidate));
			}

			if (anyResolve == null || !HasPredicateVersion (anyResolve))
				return false;

			ExpressionSyntax target;
			InvocationExpressionSyntax whereInvoke;
			if (!MatchWhere (anyInvoke, out target, out whereInvoke))
				return false;
			info = nodeContext.SemanticModel.GetSymbolInfo (whereInvoke);
			IMethodSymbol whereResolve = info.Symbol as IMethodSymbol;
			if (whereResolve == null) {
				whereResolve = info.CandidateSymbols.OfType<IMethodSymbol> ().FirstOrDefault (candidate => candidate.Name == "Where" && IsQueryExtensionClass (candidate.ContainingType));
			}

			if (whereResolve == null || whereResolve.Name != "Where" || !IsQueryExtensionClass (whereResolve.ContainingType))
				return false;
			if (whereResolve.Parameters.Length != 1)
				return false;
			var predResolve = whereResolve.Parameters [0];
			if (predResolve.Type.GetTypeParameters ().Length != 2)
				return false;
			diagnostic = Diagnostic.Create (
				descriptor,
				anyInvoke.GetLocation ()
			);
			return true;
		}

		static bool HasPredicateVersion (IMethodSymbol member)
		{
			if (!IsQueryExtensionClass (member.ContainingType))
				return false;
			return member.Name == "Any";
		}

		internal static bool MatchWhere(InvocationExpressionSyntax anyInvoke, out ExpressionSyntax target, out InvocationExpressionSyntax whereInvoke)
		{
			target = null;
			whereInvoke = null;

			if (anyInvoke.ArgumentList.Arguments.Count != 0)
				return false;
			var anyInvokeBase = anyInvoke.Expression as MemberAccessExpressionSyntax;
			if (anyInvokeBase == null)
				return false;
			whereInvoke = anyInvokeBase.Expression as InvocationExpressionSyntax;
			if (whereInvoke == null || whereInvoke.ArgumentList.Arguments.Count != 1)
				return false;
			var baseMember = whereInvoke.Expression as MemberAccessExpressionSyntax;
			if (baseMember == null || baseMember.Name.Identifier.Text != "Where")
				return false;
			target = baseMember.Expression;

			return target != null;
		}



		internal static InvocationExpressionSyntax MakeSingleCall(InvocationExpressionSyntax anyInvoke)
		{
			var member = ((MemberAccessExpressionSyntax)anyInvoke.Expression).Name;
			ExpressionSyntax target;
			InvocationExpressionSyntax whereInvoke;
			if (MatchWhere(anyInvoke, out target, out whereInvoke))
			{
				var callerExpr = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, target, member).WithAdditionalAnnotations(Formatter.Annotation);
				var argument = whereInvoke.ArgumentList.Arguments[0].WithAdditionalAnnotations(Formatter.Annotation);
				return SyntaxFactory.InvocationExpression(callerExpr, SyntaxFactory.ArgumentList (SyntaxFactory.SeparatedList (new [] { argument })));
			}

			return null;
		}

		internal static bool IsQueryExtensionClass (INamedTypeSymbol typeDef)
		{
			if (typeDef == null || typeDef.ContainingNamespace == null || typeDef.ContainingNamespace.GetFullName () != "System.Linq")
				return false;
			switch (typeDef.Name) {
			case "Enumerable":
			case "ParallelEnumerable":
			case "Queryable":
				return true;
			default:
				return false;
			}
		}
	}
}