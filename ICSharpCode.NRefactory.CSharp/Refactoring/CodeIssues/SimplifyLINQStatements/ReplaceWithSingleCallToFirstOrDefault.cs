// ReplaceWithSingleCallToFirstOrDefault.cs
// 
// Author:
//      Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013 Ji Kun
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
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SaHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.CSharp;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp
{
	[IssueDescription("Replace with single call to FirstOrDefault(...)",
	                  Description = "Simplify Where(...).FirstOrDefault() To FirstOrDefault(...)",
	                  Category = IssueCategories.Redundancies,
	                  Severity = Severity.Suggestion,
	                  ResharperDisableKeyword = "ReplaceWithSingleCallToFirstOrDefault",
	                  IssueMarker = IssueMarker.Underline)]
	public class ReplaceWithSingleCallToFirstOrDefault : ICodeIssueProvider
	{	
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context).GetIssues();
		}
		
		class GatherVisitor : GatherVisitorBase<ReplaceWithSingleCallToFirst>
		{
			public GatherVisitor(BaseRefactoringContext ctx)
				: base(ctx)
			{
			}
			
			public override void VisitInvocationExpression(InvocationExpression invocationExpression)
			{
				base.VisitInvocationExpression(invocationExpression);

				if (invocationExpression == null)
					return;

				if (!(invocationExpression.Target is MemberReferenceExpression))
					return;

				MemberReferenceExpression m1 = invocationExpression.Target as MemberReferenceExpression;
			
				if (m1.MemberName != "FirstOrDefault")
					return;

				if (! (m1.Target is InvocationExpression))
					return;

				InvocationExpression m2 = m1.Target as InvocationExpression;
		
				if (!(m2.Target is MemberReferenceExpression) || m2.Arguments.Count == 0)
					return;
				
				MemberReferenceExpression m3 = m2.Target as MemberReferenceExpression;

				if (m3.MemberName != "Where")
					return;

				var arguments = m2.Arguments.Single().Clone(); 
				var id = m3.Target.Clone();

				AddIssue(invocationExpression, ctx.TranslateString("Replace with single call to First"), script => {
					var newExpression = new InvocationExpression(
							new MemberReferenceExpression(id, "FirstOrDefault"),
						arguments);
					script.Replace(invocationExpression, newExpression);
				});
				return;
			}
		}
	}
}


