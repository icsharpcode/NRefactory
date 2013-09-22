// 
// NonReadonlyReferencedInGetHashCodeIssue.cs
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
	/// Non-readonly field referenced in “GetHashCode()” 
	/// </summary>
	[IssueDescription("Warning for non-readonly field referenced in “GetHashCode()”",
	                  Description= "Warning for non-readonly field referenced in “GetHashCode()”",
	                  Category = IssueCategories.CodeQualityIssues,
	                  Severity = Severity.Warning,
	                  AnalysisDisableKeyword = "NonReadonlyReferencedInGetHashCode")]
	public class NonReadonlyReferencedInGetHashCodeIssue : GatherVisitorCodeIssueProvider
	{	
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
		}
		
		class GatherVisitor : GatherVisitorBase<NonReadonlyReferencedInGetHashCodeIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx)
				: base(ctx)
			{
			}
			
			bool IsInGetHashCode(AstNode node)
			{
				var method = node.GetParent<MethodDeclaration>();
				if (method == null)
					return false;
				
				MemberResolveResult methodResult = ctx.Resolve(method) as MemberResolveResult;
				if (methodResult.IsError || !methodResult.Member.Name.Equals("GetHashCode") || !methodResult.Member.IsOverride || !methodResult.Member.ReturnType.IsKnownType(KnownTypeCode.Int32))
					return false;
				return true;
			}
			
			public override void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
			{
				base.VisitMemberReferenceExpression(memberReferenceExpression);
				
				if (IsInGetHashCode(memberReferenceExpression)) {
					var target = memberReferenceExpression.Target;
					var resolvedResult = ctx.Resolve(target);
					var type = resolvedResult.Type;
					var Members = type.GetMembers(f => f.Name.Equals(memberReferenceExpression.MemberName));
					if (Members.Count() != 1)
						return;
					
					var member = Members.Single();
					if (member is IField) {
						if (!((IField)member).IsReadOnly) {
							AddIssue(new CodeIssue(memberReferenceExpression.MemberNameToken, "Non-Readonly field referenced in GetHashCode") { IssueMarker = IssueMarker.WavedLine });
						}
					}
				}
			}
			
			public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
			{
				base.VisitIdentifierExpression(identifierExpression);
				
				var resolvedResult = ctx.Resolve(identifierExpression);
				if (resolvedResult is MemberResolveResult) {
					var member = ((MemberResolveResult)resolvedResult).Member;
					if (member is IField) {
						if (!((IField)member).IsReadOnly) {
							AddIssue(new CodeIssue(identifierExpression, "Non-Readonly field referenced in GetHashCode") { IssueMarker = IssueMarker.WavedLine });
						}
					}
				}
			}
		}
	}
}