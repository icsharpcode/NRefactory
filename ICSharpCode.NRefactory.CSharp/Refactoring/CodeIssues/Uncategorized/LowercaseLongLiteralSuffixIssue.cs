//
// LowercaseLongLiteralIssue.cs
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
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Use of lowercase L as long literal suffix",
	                   Description = "Lowercase long literal suffix l should be avoided.",
	                   Category = IssueCategories.CodeQualityIssues,
	                   Severity = Severity.Warning,
	                   IssueMarker = IssueMarker.WavedLine)]
	public class LowercaseLongLiteralSuffixIssue : GatherVisitorCodeIssueProvider
	{
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
		}

		class GatherVisitor : GatherVisitorBase<LowercaseLongLiteralSuffixIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx)
				: base(ctx)
			{
			}

			public override void VisitPrimitiveExpression(PrimitiveExpression primitiveExpression)
			{
				if (!(primitiveExpression.Value is long || primitiveExpression.Value is ulong))
				{
					//Literals such as "l" or 'l' are perfectly acceptable.
					//Also, no point in visiting integer or boolean literals
					return;
				}

				string literalValue = primitiveExpression.LiteralValue;
				if (literalValue.Length < 2) {
					return;
				}

				if (literalValue [literalValue.Length - 1] == 'l' || literalValue [literalValue.Length - 2] == 'l') {

					AddIssue(primitiveExpression,
					        "Use of lowercase l as long literal suffix",
					        "Replace lowercase l suffix by uppercase L",
					        script => {

						object newValue = primitiveExpression.Value;
						string newLiteralValue = primitiveExpression.LiteralValue.Replace('l', 'L');
						script.Replace(primitiveExpression, new PrimitiveExpression(newValue, newLiteralValue));

					});
				}
			}
		}
	}
}

