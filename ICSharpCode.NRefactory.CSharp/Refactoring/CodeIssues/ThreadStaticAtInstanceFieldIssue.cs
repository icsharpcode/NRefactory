//
// ThreadStaticAtInstanceField.cs
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
using System.Collections.Generic;
using ICSharpCode.NRefactory.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("[ThreadStatic] doesn't work with instance fields",
	                   Description = "[ThreadStatic] doesn't work with instance fields",
	                   Category = IssueCategories.CodeQualityIssues,
	                   Severity = Severity.Warning,
	                   IssueMarker = IssueMarker.GrayOut,
	                   ResharperDisableKeyword = "ThreadStaticAtInstanceField")]
	public class ThreadStaticAtInstanceFieldIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context).GetIssues();
		}

		class GatherVisitor : GatherVisitorBase<ThreadStaticAtInstanceFieldIssue>
		{
			public GatherVisitor (BaseRefactoringContext ctx) : base (ctx)
			{
			}

			public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
			{
			}

			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
			{
			}

			public override void VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration)
			{
			}

			public override void VisitEventDeclaration(EventDeclaration eventDeclaration)
			{
			}

			public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
			{
				if (fieldDeclaration.HasModifier(Modifiers.Static))
					return;
				foreach (var section in fieldDeclaration.Attributes) {
					foreach (var attr in section.Attributes) {
						var result = ctx.Resolve(attr);
						if (result.Type.Name == "ThreadStaticAttribute" && result.Type.Namespace == "System") {
							AddIssue(attr, ctx.TranslateString("Remove attribute"), script => {
								script.RemoveAttribute (attr);
							});
						}
					}
				}
			}
		}
	}
}

