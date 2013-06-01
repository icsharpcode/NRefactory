// LockThisIssue.cs 
// 
// Author:
//      Luís Reis <luiscubal@gmail.com>
// 
// Copyright (c) 2013 Luís Reis <luiscubal@gmail.com>
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
using ICSharpCode.NRefactory.CSharp.Analysis;
using ICSharpCode.NRefactory.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("Use of lock (this) is discouraged",
	                  Description = "Warns about using lock (this).",
	                  Category = IssueCategories.CodeQualityIssues,
	                  Severity = Severity.Warning)]
	public class LockThisIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues (BaseRefactoringContext context)
		{
			return new GatherVisitor(context).GetIssues();
		}

		class GatherVisitor : GatherVisitorBase<LockThisIssue>
		{
			public GatherVisitor (BaseRefactoringContext context) : base (context)
			{
			}

			public override void VisitLockStatement(LockStatement lockStatement)
			{
				base.VisitLockStatement(lockStatement);

				var expression = lockStatement.Expression;

				if (IsThisReference(expression)) {
					AddIssue(lockStatement, ctx.TranslateString("Found lock (this)"));
				}
			}

			static bool IsThisReference (Expression expression)
			{
				if (expression is ThisReferenceExpression) {
					return true;
				}

				var parenthesizedExpression = expression as ParenthesizedExpression;
				if (parenthesizedExpression != null) {
					return IsThisReference(parenthesizedExpression.Expression);
				}

				return false;
			}
		}
	}
}

