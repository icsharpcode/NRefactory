//
// RedundantNullCoalescingExpressionIssue.cs
//
// Author:
//       Luís Reis <luiscubal@gmail.com>
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
using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using System.Linq;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.PatternMatching;
using System.Runtime.InteropServices.ComTypes;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("'??' condition is known to be null or not null",
	                  Description = "Finds redundant null coalescing expressions such as expr ?? expr",
	                  Category = IssueCategories.RedundanciesInCode,
	                  Severity = Severity.Warning,
	                  IssueMarker = IssueMarker.GrayOut,
	                  ResharperDisableKeyword = "ConstantNullCoalescingCondition")]
	public class ConstantNullCoalescingConditionIssue : GatherVisitorCodeIssueProvider
	{
		static readonly Pattern Pattern = new Choice {
			PatternHelper.CommutativeOperatorWithOptionalParentheses(
				new AnyNode("expression"),
				BinaryOperatorType.NullCoalescing,
				new NullReferenceExpression()),
			new BinaryOperatorExpression(
				new AnyNode("expression"),
				BinaryOperatorType.NullCoalescing,
				new Backreference("expression"))
		};

		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
		}

		class GatherVisitor : GatherVisitorBase<ConstantNullCoalescingConditionIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx)
				: base(ctx)
			{
			}

			public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
			{
				base.VisitBinaryOperatorExpression(binaryOperatorExpression);

				var match = Pattern.Match(binaryOperatorExpression);
				if (match.Success) {
					var expr = match.Get<Expression>("expression").Single();
					var isLeft = binaryOperatorExpression.Left == expr;
					AddIssue(
						isLeft ? binaryOperatorExpression.Left : binaryOperatorExpression.Right,
						ctx.TranslateString("Found redundant null coallescing expression"),
						isLeft ? ctx.TranslateString("Replace '??' with left operand") : ctx.TranslateString("Replace '??' with right operand"),
						script => {
							script.Replace(binaryOperatorExpression, expr.Clone());
						}
					);
				}
			}
		}
	}
}