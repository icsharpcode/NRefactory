// 
// RedundantArgumentNameIssue.cs
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
using System.IO;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.PatternMatching;
using System.Linq;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.CSharp.Resolver;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Redundant explicit argument name specification.
	/// </summary>
	[IssueDescription("Redundant explicit argument name specifications",
	                  Description= "Explicit argument name specifications are redundant if they are in the same order with the parameter list.",
	                  Category = IssueCategories.RedundanciesInCode,
	                  Severity = Severity.Suggestion,
	                  IssueMarker = IssueMarker.GrayOut,
	                  ResharperDisableKeyword = "RedundantArgumentName")]
	public class RedundantArgumentNameIssue : GatherVisitorCodeIssueProvider
	{
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context, this);
		}
		
		class GatherVisitor : GatherVisitorBase<RedundantArgumentNameIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx, RedundantArgumentNameIssue issueProvider) : base (ctx, issueProvider)
			{
			}

			private bool IsSublist(List<string> sublist, List<string> list)
			{
				foreach (string str in sublist) {
					int index = list.FindIndex(f => f.Equals(str));
					if (index < 0)
						return false;
					else {
						list.RemoveRange(0, index + 1);
					}

				}
				return true;
			}

			private bool IsRedundant(AstNode parant, List<string> parameterNames)
			{
				var arguments = parant.GetChildrenByRole(Roles.Argument).Where(k => k is NamedArgumentExpression);
				if (!arguments.Any())
					return false;

				List<string> argumentNames = new List<string>();

				foreach (var argument in arguments) {
					argumentNames.Add((argument as NamedArgumentExpression).Name);
				}

				return IsSublist(argumentNames, parameterNames);
			}

			private List<NamedArgumentExpression> CollectNodes(AstNode parant, AstNode node)
			{
				List<NamedArgumentExpression> returned = new List<NamedArgumentExpression>();	
				var children = parant.GetChildrenByRole(Roles.Argument);
				for (int i = children.Count() - 1; i > -1; i--) {
					if (children.ElementAt(i).Equals(node)) {
						for (int j = i; j > -1; j--) {
							if (children.ElementAt(j) is Expression && children.ElementAt(j).Role == Roles.Argument && children.ElementAt(j) is NamedArgumentExpression) {
								returned.Add(children.ElementAt(j) as NamedArgumentExpression);
							} else {
								break;
							}
						}
						break;
					}
				}
				return returned;
			}

			private void AddIssue(List<NamedArgumentExpression> nodes)
			{
				NamedArgumentExpression fnode = nodes.First();
				if (fnode == null)
					return;
				AddIssue(fnode, ctx.TranslateString("Explicit argument name specifications are redundant if they are in the same order with the parameter list"), ctx.TranslateString("Remove redundant argument name"),
						script =>
				{
					foreach (NamedArgumentExpression node in nodes) {
						PrimitiveExpression newExpression = new PrimitiveExpression(node.Expression.Clone());
						script.Replace(node, newExpression);
					}
				});
			}

			public override void VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression)
			{
				base.VisitNamedArgumentExpression(namedArgumentExpression);
				
				IMember member;
				List<NamedArgumentExpression> redundantNodes;
				var parent = namedArgumentExpression.Parent;
				List<IParameter> parameters;
				List<string> parameterNames = new List<string>();
				
				if (parent is IndexerExpression) {
					var resolvedResult = ctx.Resolve(parent as IndexerExpression);
					if (resolvedResult.IsError)
						return;
					member = (resolvedResult as CSharpInvocationResolveResult).Member;
					if (!(member is IProperty)) {
						return;
					}
					parameters = (member as IProperty).Parameters;
					foreach (var parameter in parameters) {
						parameterNames.Add(parameter.Name);
					}
				}
				else if (parent is InvocationExpression) {
					var resolvedResult = ctx.Resolve(parent as InvocationExpression);
					if (resolvedResult.IsError)
						return;
					member = (resolvedResult as CSharpInvocationResolveResult).Member;
					if (!(member is IMethod)) {
						return;
					}
					parameters = (member as IProperty).Parameters;
					foreach (var parameter in parameters) {
						parameterNames.Add(parameter.Name);
					}
				}
				else if (parent is ObjectCreateExpression)
				{
					var resolvedResult = ctx.Resolve(parent as ObjectCreateExpression);
					if (resolvedResult.IsError)
						return;
					member = (resolvedResult as CSharpInvocationResolveResult).Member;
					if (!(member is IMethod)) {
						return;
					}
					member = (resolvedResult as CSharpInvocationResolveResult).Member;
					parameters = (member as IProperty).Parameters;
					foreach (var parameter in parameters) {
						parameterNames.Add(parameter.Name);
					}
				}
				
				
				if (!IsRedundant(parent, parameterNames))
					return;
				redundantNodes = CollectNodes(parent, namedArgumentExpression);
				if (!redundantNodes.Any())
					return;
				AddIssue(redundantNodes);
			}
		}
	}
}
}