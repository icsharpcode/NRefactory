//
// SimplifyConditionalTernaryExpressionIssue.cs
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "SimplifyConditionalTernaryExpression")]
	public class SimplifyConditionalTernaryExpressionIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "SimplifyConditionalTernaryExpressionIssue";
		const string Description            = "Conditional expression can be simplified";
		const string MessageFormat          = "Simplify conditional expression";
		const string Category               = IssueCategories.PracticesAndImprovements;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "Simplify conditional expression");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<SimplifyConditionalTernaryExpressionIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			static bool? GetBool(Expression trueExpression)
//			{
//				var pExpr = trueExpression as PrimitiveExpression;
//				if (pExpr == null || !(pExpr.Value is bool))
//					return null;
//				return (bool)pExpr.Value;
//			}
//
//			public override void VisitConditionalExpression(ConditionalExpression conditionalExpression)
//			{
//				base.VisitConditionalExpression(conditionalExpression);
//
//				bool? trueBranch = GetBool(CSharpUtil.GetInnerMostExpression(conditionalExpression.TrueExpression));
//				bool? falseBranch = GetBool(CSharpUtil.GetInnerMostExpression(conditionalExpression.FalseExpression));
//
//				if (trueBranch == falseBranch || 
//				    trueBranch == true && falseBranch == false) // Handled by RedundantTernaryExpressionIssue
//					return;
//
//				AddIssue(new CodeIssue(
//					conditionalExpression.QuestionMarkToken.StartLocation,
//					conditionalExpression.FalseExpression.EndLocation,
//					ctx.TranslateString(""),
//					ctx.TranslateString("Simplify conditional expression"),
//					script => {
//						if (trueBranch == false && falseBranch == true) {
//							script.Replace(conditionalExpression, CSharpUtil.InvertCondition(conditionalExpression.Condition));
//							return;
//						}
//						if (trueBranch == true) {
//							script.Replace(
//								conditionalExpression,
//								new BinaryOperatorExpression(
//									conditionalExpression.Condition.Clone(), 
//									BinaryOperatorType.ConditionalOr,
//									conditionalExpression.FalseExpression.Clone()
//								)
//							);
//							return;
//						}
//
//						if (trueBranch == false) {
//							script.Replace(
//								conditionalExpression,
//								new BinaryOperatorExpression(
//									CSharpUtil.InvertCondition(conditionalExpression.Condition), 
//									BinaryOperatorType.ConditionalAnd,
//									conditionalExpression.FalseExpression.Clone()
//								)
//							);
//							return;
//						}
//						
//						if (falseBranch == true) {
//							script.Replace(
//								conditionalExpression,
//								new BinaryOperatorExpression(
//									CSharpUtil.InvertCondition(conditionalExpression.Condition), 
//									BinaryOperatorType.ConditionalOr,
//									conditionalExpression.TrueExpression.Clone()
//								)
//							);
//							return;
//						}
//
//						if (falseBranch == false) {
//							script.Replace(
//								conditionalExpression,
//								new BinaryOperatorExpression(
//									conditionalExpression.Condition.Clone(), 
//									BinaryOperatorType.ConditionalAnd,
//									conditionalExpression.TrueExpression.Clone()
//								)
//							);
//							return;
//						}
//
//						// Should never happen
//					}
//				));
//			}
		}
	}

	[ExportCodeFixProvider(SimplifyConditionalTernaryExpressionIssue.DiagnosticId, LanguageNames.CSharp)]
	public class SimplifyConditionalTernaryExpressionFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return SimplifyConditionalTernaryExpressionIssue.DiagnosticId;
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
				context.RegisterFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Simplify conditional expression", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}