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
using System.Linq;
using System.Text;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	///  Add name for argument
	/// </summary>
	using System;
	using System.Collections.Generic;
	
	[ContextAction("Add name for argument", Description = "Add name for argument in invocation expresson")]
	public class AddArgumentNameAction : SpecializedCodeAction<InvocationExpression>
	{
		protected override CodeAction GetAction (RefactoringContext context, InvocationExpression invocationExpression)
		{	
			if (invocationExpression.Arguments == null)
				return null;

			var arguments = invocationExpression.Arguments;

			List<int> argumentWithoutNameIndex = new List<int>();

			for (int i = 0; i < arguments.Count; i++) {
				var argument = arguments.ElementAt(i);
				if (!(argument is NamedArgumentExpression)) {
					argumentWithoutNameIndex.Add(i);
				}
			}

			if (!argumentWithoutNameIndex.Any()) {
				return null;
			}

			var resolvedResult = context.Resolve(invocationExpression);
			var member = (resolvedResult as InvocationResolveResult).Member;
	
			if (!(member is IMethod))
				return null;

			var parameters = (member as IMethod).Parameters;
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i< argumentWithoutNameIndex.Count; i++) {
				int index = argumentWithoutNameIndex.ElementAt(i);
				var name = parameters.ElementAt(index).Name;
				string str = string.Format("Add argument name: {0}. ", name);
				sb.Append(str);
			}

			return new CodeAction(context.TranslateString(sb.ToString()), script => {
				for (int i = 0; i< argumentWithoutNameIndex.Count; i++) {
					int index = argumentWithoutNameIndex.ElementAt(i);
					var name = parameters.ElementAt(index).Name;
					var namedArgument = new NamedArgumentExpression(name, arguments.ElementAt(index).Clone());
					script.Replace(arguments.ElementAt(index), namedArgument);
				}}, 
				invocationExpression
			);
		}
	}
}
