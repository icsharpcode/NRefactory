// 
// RedundantEmptyDefaultInSwitchIssue.cs
//  
// Author:
//       Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013  Ji Kun <jikun.nus@gmail.com>
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
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Linq;
using ICSharpCode.NRefactory.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Remove redundant empty default branch in switch, For example: Switch (foo) { case Bar: DoSomething; break; default: break;}
	/// </summary>
	[IssueDescription("Remove redundant empty default branch in switch",
			Description= "Remove redundant empty default branch in switch.",
			Category = IssueCategories.Redundancies,
			Severity = Severity.Hint,
			IssueMarker = IssueMarker.GrayOut,
			ResharperDisableKeyword = "RedundantEmptyDefaultBranchInSwitch")]
	public class RedundantEmptyDefaultBranchInSwitchIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context, this).GetIssues();
		}

		class GatherVisitor : GatherVisitorBase<RedundantEmptyDefaultBranchInSwitchIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx, RedundantEmptyDefaultBranchInSwitchIssue issueProvider) : base (ctx, issueProvider)
			{
			}

			public override void VisitSwitchSection(SwitchSection switchSection)
			{
				base.VisitSwitchSection(switchSection);

				if (switchSection.CaseLabels.Count() != 1)
					return;

				string label = switchSection.CaseLabels.First().ToString();

				if (!label.Equals("default:"))
					return;

				if (switchSection.GetChildrenByRole(Roles.EmbeddedStatement).Count() == 0)
					return;

				var expr = switchSection.GetChildrenByRole(Roles.EmbeddedStatement).FirstOrDefault();

				if (expr is BreakStatement)
					AddIssue(switchSection, ctx.TranslateString("Remove redundant empty default branch in switch"), script => {
						script.Remove(switchSection);
					}
					);
				else 
					return;
			}
		}
	}
}