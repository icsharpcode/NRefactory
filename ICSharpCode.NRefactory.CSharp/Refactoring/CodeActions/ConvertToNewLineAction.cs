// 
// ConvertToNewLineActioncs
//  
// Author:
//       Ji Kun <jikun.nus0@gmail.com>
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

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Replace literals \n, \r or \r\n with System.Environment.NewLine property
	/// </summary>
	using System;
	using System.Collections.Generic;

	[ContextAction("Use System.Environment.NewLine", Description = "Replace \n, \r or \r\n with System.Environment.NewLine")]
	public class ConvertToNewLineAction: ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var expr = GetNewLineString(context);
			if (expr == null) {
				yield break;
			}
			yield return new CodeAction(context.TranslateString("Use System.Environment.NewLine"), script => {
				script.Replace(expr, new MemberReferenceExpression(new TypeReferenceExpression(new PrimitiveType("Environment")), "NewLine"));
			}, expr);
		}
		
		static PrimitiveExpression GetNewLineString(RefactoringContext context)
		{
			var node = context.GetNode<PrimitiveExpression>();
			if (node == null || !(node.Value is string)) {
				return null;
			} else {
				if (node.Value.ToString() == "\n" || node.Value.ToString() == "\r" || node.Value.ToString() == "\r\n") {
					return node;
				} else 
					return null;
			}
		}
	}
}