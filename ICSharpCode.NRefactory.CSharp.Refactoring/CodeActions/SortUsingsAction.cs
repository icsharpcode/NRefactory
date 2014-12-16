//
// SortUsingsAction.cs
//
// Author:
//      Lopatkin Ilja
//
// Copyright (c) 2012 Lopatkin Ilja
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
	[NRefactoryCodeRefactoringProvider(Description = "Sorts usings by their origin and then alphabetically")]
	[ExportCodeRefactoringProvider("Sort usings", LanguageNames.CSharp)]
	public class SortUsingsAction: CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			var node = root.FindNode(span);
			if (node == null)
				return;

			var usingDirective = node.FirstAncestorOrSelf<SyntaxNode>(n => n is UsingDirectiveSyntax);

			// a CodeAction that doesn't change the document seems to crash VS
			// so we have to check if the usings are already sorted, and cancel if necessary.
			if (usingDirective == null || CheckAllSorted(root, model))
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(span, DiagnosticSeverity.Info, "Sort usings", cancellation => {
					var newRoot = root;
					var blocks = EnumerateUsingBlocks(root);

					foreach (var block in blocks) {
						var originalNodes = block.Select(n => new UsingInfo(n, model)).ToArray();

						newRoot = newRoot.TrackNodes(originalNodes.Select(n => n.Node));

						var sortedNodes = originalNodes.OrderBy(_ => _).ToArray();
						
						for (var i = 0; i < originalNodes.Length; ++i) {
							var replacement = sortedNodes[i].Node
								.WithTrailingTrivia(originalNodes[i].Node.GetTrailingTrivia())
								.WithLeadingTrivia(originalNodes[i].Node.GetLeadingTrivia());
                            newRoot = newRoot.ReplaceNode(newRoot.GetCurrentNode(originalNodes[i].Node), replacement);
						}
					}

					return Task.FromResult(document.WithSyntaxRoot(newRoot));
				})
			);
		}

		private static bool CheckAllSorted(SyntaxNode root, SemanticModel model)
		{
			var blocks = EnumerateUsingBlocks(root);

			foreach (var block in blocks) {
				var nodes = block.Select(n => new UsingInfo(n, model)).ToArray();
				var sorted = nodes.OrderBy(_ => _).ToArray();
				if (!nodes.SequenceEqual(sorted))
					return false;
			}

			return true;
		}

		private static IEnumerable<IEnumerable<UsingDirectiveSyntax>> EnumerateUsingBlocks(SyntaxNode root)
		{
			var alreadyAddedNodes = new HashSet<UsingDirectiveSyntax>();

			foreach (var child in root.DescendantNodes()) {
				if (child is UsingDirectiveSyntax && !alreadyAddedNodes.Contains(child)) {
					var blockNodes = child.Parent.ChildNodes().OfType<UsingDirectiveSyntax>().ToArray();

					alreadyAddedNodes.UnionWith(blockNodes);
					yield return blockNodes;
				}
			}
		}

		private sealed class UsingInfo : IComparable<UsingInfo>
		{
			public UsingDirectiveSyntax Node;

			public string Alias;
			public string Name;

			public bool IsAlias;
			public bool HasTypesFromOtherAssemblies;
			public bool IsSystem;

			public UsingInfo(UsingDirectiveSyntax node, SemanticModel context)
			{
				Alias = node.Alias?.Name?.Identifier.ValueText;
				Name = node.Name.ToString();
				IsAlias = Alias != null;

				var result = context.GetSymbolInfo(node.Name);
				if (result.Symbol is INamespaceSymbol) {
					HasTypesFromOtherAssemblies = ((INamespaceSymbol)result.Symbol).ConstituentNamespaces.Select(cn => cn.ContainingAssembly).Any(a => context.Compilation.Assembly != a);
				}

				IsSystem = HasTypesFromOtherAssemblies && (Name == "System" || Name.StartsWith("System.", StringComparison.Ordinal));

				Node = node;
			}

			public int CompareTo(UsingInfo y)
			{
				UsingInfo x = this;
				if (x.IsAlias != y.IsAlias)
					return x.IsAlias ? 1 : -1;
				else if (x.HasTypesFromOtherAssemblies != y.HasTypesFromOtherAssemblies)
					return x.HasTypesFromOtherAssemblies ? -1 : 1;
				else if (x.IsSystem != y.IsSystem)
					return x.IsSystem ? -1 : 1;
				else if (x.Alias != y.Alias)
					return StringComparer.Ordinal.Compare(x.Alias, y.Alias);
				else if (x.Name != y.Name)
					return StringComparer.Ordinal.Compare(x.Name, y.Name);
				else
					return 0;
			}
		}
	}
}
