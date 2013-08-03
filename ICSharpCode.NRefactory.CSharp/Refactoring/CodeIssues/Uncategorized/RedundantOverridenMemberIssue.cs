// 
// RedundantOverridenMemberIssue.cs
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
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Remove redundant overriden members",
	                   Description = "The override of a virtual member is redundant because it consists of only a call to the base",
	                   Category = IssueCategories.Redundancies,
	                   Severity = Severity.Warning,
	                   IssueMarker = IssueMarker.GrayOut, 
	                   ResharperDisableKeyword = "RedundantOverridenMember")]
	public class RedundantOverridenMemberIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context).GetIssues();
		}
		
		class GatherVisitor : GatherVisitorBase<RedundantOverridenMemberIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx)
				: base (ctx)
			{
			}
			
			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
			{
				base.VisitMethodDeclaration(methodDeclaration);
				
				if (!methodDeclaration.HasModifier(Modifiers.Override))
					return;
				
				if (methodDeclaration.Body.Statements.Count != 1)
					return;
				
				var expr = methodDeclaration.Body.Statements.FirstOrNullObject();
				
				if (expr == null || !(expr.FirstChild is InvocationExpression))
					return;
				
				Expression memberReferenceExpression = (expr.FirstChild as InvocationExpression).Target;
				if (memberReferenceExpression == null || 
					(memberReferenceExpression as MemberReferenceExpression).MemberName != methodDeclaration.Name ||
					!(memberReferenceExpression.FirstChild is BaseReferenceExpression))
					return;
				var title = ctx.TranslateString("Overriden methods that just call the base class methods are redundant");
				AddIssue(methodDeclaration, title, ctx.TranslateString("Remove redundant members"), script => {
					script.Remove(methodDeclaration);
				});
			}
			
			public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
			{
				base.VisitPropertyDeclaration(propertyDeclaration);
				
				if (!propertyDeclaration.HasModifier(Modifiers.Override))
					return;
				
				bool hasGetter = !propertyDeclaration.Getter.IsNull;
				bool hasSetter = !propertyDeclaration.Setter.IsNull;
				if (!hasGetter && !hasSetter)
					return;
				
				if (hasGetter && propertyDeclaration.Getter.Body.Statements.Count != 1)
					return;
				
				if (hasSetter && propertyDeclaration.Setter.Body.Statements.Count != 1)
					return;
				
				var resultProperty = ctx.Resolve(propertyDeclaration);
				var basetype = (resultProperty as MemberResolveResult).Member.DeclaringTypeDefinition.DirectBaseTypes.First();
				if (basetype == null)
					return;
				var baseProperty = basetype.GetMembers(f => f.Name.Equals(propertyDeclaration.Name)).FirstOrDefault();
				if (baseProperty == null)
					return;
				
				bool hasBaseGetter = ((baseProperty as IProperty).Getter != null);
				bool hasBaseSetter = ((baseProperty as IProperty).Setter != null);
				
				if (hasBaseGetter) {
					if (!hasGetter)
						return;
					var expr = propertyDeclaration.Getter.Body.Statements.FirstOrNullObject();
					
					if (expr == null || !(expr is ReturnStatement))
						return;
					
					Expression memberReferenceExpression = (expr as ReturnStatement).Expression;
					
					if (memberReferenceExpression == null || 
						(memberReferenceExpression as MemberReferenceExpression).MemberName != propertyDeclaration.Name ||
						!(memberReferenceExpression.FirstChild is BaseReferenceExpression))
						return;
				}
				
				if (hasBaseSetter) {
					if (!hasSetter)
						return;
					
					var expr = propertyDeclaration.Setter.Body.Statements.FirstOrNullObject();
					
					if (expr == null || !(expr.FirstChild is AssignmentExpression))
						return;
					
					Expression memberReferenceExpression = (expr.FirstChild as AssignmentExpression).Left;
					
					if (memberReferenceExpression == null || 
						(memberReferenceExpression as MemberReferenceExpression).MemberName != propertyDeclaration.Name ||
						!(memberReferenceExpression.FirstChild is BaseReferenceExpression))
						return;
				}
				
				var title = ctx.TranslateString("Overriden property that just return the base class property are redundant");
				AddIssue(propertyDeclaration, title, ctx.TranslateString("Remove redundant overriden memebrs"), script => {
					script.Remove(propertyDeclaration);
				});
			}
			
			public override void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
			{
				base.VisitIndexerDeclaration(indexerDeclaration);
				
				if (!indexerDeclaration.HasModifier(Modifiers.Override))
					return;
				
				bool hasGetter = !indexerDeclaration.Getter.IsNull;
				bool hasSetter = !indexerDeclaration.Setter.IsNull;
				if (!hasGetter && !hasSetter)
					return;
				
				if (hasGetter && indexerDeclaration.Getter.Body.Statements.Count != 1)
					return;
				
				if (hasSetter && indexerDeclaration.Setter.Body.Statements.Count != 1)
					return;
				
				var resultIndexer = ctx.Resolve(indexerDeclaration);
				var basetype = (resultIndexer as MemberResolveResult).Member.DeclaringType.DirectBaseTypes.First();
				if (basetype == null)
					return;

				var baseIndexer = basetype.GetMembers(f => f.Name == "Item").FirstOrDefault();
				if (baseIndexer == null)
					return;
				
				bool hasBaseGetter = ((baseIndexer as IProperty).Getter != null);
				bool hasBaseSetter = ((baseIndexer as IProperty).Setter != null);
				
				if (hasBaseGetter) {
					if (!hasGetter)
						return;
					
					var expr = indexerDeclaration.Getter.Body.Statements.FirstOrNullObject();
					
					if (expr == null || !(expr is ReturnStatement))
						return;
					
					Expression indexerExpression = (expr as ReturnStatement).Expression;
					
					if (indexerExpression == null || 
						!(indexerExpression.FirstChild is BaseReferenceExpression))
						return;
				}
				
				if (hasBaseSetter) {
					if (!hasSetter)
						return;
					
					var expr = indexerDeclaration.Setter.Body.Statements.FirstOrNullObject();
					
					if (expr == null || !(expr.FirstChild is AssignmentExpression))
						return;
					
					Expression memberReferenceExpression = (expr.FirstChild as AssignmentExpression).Left;
					
					if (memberReferenceExpression == null || 
						!(memberReferenceExpression.FirstChild is BaseReferenceExpression))
						return;
				}
				
				var title = ctx.TranslateString("Overriden indexers that just return the base class indexers are redundant");
				AddIssue(indexerDeclaration, title, ctx.TranslateString("Remove redundant overriden members"), script => {
					script.Remove(indexerDeclaration);
				});
			}
		}
	}
}
