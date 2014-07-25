// 
// ConvertLambdaBodyStatementToExpressionAction.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
// 
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
	[NRefactoryCodeRefactoringProvider(Description = "Converts statement of lambda body to expression")]
	[ExportCodeRefactoringProvider("Converts statement of lambda body to expression", LanguageNames.CSharp)]
	public class ConvertLambdaBodyStatementToExpressionAction : SpecializedCodeAction<SimpleLambdaExpressionSyntax>
	{
//		internal static bool TryGetConvertableExpression(AstNode body, out BlockStatement blockStatement, out Expression expr)
//		{
//			expr = null;
//			blockStatement = body as BlockStatement;
//			if (blockStatement == null || blockStatement.Statements.Count > 1)
//				return false;
//			var returnStatement = blockStatement.Statements.FirstOrNullObject() as ReturnStatement;
//			if (returnStatement != null) {
//				expr = returnStatement.Expression;
//			} else {
//				var exprStatement = blockStatement.Statements.FirstOrNullObject() as ExpressionStatement;
//				if (exprStatement == null)
//					return false;
//				expr = exprStatement.Expression;
//			}
//			return true;
//		}
//
		protected override IEnumerable<CodeAction> GetActions(Document document, SemanticModel semanticModel, SyntaxNode root, TextSpan span, SimpleLambdaExpressionSyntax node, CancellationToken cancellationToken)
		{
			yield break;
		}
//		internal static CodeAction CreateAction (BaseSemanticModel context, AstNode node, BlockStatement blockStatement, Expression expr)
//		{
//			return new CodeAction (
//				context.TranslateString ("Convert to lambda expression"),
//				script => script.Replace (blockStatement, expr.Clone ()), 
//				node
//			);
//		}
//
//		protected override CodeAction GetAction (SemanticModel context, LambdaExpression node)
//		{
//			if (!node.ArrowToken.Contains (context.Location))
//				return null;
//
//			BlockStatement blockStatement;
//			Expression expr;
//			if (!TryGetConvertableExpression(node.Body, out blockStatement, out expr))
//				return null;
//
//			
//			return CreateAction (context, node.ArrowToken, blockStatement, expr);
//		}
	}
}
