//
// FieldCanBeMadeReadOnlyCodeFixProvider.cs
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

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class FieldCanBeMadeReadOnlyCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create (NRefactoryDiagnosticIDs.FieldCanBeMadeReadOnlyAnalyzerID);

		public override FixAllProvider GetFixAllProvider ()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync (CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync (context.CancellationToken).ConfigureAwait (false);
			var diagnostic = context.Diagnostics.First ();
			var diagnosticSpan = diagnostic.Location.SourceSpan;
			var varDecl = root.FindToken (context.Span.Start).Parent.AncestorsAndSelf ().OfType<VariableDeclaratorSyntax> ().FirstOrDefault ();
			if (varDecl == null)
				return;
			context.RegisterCodeFix (
				CodeActionFactory.Create(
					context.Span,
					diagnostic.Severity,
					GettextCatalog.GetString ("To 'readonly'"),
					delegate (CancellationToken cancellationToken) {
						var fieldDeclaration = varDecl.Ancestors().OfType<FieldDeclarationSyntax>().First ();
						var nodes = new List<SyntaxNode> ();
						if (fieldDeclaration.Declaration.Variables.Count == 1) {
							nodes.Add (
								fieldDeclaration
									.AddModifiers (SyntaxFactory.Token (SyntaxKind.ReadOnlyKeyword))
									.WithAdditionalAnnotations (Formatter.Annotation)
							);
						} else {
							nodes.Add (fieldDeclaration.WithDeclaration (fieldDeclaration.Declaration.RemoveNode (varDecl, SyntaxRemoveOptions.KeepEndOfLine)));
							nodes.Add (
								fieldDeclaration.WithDeclaration (
									SyntaxFactory.VariableDeclaration (
										fieldDeclaration.Declaration.Type, 
										SyntaxFactory.SeparatedList (new [] { varDecl })
									)
								)
								.AddModifiers (SyntaxFactory.Token (SyntaxKind.ReadOnlyKeyword))
								.WithAdditionalAnnotations (Formatter.Annotation)
							);
						}
						return Task.FromResult (context.Document.WithSyntaxRoot (root.ReplaceNode (fieldDeclaration, nodes)));
					}
				), 
				diagnostic
			);
		}
	}
}