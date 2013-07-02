//
// ReplaceWithIsOperatorIssue.cs
//
// Author:
//	   Ji Kun <jikun.nus@gmail.com>
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
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.using System;
using System.Collections.Generic;
using System;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("Replace with Is operator",
	                  Description = "Operator Is can be used instead of comparing object GetType() and instances of System.Type object.",
	                  Category = IssueCategories.Opportunities,
	                  Severity = Severity.Warning,
	                  ResharperDisableKeyword = "ReplaceWithIsOpeartor",
	                  IssueMarker = IssueMarker.Underline)]
	public class ReplaceWithIsOperatorIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context).GetIssues();
		}

		class GatherVisitor : GatherVisitorBase<ReplaceWithIsOperatorIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx)
				: base(ctx)
			{
			}

			public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
			{
				base.VisitBinaryOperatorExpression(binaryOperatorExpression);
				if (binaryOperatorExpression.Operator.Equals(OperatorType.Equality))
					return;

				var resovledResult = ctx.Resolve(binaryOperatorExpression);
				if (resovledResult.IsError)
					return;

				Expression identifier = null;
				AstType type = null;

				if (binaryOperatorExpression.Left is TypeOfExpression) {
					if (binaryOperatorExpression.Right is InvocationExpression) {
						InvocationExpression right = binaryOperatorExpression.Right as InvocationExpression;
						if (right.Target is MemberReferenceExpression) {
							if ((right.Target as MemberReferenceExpression).MemberName.Equals("GetType")) {
								type = (binaryOperatorExpression.Left as TypeOfExpression).Type;
								identifier = (right.Target as MemberReferenceExpression).Target;
							} else
								return;
						} else
							return;
					}
				}

				if (binaryOperatorExpression.Right is TypeOfExpression) {
					if (binaryOperatorExpression.Left is InvocationExpression) {
						InvocationExpression left = binaryOperatorExpression.Left as InvocationExpression; 
						if (left.Target is MemberReferenceExpression) {
							if ((left.Target as MemberReferenceExpression).MemberName.Equals("GetType")) {
								type = (binaryOperatorExpression.Right as TypeOfExpression).Type;
								identifier = (left.Target as MemberReferenceExpression).Target;
							} else
								return;
						} else
							return;
					}
				}

				if (identifier == null || type == null)
					return;

				AddIssue(binaryOperatorExpression, ctx.TranslateString("Replace with Is operator"), script => {
					var isExpr = new IsExpression();
					isExpr.Type = type.Clone();
					isExpr.Expression = identifier.Clone();
					script.Replace(binaryOperatorExpression, isExpr);
				});
				return;
			}
		}
	}
}

