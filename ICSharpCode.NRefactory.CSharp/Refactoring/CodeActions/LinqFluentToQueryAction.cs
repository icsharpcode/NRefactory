// 
// LinqFluentToQueryAction.cs
// 
// Author:
//      Luís Reis <luiscubal@gmail.com>
// 
// Copyright (c) 2013 Luís Reis
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
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction ("Convert LINQ query to fluent syntax", Description = "Convert LINQ query to fluent syntax")]
	public class LinqFluentToQueryAction : SpecializedCodeAction<InvocationExpression>
	{
		static readonly List<string> LinqQueryMethods = new List<string>() {
			"Select", "SelectMany", "GroupBy",
			"OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending",
			"Where", "Cast",
			"Join", "GroupJoin"
		};

		protected override CodeAction GetAction(RefactoringContext context, InvocationExpression node)
		{
			if (!IsLinqMethodInvocation(node)) {
				return null;
			}

			while (node.Parent is MemberReferenceExpression) {
				var parentInvocation = ((MemberReferenceExpression)node.Parent).Parent;
				if (!IsLinqMethodInvocation(parentInvocation)) {
					break;
				}
				node = (InvocationExpression) parentInvocation;
			}

			IntroduceQueryExpressions queryExpressionIntroducer = new IntroduceQueryExpressions();
			CombineQueryExpressions queryExpressionCombiner = new CombineQueryExpressions();
			Expression newNode = queryExpressionIntroducer.ConvertFluentToQuery(node);

			queryExpressionCombiner.CombineQueries(newNode);

			if (!(newNode is QueryExpression)) {
				return null;
			}

			if (NeedsParenthesis(node)) {
				newNode = new ParenthesizedExpression(newNode);
			}

			return new CodeAction(context.TranslateString("Convert to query syntax"), script => {
				script.Replace(node, newNode);
			}, node);
		}

		bool NeedsParenthesis(AstNode node)
		{
			AstNode parent = node.Parent;
			if (parent is BinaryOperatorExpression)
				return true;

			UnaryOperatorExpression unaryExpression = parent as UnaryOperatorExpression;
			if (unaryExpression != null) {
				return unaryExpression.Operator == UnaryOperatorType.PostIncrement ||
					unaryExpression.Operator == UnaryOperatorType.PostDecrement;
			}

			return parent is MemberReferenceExpression ||
				parent is InvocationExpression;
		}

		bool IsLinqMethodInvocation(AstNode node)
		{
			var invocation = node as InvocationExpression;
			return invocation != null && IsLinqMethodInvocation(invocation);
		}

		bool IsLinqMethodInvocation(InvocationExpression node)
		{
			var target = node.Target as MemberReferenceExpression;
			return target != null && IsLinqMethod(target);
		}

		bool IsLinqMethod(MemberReferenceExpression node)
		{
			return LinqQueryMethods.Contains(node.MemberName);
		}
	}
}

