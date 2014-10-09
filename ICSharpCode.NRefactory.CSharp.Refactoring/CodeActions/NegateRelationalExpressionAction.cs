//
// NegateRelationalExpressionAction.cs
//
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
//      Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
// Copyright (c) 2012 Mansheng Yang <lightyang0@gmail.com>
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
	[NRefactoryCodeRefactoringProvider(Description = "Negate a relational expression")]
	[ExportCodeRefactoringProvider("Negate a relational expression", LanguageNames.CSharp)]
	public class NegateRelationalExpressionAction : CodeRefactoringProvider
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
//		public static bool GetLogicalExpression (SemanticModel context, out Expression expr, out AstNode token)
//		{
//			expr = null;
//			token = null;
//			var bOp = context.GetNode<BinaryOperatorExpression>();
//			if (bOp != null && bOp.OperatorToken.Contains(context.Location) && CSharpUtil.IsRelationalOperator (bOp.Operator)) {
//				expr = bOp;
//				token = bOp.OperatorToken;
//				return true;
//			}
//
//			var uOp = context.GetNode<UnaryOperatorExpression>();
//			if (uOp != null && uOp.OperatorToken.Contains(context.Location) && uOp.Operator == UnaryOperatorType.Not) {
//				expr = uOp;
//				token = uOp.OperatorToken;
//				return true;
//			}
//			return false;
//		}
//
//		public override System.Collections.Generic.IEnumerable<CodeAction> GetActions(SemanticModel context)
//		{
//			Expression expr = null;
//			AstNode token;
//			if (!GetLogicalExpression (context, out expr, out token))
//				yield break;
//			yield return new CodeAction (
//				string.Format (context.TranslateString ("Negate '{0}'"), expr),
//				script => {
//					script.Replace (expr, CSharpUtil.InvertCondition(expr));
//				}, 
//				token
//			);
//		}
	}
}
