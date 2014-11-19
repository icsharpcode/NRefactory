//
// NegateIsExpressionAction.cs
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
	[NRefactoryCodeRefactoringProvider(Description = "Negate an is expression")]
	[ExportCodeRefactoringProvider("Negate 'is' expression", LanguageNames.CSharp)]
	public class NegateIsExpressionAction : SpecializedCodeAction<BinaryExpressionSyntax>
	{
		protected override IEnumerable<CodeAction> GetActions(Document document, SemanticModel semanticModel, SyntaxNode root, TextSpan span, BinaryExpressionSyntax node, CancellationToken cancellationToken)
		{
			if (!node.IsKind(SyntaxKind.IsExpression) || !node.OperatorToken.Span.Contains(span))
				return Enumerable.Empty<CodeAction>();

			var pExpr = node.Parent as ParenthesizedExpressionSyntax;
			if (pExpr != null) {
				var uOp = pExpr.Parent as PrefixUnaryExpressionSyntax;
				if (uOp != null && uOp.IsKind(SyntaxKind.LogicalNotExpression)) {

					return new[] { 
						CodeActionFactory.Create(
							span, 
							DiagnosticSeverity.Info, 
							string.Format ("Negate '{0}'", uOp),
							t2 => {
								var newRoot = root.ReplaceNode((SyntaxNode)
									(SyntaxNode)uOp,
									node.WithAdditionalAnnotations(Formatter.Annotation)
								);
								return Task.FromResult(document.WithSyntaxRoot(newRoot));
							}
						) 
					};
				}
			}

			return new[] { 
				CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Info, 
					string.Format ("Negate '{0}'", node),
					t2 => {
						var newRoot = root.ReplaceNode((SyntaxNode)
							(SyntaxNode)node,
							SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, SyntaxFactory.ParenthesizedExpression(node)).WithAdditionalAnnotations(Formatter.Annotation)
						);
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				) 
			};
		}
	}
}