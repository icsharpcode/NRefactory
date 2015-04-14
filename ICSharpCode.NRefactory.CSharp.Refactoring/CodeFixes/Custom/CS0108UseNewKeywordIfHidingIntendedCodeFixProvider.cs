//
// CS0108UseNewKeywordIfHidingIntendedCodeFixProvider.cs
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

using ICSharpCode.NRefactory6.CSharp;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ICSharpCode.NRefactory6.CSharp.CodeFixes
{
	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class CS0108UseNewKeywordIfHidingIntendedCodeFixProvider : CodeFixProvider
	{
		const string CS0108 = "CS0108"; // Warning CS0108: Use the new keyword if hiding was intended

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(CS0108); }
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
			context.RegisterCodeFix(CodeActionFactory.Create(
				node.Span,
				diagnostic.Severity,
				GettextCatalog.GetString ("Add 'new' modifier"),
				token =>
				{
					SyntaxNode newRoot;
					if (node.Kind() != SyntaxKind.VariableDeclarator)
						newRoot = root.ReplaceNode(node, AddNewModifier(node));
					else //this one wants to be awkward - you can't add modifiers to a variable declarator
                    {
						SyntaxNode declaringNode = node.Parent.Parent;
						if (declaringNode is FieldDeclarationSyntax)
							newRoot = root.ReplaceNode(node.Parent.Parent, (node.Parent.Parent as FieldDeclarationSyntax).AddModifiers(SyntaxFactory.Token(SyntaxKind.NewKeyword)));
						else //it's an event declaration
							newRoot = root.ReplaceNode(node.Parent.Parent, (node.Parent.Parent as EventFieldDeclarationSyntax).AddModifiers(SyntaxFactory.Token(SyntaxKind.NewKeyword)));
					}
					return Task.FromResult(document.WithSyntaxRoot(newRoot.WithAdditionalAnnotations(Formatter.Annotation)));
				}), diagnostic);
		}

		SyntaxNode AddNewModifier(SyntaxNode node)
		{
			SyntaxToken newToken = SyntaxFactory.Token(SyntaxKind.NewKeyword);
			switch (node.Kind()) {
			//couldn't find a common base
			case SyntaxKind.IndexerDeclaration:
				var indexer = (IndexerDeclarationSyntax)node;
				return indexer.AddModifiers(newToken);
			case SyntaxKind.ClassDeclaration:
				var classDecl = (ClassDeclarationSyntax)node;
				return classDecl.AddModifiers(newToken);
			case SyntaxKind.PropertyDeclaration:
				var propDecl = (PropertyDeclarationSyntax)node;
				return propDecl.AddModifiers(newToken);
			case SyntaxKind.MethodDeclaration:
				var methDecl = (MethodDeclarationSyntax)node;
				return methDecl.AddModifiers(newToken);
			case SyntaxKind.StructDeclaration:
				var structDecl = (StructDeclarationSyntax)node;
				return structDecl.AddModifiers(newToken);
			case SyntaxKind.EnumDeclaration:
				var enumDecl = (EnumDeclarationSyntax)node;
				return enumDecl.AddModifiers(newToken);
			case SyntaxKind.InterfaceDeclaration:
				var intDecl = (InterfaceDeclarationSyntax)node;
				return intDecl.AddModifiers(newToken);
			case SyntaxKind.DelegateDeclaration:
				var deleDecl = (DelegateDeclarationSyntax)node;
				return deleDecl.AddModifiers(newToken);
			default:
				return node;
			}
		}
	}
}
