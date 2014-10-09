//
// FlipEqualsQualifierAndArgumentAction.cs
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
	[NRefactoryCodeRefactoringProvider(Description = "Swap 'Equals' target and argument")]
	[ExportCodeRefactoringProvider("Swap 'Equals' target and argument", LanguageNames.CSharp)]
	public class FlipEqualsTargetAndArgumentAction : CodeRefactoringProvider
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
//		Expression GetInnerMostExpression(Expression target)
//		{
//			while (target is ParenthesizedExpression)
//				target = ((ParenthesizedExpression)target).Expression;
//			return target;
//		}
//
//		public async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
//		{
//			var invocation = context.GetNode<InvocationExpression>();
//			if (invocation == null)
//				yield break;
//			if (invocation.Arguments.Count != 1 || invocation.Arguments.First() is NullReferenceExpression)
//				yield break;
//			var target = invocation.Target as MemberReferenceExpression;
//
//			if (target == null || target.MemberNameToken.StartLocation > context.Location || invocation.LParToken.StartLocation < context.Location)
//				yield break;
//
//			var rr = context.Resolve(invocation) as InvocationResolveResult;
//			if (rr == null || rr.Member.Name != "Equals" || rr.Member.IsStatic || !rr.Member.ReturnType.IsKnownType(KnownTypeCode.Boolean))
//				yield break;
//
//			yield return new CodeAction(
//				context.TranslateString("Flip 'Equals' target and argument"),
//				script => {
//					script.Replace(
//						invocation,
//						new InvocationExpression(
//							new MemberReferenceExpression(
//								AddParensIfRequired(invocation.Arguments.First ().Clone()),
//								"Equals"
//							),
//							GetInnerMostExpression (target.Target).Clone()
//						)
//				    );
//				},
//				invocation
//			);
//		}
//
//		Expression AddParensIfRequired(Expression expression)
//		{
//			if ((expression is BinaryOperatorExpression) ||
//			    (expression is UnaryOperatorExpression) ||
//			    (expression is CastExpression) ||
//			    (expression is AssignmentExpression) ||
//				(expression is AsExpression) ||
//			    (expression is IsExpression) ||
//			    (expression is LambdaExpression) ||
//			    (expression is ConditionalExpression)) {
//				return new ParenthesizedExpression(expression);
//			}
//
//			return expression;
//		}
	}
}

