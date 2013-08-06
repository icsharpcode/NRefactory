// 
// RedundantLambdaParameterTypeIssue.cs
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
	/// Explicit type specification can be removed as it can be implicitly inferred.
	/// </summary>
	[IssueDescription("Remove redundant explicit type specification in lambda expression",
	                  Description= "Explicit type specification can be removed as it can be implicitly inferred.",
	                  Category = IssueCategories.RedundanciesInCode,
	                  Severity = Severity.Hint,
	                  IssueMarker = IssueMarker.GrayOut,
	                  ResharperDisableKeyword = "RedundantLambdaParameterType")]
	public class RedundantLambdaParameterTypeIssue : GatherVisitorCodeIssueProvider
	{
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context, this);
		}

		class GatherVisitor : GatherVisitorBase<RedundantLambdaParameterTypeIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx, RedundantLambdaParameterTypeIssue issueProvider) : base (ctx, issueProvider)
			{
			}

			public override void VisitLambdaExpression(LambdaExpression lambdaexpression)
			{
				base.VisitLambdaExpression(lambdaexpression);

				if (lambdaexpression == null)
					return;

				var arguments = lambdaexpression.Parameters;

				if (arguments.Any(f => f.Type.IsNull))
					return;

				bool singleArgument = (arguments.Any());
				foreach (var argument in arguments) {
					var type = argument.GetChildByRole(Roles.Type);
					AddIssue(type, ctx.TranslateString("Explicit type specification can be removed as it can be implicitly inferred."), ctx.TranslateString("Remove parameter type specification"), script => {
						if (singleArgument) {
							if (argument.NextSibling.ToString().Equals(")") && argument.PrevSibling.ToString().Equals("(")) {
								script.Remove(argument.NextSibling);
								script.Remove(argument.PrevSibling);
							}
						}
						script.Remove(type);
					});
				}
			}
		}
	}
}