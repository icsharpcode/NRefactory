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
	[NRefactoryCodeRefactoringProvider(Description = "Replace postfix expression with assignment")]
	[ExportCodeRefactoringProvider("Replace postfix expression with assignment", LanguageNames.CSharp)]
	public class ReplacePostfixExpressionWithAssignmentAction : ICodeRefactoringProvider
	{

		public async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
		{
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			var token = root.FindToken(span.Start);

			PostfixUnaryExpressionSyntax postfix = token.Parent.Parent as PostfixUnaryExpressionSyntax;
			if (postfix == null || !(postfix.OperatorToken.IsKind(SyntaxKind.PlusPlusToken) || postfix.OperatorToken.IsKind(SyntaxKind.MinusMinusToken))) {
				return Enumerable.Empty<CodeAction>();
			}
			string desc;
			SyntaxKind expType;
			if (postfix.OperatorToken.IsKind(SyntaxKind.PlusPlusToken)) {
				desc = "Replace '{0}++' with '{0} += 1'";
				expType = SyntaxKind.AddAssignmentExpression;

			} else {
				desc = "Replace '{0}--' with '{0} -= 1'";
				expType = SyntaxKind.SubtractAssignmentExpression;
			}
			var binexp = SyntaxFactory.BinaryExpression(expType, postfix.Operand, SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));
			var newRoot = root.ReplaceNode(postfix as ExpressionSyntax, binexp.WithAdditionalAnnotations(Formatter.Annotation));
			desc = String.Format(desc, (postfix.Operand as IdentifierNameSyntax).Identifier.ValueText);
			return new[] { CodeActionFactory.Create(span, DiagnosticSeverity.Info, desc, document.WithSyntaxRoot(newRoot)) };
		}
	}
}

