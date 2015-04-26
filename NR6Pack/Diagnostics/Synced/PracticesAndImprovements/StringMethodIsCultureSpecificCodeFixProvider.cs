//
// StringMethodIsCultureSpecificCodeFixProvider.cs
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
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class StringMethodIsCultureSpecificCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds {
			get {
				return ImmutableArray.Create (
					NRefactoryDiagnosticIDs.StringIndexOfIsCultureSpecificAnalyzerID,
					NRefactoryDiagnosticIDs.StringEndsWithIsCultureSpecificAnalyzerID,
					NRefactoryDiagnosticIDs.StringLastIndexOfIsCultureSpecificAnalyzerID,
					NRefactoryDiagnosticIDs.StringStartsWithIsCultureSpecificAnalyzerID
				);
			}
		}

		public async override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			var cancellationToken = context.CancellationToken;
			var span = context.Span;
			var diagnostics = context.Diagnostics;
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var diagnostic = diagnostics.First ();
			var node = root.FindNode(context.Span).SkipArgument () as InvocationExpressionSyntax;
			RegisterFix(context, root, diagnostic, node, "Ordinal", cancellationToken);
			RegisterFix(context, root, diagnostic, node, "CurrentCulture", cancellationToken);
		}

		internal static void RegisterFix(CodeFixContext context, SyntaxNode root, Diagnostic diagnostic, InvocationExpressionSyntax invocationExpression, string stringComparison, CancellationToken cancellationToken = default(CancellationToken))
		{
			var stringComparisonType = SyntaxFactory.ParseTypeName("System.StringComparison").WithAdditionalAnnotations(Microsoft.CodeAnalysis.Simplification.Simplifier.Annotation);
			var stringComparisonArgument = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, stringComparisonType, (SimpleNameSyntax)SyntaxFactory.ParseName(stringComparison));
			var newArguments = invocationExpression.ArgumentList.AddArguments(SyntaxFactory.Argument(stringComparisonArgument));
			var newInvocation = SyntaxFactory.InvocationExpression(invocationExpression.Expression, newArguments);
			var newRoot = root.ReplaceNode(invocationExpression, newInvocation.WithAdditionalAnnotations(Formatter.Annotation));

			context.RegisterCodeFix(CodeActionFactory.Create(invocationExpression.Span, diagnostic.Severity, string.Format ("Add 'StringComparison.{0}'", stringComparison), context.Document.WithSyntaxRoot(newRoot)), diagnostic);
		}
	}
}