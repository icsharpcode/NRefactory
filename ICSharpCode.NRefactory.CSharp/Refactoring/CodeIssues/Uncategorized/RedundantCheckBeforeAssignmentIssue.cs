// 
// RedundantCheckBeforeAssignmentIssue.cs
// 
// Author:
//      Ji Kun <jikun.nus@gmail.com>
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
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Check for inequality before assignment is redundant if (x!=value) x=value",
	                   Description = "Remove redundant check before assignment",
	                   Category = IssueCategories.RedundanciesInCode,
	                   Severity = Severity.Warning,
	                   IssueMarker = IssueMarker.WavedLine, 
	                   ResharperDisableKeyword = "RedundantCheckBeforeAssignment")]
	public class RedundantCheckBeforeAssignmentIssue : GatherVisitorCodeIssueProvider
	{
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
		}
		
		class GatherVisitor : GatherVisitorBase<RedundantCheckBeforeAssignmentIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx)
				: base (ctx)
			{
			}
		
			private Expression UnpackExpression(Expression expression)
			{
				var returnedExpression = expression;
				while (returnedExpression is ParenthesizedExpression) {
					returnedExpression = (returnedExpression as ParenthesizedExpression).Expression;
				}
				return returnedExpression;
			}

			public override void VisitIfElseStatement(IfElseStatement ifElseStatement)
			{
				base.VisitIfElseStatement(ifElseStatement);

				if (ifElseStatement.Condition == null)
					return;

				if (!(ifElseStatement.Condition is BinaryOperatorExpression))
					return;

				if ((ifElseStatement.Condition as BinaryOperatorExpression).Operator != BinaryOperatorType.InEquality)
					return;

				if (ifElseStatement.TrueStatement == null)
					return;

				if (!ifElseStatement.FalseStatement.IsNull)
					return;

				Expression left = null;
				Expression right = null;
				if (ifElseStatement.TrueStatement is BlockStatement) {
					if ((ifElseStatement.TrueStatement as BlockStatement).Statements.Count == 1) {
						if ((ifElseStatement.TrueStatement as BlockStatement).Statements.Single().FirstChild is AssignmentExpression) {
							AssignmentExpression assignmentExpression = (ifElseStatement.TrueStatement as BlockStatement).Statements.Single().FirstChild as AssignmentExpression;
							left = assignmentExpression.Left;
							right = assignmentExpression.Right;
						}
					}
				} else if (ifElseStatement.TrueStatement is ExpressionStatement) {
					AssignmentExpression assignmentExpression = (ifElseStatement.TrueStatement as ExpressionStatement).Expression as AssignmentExpression;
					left = assignmentExpression.Left;
					right = assignmentExpression.Right;
				}

				if (left == null || right == null) {
					return;
				}

				left = UnpackExpression(left);
				right = UnpackExpression(right);

				var conditionLeft = UnpackExpression((ifElseStatement.Condition as BinaryOperatorExpression).Left);
				var conditionRight = UnpackExpression((ifElseStatement.Condition as BinaryOperatorExpression).Right);

				if (left.ToString().Equals(conditionLeft.ToString())) {
					if (right.ToString().Equals(conditionRight.ToString()))
						AddIssue(ifElseStatement, ctx.TranslateString("Redundant condition check before assignment."));
				} else if (left.ToString().Equals(conditionRight.ToString())) {
					if (right.ToString().Equals(conditionLeft.ToString()))
						AddIssue(ifElseStatement, ctx.TranslateString("Redundant condition check before assignment."));
				}
			}
		}
	}
}
