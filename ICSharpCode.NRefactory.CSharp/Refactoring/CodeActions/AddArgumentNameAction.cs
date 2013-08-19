// 
// AddArgumentNameAction.cs
//  
// Author:
//       Ji Kun <jikun.nus@gmail.com>
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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.Refactoring;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.CSharp.Resolver;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	///  Add name for argument
	/// </summary>
	using System;
	using System.Collections.Generic;
	
	[ContextAction("Add name for argument", Description = "Add name for argument including method, indexer invocation and Attibute Usage")]
	public class AddArgumentNameAction : SpecializedCodeAction<Expression>
	{
		private List<Expression> CollectNodes(AstNode parant, AstNode node)
		{
			List<Expression> returned = new List<Expression>();

			var children = parant.GetChildrenByRole(Roles.Argument);
			for (int i = 0; i< children.Count(); i++) {
				if (children.ElementAt(i).Equals(node)) {
					for (int j = i; j< children.Count(); j++) {
						if (children.ElementAt(j) is Expression && children.ElementAt(j).Role == Roles.Argument && !(children.ElementAt(j) is NamedArgumentExpression)) {
							returned.Add(children.ElementAt(j));
						} else {
							break;
						}
					}
				}
			}
			return returned;
		}

		protected override CodeAction GetAction(RefactoringContext context, Expression expression)
		{	
			if (expression == null)
				return null;
			if (expression.Role != Roles.Argument || expression is NamedArgumentExpression)
				return null;
			var parant = expression.Parent;
			if (!(parant is CSharp.Attribute) && !(parant is IndexerExpression) && !(parant is InvocationExpression))
				return null;
			if (parant is CSharp.Attribute) {
				var resolvedResult = context.Resolve(parant as CSharp.Attribute);
				if (resolvedResult.IsError)
					return null;
				var arguments = (parant as CSharp.Attribute).Arguments;
				IMember member = (resolvedResult as CSharpInvocationResolveResult).Member;

				int index = 0;
				int temp = 0; 
				List<Expression> nodes = new List<Expression>();
				foreach (var argument in arguments) {
					if (argument.Equals(expression)) {
						nodes = CollectNodes(parant, expression);
						break;
					}
					temp++;
				}
				index = temp;
				if (!nodes.Any())
					return null;
				if (!(member is IMethod)) {
					return null;
				}
				var parameterMap = (resolvedResult as CSharpInvocationResolveResult).GetArgumentToParameterMap();
				var parameters = (member as IMethod).Parameters;
				var name = parameters.ElementAt(parameterMap [index]).Name;
				string des = "Add argument name:" + name;
				return new CodeAction(des, script => {
					for (int i = 0; i< nodes.Count; i++) {
						int p = index + i;
						name = parameters.ElementAt(parameterMap [p]).Name;
						var namedArgument = new NamedArgumentExpression(name, arguments.ElementAt(p).Clone());
						script.Replace(arguments.ElementAt(p), namedArgument);
					}}, 
									expression
				);
			} else if (parant is IndexerExpression) {
				var resolvedResult = context.Resolve(parant as IndexerExpression);
				if (resolvedResult.IsError)
					return null;
				var arguments = (parant as IndexerExpression).Arguments;
				IMember member = (resolvedResult as CSharpInvocationResolveResult).Member;
				
				int index = 0;
				int temp = 0; 
				List<Expression> nodes = new List<Expression>();
				foreach (var argument in arguments) {
					if (argument.Equals(expression)) {
						nodes = CollectNodes(parant, expression);
						break;
					}
					temp++;
				}
				index = temp;
				if (!nodes.Any())
					return null;
				if (!(member is IProperty)) {
					return null;
				}
				var parameterMap = (resolvedResult as CSharpInvocationResolveResult).GetArgumentToParameterMap();
				var parameters = (member as IProperty).Parameters;
				var name = parameters.ElementAt(parameterMap [index]).Name;
				string des = "Add argument name:" + name;
				return new CodeAction(des, script => {
					for (int i = 0; i< nodes.Count; i++) {
						int p = index + i;
						name = parameters.ElementAt(parameterMap [p]).Name;
						var namedArgument = new NamedArgumentExpression(name, arguments.ElementAt(p).Clone());
						script.Replace(arguments.ElementAt(p), namedArgument);
					}}, 
				expression
				);
			} else if (parant is InvocationExpression) {
				var resolvedResult = context.Resolve(parant as InvocationExpression);
				if (resolvedResult.IsError)
					return null;
				var arguments = (parant as InvocationExpression).Arguments;
				IMember member = (resolvedResult as CSharpInvocationResolveResult).Member;
				
				int index = 0;
				int temp = 0; 
				List<Expression> nodes = new List<Expression>();
				foreach (var argument in arguments) {
					if (argument.Equals(expression)) {
						nodes = CollectNodes(parant, expression);
						break;
					}
					temp++;
				}
				index = temp;
				if (!nodes.Any())
					return null;
			
				if (!(member is IMethod)) {
					return null;
				}
				var parameterMap = (resolvedResult as CSharpInvocationResolveResult).GetArgumentToParameterMap();
				var parameters = (member as IMethod).Parameters;
				var name = parameters.ElementAt(parameterMap [index]).Name;
				string des = "Add argument name:" + name;
				return new CodeAction(des, script => {
					for (int i = 0; i< nodes.Count; i++) {
						int p = index + i;
						name = parameters.ElementAt(parameterMap [p]).Name;
						var namedArgument = new NamedArgumentExpression(name, arguments.ElementAt(p).Clone());
						script.Replace(arguments.ElementAt(p), namedArgument);
					}}, 
				expression
				);
			}
			return null;
		}
	}
}
