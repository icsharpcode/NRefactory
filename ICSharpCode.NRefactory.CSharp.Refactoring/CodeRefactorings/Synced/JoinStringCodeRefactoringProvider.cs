// 
// JoinStringAction.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
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

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Join string literal")]
	public class JoinStringCodeRefactoringProvider : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
				return;
			var span = context.Span;
			if (!span.IsEmpty)
				return;
			var cancellationToken = context.CancellationToken;
			if (cancellationToken.IsCancellationRequested)
				return;
			var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			if (model.IsFromGeneratedCode())
				return;
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

			var node = root.FindNode(span) as BinaryExpressionSyntax;
			//ignore nodes except string concat.
			if (node == null || !node.OperatorToken.IsKind(SyntaxKind.PlusToken))
				return;

			LiteralExpressionSyntax left;
			var leftBinaryExpr = node.Left as BinaryExpressionSyntax;
			//if there is something other than a string literal on the left, then just take the right node (e.g. a+b+c => a+(b+c))
			if (leftBinaryExpr != null && leftBinaryExpr.OperatorToken.IsKind(SyntaxKind.PlusToken))
				left = leftBinaryExpr.Right as LiteralExpressionSyntax;
			else
				left = node.Left as LiteralExpressionSyntax;

			var right = node.Right as LiteralExpressionSyntax;

			//ignore non-string literals
			if (left == null || right == null || !left.IsKind(SyntaxKind.StringLiteralExpression) || !right.IsKind(SyntaxKind.StringLiteralExpression))
				return;

			bool isLeftVerbatim = left.Token.IsVerbatimStringLiteral();
			bool isRightVerbatim = right.Token.IsVerbatimStringLiteral();
			if (isLeftVerbatim != isRightVerbatim)
				return;

			String newString = left.Token.ValueText + right.Token.ValueText;
			LiteralExpressionSyntax stringLit;

			if (isLeftVerbatim)
				stringLit = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("@\"" + newString + "\"", newString));
			else
				stringLit = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(newString));

			ExpressionSyntax exprNode;

			if (leftBinaryExpr == null)
				exprNode = stringLit;
			else
				exprNode = leftBinaryExpr.WithRight(stringLit);
			context.RegisterRefactoring(
				CodeActionFactory.Create(span, DiagnosticSeverity.Info, GettextCatalog.GetString ("Join strings"), document.WithSyntaxRoot(root.ReplaceNode((SyntaxNode)node, exprNode as ExpressionSyntax)))
			);
		}
	}
}
