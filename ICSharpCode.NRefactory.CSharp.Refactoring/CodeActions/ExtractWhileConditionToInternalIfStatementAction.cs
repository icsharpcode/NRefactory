//
// ExtractWhileConditionToInternalIfStatementAction.cs
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
	[NRefactoryCodeRefactoringProvider(Description = "Extracts a field from a local variable declaration")]
	[ExportCodeRefactoringProvider("Extract field", LanguageNames.CSharp)]
	public class ExtractWhileConditionToInternalIfStatementAction : SpecializedCodeAction<WhileStatementSyntax>
	{
		protected override IEnumerable<CodeAction> GetActions(SemanticModel semanticModel, SyntaxNode root, TextSpan span, WhileStatementSyntax node, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
//		protected override CodeAction GetAction(SemanticModel context, WhileStatement node)
//		{
//			if (!node.WhileToken.Contains(context.Location))
//				return null;
//
//			return new CodeAction(
//				context.TranslateString("Extract condition to internal 'if' statement"),
//				script => {
//					script.Replace(node.Condition, new PrimitiveExpression(true));
//					var ifStmt = new IfElseStatement(
//						CSharpUtil.InvertCondition(node.Condition),
//						new BreakStatement()
//					);
//
//					var block = node.EmbeddedStatement as BlockStatement;
//					if (block != null) {
//						script.InsertAfter(block.LBraceToken, ifStmt);
//					} else {
//						var blockStatement = new BlockStatement {
//							ifStmt
//						};
//						if (!(node.EmbeddedStatement is EmptyStatement))
//							blockStatement.Statements.Add(node.EmbeddedStatement.Clone());
//						script.Replace(node.EmbeddedStatement, blockStatement);
//					}
//				},
//				node
//			);
//		}
	}
}
