//
// RedundantNullCoalescingExpressionIssue.cs
//
// Author:
//       Luís Reis <luiscubal@gmail.com>
//
// Copyright (c) 2013 Luís Reis
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
	[NRefactoryCodeDiagnosticAnalyzer (AnalysisDisableKeyword = "ConstantNullCoalescingCondition")]
	public class ConstantNullCoalescingConditionIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "ConstantNullCoalescingConditionIssue";
		const string Description = "Finds redundant null coalescing expressions such as expr ?? expr";
		const string Category               = IssueCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule1 = new DiagnosticDescriptor (DiagnosticId, "Redundant ??. Right side is always null.", "Remove redundant right side", Category, DiagnosticSeverity.Warning, true, "'??' condition is known to be null or not null");
		static readonly DiagnosticDescriptor Rule2 = new DiagnosticDescriptor (DiagnosticId, "Redundant ??. Left side is always null.", "Remove redundant left side", Category, DiagnosticSeverity.Warning, true, "'??' condition is known to be null or not null");
		static readonly DiagnosticDescriptor Rule3 = new DiagnosticDescriptor (DiagnosticId, "Redundant ??. Left side is never null.", "Remove redundant right side", Category, DiagnosticSeverity.Warning, true, "'??' condition is known to be null or not null");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule1, Rule2, Rule3);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<ConstantNullCoalescingConditionIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			Dictionary<AstNode, NullValueAnalysis> cachedNullAnalysis = new Dictionary<AstNode, NullValueAnalysis>();
//
//			public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
//			{
//				base.VisitBinaryOperatorExpression(binaryOperatorExpression);
//
//				if (binaryOperatorExpression.Operator != BinaryOperatorType.NullCoalescing) {
//					//The issue is not applicable
//					return;
//				}
//
//				var parentFunction = GetParentFunctionNode(binaryOperatorExpression);
//				var analysis = GetAnalysis(parentFunction);
//
//				NullValueStatus leftStatus = analysis.GetExpressionResult(binaryOperatorExpression.Left);
//				if (leftStatus == NullValueStatus.DefinitelyNotNull) {
//					AddIssue(new CodeIssue(binaryOperatorExpression.OperatorToken.StartLocation,
//					         binaryOperatorExpression.Right.EndLocation,
//					         ctx.TranslateString(""),
//					         ctx.TranslateString(""),
//					         script => {
//
//						script.Replace(binaryOperatorExpression, binaryOperatorExpression.Left.Clone());
//
//						}) { IssueMarker = IssueMarker.GrayOut });
//					return;
//				}
//				if (leftStatus == NullValueStatus.DefinitelyNull) {
//					AddIssue(new CodeIssue(binaryOperatorExpression.Left.StartLocation,
//					         binaryOperatorExpression.OperatorToken.EndLocation,
//					         ctx.TranslateString(""),
//					         ctx.TranslateString(""),
//					         script => {
//
//						script.Replace(binaryOperatorExpression, binaryOperatorExpression.Right.Clone());
//
//						}));
//					return;
//				}
//				NullValueStatus rightStatus = analysis.GetExpressionResult(binaryOperatorExpression.Right);
//				if (rightStatus == NullValueStatus.DefinitelyNull) {
//					AddIssue(new CodeIssue(binaryOperatorExpression.OperatorToken.StartLocation,
//					         binaryOperatorExpression.Right.EndLocation,
//					         ctx.TranslateString(""),
//					         ctx.TranslateString(""),
//					         script => {
//
//						script.Replace(binaryOperatorExpression, binaryOperatorExpression.Left.Clone());
//
//						}));
//					return;
//				}
			}

//			NullValueAnalysis GetAnalysis(AstNode parentFunction)
//			{
//				NullValueAnalysis analysis;
//				if (cachedNullAnalysis.TryGetValue(parentFunction, out analysis)) {
//					return analysis;
//				}
//
//				analysis = new NullValueAnalysis(ctx, parentFunction.GetChildByRole(Roles.Body), parentFunction.GetChildrenByRole(Roles.Parameter), ctx.CancellationToken);
//				analysis.IsParametersAreUninitialized = true;
//				analysis.Analyze();
//				cachedNullAnalysis [parentFunction] = analysis;
//				return analysis;
//			}
//		}
//
//		public static AstNode GetParentFunctionNode(AstNode node)
//		{
//			do {
//				node = node.Parent;
//			} while (node != null && !IsFunctionNode(node));
//
//			return node;
//		}
//
//		static bool IsFunctionNode(AstNode node)
//		{
//			return node is EntityDeclaration ||
//				node is LambdaExpression ||
//					node is AnonymousMethodExpression;
//		}
	}

	[ExportCodeFixProvider(ConstantNullCoalescingConditionIssue.DiagnosticId, LanguageNames.CSharp)]
	public class ConstantNullCoalescingConditionFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ConstantNullCoalescingConditionIssue.DiagnosticId;
		}

		public override async Task ComputeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			var cancellationToken = context.CancellationToken;
			var span = context.Span;
			var diagnostics = context.Diagnostics;
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagnostic in diagnostics) {
				var node = root.FindNode(diagnostic.Location.SourceSpan);
				//if (!node.IsKind(SyntaxKind.BaseList))
				//	continue;
				var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
				context.RegisterFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, diagnostic.GetMessage(), document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}