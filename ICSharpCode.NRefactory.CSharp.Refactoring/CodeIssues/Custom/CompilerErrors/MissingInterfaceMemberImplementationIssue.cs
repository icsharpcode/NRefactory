//
// MissingInterfaceMemberImplementationIssue.cs
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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Missing interface members",
		Description = "",
		Category = IssueCategories.CompilerErrors,
		ActionProvider = typeof(ImplementInterfaceAction),
		Severity = Severity.Error)]
	public class MissingInterfaceMemberImplementationIssue : GatherVisitorCodeIssueProvider
	{
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
		}

		class GatherVisitor : GatherVisitorBase<NegativeRelationalExpressionIssue>
		{
			public GatherVisitor (BaseRefactoringContext ctx)
				: base (ctx)
			{
			}

			public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
			{
				base.VisitTypeDeclaration(typeDeclaration);
				var rr = ctx.Resolve(typeDeclaration);
				if (rr.IsError)
					return;
				foreach (var baseType in typeDeclaration.BaseTypes) {
					var bt = ctx.Resolve(baseType);
					if (bt.IsError || bt.Type.Kind != TypeKind.Interface)
						continue;
					var toImplement = ImplementInterfaceAction.CollectMembersToImplement(rr.Type.GetDefinition(), bt.Type, false);
					if (toImplement.Count == 0)
						continue;

					AddIssue(baseType, ctx.TranslateString("Missing interface member implementations"));
				}
			}
		}
	}
}

