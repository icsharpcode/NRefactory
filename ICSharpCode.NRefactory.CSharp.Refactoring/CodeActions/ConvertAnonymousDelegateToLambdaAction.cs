//
// ConvertAnonymousDelegateToExpression.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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
	[NRefactoryCodeRefactoringProvider(Description = "Converts an anonymous delegate into a lambda")]
	[ExportCodeRefactoringProvider("Convert anonymous delegate to lambda", LanguageNames.CSharp)]
	public class ConvertAnonymousDelegateToLambdaAction : SpecializedCodeAction<AnonymousMethodExpressionSyntax>
	{
		protected override IEnumerable<CodeAction> GetActions(Document document, SemanticModel semanticModel, SyntaxNode root, TextSpan span, AnonymousMethodExpressionSyntax node, CancellationToken cancellationToken)
		{
			yield break;
		}

//		protected override CodeAction GetAction(SemanticModel context, AnonymousMethodExpression node)
//		{
//			if (context.Location < node.DelegateToken.StartLocation || context.Location >= node.Body.StartLocation)
//				return null;
//
//			Expression convertExpression = null;
//
//			var stmt = node.Body.Statements.FirstOrDefault();
//			if (stmt == null)
//				return null;
//			if (stmt.GetNextSibling(s => s.Role == BlockStatement.StatementRole) == null) {
//				var exprStmt = stmt as ExpressionStatement;
//				if (exprStmt != null) {
//					convertExpression = exprStmt.Expression;
//				}
//			}
//
//			IType guessedType = null;
//			if (!node.HasParameterList) {
//				guessedType = TypeGuessing.GuessType(context, node);
//				if (guessedType.Kind != TypeKind.Delegate)
//					return null;
//			}
//
//			return new CodeAction(context.TranslateString("Convert to lambda"), 
//				script => {
//					var parent = node.Parent;
//					while (!(parent is Statement))
//						parent = parent.Parent;
//					bool explicitLambda = parent is VariableDeclarationStatement && ((VariableDeclarationStatement)parent).Type.IsVar();
//					var lambda = new LambdaExpression ();
//
//					if (convertExpression != null) {
//						lambda.Body = convertExpression.Clone();
//					} else {
//						lambda.Body = node.Body.Clone();
//					}
//					if (node.HasParameterList) {
//						foreach (var parameter in node.Parameters) {
//							if (explicitLambda) {
//								lambda.Parameters.Add(new ParameterDeclaration { Type = parameter.Type.Clone(), Name = parameter.Name });
//							} else {
//								lambda.Parameters.Add(new ParameterDeclaration { Name = parameter.Name });
//							}
//						}
//					} else {
//						var method = guessedType.GetDelegateInvokeMethod ();
//						foreach (var parameter in method.Parameters) {
//							lambda.Parameters.Add(new ParameterDeclaration { Name = parameter.Name });
//						}
//					}
//					script.Replace(node, lambda);
//				}, 
//				node);
//		}

	}
}

