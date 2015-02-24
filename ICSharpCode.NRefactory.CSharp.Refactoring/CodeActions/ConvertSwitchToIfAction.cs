// 
// ConvertSwitchToIfAction.cs
//  
// Author:
//       Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang <lightyang0@gmail.com>
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
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Convert 'switch' statement to 'if' statement")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Convert 'switch' to 'if'")]
	public class ConvertSwitchToIfAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var node = root.FindNode(span) as SwitchStatementSyntax;

			if (node == null || node.Sections.Count == 0 || node.Sections.All(l => l.Labels.Any(s => s.Keyword.IsKind(SyntaxKind.DefaultKeyword))))
				return;

			foreach (var section in node.Sections) {
				var lastStatement = section.Statements.LastOrDefault();
				//ignore non-trailing breaks
				if(HasNonTrailingBreaks(section, lastStatement as BreakStatementSyntax))
					return;
			}

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Info,
					"Convert to 'if'",
					t2 => {
						var ifNodes = new List<IfStatementSyntax>();
						ElseClauseSyntax defaultElse = null;

						foreach (var section in node.Sections) {
							var condition = CollectCondition(node.Expression, section.Labels);
							var body = SyntaxFactory.Block();
							var last = section.Statements.LastOrDefault();
							foreach (var statement in section.Statements) {
								if (statement.IsEquivalentTo(last) && statement is BreakStatementSyntax)
									continue;
								body = body.WithStatements(body.Statements.Add(statement));
							}

							//default => else
							if (condition == null) {
								defaultElse = SyntaxFactory.ElseClause(body)
									.WithLeadingTrivia(section.GetLeadingTrivia())
									.WithTrailingTrivia(section.GetTrailingTrivia());
								break;
							}
							ifNodes.Add(SyntaxFactory.IfStatement(condition, body).WithLeadingTrivia(section.GetLeadingTrivia()));
						}

						IfStatementSyntax ifStatement = null;
						//reverse the list and chain them
						foreach (IfStatementSyntax ifs in ifNodes.Reverse<IfStatementSyntax>()) {
							if (ifStatement == null) {
								ifStatement = ifs;
								if (defaultElse != null)
									ifStatement = ifStatement.WithElse(defaultElse);
							}
							else
								ifStatement = ifs.WithElse(SyntaxFactory.ElseClause(ifStatement));
						}

						ifStatement = ifStatement.WithLeadingTrivia(node.GetLeadingTrivia().Concat(ifStatement.GetLeadingTrivia())).WithTrailingTrivia(node.GetTrailingTrivia());

						return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode((SyntaxNode)node, ifStatement.WithAdditionalAnnotations(Formatter.Annotation))));
					})
			);
		}

		ExpressionSyntax CollectCondition(ExpressionSyntax expressionSyntax, SyntaxList<SwitchLabelSyntax> labels)
		{
			//default
			if (labels.Count == 0 || labels.OfType<DefaultSwitchLabelSyntax>().Any())
				return null;

			var conditionList = 
				labels
					.OfType<CaseSwitchLabelSyntax>()
					.Select(l => SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, expressionSyntax, l.Value)).ToList();

			//attempt to add parentheses
			//TODO: port InsertParentheses in-full rather than a make-do (but I didn't think I had the time to do a full-port)
			for (int i = 0; i < conditionList.Count; ++i ) {
				var cond = conditionList[i];
				if (NeedsParentheses((cond.Right))) {
					conditionList[i] = cond.WithRight(SyntaxFactory.ParenthesizedExpression(cond.Right));
				}
			}

			if (conditionList.Count == 1)
				return conditionList.First();

			//combine case labels
			BinaryExpressionSyntax condition = conditionList[0];
			for(int i = 1; i < conditionList.Count; ++i) {
				condition = SyntaxFactory.BinaryExpression(SyntaxKind.LogicalOrExpression, condition, conditionList[i]);
			}
			return condition;
		}

		internal bool HasNonTrailingBreaks(SyntaxNode node, BreakStatementSyntax trailing)
		{
			//if our trailing 'break' is actually return, then /any/ break is non-trailing
			if (node is BreakStatementSyntax && (trailing == null || !node.GetLocation().Equals(trailing.GetLocation())))
				return true;
			return node.DescendantNodes().Any(n => HasNonTrailingBreaks(n, trailing));
		}

		internal bool NeedsParentheses(ExpressionSyntax expr)
		{
			if (expr.IsKind(SyntaxKind.ConditionalExpression) || expr.IsKind(SyntaxKind.EqualsExpression) || expr.IsKind(SyntaxKind.GreaterThanExpression) || 
				expr.IsKind(SyntaxKind.GreaterThanOrEqualExpression)
				|| expr.IsKind(SyntaxKind.LessThanExpression) || expr.IsKind(SyntaxKind.LessThanOrEqualExpression) || expr.IsKind(SyntaxKind.LogicalAndExpression) ||
				expr.IsKind(SyntaxKind.LogicalOrExpression) || expr.IsKind(SyntaxKind.NotEqualsExpression)) {
					return true;
			}

			var bOp = expr as BinaryExpressionSyntax;
			return bOp != null && NeedsParentheses(bOp.Right);
		}
	}
}
