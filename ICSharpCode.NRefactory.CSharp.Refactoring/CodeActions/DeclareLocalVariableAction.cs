// 
// DeclareLocalVariableAction.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
	[NRefactoryCodeRefactoringProvider(Description = "Declare a local variable out of a selected expression")]
	[ExportCodeRefactoringProvider("Declare local variable", LanguageNames.CSharp)]
	public class DeclareLocalVariableAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			if (span.Start == span.End)
				return;

			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			var node = root.FindNode(span);
			node = node.DescendantNodes().FirstOrDefault(n => span.Contains(n.Span)) ?? node;
			if (node is ExpressionStatementSyntax)
				node = ((ExpressionStatementSyntax)node).Expression;
			if (node is ArgumentSyntax)
				node = ((ArgumentSyntax)node).Expression;
			var expr = node as ExpressionSyntax;
			if (expr == null)
				return;
			var containingBlock = node.Ancestors().FirstOrDefault(a => a is BlockSyntax);
			List<SyntaxNode> nodeList;
			if (containingBlock != null) {
				nodeList = containingBlock.DescendantNodes().Where(n => span.Start <= n.SpanStart && SyntaxFactory.AreEquivalent(n, node)).ToList();
			} else {
				return;
			}

//			if (expr is ArrayInitializerExpression) {
//				var arr = (ArrayInitializerExpression)expr;
//				if (arr.IsSingleElement) {
//					expr = arr.Elements.First();
//				} else {
//					yield break;
//				}
//			}
//
			var result = new List<CodeAction>();
			context.RegisterRefactoring(CodeActionFactory.Create(
				expr.Span,
				DiagnosticSeverity.Info,
				"Declare local variable",
				t2 => {
					var resolveResult = model.GetTypeInfo(expr);
					var guessedType = resolveResult.Type ?? resolveResult.ConvertedType;
					var name = RefactoringHelpers.CreateBaseName(expr, guessedType);
//					name = context.GetLocalNameProposal(name, expr.StartLocation);
					var type = /*context.UseExplicitTypes ? context.CreateShortType(guessedType) :*/ SyntaxFactory.ParseTypeName("var");

					var varDecl = SyntaxFactory.LocalDeclarationStatement(
						SyntaxFactory.VariableDeclaration(
							type,
							SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>(new [] {
								SyntaxFactory.VariableDeclarator(name).WithInitializer(
									SyntaxFactory.EqualsValueClause(expr.SkipParens())
								)
							})
						)
					).WithAdditionalAnnotations(Formatter.Annotation);

					SyntaxNode newRoot = root;

					SyntaxNode replaceNode = expr;
					while (replaceNode.Parent is ParenthesizedExpressionSyntax)
						replaceNode = replaceNode.Parent;

					if (replaceNode.Parent is ExpressionStatementSyntax) {
						newRoot = root.ReplaceNode((SyntaxNode)replaceNode.Parent, varDecl);
					} else {
						var identifierExpression = SyntaxFactory.IdentifierName(name);
						newRoot = root.ReplaceNode((SyntaxNode)replaceNode, identifierExpression.WithAdditionalAnnotations(Formatter.Annotation));

						var containing = newRoot.FindNode(expr.Span);
						while (!(containing.Parent is BlockSyntax)) {
							containing = containing.Parent;
						}
						newRoot = newRoot.InsertNodesBefore(containing, new [] { varDecl });
//						script.Link(varDecl.Variables.First().NameToken, identifierExpression);
					}
					return Task.FromResult(document.WithSyntaxRoot(newRoot));
				}
			));

			if (nodeList.Count > 1) {
				context.RegisterRefactoring(CodeActionFactory.Create(
					expr.Span,
					DiagnosticSeverity.Info,
					string.Format("Declare local variable (replace '{0}' occurrences)", nodeList.Count),
					t2 => {
						var resolveResult = model.GetTypeInfo(expr);
						var guessedType = resolveResult.Type ?? resolveResult.ConvertedType;
						var name = CreateMethodDeclarationAction.CreateBaseName(expr, guessedType);
//					name = context.GetLocalNameProposal(name, expr.StartLocation);
						var type = /*context.UseExplicitTypes ? context.CreateShortType(guessedType) :*/ SyntaxFactory.ParseTypeName("var");
						var varDecl = SyntaxFactory.LocalDeclarationStatement(
							SyntaxFactory.VariableDeclaration(
								type,
								SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>(new [] {
									SyntaxFactory.VariableDeclarator(name).WithAdditionalAnnotations(RenameAnnotation.Create ()).WithInitializer(
										SyntaxFactory.EqualsValueClause(expr.SkipParens())
									)
								})
							)
						).WithAdditionalAnnotations(Formatter.Annotation);

						SyntaxNode newRoot = root.TrackNodes(nodeList);

						SyntaxNode replaceNode = nodeList[0];

						var identifierExpression = SyntaxFactory.IdentifierName(name).WithAdditionalAnnotations(Formatter.Annotation);

						if (replaceNode.Parent is ExpressionStatementSyntax) {
							newRoot = newRoot.ReplaceNode((SyntaxNode)replaceNode.Parent, varDecl);
						} else {
							var curReplaceNode = newRoot.GetCurrentNode(replaceNode);
							while (curReplaceNode.Parent is ParenthesizedExpressionSyntax)
								curReplaceNode = curReplaceNode.Parent;

							newRoot = newRoot.ReplaceNode((SyntaxNode)curReplaceNode, identifierExpression);

							var containing = newRoot.FindNode(TextSpan.FromBounds(expr.SpanStart, expr.SpanStart));
							while (!(containing.Parent is BlockSyntax)) {
								if (containing.Parent == null)
									break;
								containing = containing.Parent;
							}
							newRoot = newRoot.InsertNodesBefore(containing, new [] { varDecl });
						}

						for (int i = 1; i < nodeList.Count; i++) {
							var curReplaceNode = newRoot.GetCurrentNode(nodeList[i]);
							while (curReplaceNode.Parent is ParenthesizedExpressionSyntax)
								curReplaceNode = curReplaceNode.Parent;
							newRoot = newRoot.ReplaceNode((SyntaxNode)curReplaceNode, identifierExpression);
						}

						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				));
			}
		}
	}
}