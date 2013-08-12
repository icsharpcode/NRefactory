// 
// DelegateParametersAreUnusedIssue.cs
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
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Linq;
using System.Runtime.InteropServices;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Delegate parameters are unused",
	                   Description = "Detects when no delegate parameter is used in the delegate body.",
	                   Category = IssueCategories.Opportunities,
	                   Severity = Severity.Warning,
	                   IssueMarker = IssueMarker.WavedLine)]
	public class DelegateParametersAreUnusedIssue : GatherVisitorCodeIssueProvider
	{
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
		}

		class GatherVisitor : GatherVisitorBase<DelegateParametersAreUnusedIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx)
				: base(ctx)
			{
			}

			public override void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
			{
				if (!anonymousMethodExpression.HasParameterList) {
					base.VisitAnonymousMethodExpression(anonymousMethodExpression);
					return;
				}

				var parameterNames = anonymousMethodExpression.Parameters.Select(parameter => parameter.Name);

				var identifiers = anonymousMethodExpression.Body.Descendants.OfType<IdentifierExpression>();
				if (identifiers.Any(identifier => parameterNames.Contains(identifier.Identifier))) {
					base.VisitAnonymousMethodExpression(anonymousMethodExpression);
					return;
				}

				if (!RedundantLambdaParameterTypeIssue.LambdaTypeCanBeInferred(ctx, anonymousMethodExpression, anonymousMethodExpression.Parameters)) {
					base.VisitAnonymousMethodExpression(anonymousMethodExpression);
					return;
				}

				AddIssue(anonymousMethodExpression.LParToken.StartLocation,
				         anonymousMethodExpression.RParToken.EndLocation,
				         ctx.TranslateString("Redundant parameter list (all parameters are unused)"),
				         ctx.TranslateString("Remove delegate parameter list"),
				         script => {

					int start = script.GetCurrentOffset(anonymousMethodExpression.LParToken.StartLocation);
					int end = script.GetCurrentOffset(anonymousMethodExpression.RParToken.EndLocation);

					script.RemoveText(start, end - start);

				});

				base.VisitAnonymousMethodExpression(anonymousMethodExpression);
			}
		}
	}
}

