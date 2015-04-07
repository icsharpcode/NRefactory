//
// ConvertClosureToMethodGroupFixProvider.cs
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class ConvertClosureToMethodGroupCodeFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return NRefactoryDiagnosticIDs.ConvertClosureToMethodDiagnosticID;
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
			var c1 = node as AnonymousMethodExpressionSyntax;
			var c2 = node as ParenthesizedLambdaExpressionSyntax;
			var c3 = node as SimpleLambdaExpressionSyntax;
			if (c1 == null && c2 == null && c3 == null)
				return;
			context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, GettextCatalog.GetString ("Replace with method group"), token => {
				InvocationExpressionSyntax invoke = null;
				if (c1 != null)
					invoke = ConvertClosureToMethodGroupAnalyzer.AnalyzeBody(c1.Block);
				if (c2 != null)
					invoke = ConvertClosureToMethodGroupAnalyzer.AnalyzeBody(c2.Body);
				if (c3 != null)
					invoke = ConvertClosureToMethodGroupAnalyzer.AnalyzeBody(c3.Body);
				var newRoot = root.ReplaceNode((SyntaxNode)node, invoke.Expression.WithLeadingTrivia (node.GetLeadingTrivia ()).WithTrailingTrivia (node.GetTrailingTrivia ()));
				return Task.FromResult(document.WithSyntaxRoot(newRoot));
			}), diagnostic);
		}
	}
}