// 
// InvertLogicalExpressionAction.cs
// 
// Author:
//      Ji Kun<jikun.nus@gmail.com>
// 
// Copyright (c) 2012 Ji Kun<jikun.nus@gmail.com>
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
	[NRefactoryCodeRefactoringProvider(Description = "Inverts a logical expression")]
	[ExportCodeRefactoringProvider("Invert logical expression", LanguageNames.CSharp)]
	public class InvertLogicalExpressionAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			ExpressionSyntax expr;
			SyntaxToken token;
			if (!NegateRelationalExpressionAction.GetRelationalExpression (root, span, out expr, out token))
				return;
			if (expr.IsKind(SyntaxKind.LogicalNotExpression)) {
				context.RegisterRefactoring(
					CodeActionFactory.Create(
						span, 
						DiagnosticSeverity.Info, 
						string.Format ("Invert '{0}'", expr),
						t2 => {
							var uOp = expr as PrefixUnaryExpressionSyntax;
							var newRoot = root.ReplaceNode((SyntaxNode)
								expr,
								CSharpUtil.InvertCondition(uOp.Operand.SkipParens()).WithAdditionalAnnotations(Formatter.Annotation)
							);
							return Task.FromResult(document.WithSyntaxRoot(newRoot));
						}
					) 
				);
			}

			if (expr.Parent is ParenthesizedExpressionSyntax && expr.Parent.Parent is PrefixUnaryExpressionSyntax) {
				var unaryOperatorExpression = expr.Parent.Parent as PrefixUnaryExpressionSyntax;
				if (unaryOperatorExpression.IsKind(SyntaxKind.LogicalNotExpression)) {

					context.RegisterRefactoring(
						CodeActionFactory.Create(
							span, 
							DiagnosticSeverity.Info, 
							string.Format ("Invert '{0}'", unaryOperatorExpression),
							t2 => {
								var uOp = expr as PrefixUnaryExpressionSyntax;
								var newRoot = root.ReplaceNode((SyntaxNode)
									unaryOperatorExpression,
									CSharpUtil.InvertCondition(expr).WithAdditionalAnnotations(Formatter.Annotation)
								);
								return Task.FromResult(document.WithSyntaxRoot(newRoot));
							}
						) 
					);
				}
			}

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Info, 
					string.Format ("Invert '{0}'", expr),
					t2 => {
						var newRoot = root.ReplaceNode((SyntaxNode)
							expr,
							SyntaxFactory.PrefixUnaryExpression(
								SyntaxKind.LogicalNotExpression, 
								SyntaxFactory.ParenthesizedExpression(CSharpUtil.InvertCondition(expr))
							).WithAdditionalAnnotations(Formatter.Annotation)
						);
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				) 
			);
		}
	}
}