//
// ReplaceOperatorAssignmentWithAssignmentAction.cs
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
	[NRefactoryCodeRefactoringProvider(Description = "Replace operator assignment with assignment")]
	[ExportCodeRefactoringProvider("Replace operator assignment with assignment", LanguageNames.CSharp)]
	public class ReplaceOperatorAssignmentWithAssignmentAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			var token = root.FindToken(span.Start);

			var node = token.Parent as AssignmentExpressionSyntax;
			if (node == null || !node.OperatorToken.Span.Contains(span))
				return;
			if (node.IsKind(SyntaxKind.SimpleAssignmentExpression))
				return;
			var assignment = GetAssignmentOperator(node.CSharpKind());
			if (assignment == SyntaxKind.None)
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					token.Span,
					DiagnosticSeverity.Info,
					"Replace with '='",
					t2 => {
						var newNode = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, node.Left, SyntaxFactory.BinaryExpression(GetAssignmentOperator(node.CSharpKind()), node.Left, node.Right));
						var newRoot = root.ReplaceNode((SyntaxNode)node, newNode.WithAdditionalAnnotations(Formatter.Annotation));
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
			);
		}

		static SyntaxKind GetAssignmentOperator(SyntaxKind op)
		{
			switch (op) {
				case SyntaxKind.AndAssignmentExpression:
					return SyntaxKind.BitwiseAndExpression;
				case SyntaxKind.OrAssignmentExpression:
					return SyntaxKind.BitwiseOrExpression;
				case SyntaxKind.ExclusiveOrAssignmentExpression:
					return SyntaxKind.ExclusiveOrExpression;
				case SyntaxKind.AddAssignmentExpression:
					return SyntaxKind.AddExpression;
				case SyntaxKind.SubtractAssignmentExpression:
					return SyntaxKind.SubtractExpression;
				case SyntaxKind.MultiplyAssignmentExpression:
					return SyntaxKind.MultiplyExpression;
				case SyntaxKind.DivideAssignmentExpression:
					return SyntaxKind.DivideExpression;
				case SyntaxKind.ModuloAssignmentExpression:
					return SyntaxKind.ModuloExpression;
				case SyntaxKind.LeftShiftAssignmentExpression:
					return SyntaxKind.LeftShiftExpression;
				case SyntaxKind.RightShiftAssignmentExpression:
					return SyntaxKind.RightShiftExpression;
				default:
					return SyntaxKind.None;
			}
		}
	}
}

