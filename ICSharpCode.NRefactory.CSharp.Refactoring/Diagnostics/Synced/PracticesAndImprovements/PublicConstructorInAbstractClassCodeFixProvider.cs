//
// PublicConstructorInAbstractClassCodeFixProvider.cs
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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class PublicConstructorInAbstractClassCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds {
			get {
				return ImmutableArray.Create (NRefactoryDiagnosticIDs.PublicConstructorInAbstractClassAnalyzerID);
			}
		}

		public override FixAllProvider GetFixAllProvider ()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public async override Task RegisterCodeFixesAsync (CodeFixContext context)
		{
			var document = context.Document;
			var cancellationToken = context.CancellationToken;
			var span = context.Span;
			var diagnostics = context.Diagnostics;
			var root = await document.GetSyntaxRootAsync (cancellationToken);
			var diagnostic = diagnostics.First ();
			var constructor = root.FindToken(span.Start).Parent.AncestorsAndSelf().OfType<ConstructorDeclarationSyntax>().First();
			context.RegisterCodeFix (CodeActionFactory.Create (span, diagnostic.Severity, "Make constructor protected",delegate {
				var publicToken = constructor.Modifiers.First(m => m.IsKind(SyntaxKind.PublicKeyword));
				var newConstructor = constructor.WithModifiers (constructor.Modifiers.Replace (publicToken,  SyntaxFactory.Token(publicToken.LeadingTrivia, SyntaxKind.ProtectedKeyword,
				                                                                                                                 publicToken.TrailingTrivia)));
				var newRoot = root.ReplaceNode(constructor, newConstructor);
				return Task.FromResult (document.WithSyntaxRoot (newRoot));
			}), diagnostic);
			context.RegisterCodeFix (CodeActionFactory.Create (span, diagnostic.Severity, "Make constructor private",delegate {
				var publicToken = constructor.Modifiers.First(m => m.IsKind(SyntaxKind.PublicKeyword));
				var newConstructor = constructor.WithModifiers (constructor.Modifiers.Remove (publicToken));
				var newRoot = root.ReplaceNode(constructor, newConstructor);
				return Task.FromResult (document.WithSyntaxRoot (newRoot));
			}), diagnostic);
		}
	}
}