//
// ConvertEqualsToEqualityOperatorAction.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
	/// Convert do...while to while. For instance, { do x++; while (Foo(x)); } becomes { while(Foo(x)) x++; }.
	/// Note that this action will often change the semantics of the code.
	/// </summary>
	[NRefactoryCodeRefactoringProvider(Description = "Converts 'Equals' call to '=='")]
	[ExportCodeRefactoringProvider("Convert 'Equals' to '=='", LanguageNames.CSharp)]
	public class ConvertEqualsToEqualityOperatorAction : ICodeRefactoringProvider
	{
		public async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
		{
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			return null;
		}
//		public async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
//		{
//			var node = context.GetNode<InvocationExpression>();
//			if (node == null)
//				yield break;
//			if ((node.Target is IdentifierExpression) && !node.Target.IsInside(context.Location))
//				yield break;
//			var memberRefExpr = node.Target as MemberReferenceExpression;
//			if ((memberRefExpr != null) && !memberRefExpr.MemberNameToken.IsInside(context.Location))
//				yield break;
//			var rr = context.Resolve(node) as CSharpInvocationResolveResult;
//			if (rr == null || rr.IsError || rr.Member.Name != "Equals" || !rr.Member.DeclaringType.IsKnownType(KnownTypeCode.Object))
//				yield break;
//			Expression expr = node;
//			bool useEquality = true;
//			var uOp = node.Parent as UnaryOperatorExpression;
//			if (uOp != null && uOp.Operator == UnaryOperatorType.Not) {
//				expr = uOp;
//				useEquality = false;
//			}
//			if (node.Arguments.Count != 2 && (memberRefExpr == null || node.Arguments.Count != 1))
//				yield break;
//			yield return new CodeAction(
//				useEquality ? context.TranslateString("Use '=='") : context.TranslateString("Use '!='"),
//				script => {
//					script.Replace(
//						expr,
//						new BinaryOperatorExpression(
//							node.Arguments.Count == 1 ? memberRefExpr.Target.Clone() : node.Arguments.First().Clone(),
//							useEquality ? BinaryOperatorType.Equality :  BinaryOperatorType.InEquality,
//							node.Arguments.Last().Clone()
//						)
//					);
//				}, 
//				node.Target
//			);
//		}
	}
}

