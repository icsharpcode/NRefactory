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
using ICSharpCode.NRefactory.PatternMatching;
using System.Runtime.InteropServices;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Redundant member override",
	                   Description = "The override of a virtual member is redundant because it consists of only a call to the base",
	                   Category = IssueCategories.RedundanciesInDeclarations,
	                   Severity = Severity.Warning,
	                   IssueMarker = IssueMarker.GrayOut, 
	                   ResharperDisableKeyword = "RedundantOverridenMember")]
	public class RedundantOverridenMemberIssue : GatherVisitorCodeIssueProvider
	{
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
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
				//Debuger.WriteInFile(expr.FirstChild.ToString());
				if (expr == null)
					return;
				if (expr.FirstChild is InvocationExpression) {
					var memberReferenceExpression = (expr.FirstChild as InvocationExpression).Target as MemberReferenceExpression;
					if (memberReferenceExpression == null || 
						memberReferenceExpression.MemberName != methodDeclaration.Name ||
						!(memberReferenceExpression.FirstChild is BaseReferenceExpression))
						return;
					var title = ctx.TranslateString("Redundant method override");
					AddIssue(methodDeclaration, title, ctx.TranslateString("Remove redundant method override"), script => {
						script.Remove(methodDeclaration);
					});
				} else if (expr.FirstChild is CSharpTokenNode && expr.FirstChild.ToString().Equals("return")) {
					var invocationExpression = expr.FirstChild.NextSibling as InvocationExpression;
					if (invocationExpression == null)
						return;
					var memberReferenceExpression = invocationExpression.Target as MemberReferenceExpression;
					if (memberReferenceExpression == null || 
						memberReferenceExpression.MemberName != methodDeclaration.Name ||
						!(memberReferenceExpression.FirstChild is BaseReferenceExpression))
						return;
					var title = ctx.TranslateString("Redundant method override");
					AddIssue(methodDeclaration, title, ctx.TranslateString("Remove redundant method override"), script => {
						script.Remove(methodDeclaration);
					});
				}
				return;
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
				
				var resultProperty = ctx.Resolve(propertyDeclaration) as MemberResolveResult;
				var basetype = resultProperty.Member.DeclaringTypeDefinition.DirectBaseTypes.First();
				if (basetype == null)
					return;
				var baseProperty = basetype.GetMembers(f => f.Name.Equals(propertyDeclaration.Name)).FirstOrDefault();
				if (baseProperty == null)
					return;
				
				bool hasBaseGetter = ((baseProperty as IProperty).Getter != null);
				bool hasBaseSetter = ((baseProperty as IProperty).Setter != null);
				
				if (hasBaseGetter) {
					if (hasGetter) {
						var expr = propertyDeclaration.Getter.Body.Statements.FirstOrNullObject();
					
						if (expr == null || !(expr is ReturnStatement))
							return;
					
						var memberReferenceExpression = (expr as ReturnStatement).Expression as MemberReferenceExpression;
					
						if (memberReferenceExpression == null || 
							memberReferenceExpression.MemberName != propertyDeclaration.Name ||
							!(memberReferenceExpression.FirstChild is BaseReferenceExpression))
							return;
					}
				}
				
				if (hasBaseSetter) {
					if (hasSetter) {
					
						var expr = propertyDeclaration.Setter.Body.Statements.FirstOrNullObject();
					
						if (expr == null || !(expr.FirstChild is AssignmentExpression))
							return;
					
						var memberReferenceExpression = (expr.FirstChild as AssignmentExpression).Left as MemberReferenceExpression;
					
						if (memberReferenceExpression == null || 
							memberReferenceExpression.MemberName != propertyDeclaration.Name ||
							!(memberReferenceExpression.FirstChild is BaseReferenceExpression))
							return;
					}
				}
				
				var title = ctx.TranslateString("Redundant property override");
				AddIssue(propertyDeclaration, title, ctx.TranslateString("Remove redundant property override"), script => {
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
				
				var resultIndexer = ctx.Resolve(indexerDeclaration) as MemberResolveResult;
				var basetype = resultIndexer.Member.DeclaringType.DirectBaseTypes.First();
				if (basetype == null)
					return;

				var baseIndexer = basetype.GetMembers(f => f.Name == "Item").FirstOrDefault();
				if (baseIndexer == null)
					return;
				
				bool hasBaseGetter = ((baseIndexer as IProperty).Getter != null);
				bool hasBaseSetter = ((baseIndexer as IProperty).Setter != null);
				
				if (hasBaseGetter) {
					if (hasGetter) {
					
						var expr = indexerDeclaration.Getter.Body.Statements.FirstOrNullObject() as ReturnStatement;
					
						if (expr == null)
							return;
					
						Expression indexerExpression = expr.Expression;
					
						if (indexerExpression == null || 
							!(indexerExpression.FirstChild is BaseReferenceExpression))
							return;
					}
				}
				
				if (hasBaseSetter) {
					if (hasSetter) {
					
						var expr = indexerDeclaration.Setter.Body.Statements.FirstOrNullObject();
					
						if (expr == null || !(expr.FirstChild is AssignmentExpression))
							return;
					
						Expression memberReferenceExpression = (expr.FirstChild as AssignmentExpression).Left;
					
						if (memberReferenceExpression == null || 
							!(memberReferenceExpression.FirstChild is BaseReferenceExpression))
							return;
					}
				}
				
				var title = ctx.TranslateString("Redundant indexer override");
				AddIssue(indexerDeclaration, title, ctx.TranslateString("Remove redundant indexer override"), script => {
					script.Remove(indexerDeclaration);
				});
			}

			static readonly AstNode customEventPattern =
				new CustomEventDeclaration {
					Modifiers = Modifiers.Any,
					Name = Pattern.AnyString,
					ReturnType = new AnyNode(), 
					AddAccessor = new Accessor {
						Body = new BlockStatement {
							new ExpressionStatement(new AssignmentExpression {
								Left = new NamedNode ("baseRef", new MemberReferenceExpression(new BaseReferenceExpression(), Pattern.AnyString)),
								Operator = AssignmentOperatorType.Add,
								Right = new IdentifierExpression("value")
							})
						}
					},
					RemoveAccessor = new Accessor {
						Body = new BlockStatement {
							new ExpressionStatement(new AssignmentExpression {
								Left = new Backreference("baseRef"),
								Operator = AssignmentOperatorType.Subtract,
								Right = new IdentifierExpression("value")
							})
						}
					},
				};
			
			public override void VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration)
			{
				var m = customEventPattern.Match(eventDeclaration);
				if (!m.Success)
					return;
				var baseRef = m.Get<MemberReferenceExpression>("baseRef").First();
				if (baseRef == null || baseRef.MemberName != eventDeclaration.Name)
					return;

				var title = ctx.TranslateString("Redundant event override");
				AddIssue(eventDeclaration, title, ctx.TranslateString("Remove event override"), script => {
					script.Remove(eventDeclaration);
				});
			}
		}
	}
}
