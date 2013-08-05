// 
// RedundantBaseQualifierIssue.cs
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
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Linq;
using ICSharpCode.NRefactory.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Finds redundant base qualifier 
	/// </summary>
	[IssueDescription("Redundant 'base.' qualifier",
			Description= "'base.' is redundant and can safely be removed.",
			Category = IssueCategories.RedundanciesInCode,
			Severity = Severity.Warning,
			IssueMarker = IssueMarker.GrayOut,
			ResharperDisableKeyword = "RedundantBaseQualifier")]
	public class RedundantBaseQualifierIssue : GatherVisitorCodeIssueProvider
	{
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context, this);
		}

		class GatherVisitor : GatherVisitorBase<RedundantBaseQualifierIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx, RedundantBaseQualifierIssue issueProvider) : base (ctx, issueProvider)
			{
			}

			static IMember GetMember(ResolveResult result)
			{
				if (result is MemberResolveResult)
					return ((MemberResolveResult)result).Member;
				if (result is MethodGroupResolveResult)
					return ((MethodGroupResolveResult)result).Methods.FirstOrDefault();
				return null;
			}

			public override void VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression)
			{
				base.VisitBaseReferenceExpression(baseReferenceExpression);
				var memberReference = baseReferenceExpression.Parent as MemberReferenceExpression;
				if (memberReference == null) {
					return;
				}

				var state = ctx.GetResolverStateAfter(baseReferenceExpression);
				var wholeResult = ctx.Resolve(memberReference);
				
				IMember member = GetMember(wholeResult);
				if (member == null || member.IsOverridable) { 
					return;
				}

				if (member.SymbolKind == SymbolKind.Field) {
					var method = memberReference.Parent;
					while (!(method is MethodDeclaration)) {
						method = method.Parent;
						if (method == null)
							return;
					}
					var parameters = (method as MethodDeclaration).Parameters;

					if (parameters.Any(f => f.NameToken.ToString() == member.Name)) {
						return;
					}

					var localvariables = state.LocalVariables;
					if (localvariables.Any(f => f.Name == member.FullName)) {
						return;
					}
				}

				if (state.CurrentTypeDefinition.DirectBaseTypes == null)
					return;

				var basicMembers = state.CurrentTypeDefinition.DirectBaseTypes.First().GetMembers();
				var extendedMembers = state.CurrentTypeDefinition.GetMembers().Except(basicMembers);

				bool isRedundant = !extendedMembers.Any(f => f.Name.Equals(member.Name));

				if (isRedundant) {
					AddIssue(
						baseReferenceExpression.StartLocation, 
						memberReference.MemberNameToken.StartLocation, 
						ctx.TranslateString("Qualifier 'base.' is redundant"), 
						new CodeAction(
							ctx.TranslateString("Remove redundant 'base.'"),
							script => {
								script.Replace(memberReference, RefactoringAstHelper.RemoveTarget(memberReference));
							},
							baseReferenceExpression.StartLocation,
							memberReference.MemberNameToken.StartLocation
						) 
					);
				}
			}
		}
	}
}