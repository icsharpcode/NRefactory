//
// SimplifyIfInLoopsFlowAction.cs
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
	[ExportCodeRefactoringProvider("Simplify if flow in loops", LanguageNames.CSharp)]
	public class SimplifyIfInLoopsFlowAction : CodeRefactoringProvider
	{
		public override async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			var ifStatement = GetIfElseStatement(root, span);
			if (ifStatement == null)
				return Enumerable.Empty<CodeAction>();

			return new []  { 
				CodeActionFactory.Create(
					ifStatement.Span,
					DiagnosticSeverity.Info,
					"Simplify if in loops",
					t2 => {
						var mergedIfStatement = SyntaxFactory.IfStatement(CSharpUtil.InvertCondition(ifStatement.Condition), SyntaxFactory.ContinueStatement())
							.WithAdditionalAnnotations(Formatter.Annotation);
						var newRoot = root.ReplaceNode(ifStatement, new SyntaxNode[] { mergedIfStatement }.Concat(GetStatements(ifStatement.Statement)));
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
			};
		}

		internal static IEnumerable<SyntaxNode> GetStatements(StatementSyntax statement)
		{
			var blockSyntax = statement as BlockSyntax;
			if (blockSyntax != null) {
				foreach (var stmt in blockSyntax.Statements)
					yield return stmt.WithAdditionalAnnotations(Formatter.Annotation);
			} else {
				yield return statement.WithAdditionalAnnotations(Formatter.Annotation);
			}
		}

		static IfStatementSyntax GetIfElseStatement(SyntaxNode root, TextSpan span)
		{
			var result = root.FindNode(span) as IfStatementSyntax;
			if (result == null)
				return null;
			if (!result.IfKeyword.Span.Contains(span) ||
				result.Statement == null ||
				result.Else != null)
				return null;

			var parentBlock = result.Parent as BlockSyntax;
			if (parentBlock == null)
				return null;

			if (!(parentBlock.Parent is WhileStatementSyntax || 
				parentBlock.Parent is ForEachStatementSyntax || 
				parentBlock.Parent is ForStatementSyntax))
				return null;

			int i = parentBlock.Statements.IndexOf(result);
			if (i + 1 >= parentBlock.Statements.Count)
				return result;
			return null;
		}
	}
}