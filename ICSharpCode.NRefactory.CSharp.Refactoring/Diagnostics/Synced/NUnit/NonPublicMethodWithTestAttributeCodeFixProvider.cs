//
// NonPublicMethodWithTestAttributeCodeFixProvider.cs
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeFixes;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class NonPublicMethodWithTestAttributeCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds {
			get {
				return ImmutableArray.Create (NRefactoryDiagnosticIDs.NonPublicMethodWithTestAttributeAnalyzerID);
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
			var diagnostic = diagnostics.First ();
			var node = root.FindNode(context.Span) as MethodDeclarationSyntax;
			if (node == null)
				return;

			Func<SyntaxToken, bool> isModifierToRemove =
				m => (m.IsKind(SyntaxKind.PrivateKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword) || m.IsKind(SyntaxKind.InternalKeyword));

			// Get trivia for new modifier
			var leadingTrivia = SyntaxTriviaList.Empty;
			var trailingTrivia = SyntaxTriviaList.Create(SyntaxFactory.Space);
			var removedModifiers = node.Modifiers.Where(isModifierToRemove);
			if (removedModifiers.Any())
			{
				leadingTrivia = removedModifiers.First().LeadingTrivia;
			}
			else
			{
				// Method begins directly with return type, use its leading trivia
				leadingTrivia = node.ReturnType.GetLeadingTrivia();
			}

			var newMethod = node.WithModifiers(SyntaxFactory.TokenList(new SyntaxTokenList()
				.Add(SyntaxFactory.Token(leadingTrivia, SyntaxKind.PublicKeyword, trailingTrivia))
				.AddRange(node.Modifiers.ToArray().Where(m => !isModifierToRemove(m)))))
				.WithReturnType(node.ReturnType.WithoutLeadingTrivia());
			var newRoot = root.ReplaceNode(node, newMethod);
			context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Make method public", document.WithSyntaxRoot(newRoot)), diagnostic);
		}
	}
}