//
// ReplacePostfixExpressionWithAssignmentAction.cs
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
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Replace postfix expression with assignment")]
	public class ReplacePostfixExpressionWithAssignmentCodeRefactoringProvider : CodeRefactoringProvider
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
			if (model.IsFromGeneratedCode(cancellationToken))
				return;
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
			var token = root.FindToken(span.Start);

			var postfix = token.Parent.Parent as PostfixUnaryExpressionSyntax;
			if (postfix == null || !(postfix.IsKind(SyntaxKind.PostIncrementExpression) || postfix.IsKind(SyntaxKind.PostDecrementExpression)))
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					token.Span,
					DiagnosticSeverity.Info,
					string.Format(postfix.IsKind(SyntaxKind.PostIncrementExpression)  ? GettextCatalog.GetString ("Replace '{0}++' with '{0} += 1'") : GettextCatalog.GetString ("Replace '{0}--' with '{0} -= 1'"), postfix.Operand.ToString()),
					t2 => {
						var op = postfix.OperatorToken.IsKind(SyntaxKind.PlusPlusToken) ? SyntaxKind.AddAssignmentExpression : SyntaxKind.SubtractAssignmentExpression;
						var binexp = SyntaxFactory.AssignmentExpression(op, postfix.Operand, SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));
						var newRoot = root.ReplaceNode((SyntaxNode)postfix, binexp.WithAdditionalAnnotations(Formatter.Annotation).WithLeadingTrivia(postfix.GetLeadingTrivia()));
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
			);
		}
	}
}