// 
// InvertIf.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[NRefactoryCodeRefactoringProvider(Description = "Inverts an 'if ... else' expression")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Invert if")]
	public class InvertIfCodeRefactoringProvider : CodeRefactoringProvider
	{
		static readonly string invertIfFixMessage = GettextCatalog.GetString ("Invert 'if'");

		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
				return;
			var span = context.Span;
			if (!span.IsEmpty)
				return;
			var cancellationToken = context.CancellationToken;
			if (cancellationToken.IsCancellationRequested)
				return;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			var complexIfElseStatement = GetIfElseStatement(root, span);
			if (complexIfElseStatement != null) {
				context.RegisterRefactoring (
					CodeActionFactory.Create (
						span, 
						DiagnosticSeverity.Info, 
						invertIfFixMessage, 
						t2 => {
							var newRoot = root.ReplaceNode((SyntaxNode)complexIfElseStatement, GenerateNewScript(complexIfElseStatement));
							return Task.FromResult(document.WithSyntaxRoot(newRoot));
						}
					) 
				);
				return;
			}

			var simpleIfElseStatement = GetIfElseStatementSimple(root, span);
			if (simpleIfElseStatement != null) {
				context.RegisterRefactoring (
					CodeActionFactory.Create (
						span, 
						DiagnosticSeverity.Info, 
						invertIfFixMessage, 
						t2 => {
							var newRoot = root.ReplaceNode ((SyntaxNode)
								simpleIfElseStatement,
								              simpleIfElseStatement
								.WithCondition (CSharpUtil.InvertCondition (simpleIfElseStatement.Condition))
								.WithStatement (simpleIfElseStatement.Else.Statement)
								.WithElse (simpleIfElseStatement.Else.WithStatement (simpleIfElseStatement.Statement))
								.WithAdditionalAnnotations (Formatter.Annotation)
							              );
							return Task.FromResult (document.WithSyntaxRoot (newRoot));
						}
					) 
				);
				return;
			}

			var ifStatement = GetIfStatement(root, span);
			if (ifStatement != null) {
				context.RegisterRefactoring (
					CodeActionFactory.Create (
						ifStatement.Span,
						DiagnosticSeverity.Info,
						invertIfFixMessage,
						t2 => {
							var mergedIfStatement = SyntaxFactory.IfStatement (CSharpUtil.InvertCondition (ifStatement.Condition), SyntaxFactory.ReturnStatement ())
								.WithAdditionalAnnotations (Formatter.Annotation);
							var newRoot = root.ReplaceNode ((SyntaxNode)ifStatement, new SyntaxNode[] { mergedIfStatement }.Concat (GetStatements (ifStatement.Statement)));
							return Task.FromResult (document.WithSyntaxRoot (newRoot));
						}
					)
				);
			}


			var ifStatementInLoop = GetIfElseStatementInLoop(root, span);
			if (ifStatementInLoop != null) {
				context.RegisterRefactoring (
					CodeActionFactory.Create (
						ifStatementInLoop.Span,
						DiagnosticSeverity.Info,
						invertIfFixMessage,
						t2 => {
							var mergedIfStatement = SyntaxFactory.IfStatement (CSharpUtil.InvertCondition (ifStatementInLoop.Condition), SyntaxFactory.ContinueStatement ())
								.WithAdditionalAnnotations (Formatter.Annotation);
							var newRoot = root.ReplaceNode ((SyntaxNode)ifStatementInLoop, new SyntaxNode[] { mergedIfStatement }.Concat (GetStatements (ifStatementInLoop.Statement)));
							return Task.FromResult (document.WithSyntaxRoot (newRoot));
						}
					)
				);
				return;
			}
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


		static IfStatementSyntax GetIfElseStatementSimple(SyntaxNode root, TextSpan span)
		{
			var result = root.FindNode(span) as IfStatementSyntax;
			if (result == null || !result.IfKeyword.Span.Contains(span) || result.Else == null)
				return null;
			return result;
		}


		static IfStatementSyntax GetIfStatement(SyntaxNode root, TextSpan span)
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

			var method = parentBlock.Parent as MethodDeclarationSyntax;
			if (method == null || method.ReturnType.ToString() != "void")
				return null;

			int i = parentBlock.Statements.IndexOf(result);
			if (i + 1 >= parentBlock.Statements.Count)
				return result;
			return null;
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

		static IfStatementSyntax GetIfElseStatementInLoop(SyntaxNode root, TextSpan span)
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