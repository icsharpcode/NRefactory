//
// ConvertForToWhileAction.cs
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
	[NRefactoryCodeRefactoringProvider(Description = "Works on 'for' loops")]
	[ExportCodeRefactoringProvider("Convert 'for' loop to 'while'", LanguageNames.CSharp)]
	public class ConvertForToWhileAction : SpecializedCodeAction<ForStatementSyntax>
	{
		protected override IEnumerable<CodeAction> GetActions(Document document, SemanticModel semanticModel, SyntaxNode root, TextSpan span, ForStatementSyntax node, CancellationToken cancellationToken)
		{
			if (!node.ForKeyword.Span.Contains(span))
				return Enumerable.Empty<CodeAction>();

			return new [] { CodeActionFactory.Create(
				node.Span,
				DiagnosticSeverity.Info,
				"Convert to 'while'",
				t2 => {
					var statements = new List<StatementSyntax>();
					var blockSyntax = node.Statement as BlockSyntax;
					if (blockSyntax != null) {
						statements.AddRange(blockSyntax.Statements);
					} else {
						statements.Add(node.Statement);
					}
					statements.AddRange(node.Incrementors.Select(i => SyntaxFactory.ExpressionStatement(i)));

					var whileStatement = SyntaxFactory.WhileStatement(
							node.Condition ?? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression),
							SyntaxFactory.Block(statements));
					var replaceStatements = new List<StatementSyntax>();
					if (node.Declaration != null)
						replaceStatements.Add(SyntaxFactory.LocalDeclarationStatement(node.Declaration).WithAdditionalAnnotations(Formatter.Annotation));

					foreach (var init in node.Initializers) {
						replaceStatements.Add(SyntaxFactory.ExpressionStatement(init).WithAdditionalAnnotations(Formatter.Annotation));
					}
					replaceStatements.Add (whileStatement.WithAdditionalAnnotations(Formatter.Annotation));
					replaceStatements[0] = replaceStatements[0].WithLeadingTrivia(node.GetLeadingTrivia());

					var newRoot = root.ReplaceNode((SyntaxNode)node, replaceStatements);
					return Task.FromResult(document.WithSyntaxRoot(newRoot));
				}
			)};
		}
	}
}
