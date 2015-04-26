// 
// JoinDeclarationAndAssignmentAction.cs
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

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Join local variable declaration and assignment")]
	public class JoinLocalVariableDeclarationAndAssignmentCodeRefactoringProvider : SpecializedCodeRefactoringProvider<VariableDeclaratorSyntax>
	{
		protected override IEnumerable<CodeAction> GetActions(Document document, SemanticModel semanticModel, SyntaxNode root, TextSpan span, VariableDeclaratorSyntax node, CancellationToken cancellationToken)
		{
			var variableDecl = node.Parent.Parent as LocalDeclarationStatementSyntax;
			if (variableDecl == null || node.Initializer != null)
				yield break;
			var block = variableDecl.Parent as BlockSyntax;
			StatementSyntax nextStatement = null;
			for (int i = 0; i < block.Statements.Count; i++) {
				if (block.Statements[i] == variableDecl && i + 1 < block.Statements.Count) {
					nextStatement = block.Statements[i + 1];
					break;
				}
			}
			var expr = nextStatement as ExpressionStatementSyntax;
			if (expr == null)
				yield break;
			var assignment = expr.Expression as AssignmentExpressionSyntax;
			if (assignment == null || assignment.Left.ToString() != node.Identifier.ToString())
				yield break;

			yield return
				CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Info, 
					GettextCatalog.GetString ("Join declaration and assignment"), 
					t2 => {
						root = root.TrackNodes(new SyntaxNode[] { node, nextStatement } );
						var newRoot = root.ReplaceNode((SyntaxNode)
							root.GetCurrentNode(node),
							node.WithInitializer(SyntaxFactory.EqualsValueClause(assignment.Right)).WithAdditionalAnnotations(Formatter.Annotation)
						);
						newRoot = newRoot.RemoveNode(newRoot.GetCurrentNode(nextStatement), SyntaxRemoveOptions.KeepNoTrivia);
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				);
		}
	}
}