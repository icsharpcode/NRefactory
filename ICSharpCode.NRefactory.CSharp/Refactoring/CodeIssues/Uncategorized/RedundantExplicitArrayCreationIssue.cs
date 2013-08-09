// RedundantExplicitArrayCreationIssue.cs
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

using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.PatternMatching;
using System.Linq;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.CSharp.Resolver;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("RedundantExplicitArrayCreation",
						Description = "Redundant explicit type in array creation",
						Category = IssueCategories.RedundanciesInCode,
						Severity = Severity.Warning,
						ResharperDisableKeyword = "RedundantExplicitArrayCreation",
						IssueMarker = IssueMarker.GrayOut)]
	public class RedundantExplicitArrayCreationIssue : GatherVisitorCodeIssueProvider
	{

		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context, this);
		}


		class GatherVisitor : GatherVisitorBase<RedundantExplicitArrayCreationIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx, RedundantExplicitArrayCreationIssue issueProvider) : base (ctx, issueProvider)
			{
			}

			public override void VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression)
			{
				base.VisitArrayCreateExpression(arrayCreateExpression);
				var arrayCreateExpressionRoleType = arrayCreateExpression.GetChildByRole(Roles.Type);
				if (!arrayCreateExpressionRoleType.IsNull && arrayCreateExpression.Initializer != null)
				{
					AddIssue(arrayCreateExpression.Type, ctx.TranslateString("Redundant explicit type in array creation"), ctx.TranslateString("Remove explicit type in array creation"),
						script =>
						{
							var startOffset = script.GetCurrentOffset(arrayCreateExpression.NewToken.EndLocation);
							var endOffset = script.GetCurrentOffset(arrayCreateExpression.AdditionalArraySpecifiers.FirstOrNullObject().StartLocation);
							if (startOffset < endOffset)
								script.RemoveText(startOffset, endOffset - startOffset);
						});
				}
			}
		}
	}
}