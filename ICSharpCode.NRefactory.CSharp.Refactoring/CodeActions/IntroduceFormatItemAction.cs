// 
// IntroduceFormatItem.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	/// <summary>
	/// Introduce format item. Works on strings that contain selections.
	/// "this is <some> string" => string.Format ("this is {0} string", <some>)
	/// </summary>
	[NRefactoryCodeRefactoringProvider(Description = "Creates a string.format call with the selection as parameter")]
	[ExportCodeRefactoringProvider("Introduce format item", LanguageNames.CSharp)]
	public class IntroduceFormatItemAction : CodeRefactoringProvider
	{
		public override async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			return null;
		}
//		readonly static MemberReferenceExpression PrototypeFormatReference = new PrimitiveType ("string").Member("Format");
//		
//		public async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
//		{
//			if (!context.IsSomethingSelected) {
//				yield break;
//			}
//			var pexpr = context.GetNode<PrimitiveExpression>();
//			if (pexpr == null || !(pexpr.Value is string)) {
//				yield break;
//			}
//			if (pexpr.LiteralValue.StartsWith("@", StringComparison.Ordinal)) {
//				if (!(pexpr.StartLocation < new TextLocation(context.Location.Line, context.Location.Column - 1) && new TextLocation(context.Location.Line, context.Location.Column + 1) < pexpr.EndLocation)) {
//					yield break;
//				}
//			} else {
//				if (!(pexpr.StartLocation < context.Location && context.Location < pexpr.EndLocation)) {
//					yield break;
//				}
//			}
//
//			yield return new CodeAction (context.TranslateString("Introduce format item"), script => {
//				var invocation = context.GetNode<InvocationExpression>();
//				if (invocation != null && invocation.Target.IsMatch(PrototypeFormatReference)) {
//					AddFormatCallToInvocation(context, script, pexpr, invocation);
//					return;
//				}
//			
//				var arg = CreateFormatArgument(context);
//				var newInvocation = new InvocationExpression (PrototypeFormatReference.Clone()) {
//					Arguments = { CreateFormatString(context, pexpr, 0), arg }
//				};
//			
//				script.Replace(pexpr, newInvocation);
//				script.Select(arg);
//			}, pexpr);
//
//		}
//		
//		void AddFormatCallToInvocation (SemanticModel context, Script script, PrimitiveExpression pExpr, InvocationExpression invocation)
//		{
//			var newInvocation = (InvocationExpression)invocation.Clone ();
//			
//			newInvocation.Arguments.First ().ReplaceWith (CreateFormatString (context, pExpr, newInvocation.Arguments.Count () - 1));
//			newInvocation.Arguments.Add (CreateFormatArgument (context));
//			
//			script.Replace (invocation, newInvocation);
//		}
//		
//		static PrimitiveExpression CreateFormatArgument (SemanticModel context)
//		{
//			return new PrimitiveExpression (context.SelectedText);
//		}
//		
//		static PrimitiveExpression CreateFormatString(SemanticModel context, PrimitiveExpression pExpr, int argumentNumber)
//		{
//			var start = context.GetOffset(pExpr.StartLocation);
//			var end = context.GetOffset(pExpr.EndLocation);
//			var sStart = context.GetOffset(context.SelectionStart);
//			var sEnd = context.GetOffset(context.SelectionEnd);
//			return new PrimitiveExpression("", context.GetText(start, sStart - start) + "{" + argumentNumber + "}" + context.GetText(sEnd, end - sEnd));
//		}
	}
}
