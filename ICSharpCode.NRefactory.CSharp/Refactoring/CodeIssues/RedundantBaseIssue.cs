﻿// 
// RedundantBaseIssue.cs
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
	/// Finds redundant base qualifier 
	/// </summary>
	[IssueDescription("Remove redundant 'base.'",
			Description= "Removes 'base.' references that are not required.",
			Category = IssueCategories.Redundancies,
			Severity = Severity.Hint,
			IssueMarker = IssueMarker.GrayOut,
			ResharperDisableKeyword = "RedundantBaseQualifier")]
	public class RedundantBaseIssue : ICodeIssueProvider
	{
		bool ignoreConstructors = true;

		/// <summary>
		/// Specifies whether to ignore redundant 'base' in constructors.
		/// "base.Name = name;"
		/// </summary>
		public bool IgnoreConstructors {
			get {
				return ignoreConstructors;
			}
			set {
				ignoreConstructors = value;
			}
		}
		
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context, this).GetIssues();
		}

		class GatherVisitor : GatherVisitorBase<RedundantBaseIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx, RedundantBaseIssue issueProvider) : base (ctx, issueProvider)
			{
			}

			static IMember GetMember(ResolveResult result)
			{
				if (result is MemberResolveResult) {
					return ((MemberResolveResult)result).Member;
				} else if (result is MethodGroupResolveResult) {
					return ((MethodGroupResolveResult)result).Methods.FirstOrDefault();
				}

				return null;
			}
			
			public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
			{
				if (IssueProvider.IgnoreConstructors)
					return;
				base.VisitConstructorDeclaration(constructorDeclaration);
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

				if (state.CurrentTypeDefinition.DirectBaseTypes == null)
					return;

				var basicMembers = state.CurrentTypeDefinition.DirectBaseTypes.First().GetMembers();
				var extendedMembers = state.CurrentTypeDefinition.GetMembers().Except(basicMembers);

				bool isRedundant = !extendedMembers.Any(f => f.Name.Equals(member.Name));

				if (isRedundant) {
					AddIssue(baseReferenceExpression.StartLocation, memberReference.MemberNameToken.StartLocation, ctx.TranslateString("Remove redundant 'base.'"), script => {
						script.Replace(memberReference, RefactoringAstHelper.RemoveTarget(memberReference));
					}
					);
				}
			}
		}
	}
}