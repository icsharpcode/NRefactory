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
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Invert logical expression", Description = "Inverts a logical expression")]
	public class InvertLogicalExpressionAction : SpecializedCodeAction<BinaryOperatorExpression>
	{
		protected override CodeAction GetAction(RefactoringContext context, BinaryOperatorExpression node)
		{
			if (!node.OperatorToken.IsInside(context.Location))
				return null;
			var negativeExpression = CSharpUtil.InvertCondition(node);
			if (node.Parent is ParenthesizedExpression && node.Parent.Parent is UnaryOperatorExpression) {
				var unaryOperatorExpression = node.Parent.Parent as UnaryOperatorExpression;
				if (unaryOperatorExpression.Operator == UnaryOperatorType.Not) {
					return new CodeAction(
						string.Format(context.TranslateString("Invert '{0}'"), unaryOperatorExpression),
						script => {
							script.Replace(unaryOperatorExpression, negativeExpression);
						}, node
					);	
				}
			}
			var newExpression = new UnaryOperatorExpression(UnaryOperatorType.Not, new ParenthesizedExpression(negativeExpression));
			return new CodeAction(
				string.Format(context.TranslateString("Invert '{0}'"), node),
				script => {
					script.Replace(node, newExpression);
				}, node
			);
			return null;
		}
	}
}