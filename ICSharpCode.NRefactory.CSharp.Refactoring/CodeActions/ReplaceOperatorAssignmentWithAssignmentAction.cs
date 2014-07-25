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
	public class ReplaceOperatorAssignmentWithAssignmentAction : SpecializedCodeAction<BinaryExpressionSyntax>
	{
		protected override IEnumerable<CodeAction> GetActions(Document document, SemanticModel semanticModel, SyntaxNode root, TextSpan span, BinaryExpressionSyntax node, CancellationToken cancellationToken)
		{
			yield break;
		}
//		protected override CodeAction GetAction(SemanticModel context, AssignmentExpression node)
//		{
//			if (!node.OperatorToken.Contains(context.Location))
//				return null;
//			var op = GetAssignmentOperator(node.Operator);
//			if (op == BinaryOperatorType.Any)
//				return null;
//
//			return new CodeAction(
//				context.TranslateString("Replace with '='"),
//				s => s.Replace(
//					node,
//					new AssignmentExpression(
//						node.Left.Clone(), 
//						new BinaryOperatorExpression(node.Left.Clone(), op, node.Right.Clone())
//					)
//				),
//				node.OperatorToken
//			);
//		}
//
//		static BinaryOperatorType GetAssignmentOperator(AssignmentOperatorType op)
//		{
//			switch (op) {
//				case AssignmentOperatorType.BitwiseAnd:
//					return BinaryOperatorType.BitwiseAnd;
//				case AssignmentOperatorType.BitwiseOr:
//					return BinaryOperatorType.BitwiseOr;
//				case AssignmentOperatorType.ExclusiveOr:
//					return BinaryOperatorType.ExclusiveOr;
//				case AssignmentOperatorType.Add:
//					return BinaryOperatorType.Add;
//				case AssignmentOperatorType.Subtract:
//					return BinaryOperatorType.Subtract;
//				case AssignmentOperatorType.Multiply:
//					return BinaryOperatorType.Multiply;
//				case AssignmentOperatorType.Divide:
//					return BinaryOperatorType.Divide;
//				case AssignmentOperatorType.Modulus:
//					return BinaryOperatorType.Modulus;
//				case AssignmentOperatorType.ShiftLeft:
//					return BinaryOperatorType.ShiftLeft;
//				case AssignmentOperatorType.ShiftRight:
//					return BinaryOperatorType.ShiftRight;
//				default:
//					return BinaryOperatorType.Any;
//			}
//		}
	}
}

