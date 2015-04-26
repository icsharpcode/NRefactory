//
// CS0152DuplicateCaseLabelValueCodeFixProvider.cs
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

namespace ICSharpCode.NRefactory6.CSharp.CodeFixes
{
	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class CS0152DuplicateCaseLabelValueCodeFixProvider : CodeFixProvider
	{
		const string CS0152 = "CS0152"; // CS0152: The label 'label' already occurs in this switch statement

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(CS0152); }
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
			var node = root.FindNode(context.Span) as CaseSwitchLabelSyntax;
			if (node == null)
				return;
			var switchSection = node.Parent as SwitchSectionSyntax;
			if (switchSection.Labels.Count == 1) {
				var switchStatement = switchSection.Parent as SwitchStatementSyntax;
				if (switchStatement.Sections.Count (sect => sect.IsEquivalentTo (switchSection)) <= 1)
					return;
				context.RegisterCodeFix (CodeActionFactory.Create (
					node.Span,
					diagnostic.Severity,
					string.Format (GettextCatalog.GetString ("Remove 'case {0}' switch section"), node.Value),
					token => {
						var newRoot = root.RemoveNode (node.Parent, SyntaxRemoveOptions.KeepNoTrivia);
						return Task.FromResult (document.WithSyntaxRoot (newRoot));
					}), diagnostic);
			} else {
				if (switchSection.Labels.Count (label => label.IsEquivalentTo (node)) > 1) {
					context.RegisterCodeFix (CodeActionFactory.Create (
					node.Span,
					diagnostic.Severity,
					string.Format (GettextCatalog.GetString ("Remove 'case {0}' label"), node.Value),
					token => {
						var newRoot = root.RemoveNode (node, SyntaxRemoveOptions.KeepNoTrivia);
						return Task.FromResult (document.WithSyntaxRoot (newRoot));
					}), diagnostic);
				}
			}
		}
	}
}