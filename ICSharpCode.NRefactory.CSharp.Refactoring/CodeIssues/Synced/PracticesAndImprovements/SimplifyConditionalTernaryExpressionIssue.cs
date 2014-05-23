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
	[ExportDiagnosticAnalyzer("", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(Description = "", AnalysisDisableKeyword = "")]
	[IssueDescription("Simplify conditional expression",
	                  Description = "Conditional expression can be simplified",
	                  Category = IssueCategories.PracticesAndImprovements,
	                  Severity = Severity.Suggestion,
	                  AnalysisDisableKeyword = "SimplifyConditionalTernaryExpression")]
	public class SimplifyConditionalTernaryExpressionIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "";
		const string Description            = "";
		const string MessageFormat          = "";
		const string Category               = IssueCategories.PracticesAndImprovements;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning);

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

			static bool? GetBool(Expression trueExpression)
			{
				var pExpr = trueExpression as PrimitiveExpression;
				if (pExpr == null || !(pExpr.Value is bool))
					return null;
				return (bool)pExpr.Value;
			}

			public override void VisitConditionalExpression(ConditionalExpression conditionalExpression)
			{
				base.VisitConditionalExpression(conditionalExpression);

				bool? trueBranch = GetBool(CSharpUtil.GetInnerMostExpression(conditionalExpression.TrueExpression));
				bool? falseBranch = GetBool(CSharpUtil.GetInnerMostExpression(conditionalExpression.FalseExpression));

				if (trueBranch == falseBranch || 
				    trueBranch == true && falseBranch == false) // Handled by RedundantTernaryExpressionIssue
					return;

				AddIssue(new CodeIssue(
					conditionalExpression.QuestionMarkToken.StartLocation,
					conditionalExpression.FalseExpression.EndLocation,
					ctx.TranslateString("Simplify conditional expression"),
					ctx.TranslateString("Simplify conditional expression"),
					script => {
						if (trueBranch == false && falseBranch == true) {
							script.Replace(conditionalExpression, CSharpUtil.InvertCondition(conditionalExpression.Condition));
							return;
						}
						if (trueBranch == true) {
							script.Replace(
								conditionalExpression,
								new BinaryOperatorExpression(
									conditionalExpression.Condition.Clone(), 
									BinaryOperatorType.ConditionalOr,
									conditionalExpression.FalseExpression.Clone()
								)
							);
							return;
						}

						if (trueBranch == false) {
							script.Replace(
								conditionalExpression,
								new BinaryOperatorExpression(
									CSharpUtil.InvertCondition(conditionalExpression.Condition), 
									BinaryOperatorType.ConditionalAnd,
									conditionalExpression.FalseExpression.Clone()
								)
							);
							return;
						}
						
						if (falseBranch == true) {
							script.Replace(
								conditionalExpression,
								new BinaryOperatorExpression(
									CSharpUtil.InvertCondition(conditionalExpression.Condition), 
									BinaryOperatorType.ConditionalOr,
									conditionalExpression.TrueExpression.Clone()
								)
							);
							return;
						}

						if (falseBranch == false) {
							script.Replace(
								conditionalExpression,
								new BinaryOperatorExpression(
									conditionalExpression.Condition.Clone(), 
									BinaryOperatorType.ConditionalAnd,
									conditionalExpression.TrueExpression.Clone()
								)
							);
							return;
						}

						// Should never happen
					}
				));
			}
		}
	}

	[ExportCodeFixProvider(.DiagnosticId, LanguageNames.CSharp)]
	public class FixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return .DiagnosticId;
		}

		public async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagonstic in diagnostics) {
				var node = root.FindNode(diagonstic.Location.SourceSpan);
				//if (!node.IsKind(SyntaxKind.BaseList))
				//	continue;
				var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
				result.Add(CodeActionFactory.Create(node.Span, diagonstic.Severity, diagonstic.GetMessage(), document.WithSyntaxRoot(newRoot)));
			}
			return result;
		}
	}
}