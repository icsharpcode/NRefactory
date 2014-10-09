//
// SimplifyIfFlowAction.cs
//
// Author:
//      Ciprian Khlud <ciprian.mustiata@yahoo.com>
//
// Copyright (c) 2013 Ciprian Khlud <ciprian.mustiata@yahoo.com>
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
	[NRefactoryCodeRefactoringProvider(Description = "Inverts if and reduces branching")]
	[ExportCodeRefactoringProvider("Simplify if flow", LanguageNames.CSharp)]
	public class SimplifyIfFlowAction : CodeRefactoringProvider
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
//		readonly InsertParenthesesVisitor _insertParenthesesVisitor = new InsertParenthesesVisitor();
//
//		public async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
//		{
//			var ifStatement = GetIfElseStatement(context);
//			if (ifStatement == null)
//				yield break;
//			yield return new CodeAction(context.TranslateString("Invert if"), script =>
//			{
//				GenerateNewScript(script, ifStatement);
//			}, ifStatement);
//		}
//
//		internal static void InsertBody(Script script, IfElseStatement ifStatement)
//		{
//			var ifBody = ifStatement.TrueStatement.Clone();
//
//			if (ifBody is BlockStatement) {
//				AstNode last = ifStatement;
//				foreach (var stmt in ((BlockStatement)ifBody).Children) {
//					if (stmt.Role == Roles.LBrace || stmt.Role == Roles.RBrace || stmt.Role == Roles.NewLine)
//						continue;
//					script.InsertAfter(last, stmt);
//					last = stmt;
//				}
//			} else {
//				script.InsertAfter(ifStatement, ifBody);
//			}
//			script.FormatText(ifStatement.Parent);
//		}
//		
//		void GenerateNewScript(Script script, IfElseStatement ifStatement)
//		{
//			var mergedIfStatement = new IfElseStatement {
//				Condition = CSharpUtil.InvertCondition(ifStatement.Condition),
//				TrueStatement = new ReturnStatement()
//			};
//			mergedIfStatement.Condition.AcceptVisitor(_insertParenthesesVisitor);
//			
//			script.Replace(ifStatement, mergedIfStatement);
//
//			InsertBody(script, ifStatement);
//		}
//		
//		static IfElseStatement GetIfElseStatement(SemanticModel context)
//		{
//			var result = context.GetNode<IfElseStatement>();
//			if (result == null)
//				return null;
//			if (!(result.IfToken.Contains(context.Location)
//				&& !result.TrueStatement.IsNull
//				&& result.FalseStatement.IsNull))
//				return null;
//			if (!(result.Parent is BlockStatement) || !(result.Parent.Parent is MethodDeclaration))
//				return null;
//			var parentMethod = (MethodDeclaration)result.Parent.Parent;
//			if (parentMethod.ReturnType.ToString() != "void")
//				return null;
//			var nextSibling = result.GetNextSibling(n => n is Statement);
//			if (nextSibling == null)
//				return result;
//			nextSibling = nextSibling.GetNextSibling (n => n is Statement);
//			if (nextSibling != null)
//				return null;
//			return result;
//		}
	}
}