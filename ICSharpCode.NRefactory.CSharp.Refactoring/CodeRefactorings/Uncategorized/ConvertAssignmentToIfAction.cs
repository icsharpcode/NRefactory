//
// ConvertAssignmentToIfAction.cs
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

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[NRefactoryCodeRefactoringProvider(Description = "Convert assignment to 'if'")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Convert assignment to 'if'")]
	public class ConvertAssignmentToIfAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);

			var node = root.FindNode(span) as AssignmentExpressionSyntax;
			if (node == null)
				return;

			if (node.Right.IsKind(SyntaxKind.ConditionalExpression)) {
				context.RegisterRefactoring(
					CodeActionFactory.Create(
						span, 
						DiagnosticSeverity.Info, 
						GettextCatalog.GetString ("Replace with 'if' statement"), 
						t2 => {
							var ifStatement = CreateForConditionalExpression(node, (ConditionalExpressionSyntax)node.Right);
							return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode((SyntaxNode)node.Parent, ifStatement.WithAdditionalAnnotations(Formatter.Annotation))));
						}
					)
				);
			}
			if (node.Right.IsKind(SyntaxKind.CoalesceExpression)) {
				context.RegisterRefactoring(
					CodeActionFactory.Create(
						span, 
						DiagnosticSeverity.Info, 
						GettextCatalog.GetString ("Replace with 'if' statement"), 
						t2 => {
							var ifStatement = CreateForNullCoalescingExpression(node, (BinaryExpressionSyntax)node.Right);
							return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode((SyntaxNode)node.Parent, ifStatement.WithAdditionalAnnotations(Formatter.Annotation))));
						}
					)
				);
			}
		}

		static IfStatementSyntax CreateForConditionalExpression(AssignmentExpressionSyntax expr, ConditionalExpressionSyntax conditional)
		{
			return SyntaxFactory.IfStatement(
				conditional.Condition, 
				SyntaxFactory.ExpressionStatement(
					SyntaxFactory.AssignmentExpression(expr.Kind(), expr.Left, conditional.WhenTrue)
				),
				SyntaxFactory.ElseClause(
					SyntaxFactory.ExpressionStatement(
						SyntaxFactory.AssignmentExpression(expr.Kind(), expr.Left, conditional.WhenFalse)
					)
				)
			);
		}

		static IfStatementSyntax CreateForNullCoalescingExpression(AssignmentExpressionSyntax expr, BinaryExpressionSyntax bOp)
		{
			return SyntaxFactory.IfStatement(SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, bOp.Left, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)), 
				SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(expr.Kind(), expr.Left, bOp.Left)),
				SyntaxFactory.ElseClause(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(expr.Kind(), expr.Left, bOp.Right))));
		}
	}
}