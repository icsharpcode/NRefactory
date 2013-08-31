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
	                   Severity = Severity.Hint,
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
			
			private static readonly Pattern pattern
			= new Choice {
				new IfElseStatement(
					PatternHelper.CommutativeOperator(new AnyNode("a"),BinaryOperatorType.InEquality, new AnyNode("b")),
					new ExpressionStatement(new AssignmentExpression(new Backreference("a"), new Backreference("b"))))
				,
				new IfElseStatement(
					PatternHelper.CommutativeOperator(new AnyNode("a"),BinaryOperatorType.InEquality, new AnyNode("b")),
					new BlockStatement{new AssignmentExpression(new Backreference("a"), new Backreference("b"))})
			};
			
			public override void VisitIfElseStatement(IfElseStatement ifElseStatement)
			{
				base.VisitIfElseStatement(ifElseStatement);
				
				Match m = pattern.Match(ifElseStatement);
				
				if (m.Success)
				{
					AddIssue(ifElseStatement.Condition, ctx.TranslateString("Redundant condition check before assignment."));
				}
			}
		}
	}
}
