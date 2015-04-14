//
// CS0162UnreachableCodeDetectedCodeFixProvider.cs
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

using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.NRefactory6.CSharp.CodeFixes
{
	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class CS0162UnreachableCodeDetectedCodeFixProvider : CodeFixProvider
	{
		const string CS0162 = "CS0162"; // CS0162: The compiler detected code that will never be executed.

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(CS0162); }
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
			var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			if (model.IsFromGeneratedCode(cancellationToken))
				return;
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

			var diagnostic = diagnostics.First ();
			var node = root.FindNode(context.Span);
			if (node == null)
				return;

			context.RegisterCodeFix (CodeActionFactory.Create (
				node.Span,
				diagnostic.Severity,
				GettextCatalog.GetString ("Remove redundant code"),
				token => {
					var newRoot = GetNewRoot (root, node);

					return Task.FromResult(document.WithSyntaxRoot(newRoot));
				}), diagnostic);
		}

		static SyntaxNode GetNewRoot (SyntaxNode root, SyntaxNode node)
		{
			var decl = node.AncestorsAndSelf ().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault ();
			if (decl != null)
				return root.RemoveNode(decl, SyntaxRemoveOptions.KeepNoTrivia);
			if (node.Parent.IsKind (SyntaxKind.ElseClause)) 
				return root.RemoveNode (node.Parent, SyntaxRemoveOptions.KeepNoTrivia);

			var statement = node as StatementSyntax;
			if (statement != null) 
				return root.RemoveNode (statement, SyntaxRemoveOptions.KeepNoTrivia);
			
			return root.RemoveNode(node.Parent, SyntaxRemoveOptions.KeepNoTrivia);
		}
	}
}