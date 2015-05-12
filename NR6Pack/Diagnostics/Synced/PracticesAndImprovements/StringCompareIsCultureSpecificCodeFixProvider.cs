//
// StringCompareIsCultureSpecificCodeFixProvider.cs
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
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using System.Threading;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class StringCompareIsCultureSpecificCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds {
			get {
				return ImmutableArray.Create (NRefactoryDiagnosticIDs.StringCompareIsCultureSpecificAnalyzerID);
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
			var model = await document.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
			var root = await model.SyntaxTree.GetRootAsync (cancellationToken).ConfigureAwait (false);

			var diagnostic = diagnostics.First ();
			var node = root.FindNode(context.Span).SkipArgument () as InvocationExpressionSyntax;
			if (node == null)
				return;

			RegisterFix(context, model, root, diagnostic, node, "Ordinal", GettextCatalog.GetString ("Use ordinal comparison"), cancellationToken);
			RegisterFix(context, model, root, diagnostic, node, "CurrentCulture", GettextCatalog.GetString ("Use culture-aware comparison"), cancellationToken);
		}

		static void RegisterFix(CodeFixContext context, SemanticModel model, SyntaxNode root, Diagnostic diagnostic, InvocationExpressionSyntax invocationExpression, string stringComparison, string message, CancellationToken cancellationToken = default(CancellationToken))
		{
			bool? ignoreCase = null;
			ExpressionSyntax caseArg = null;

			if (invocationExpression.ArgumentList.Arguments.Count == 3) {
				var arg = model.GetConstantValue (invocationExpression.ArgumentList.Arguments[2].Expression, cancellationToken);
				if (arg.HasValue) {
					ignoreCase = (bool)arg.Value;
				} else {
					caseArg = invocationExpression.ArgumentList.Arguments[2].Expression;
				}
			}

			if (invocationExpression.ArgumentList.Arguments.Count  == 6) {
				var arg = model.GetConstantValue (invocationExpression.ArgumentList.Arguments[5].Expression, cancellationToken);
				if (arg.HasValue) {
					ignoreCase = (bool)arg.Value;
				} else {
					caseArg = invocationExpression.ArgumentList.Arguments[5].Expression;
				}
			}
			var argumentList = new List<ArgumentSyntax> ();
			if (invocationExpression.ArgumentList.Arguments.Count <= 3) {
				argumentList.AddRange (invocationExpression.ArgumentList.Arguments.Take (2));
			} else {
				argumentList.AddRange (invocationExpression.ArgumentList.Arguments.Take (5));
			}

			argumentList.Add (SyntaxFactory.Argument (CreateCompareArgument (invocationExpression, ignoreCase, caseArg, stringComparison)));
            var newArguments = SyntaxFactory.ArgumentList (SyntaxFactory.SeparatedList (argumentList));
			var newInvocation = SyntaxFactory.InvocationExpression(invocationExpression.Expression, newArguments);
			var newRoot = root.ReplaceNode(invocationExpression, newInvocation.WithAdditionalAnnotations(Formatter.Annotation));

			context.RegisterCodeFix(CodeActionFactory.Create(invocationExpression.Span, diagnostic.Severity, message, context.Document.WithSyntaxRoot(newRoot)), diagnostic);
		}

		static ExpressionSyntax CreateCompareArgument (InvocationExpressionSyntax invocationExpression, bool? ignoreCase, ExpressionSyntax caseArg, string stringComparison)
		{
			var stringComparisonType = SyntaxFactory.ParseTypeName("System.StringComparison").WithAdditionalAnnotations(Microsoft.CodeAnalysis.Simplification.Simplifier.Annotation);

			if (caseArg == null)
				return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, stringComparisonType, (SimpleNameSyntax)SyntaxFactory.ParseName(ignoreCase == true ? stringComparison + "IgnoreCase" : stringComparison));

			return SyntaxFactory.ConditionalExpression(
				caseArg,
				SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, stringComparisonType, (SimpleNameSyntax)SyntaxFactory.ParseName(stringComparison + "IgnoreCase")),
				SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, stringComparisonType, (SimpleNameSyntax)SyntaxFactory.ParseName(stringComparison))
			);
		}
	}
}