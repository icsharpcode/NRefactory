//
// SimplifyAnonymousMethodToDelegateIssue.cs
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
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using System.Threading;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ConvertClosureToMethodGroup")]
	public class ConvertClosureToMethodGroupIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "ConvertClosureToMethodGroupIssue";
		const string Description            = "Anonymous method or lambda expression can be simplified to method group";
		const string MessageFormat          = "{0}"; // "Anonymous method can be simplified to method group" / "Lambda expression can be simplified to method group"
		const string Category               = IssueCategories.Opportunities;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "Convert anonymous method to method group");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
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
			}
			return result;
		}

		class GatherVisitor : GatherVisitorBase<ConvertClosureToMethodGroupIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
			{
				base.VisitSimpleLambdaExpression(node);
				AnalyzeExpression(node, node.Body, new [] { node.Parameter });
			}

			public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
			{
				base.VisitParenthesizedLambdaExpression(node);
				AnalyzeExpression(node, node.Body, node.ParameterList.Parameters);
			}

			public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
			{
				base.VisitAnonymousMethodExpression(node);
				AnalyzeExpression(node, node.Block, node.ParameterList.Parameters);
			}

			static bool IsSimpleTarget(ExpressionSyntax target)
			{
				if (target is IdentifierNameSyntax)
					return true;
				var mref = target as MemberAccessExpressionSyntax;
				if (mref != null)
					return IsSimpleTarget (mref.Expression);
				return false;
			}

			void AnalyzeExpression(SyntaxNode node, SyntaxNode body, IReadOnlyList<ParameterSyntax> parameters)
			{
				var invocation = AnalyzeBody(body);
				if (invocation == null)
					return;
				if (!IsSimpleTarget (invocation.Expression))
					return;

//				var rr = ctx.Resolve (invocation) as CSharpInvocationResolveResult;
//				if (rr == null)
//					return;
//				var lambdaParameters = parameters.ToList();
//				var arguments = rr.GetArgumentsForCall();
//				if (lambdaParameters.Count != arguments.Count)
//					return;
//				for (int i = 0; i < arguments.Count; i++) {
//					var arg = UnpackImplicitIdentityOrReferenceConversion(arguments[i]) as LocalResolveResult;
//					if (arg == null || arg.Variable.Name != lambdaParameters[i].Name)
//						return;
//				}
//				var returnConv = ctx.GetConversion(invocation);
//				if (returnConv.IsExplicit || !(returnConv.IsIdentityConversion || returnConv.IsReferenceConversion))
//					return;
//				var validTypes = TypeGuessing.GetValidTypes (ctx.Resolver, expression).ToList ();
//
//				// search for method group collisions
//				var targetResult = ctx.Resolve(invocation.Target) as MethodGroupResolveResult;
//				if (targetResult != null) {
//					foreach (var t in validTypes) {
//						if (t.Kind != TypeKind.Delegate)
//							continue;
//						var invokeMethod = t.GetDelegateInvokeMethod();
//
//						foreach (var otherMethod in targetResult.Methods) {
//							if (otherMethod == rr.Member)
//								continue;
//							if (ParameterListComparer.Instance.Equals(otherMethod.Parameters, invokeMethod.Parameters))
//								return;
//						}
//					}
//				}
//
//				bool isValidReturnType = false;
//				foreach (var t in validTypes) {
//					if (t.Kind != TypeKind.Delegate)
//						continue;
//					var invokeMethod = t.GetDelegateInvokeMethod();
//					isValidReturnType = rr.Member.ReturnType == invokeMethod.ReturnType || rr.Member.ReturnType.GetAllBaseTypes().Contains(invokeMethod.ReturnType);
//					if (isValidReturnType)
//						break;
//				}
//				if (!isValidReturnType)
//					return;
//
//				if (rr.IsDelegateInvocation) {
//					if (!validTypes.Contains(rr.Member.DeclaringType))
//						return;
//				}
//
//				AddIssue(new CodeIssue(expression,
//				         expression is AnonymousMethodExpression ? ctx.TranslateString("Anonymous method can be simplified to method group") : ctx.TranslateString("Lambda expression can be simplified to method group"), 
//				         ctx.TranslateString(), script =>  {
//					if (validTypes.Any (t => t.FullName == "System.Func" && t.TypeParameterCount == 1 + parameters.Count) && validTypes.Any (t => t.FullName == "System.Action")) {
//						if (rr != null && rr.Member.ReturnType.Kind != TypeKind.Void) {
//							var builder = ctx.CreateTypeSystemAstBuilder (expression);
//							var type = builder.ConvertType(new TopLevelTypeName("System", "Func", 1));
//							var args = type.GetChildrenByRole(Roles.TypeArgument);
//							args.Clear ();
//							foreach (var pde in parameters) {
//								args.Add (builder.ConvertType (ctx.Resolve (pde).Type));
//							}
//							args.Add (builder.ConvertType (rr.Member.ReturnType));
//							script.Replace(expression, new CastExpression (type, invocation.Target.Clone()));
//							return;
//						}
//					}
//					script.Replace(expression, invocation.Target.Clone());
//				}));
			}
//			
//			static ResolveResult UnpackImplicitIdentityOrReferenceConversion(ResolveResult rr)
//			{
//				var crr = rr as ConversionResolveResult;
//				if (crr != null && crr.Conversion.IsImplicit && (crr.Conversion.IsIdentityConversion || crr.Conversion.IsReferenceConversion))
//					return crr.Input;
//				return rr;
//			}
		}
	}

	[ExportCodeFixProvider(ConvertClosureToMethodGroupIssue.DiagnosticId, LanguageNames.CSharp)]
	public class ConvertClosureToMethodGroupFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ConvertClosureToMethodGroupIssue.DiagnosticId;
		}
		public override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}
		public override async Task ComputeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			var cancellationToken = context.CancellationToken;
			var span = context.Span;
			var diagnostics = context.Diagnostics;
			var root = await document.GetSyntaxRootAsync(cancellationToken);

			foreach (var diagnostic in diagnostics) {
				var node = root.FindNode(diagnostic.Location.SourceSpan);
				if (!node.IsKind(SyntaxKind.BaseList))
					continue;
				context.RegisterFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Replace with method group", token => {
					var c1 = node as AnonymousMethodExpressionSyntax;
					var c2 = node as ParenthesizedLambdaExpressionSyntax;
					var c3 = node as SimpleLambdaExpressionSyntax;
					InvocationExpressionSyntax invoke = null;
					if (c1 != null)
						invoke = ConvertClosureToMethodGroupIssue.AnalyzeBody(c1.Block);
					if (c2 != null)
						invoke = ConvertClosureToMethodGroupIssue.AnalyzeBody(c2.Body);
					if (c3 != null)
						invoke = ConvertClosureToMethodGroupIssue.AnalyzeBody(c3.Body);
					var newRoot = root.ReplaceNode((SyntaxNode)node, invoke.Expression);
					return Task.FromResult(document.WithSyntaxRoot(newRoot));
				}), diagnostic);
			}
		}
	}
}