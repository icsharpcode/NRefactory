//
// InvertIfAndSimplify.cs
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
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Inverts if and simplify branching")]
	[ExportCodeRefactoringProvider("Invert If and Simplify", LanguageNames.CSharp)]
	public class InvertIfAndSimplify : CodeRefactoringProvider
	{
		// if (condition) {CodeBlock();}else { return|break|continue;} 
		// will be reduced to:
		//if (!condition) return|break|continue;
		//CodeBlock();

		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			var ifStatement = GetIfElseStatement(root, span);
			if (ifStatement == null)
				return;
			context.RegisterRefactoring(
				CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Info, 
					"Simplify if in loops", 
					t2 => {
						var newRoot = root.ReplaceNode((SyntaxNode)ifStatement, GenerateNewScript(ifStatement));
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				) 
			);
		}

		static StatementSyntax GenerateNewTrueStatement(StatementSyntax falseStatement)
		{
			var blockStatement = falseStatement as BlockSyntax;
			if (blockStatement != null) {
				if (blockStatement.Statements.Count == 1) {
					var stmt = blockStatement.Statements.First();
					if (stmt.GetLeadingTrivia().All(triva => triva.IsKind(SyntaxKind.WhitespaceTrivia)))
						return stmt;
				}
			}
			return falseStatement;
		}

		static IEnumerable<SyntaxNode> GenerateNewScript(IfStatementSyntax ifStatement)
		{
			yield return SyntaxFactory.IfStatement(
				CSharpUtil.InvertCondition(ifStatement.Condition),
				GenerateNewTrueStatement(ifStatement.Else.Statement)
			).WithAdditionalAnnotations(Formatter.Annotation);

			var body = ifStatement.Statement as BlockSyntax;
			if (body != null) {
				foreach (var stmt in body.Statements) {
					yield return stmt.WithAdditionalAnnotations(Formatter.Annotation);
				}
			} else {
				yield return ifStatement.Statement.WithAdditionalAnnotations(Formatter.Annotation);
			}
		}

		static IfStatementSyntax GetIfElseStatement(SyntaxNode root, TextSpan span)
		{
			var result = root.FindNode(span) as IfStatementSyntax;
			if (result == null || !result.IfKeyword.Span.Contains(span) || result.Else == null)
				return null;

			var falseStatement = result.Else.Statement;
			var isQuitingStatement = falseStatement;
			var blockStatement = falseStatement as BlockSyntax;
			if (blockStatement != null) {
				isQuitingStatement = blockStatement.Statements.FirstOrDefault() ?? blockStatement;
			}
			if (isQuitingStatement.IsKind(SyntaxKind.ReturnStatement) || isQuitingStatement.IsKind(SyntaxKind.ContinueStatement) || isQuitingStatement.IsKind(SyntaxKind.BreakStatement))
				return result;
			return null;
		}
	}
}