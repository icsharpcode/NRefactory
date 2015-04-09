//
// RedundantArgumentNameCodeFixProvider.cs
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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class RedundantArgumentNameCodeFixProvider : CodeFixProvider
	{
		const string CodeActionMessage = "Remove argument name specification";

		public override ImmutableArray<string> FixableDiagnosticIds {
			get {
				return ImmutableArray.Create (NRefactoryDiagnosticIDs.RedundantArgumentNameAnalyzerID);
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
			var node = root.FindNode(context.Span);
			var argListSyntax = node.Parent.Parent as BaseArgumentListSyntax;
			if (node.IsKind(SyntaxKind.NameColon) && argListSyntax != null) {
				bool replace = true;
				var newRoot = root;
				var args = new List<ArgumentSyntax> ();

				foreach (var arg in argListSyntax.Arguments) {
					if (replace) {
						args.Add(arg);
					}
					replace &= arg != node.Parent;

				}
				newRoot = newRoot.ReplaceNodes(args, (arg, arg2) => SyntaxFactory.Argument(arg.Expression).WithAdditionalAnnotations(Formatter.Annotation));

				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, CodeActionMessage, document.WithSyntaxRoot(newRoot)), diagnostic);
				return;
			}
			var attrListSyntax = node.Parent.Parent as AttributeArgumentListSyntax;
			if (node.IsKind(SyntaxKind.NameColon) && attrListSyntax != null) {
				bool replace = true;
				var newRoot = root;
				var args = new List<AttributeArgumentSyntax> ();

				foreach (var arg in attrListSyntax.Arguments) {
					if (replace) {
						args.Add(arg);
					}
					replace &= arg != node.Parent;

				}
				newRoot = newRoot.ReplaceNodes(args, (arg, arg2) => SyntaxFactory.AttributeArgument(arg.Expression).WithAdditionalAnnotations(Formatter.Annotation));

				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, CodeActionMessage, document.WithSyntaxRoot(newRoot)), diagnostic);
				return;
			}
		}
	}

}