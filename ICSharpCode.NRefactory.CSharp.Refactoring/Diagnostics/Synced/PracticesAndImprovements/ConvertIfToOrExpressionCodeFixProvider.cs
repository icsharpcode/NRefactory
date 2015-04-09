//
// ConvertIfToOrExpressionCodeFixProvider.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class ConvertIfToOrExpressionCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds {
			get {
				return ImmutableArray.Create (NRefactoryDiagnosticIDs.ConvertIfToOrExpressionAnalyzerID);
			}
		}

		public override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public async override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			var cancellationToken = context.CancellationToken;
			var span = context.Span;
			var diagnostics = context.Diagnostics;
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			foreach (var diagnostic in diagnostics)
			{
				var node = root.FindNode(context.Span) as IfStatementSyntax;
				ExpressionSyntax target;
				SyntaxTriviaList assignmentTrailingTriviaList;
				ConvertIfToOrExpressionAnalyzer.MatchIfElseStatement(node, SyntaxKind.TrueLiteralExpression, out target, out assignmentTrailingTriviaList);
				SyntaxNode newRoot = null;
				var varDeclaration = ConvertIfToOrExpressionAnalyzer.FindPreviousVarDeclaration(node);
				if (varDeclaration != null)
				{
					var varDeclarator = varDeclaration.Declaration.Variables[0];
					newRoot = root.ReplaceNodes(new SyntaxNode[] { varDeclaration, node }, (arg, arg2) =>
					{
						if (arg is LocalDeclarationStatementSyntax)
							return SyntaxFactory.LocalDeclarationStatement(
									SyntaxFactory.VariableDeclaration(varDeclaration.Declaration.Type,
										SyntaxFactory.SeparatedList(
											new[] {
												SyntaxFactory.VariableDeclarator(varDeclarator.Identifier.ValueText)
													.WithInitializer(
														SyntaxFactory.EqualsValueClause(
															SyntaxFactory.BinaryExpression(SyntaxKind.LogicalOrExpression, ConvertIfToOrExpressionAnalyzer.AddParensToComplexExpression(varDeclarator.Initializer.Value), ConvertIfToOrExpressionAnalyzer.AddParensToComplexExpression(node.Condition)))
																.WithAdditionalAnnotations(Formatter.Annotation)
													)
											}
										))
								).WithLeadingTrivia(varDeclaration.GetLeadingTrivia()).WithTrailingTrivia(node.GetTrailingTrivia());
						return null;
					});
				}
				else
				{
					newRoot = root.ReplaceNode((SyntaxNode)node,
						SyntaxFactory.ExpressionStatement(
							SyntaxFactory.AssignmentExpression(
								SyntaxKind.OrAssignmentExpression,
								ConvertIfToOrExpressionAnalyzer.AddParensToComplexExpression(target),
								ConvertIfToOrExpressionAnalyzer.AddParensToComplexExpression(node.Condition).WithAdditionalAnnotations(Formatter.Annotation)
							)
						).WithLeadingTrivia(node.GetLeadingTrivia()).WithTrailingTrivia(node.GetTrailingTrivia()));
				}

				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, diagnostic.GetMessage(), document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}