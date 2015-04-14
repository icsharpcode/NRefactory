//
// FlipRelationalOperatorArgumentsCodeRefactoringProvider.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Flip an relational operator operands")]
	public class FlipRelationalOperatorArgumentsCodeRefactoringProvider : CodeRefactoringProvider
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
			var binop = root.FindToken(span.Start).Parent as BinaryExpressionSyntax;

			if (binop == null || !binop.OperatorToken.Span.Contains(span))
				return;

			SyntaxKind flippedKind;
			string operatorText;
			if (TryFlip (binop, out flippedKind, out operatorText)) {
				context.RegisterRefactoring (
					CodeActionFactory.Create (
						binop.OperatorToken.Span,
						DiagnosticSeverity.Info,
						string.Format (GettextCatalog.GetString ("Flip '{0}' operator to '{1}'"), binop.OperatorToken, operatorText),
						t2 => {
							var newBinop = SyntaxFactory.BinaryExpression (flippedKind, binop.Right, binop.Left)
								.WithAdditionalAnnotations (Formatter.Annotation);
							var newRoot = root.ReplaceNode ((SyntaxNode)binop, newBinop);
							return Task.FromResult (document.WithSyntaxRoot (newRoot));
						}
					)
				);
				return;
			}
		}

		static bool TryFlip (BinaryExpressionSyntax expr, out SyntaxKind flippedKind, out string operatorText)
		{
			switch (expr.Kind ()) {
			case SyntaxKind.LessThanExpression:
				flippedKind = SyntaxKind.GreaterThanExpression;
				operatorText = ">";
				return true;
			case SyntaxKind.LessThanOrEqualExpression:
				flippedKind = SyntaxKind.GreaterThanOrEqualExpression;
				operatorText = ">=";
				return true;
			case SyntaxKind.GreaterThanExpression:
				flippedKind = SyntaxKind.LessThanExpression;
				operatorText = "<";
				return true;
			case SyntaxKind.GreaterThanOrEqualExpression:
				flippedKind = SyntaxKind.LessThanOrEqualExpression;
				operatorText = "<=";
				return true;
			}
			flippedKind = SyntaxKind.None;
			operatorText = null;
			return false;
		}
	}
}
