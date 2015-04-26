//
// ReplaceWithFirstOrDefaultCodeFixProvider.cs
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
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class ReplaceWithFirstOrDefaultCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds {
			get {
				return ImmutableArray.Create (NRefactoryDiagnosticIDs.ReplaceWithFirstOrDefaultAnalyzerID);
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
			var node = root.FindNode(context.Span) as ConditionalExpressionSyntax;
			//replace a conditional Any(x) ? First(x) : null/default with FirstOrDefault(x)
			var parameterExpr = ((InvocationExpressionSyntax)node.Condition).ArgumentList;
			var baseExpression = ((MemberAccessExpressionSyntax)((InvocationExpressionSyntax)node.Condition).Expression).Expression;
			var newNode = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, baseExpression,
				SyntaxFactory.IdentifierName("FirstOrDefault")), parameterExpr);
			var newRoot = root.ReplaceNode((ExpressionSyntax)node, newNode.WithAdditionalAnnotations(Formatter.Annotation));
			context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Replace with 'FirstOrDefault<T>()'", document.WithSyntaxRoot(newRoot)), diagnostic);
		}
	}
}