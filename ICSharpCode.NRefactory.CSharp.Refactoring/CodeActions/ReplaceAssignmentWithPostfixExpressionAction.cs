//
// ReplaceAssignmentWithPostfixExpressionAction.cs
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
	[NRefactoryCodeRefactoringProvider(Description = "Replace assignment with postfix expression")]
	[ExportCodeRefactoringProvider("Replace assignment with postfix expression", LanguageNames.CSharp)]
	public class ReplaceAssignmentWithPostfixExpressionAction : CodeRefactoringProvider
	{
		public override async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			var token = root.FindToken(span.Start);

			var node = token.Parent as AssignmentExpressionSyntax;
			if (node == null || !node.OperatorToken.Span.Contains(span))
				return Enumerable.Empty<CodeAction>();

			var updatedNode = ReplaceWithOperatorAssignmentAction.CreateAssignment(node) ?? node;

			if ((!updatedNode.IsKind(SyntaxKind.AddAssignmentExpression) && !updatedNode.IsKind(SyntaxKind.SubtractAssignmentExpression)))
				return Enumerable.Empty<CodeAction>();

			var rightLiteral = updatedNode.Right as LiteralExpressionSyntax;
			if (rightLiteral == null || ((int)rightLiteral.Token.Value) != 1)
				return Enumerable.Empty<CodeAction>();

			return new []  { 
				CodeActionFactory.Create(
					token.Span,
					DiagnosticSeverity.Info,
					updatedNode.IsKind(SyntaxKind.AddAssignmentExpression) ? "Replace with '{0}++'" : "Replace with '{0}--'",
					t2 => {
						var newNode = SyntaxFactory.PostfixUnaryExpression(updatedNode.IsKind(SyntaxKind.AddAssignmentExpression) ? SyntaxKind.PostIncrementExpression : SyntaxKind.PostDecrementExpression, updatedNode.Left);
						var newRoot = root.ReplaceNode(node, newNode.WithAdditionalAnnotations(Formatter.Annotation));
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
			};
		}
	}
}

