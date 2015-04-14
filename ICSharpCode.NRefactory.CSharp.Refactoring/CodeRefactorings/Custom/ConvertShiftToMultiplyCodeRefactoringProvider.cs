//
// ConvertShiftToMultiplyAction.cs
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

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Convert '<<'/'>>' to '*'/'/'")]
	public class ConvertShiftToMultiplyCodeRefactoringProvider : SpecializedCodeRefactoringProvider<BinaryExpressionSyntax>
	{
		protected override IEnumerable<CodeAction> GetActions(Document document, SemanticModel semanticModel, SyntaxNode root, TextSpan span, BinaryExpressionSyntax node, CancellationToken cancellationToken)
		{
			if (!node.OperatorToken.Span.Contains(span) || !(node.OperatorToken.IsKind(SyntaxKind.LessThanLessThanToken) || node.OperatorToken.IsKind(SyntaxKind.GreaterThanGreaterThanToken)))
				return Enumerable.Empty<CodeAction>();

			var rightSide = node.Right as LiteralExpressionSyntax;
			if (rightSide == null || !(rightSide.Token.Value is int))
				return Enumerable.Empty<CodeAction>();
			bool isLeftShift = node.OperatorToken.IsKind(SyntaxKind.LessThanLessThanToken);
			return new[] {
				CodeActionFactory.Create(
					span,
					DiagnosticSeverity.Info,
					isLeftShift ? GettextCatalog.GetString ("To '*'") : GettextCatalog.GetString ("To '/'"),
					t2 => {
						var newRoot = root.ReplaceNode((SyntaxNode)node, SyntaxFactory.BinaryExpression(isLeftShift ? SyntaxKind.MultiplyExpression : SyntaxKind.DivideExpression, node.Left,
							SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1 << (int)rightSide.Token.Value))).WithAdditionalAnnotations(Formatter.Annotation));
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
			};
		}
	}
}