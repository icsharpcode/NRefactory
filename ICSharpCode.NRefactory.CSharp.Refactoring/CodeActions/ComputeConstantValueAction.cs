// 
// ComputeConstantValueAction.cs
//  
// Author:
//       Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013 Ji Kun <jikun.nus@gmail.com>
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
	[NRefactoryCodeRefactoringProvider(Description = "Computes the value of the current expression and replaces it")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Compute constant value")]
	public class ComputeConstantValueAction : CodeRefactoringProvider
	{
		internal static ExpressionSyntax GetLiteralExpression(object value)
		{
			if (value is bool)
				return SyntaxFactory.LiteralExpression((bool)value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
			if (value is byte)
				return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((byte)value));
			if (value is sbyte)
				return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((sbyte)value));
			if (value is short)
				return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((short)value));
			if (value is ushort)
				return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((ushort)value));
			if (value is int)
				return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((int)value));
			if (value is uint)
				return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((uint)value));
			if (value is long)
				return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((long)value));
			if (value is ulong)
				return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((ulong)value));

			if (value is float)
				return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((float)value));
			if (value is double)
				return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((double)value));
			if (value is decimal)
				return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((decimal)value));

			if (value is char)
				return SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal((char)value));

			if (value is string)
				return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal((string)value));

			if (value == null)
				return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

			return null;
		}

		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			var node = root.FindNode(span);
			var expr = node.FirstAncestorOrSelf<ExpressionSyntax>(n => n is BinaryExpressionSyntax || n is PostfixUnaryExpressionSyntax || n is PrefixUnaryExpressionSyntax);
			if (expr == null)
				return;
			if (expr is BinaryExpressionSyntax) {
				if (((BinaryExpressionSyntax)expr).OperatorToken.SpanStart != span.Start && expr.SpanStart != span.Start)
					return;
			} else {
				if (expr.SpanStart != span.Start)
					return;
			}

			var result = model.GetConstantValue(expr, cancellationToken);
			if (!result.HasValue)
				return;
			var syntaxNode = GetLiteralExpression(result.Value);
			if (syntaxNode == null)
				return;
			context.RegisterRefactoring(
				CodeActionFactory.Create(
					node.Span,
					DiagnosticSeverity.Info,
					"Compute constant value",
					t2 => {
						var newRoot = root.ReplaceNode((SyntaxNode)expr, syntaxNode.WithAdditionalAnnotations(Formatter.Annotation));
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
			);
		}
	}
}