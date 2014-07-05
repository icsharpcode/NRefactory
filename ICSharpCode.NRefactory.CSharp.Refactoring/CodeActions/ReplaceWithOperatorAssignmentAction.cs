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
	[ExportCodeRefactoringProvider("Replace assignment with operator assignment", LanguageNames.CSharp)]
	public class ReplaceWithOperatorAssignmentAction : ICodeRefactoringProvider
	{
        public async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken);
            var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
            var token = root.FindToken(span.Start);

            /*var binOp = token.Parent as BinaryExpressionSyntax;
            if (binOp == null)
                return Enumerable.Empty<CodeAction>();
            var outerLeft = GetOuterLeft(binOp);
            if(outerLeft == binOp.Left)
                return Enumerable.Empty<CodeAction>();
            var op = GetAssignmentOperator(binOp.OperatorToken);
            if(IsOpAssignment(op))
            {

            }*/
            throw new NotImplementedException();
            return null;
        }

        internal static ExpressionSyntax GetOuterLeft(BinaryExpressionSyntax bop)
		{
            var leftBop = bop.Left as BinaryExpressionSyntax;
			if (leftBop != null && bop.OperatorToken.IsKind(leftBop.OperatorToken.CSharpKind()))
				return GetOuterLeft(leftBop);
			return bop.Left;
		}

        internal static BinaryExpressionSyntax CreateAssignment(BinaryExpressionSyntax node)
        {
            var bop = node.Right as BinaryExpressionSyntax;
            if (bop == null)
                return null;
            var outerLeft = GetOuterLeft(bop);
            if (!outerLeft.IsEquivalentTo(bop.Left))
                return null;
            var op = GetAssignmentOperator(bop.OperatorToken);
            if(op == null)
                return null;
            return SyntaxFactory.BinaryExpression(op, node.Left, SplitIfAction.GetRightSide(outerLeft.Parent as BinaryExpressionSyntax));
        }

		/*internal static AssignmentExpression CreateAssignment(AssignmentExpression node)
		{
			var bop = node.Right as BinaryOperatorExpression;
			if (bop == null)
				return null;
			var outerLeft = GetOuterLeft(bop);
			if (!outerLeft.IsMatch(node.Left))
				return null;
			var op = GetAssignmentOperator(bop.Operator);
			if (op == AssignmentOperatorType.Any)
				return null;
			return new AssignmentExpression(node.Left.Clone(), op, SplitIfAction.GetRightSide((BinaryOperatorExpression)outerLeft.Parent));
		}*/

        internal static SyntaxKind GetAssignmentOperator(SyntaxToken token)
        {
			switch (token.CSharpKind()) {
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
                    return SyntaxKind.SimpleAssignmentExpression;
			}
		}

//		internal static AssignmentExpression CreateAssignment(AssignmentExpression node)
//		{
//			var bop = node.Right as BinaryOperatorExpression;
//			if (bop == null)
//				return null;
//			var outerLeft = GetOuterLeft(bop);
//			if (!outerLeft.IsMatch(node.Left))
//				return null;
//			var op = GetAssignmentOperator(bop.Operator);
//			if (op == AssignmentOperatorType.Any)
//				return null;
//			return new AssignmentExpression(node.Left.Clone(), op, SplitIfAction.GetRightSide((BinaryOperatorExpression)outerLeft.Parent));
//		}
//
//		protected override CodeAction GetAction(SemanticModel context, AssignmentExpression node)
//		{
//			if (!node.OperatorToken.Contains(context.Location))
//				return null;
//
//			var ae = CreateAssignment(node);
//			if (ae == null)
//				return null;
//			return new CodeAction (
//				string.Format(context.TranslateString("Replace with '{0}='"), ((BinaryOperatorExpression)node.Right).OperatorToken),
//				s => s.Replace(node, ae),
//				node.OperatorToken
//			);
//		}
//
//		static AssignmentOperatorType GetAssignmentOperator(BinaryOperatorType op)
//		{
//			switch (op) {
//				case BinaryOperatorType.BitwiseAnd:
//					return AssignmentOperatorType.BitwiseAnd;
//				case BinaryOperatorType.BitwiseOr:
//					return AssignmentOperatorType.BitwiseOr;
//				case BinaryOperatorType.ExclusiveOr:
//					return AssignmentOperatorType.ExclusiveOr;
//				case BinaryOperatorType.Add:
//					return AssignmentOperatorType.Add;
//				case BinaryOperatorType.Subtract:
//					return AssignmentOperatorType.Subtract;
//				case BinaryOperatorType.Multiply:
//					return AssignmentOperatorType.Multiply;
//				case BinaryOperatorType.Divide:
//					return AssignmentOperatorType.Divide;
//				case BinaryOperatorType.Modulus:
//					return AssignmentOperatorType.Modulus;
//				case BinaryOperatorType.ShiftLeft:
//					return AssignmentOperatorType.ShiftLeft;
//				case BinaryOperatorType.ShiftRight:
//					return AssignmentOperatorType.ShiftRight;
//				default:
//					return AssignmentOperatorType.Any;
//			}
//		}
//
//		static Expression GetOuterLeft (BinaryOperatorExpression bop)
//		{
//			var leftBop = bop.Left as BinaryOperatorExpression;
//			if (leftBop != null && bop.Operator == leftBop.Operator)
//				return GetOuterLeft(leftBop);
//			return bop.Left;
//		}
	}
}

