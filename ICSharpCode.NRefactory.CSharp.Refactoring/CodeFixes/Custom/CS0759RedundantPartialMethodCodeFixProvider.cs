// CS0759RedundantPartialMethodCodeFixProvider.cs
// 
// Author:
//      Luís Reis <luiscubal@gmail.com>
// 
// Copyright (c) 2013 Luís Reis
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

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeFixes;

namespace ICSharpCode.NRefactory6.CSharp.CodeFixes
{
	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class CS0759RedundantPartialMethodCodeFixProvider : CodeFixProvider
	{
		const string CS0759 = "CS0759"; // Error CS0108: No defining declaration found for implementing declaration of partial method 'method'.

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(CS0759); }
		}

		public override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public async override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
				return;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			if (cancellationToken.IsCancellationRequested)
				return;
			var diagnostic = context.Diagnostics.First ();
			var model = await document.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
			var root = await model.SyntaxTree.GetRootAsync (cancellationToken).ConfigureAwait (false);
			var node = root.FindNode (span) as MethodDeclarationSyntax;
			if (node == null || !node.Identifier.Span.Contains (span))
				return;
			context.RegisterCodeFix (
				CodeActionFactory.Create (
					node.Span,
					DiagnosticSeverity.Error,
					GettextCatalog.GetString ("Remove 'partial'"),
					t => Task.FromResult (
						document.WithSyntaxRoot (
							root.ReplaceNode (
								(SyntaxNode)node,
								node.WithModifiers(SyntaxFactory.TokenList(node.Modifiers.Where(m => !m.IsKind(SyntaxKind.PartialKeyword))))
								.WithLeadingTrivia(node.GetLeadingTrivia())
							)
						)
					)
				),
				diagnostic
			);
		}
	}
}