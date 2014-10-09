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
using System;
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
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Convert assignment to 'if'")]
	[ExportCodeRefactoringProvider("Convert assignment to 'if'", LanguageNames.CSharp)]
	public class ConvertAssignmentToIfAction : CodeRefactoringProvider
	{
		public override async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);

			var node = root.FindNode(span) as BinaryExpressionSyntax;
			if (node == null || !IsAssignment(node))
				return Enumerable.Empty<CodeAction>();

			if (node.Right is ConditionalExpressionSyntax) {
				var ifStatement = CreateForConditionalExpression(model, node, (ConditionalExpressionSyntax)node.Right);
				return new [] { CodeActionFactory.Create(span, DiagnosticSeverity.Info, "Replace with 'if' statement", document.WithSyntaxRoot(root.ReplaceNode(node.Parent, ifStatement)))};
			}

			var bOp = node.Right as BinaryExpressionSyntax;
			//if null coalesce
			if (bOp != null && bOp.OperatorToken.IsKind(SyntaxKind.QuestionQuestionToken)) {
				var ifStatement = CreateForNullCoalescingExpression(model, node, bOp);
				return new[] { CodeActionFactory.Create(span, DiagnosticSeverity.Info, "Replace with 'if' statement", document.WithSyntaxRoot(root.ReplaceNode(node.Parent, ifStatement))) };
			}

			return Enumerable.Empty<CodeAction>();
		}

		private IfStatementSyntax CreateForConditionalExpression(SemanticModel model, BinaryExpressionSyntax expr, ConditionalExpressionSyntax conditional)
		{
			return SyntaxFactory.IfStatement(conditional.Condition, SyntaxFactory.ExpressionStatement(SyntaxFactory.BinaryExpression(expr.CSharpKind(), expr.Left, 
				conditional.WhenTrue)),
				SyntaxFactory.ElseClause(SyntaxFactory.ExpressionStatement(SyntaxFactory.BinaryExpression(expr.CSharpKind(), expr.Left, conditional.WhenFalse))));
		}

		private IfStatementSyntax CreateForNullCoalescingExpression(SemanticModel model, BinaryExpressionSyntax expr, BinaryExpressionSyntax bOp)
		{
			return SyntaxFactory.IfStatement(SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, bOp.Left, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)), 
				SyntaxFactory.ExpressionStatement(SyntaxFactory.BinaryExpression(expr.CSharpKind(), expr.Left,
				bOp.Left)),
				SyntaxFactory.ElseClause(SyntaxFactory.ExpressionStatement(SyntaxFactory.BinaryExpression(expr.CSharpKind(), expr.Left, bOp.Right))));
		}

		public static bool IsAssignment(BinaryExpressionSyntax node)
		{
			return node.IsKind(SyntaxKind.AddAssignmentExpression) || node.IsKind(SyntaxKind.AndAssignmentExpression) || node.IsKind(SyntaxKind.DivideAssignmentExpression) ||
				node.IsKind(SyntaxKind.ExclusiveOrAssignmentExpression) || node.IsKind(SyntaxKind.LeftShiftAssignmentExpression) || node.IsKind(SyntaxKind.ModuloAssignmentExpression) ||
				node.IsKind(SyntaxKind.MultiplyAssignmentExpression) || node.IsKind(SyntaxKind.OrAssignmentExpression) || node.IsKind(SyntaxKind.RightShiftAssignmentExpression) ||
				node.IsKind(SyntaxKind.SimpleAssignmentExpression) || node.IsKind(SyntaxKind.SubtractAssignmentExpression);
		}
	}
}

