//
// ConvertReturnStatementToIfAction.cs
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
	[NRefactoryCodeRefactoringProvider(Description = "Convert 'return' to 'if'")]
	[ExportCodeRefactoringProvider("Convert 'return' to 'if'", LanguageNames.CSharp)]
	public class ConvertReturnStatementToIfAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var node = root.FindNode(span) as ReturnStatementSyntax;
			if (node == null)
				return;
			if (node.Expression is BinaryExpressionSyntax && !node.Expression.IsKind(SyntaxKind.CoalesceExpression))
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(span, DiagnosticSeverity.Info, "Replace with 'if' statement", 
					t2 => {

						StatementSyntax statement;
						ReturnStatementSyntax returnAfter;
						if (node.Expression is ConditionalExpressionSyntax) {
							var condition = (ConditionalExpressionSyntax)node.Expression;
							statement = SyntaxFactory.IfStatement(condition.Condition, SyntaxFactory.ReturnStatement(condition.WhenTrue));
							returnAfter = SyntaxFactory.ReturnStatement(condition.WhenFalse);
						} else {
							var bOp = node.Expression as BinaryExpressionSyntax;
							if (bOp != null && bOp.IsKind(SyntaxKind.CoalesceExpression)) {
								statement = SyntaxFactory.IfStatement(SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, bOp.Left, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)), SyntaxFactory.ReturnStatement(bOp.Left));
								returnAfter = SyntaxFactory.ReturnStatement(bOp.Right);
							} else {
								return null;
							}
						}

						var newRoot = root.ReplaceNode((SyntaxNode)node, new SyntaxNode[] { 
							statement.WithAdditionalAnnotations(Formatter.Annotation),
							returnAfter.WithAdditionalAnnotations(Formatter.Annotation)
						});
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
			);
		}
	}
}

