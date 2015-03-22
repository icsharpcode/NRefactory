//
// ReplaceWithOperatorAssignmentAction.cs
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
	[NRefactoryCodeRefactoringProvider(Description = "Replace assignment with operator assignment")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Replace assignment with operator assignment")]
	public class ReplaceWithOperatorAssignmentAction : CodeRefactoringProvider
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
			if (node == null)
				return;
			var assignment = CreateAssignment(node);
			if (assignment == null)
				return;
			assignment = assignment.WithAdditionalAnnotations(Formatter.Annotation);
            context.RegisterRefactoring(
				CodeActionFactory.Create(span, DiagnosticSeverity.Info, String.Format("Replace with '{0}='", node.Left.ToString()), document.WithSyntaxRoot(
                root.ReplaceNode((SyntaxNode)node, assignment)))
			);
		}

		internal static ExpressionSyntax GetOuterLeft(BinaryExpressionSyntax bop)
		{
			var leftBop = bop.Left as BinaryExpressionSyntax;
			if (leftBop != null && bop.OperatorToken.IsKind(leftBop.OperatorToken.Kind()))
				return GetOuterLeft(leftBop);
			return bop.Left;
		}

		internal static AssignmentExpressionSyntax CreateAssignment(AssignmentExpressionSyntax node)
		{
			var bop = node.Right as BinaryExpressionSyntax;
			if (bop == null)
				return null;
			var outerLeft = GetOuterLeft(bop);
			if (!((IdentifierNameSyntax)outerLeft).Identifier.Value.Equals(((IdentifierNameSyntax)node.Left).Identifier.Value))
				return null;
			var op = GetAssignmentOperator(bop.OperatorToken);
			if (op == SyntaxKind.None)
				return null;
			return SyntaxFactory.AssignmentExpression(op, node.Left, SplitIfWithAndConditionInTwoCodeRefactoringProvider.GetRightSide(outerLeft.Parent as BinaryExpressionSyntax));
		}

		internal static SyntaxKind GetAssignmentOperator(SyntaxToken token)
		{
			switch (token.Kind()) {
				case SyntaxKind.AmpersandToken:
					return SyntaxKind.AndAssignmentExpression;
				case SyntaxKind.BarToken:
					return SyntaxKind.OrAssignmentExpression;
				case SyntaxKind.CaretToken:
					return SyntaxKind.ExclusiveOrAssignmentExpression;
				case SyntaxKind.PlusToken:
					return SyntaxKind.AddAssignmentExpression;
				case SyntaxKind.MinusToken:
					return SyntaxKind.SubtractAssignmentExpression;
				case SyntaxKind.AsteriskToken:
					return SyntaxKind.MultiplyAssignmentExpression;
				case SyntaxKind.SlashToken:
					return SyntaxKind.DivideAssignmentExpression;
				case SyntaxKind.PercentToken:
					return SyntaxKind.ModuloAssignmentExpression;
				case SyntaxKind.LessThanLessThanToken:
					return SyntaxKind.LeftShiftAssignmentExpression;
				case SyntaxKind.GreaterThanGreaterThanToken:
					return SyntaxKind.RightShiftAssignmentExpression;
				default:
					return SyntaxKind.None;
			}
		}

	}
}

