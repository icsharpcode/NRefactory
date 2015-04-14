// 
// RemoveBraces.cs
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

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Remove braces")]
	public class RemoveBracesCodeRefactoringProvider : CodeRefactoringProvider
	{
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
			var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			if (model.IsFromGeneratedCode())
				return;
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
			var token = root.FindToken(span.Start);

			string keyword;
			StatementSyntax embeddedStatement;
			BlockSyntax block = null;
			if (IsSpecialNode(token, out keyword, out embeddedStatement)) {
				block = embeddedStatement as BlockSyntax;
				if (block == null || block.Statements.Count != 1 || block.Statements.First() is LabeledStatementSyntax || block.Statements.First() is LocalDeclarationStatementSyntax)
					return;
			} 

			if (block == null)
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					token.Span,
					DiagnosticSeverity.Info,
					string.Format(GettextCatalog.GetString ("Remove braces from '{0}'"), keyword),
					t2 => {
						var parent = block.Parent.ReplaceNode((SyntaxNode)block, block.Statements.First())
							.WithAdditionalAnnotations(Formatter.Annotation);

						var newRoot = root.ReplaceNode((SyntaxNode)block.Parent, parent);
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
			);
		}

		internal static bool IsSpecialNode (SyntaxToken token, out string keyword, out StatementSyntax embeddedStatement)
		{
			var node = token.Parent;
			if (node is BlockSyntax) {
				node = node.Parent;
			}
			switch (node.Kind())  {
				case SyntaxKind.IfStatement:
					keyword = "if";
					embeddedStatement = ((IfStatementSyntax)node).Statement;
					return true;
				case SyntaxKind.ElseClause:
					keyword = "else";
					embeddedStatement = ((ElseClauseSyntax)node).Statement;
					return true;
				case SyntaxKind.DoStatement:
					keyword = "do";
					embeddedStatement = ((DoStatementSyntax)node).Statement;
					return true;
				case SyntaxKind.ForEachStatement:
					keyword = "foreach";
					embeddedStatement = ((ForEachStatementSyntax)node).Statement;
					return true;
				case SyntaxKind.ForStatement:
					keyword = "for";
					embeddedStatement = ((ForStatementSyntax)node).Statement;
					return true;
				case SyntaxKind.LockStatement:
					keyword = "lock";
					embeddedStatement = ((LockStatementSyntax)node).Statement;
					return true;
				case SyntaxKind.UsingStatement:
					keyword = "using";
					embeddedStatement = ((UsingStatementSyntax)node).Statement;
					return true;
				case SyntaxKind.WhileStatement:
					keyword = "while";
					embeddedStatement = ((WhileStatementSyntax)node).Statement;
					return true;
			}
			keyword = null;
			embeddedStatement = null;
			return false;
		}
		/*
		static BlockStatement GetBlockStatement(SemanticModel context)
		{
			var block = context.GetNode<BlockStatement>();
			if (block == null || block.LBraceToken.IsNull || block.RBraceToken.IsNull)
				return null;
			if (!(block.LBraceToken.IsInside(context.Location) || block.RBraceToken.IsInside(context.Location)))
				return null;
			if (!(block.Parent is Statement) || block.Parent is TryCatchStatement) 
				return null;
			if (block.Statements.Count != 1 || block.Statements.First () is VariableDeclarationStatement) 
				return null;
			return block;
		}*/
	}
}
