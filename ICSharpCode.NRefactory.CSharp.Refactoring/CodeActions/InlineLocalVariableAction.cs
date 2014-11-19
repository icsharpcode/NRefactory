// 
// InlineLocalVariableAction.cs
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
using Microsoft.CodeAnalysis.FindSymbols;
using System.Reflection;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Inlines a local variable")]
	[ExportCodeRefactoringProvider("Inline local variable", LanguageNames.CSharp)]
	public class InlineLocalVariableAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			var node = root.FindNode(span) as VariableDeclaratorSyntax;
			if (node == null)
				return;
			var parent = node.Parent as VariableDeclarationSyntax;
			if (parent == null || parent.Variables.Count != 1)
				return;

			var sym = model.GetDeclaredSymbol(node) as ILocalSymbol;
			if (sym == null)
				return;
			context.RegisterRefactoring(
				CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Info, 
					"Inline local variable", 
					t2 => {
						var nodes = new List<SyntaxNode>();
						foreach (var reference in SymbolFinder.FindReferencesAsync(sym, context.Document.Project.Solution, cancellationToken).Result) {
							foreach (var refLoc in reference.Locations) {
								var foundNode = root.FindNode(refLoc.Location.SourceSpan);
								var arg = foundNode as ArgumentSyntax;
								if (arg != null)
									foundNode = arg.Expression;
								nodes.Add(foundNode);
							}
						}
						var newRoot = root.TrackNodes(nodes.Concat(new [] { parent.Parent }));

						if (parent.Variables.Count == 1) {
							newRoot = newRoot.RemoveNode(newRoot.GetCurrentNode(parent.Parent), SyntaxRemoveOptions.KeepNoTrivia);
						}
						var replaceExpr = node.Initializer.Value.WithAdditionalAnnotations(Formatter.Annotation);
						foreach (var removeNode in nodes) {
							newRoot = newRoot.ReplaceNode((SyntaxNode)newRoot.GetCurrentNode(removeNode), AddParensIfRequired(removeNode, replaceExpr));
						}

						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
			);
		}

		public static bool RequiresParens(SyntaxNode replaceNode, SyntaxNode replaceWithNode)
		{
			if (!(replaceWithNode is BinaryExpressionSyntax) &&
				!(replaceWithNode is AssignmentExpressionSyntax) &&
				!(replaceWithNode is CastExpressionSyntax) &&
				!(replaceWithNode is SimpleLambdaExpressionSyntax) &&
				!(replaceWithNode is ParenthesizedLambdaExpressionSyntax) &&
				!(replaceWithNode is ConditionalExpressionSyntax)) {
				return false;
			}

			var cond = replaceNode.Parent as ConditionalExpressionSyntax;
			if (cond != null && cond.Condition == replaceNode)
				return true;

			var indexer = replaceNode.Parent as ElementAccessExpressionSyntax;
			if (indexer != null && indexer.Expression == replaceNode)
				return true;

			return replaceNode.Parent is BinaryExpressionSyntax || 
				replaceNode.Parent is PostfixUnaryExpressionSyntax || 
				replaceNode.Parent is PrefixUnaryExpressionSyntax || 
				replaceNode.Parent is AssignmentExpressionSyntax || 
				replaceNode.Parent is MemberAccessExpressionSyntax ||
				replaceNode.Parent is CastExpressionSyntax ||
				replaceNode.Parent is ParenthesizedLambdaExpressionSyntax ||
				replaceNode.Parent is SimpleLambdaExpressionSyntax;
		}

		static ExpressionSyntax AddParensIfRequired(SyntaxNode replaceNode, ExpressionSyntax expression)
		{
			if (RequiresParens(replaceNode, expression))
				return SyntaxFactory.ParenthesizedExpression(expression);
			return expression;
		}
	}
}
