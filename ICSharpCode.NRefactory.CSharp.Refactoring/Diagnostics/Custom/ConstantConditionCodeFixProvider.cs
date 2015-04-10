//
// ConstantConditionCodeFixProvider.cs
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

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using System.Threading;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class ConstantConditionCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds {
			get {
				return ImmutableArray.Create (NRefactoryDiagnosticIDs.ConstantConditionAnalyzerID);
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
			var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
			var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
			var diagnostic = diagnostics.First ();
			var node = root.FindNode(context.Span);
			//if (!node.IsKind(SyntaxKind.BaseList))
			//	continue;

			var value = bool.Parse(diagnostic.Descriptor.CustomTags.First());

			var conditionalExpr = node.Parent as ConditionalExpressionSyntax;
			var ifElseStatement = node.Parent as IfStatementSyntax;
			var valueStr = value.ToString().ToLowerInvariant();

			if (conditionalExpr != null)
			{
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, string.Format("Replace '?:' with '{0}' branch", valueStr), token =>
				{
					var replaceWith = value ? conditionalExpr.WhenTrue : conditionalExpr.WhenFalse;
					var newRoot = root.ReplaceNode((SyntaxNode)conditionalExpr, replaceWith.WithAdditionalAnnotations(Formatter.Annotation));
					return Task.FromResult(document.WithSyntaxRoot(newRoot));
				}), diagnostic);
			}
			else if (ifElseStatement != null)
			{
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, string.Format("Replace 'if' with '{0}' branch", valueStr), token =>
				{
					var list = new List<SyntaxNode>();
					StatementSyntax branch;
					if (value)
					{
						branch = ifElseStatement.Statement;
					}
					else
					{
						if (ifElseStatement.Else == null)
							return Task.FromResult(document.WithSyntaxRoot(root.RemoveNode(ifElseStatement, SyntaxRemoveOptions.KeepNoTrivia)));
						branch = ifElseStatement.Else.Statement;
					}

					var block = branch as BlockSyntax;
					if (block != null)
					{
						foreach (var stmt in block.Statements)
							list.Add(stmt.WithAdditionalAnnotations(Formatter.Annotation));
					}
					else
					{
						if (branch != null)
							list.Add(branch.WithAdditionalAnnotations(Formatter.Annotation));
					}
					if (list.Count == 0)
						return Task.FromResult(document);
					var newRoot = root.ReplaceNode((SyntaxNode)ifElseStatement, list);
					return Task.FromResult(document.WithSyntaxRoot(newRoot));
				}), diagnostic);
			}
			else
			{
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, string.Format("Replace expression with '{0}'", valueStr), token =>
				{
					var replaceWith = SyntaxFactory.LiteralExpression(value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
					var newRoot = root.ReplaceNode((SyntaxNode)node, replaceWith.WithAdditionalAnnotations(Formatter.Annotation));
					return Task.FromResult(document.WithSyntaxRoot(newRoot));
				}), diagnostic);
			}
		}
	}
}