//
// RedundantStringFormatArgumentIssue.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Linq;
using ICSharpCode.NRefactory.Utils;
using ICSharpCode.NRefactory.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	// It's technically part of FormatStringProblemIssue but needs another marker
	[IssueDescription("Redundant format item arguments",
	                  Description = "Finds redundant format item arguments",
	                  Category = IssueCategories.RedundanciesInCode,
	                  Severity = Severity.Warning,
	                  ResharperDisableKeyword = "FormatStringProblem")]
	public class RedundantStringFormatArgumentIssue : GatherVisitorCodeIssueProvider
	{
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
		}

		class GatherVisitor : GatherVisitorBase<RedundantStringFormatArgumentIssue>
		{
			public GatherVisitor(BaseRefactoringContext context) : base (context)
			{
			}

			public override void VisitInvocationExpression(InvocationExpression invocationExpression)
			{
				base.VisitInvocationExpression(invocationExpression);

				// Speed up the inspector by discarding some invocations early
				var hasEligibleArgument = invocationExpression.Arguments.Any(argument => {
					var primitiveArg = argument as PrimitiveExpression;
					return primitiveArg != null && primitiveArg.Value is string;
				});
				if (!hasEligibleArgument)
					return;

				var invocationResolveResult = ctx.Resolve(invocationExpression) as CSharpInvocationResolveResult;
				if (invocationResolveResult == null)
					return;
				Expression formatArgument;
				IList<Expression> formatArguments;
				if (!FormatStringHelper.TryGetFormattingParameters(invocationResolveResult, invocationExpression,
				                                                   out formatArgument, out formatArguments, null)) {
					return;
				}
				var primitiveArgument = formatArgument as PrimitiveExpression;
				if (primitiveArgument == null || !(primitiveArgument.Value is string))
					return;
				var format = (string)primitiveArgument.Value;
				var parsingResult = ctx.ParseFormatString(format);
				int maxIndex = parsingResult.Segments.OfType<FormatItem>().Max(s => s.Index);
				int i = 0;
				foreach (var expression in invocationExpression.Arguments.Skip(1)) {
					if (i > maxIndex)
						AddIssue(expression, IssueMarker.GrayOut, ctx.TranslateString("Argument is not used in format string"));
					i++;
				}
			}

		}
	}
}

