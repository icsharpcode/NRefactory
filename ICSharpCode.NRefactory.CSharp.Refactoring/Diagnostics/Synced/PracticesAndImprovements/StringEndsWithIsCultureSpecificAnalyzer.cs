//
// StringEndsWithIsCultureSpecific.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "StringEndsWithIsCultureSpecific")]
	public class StringEndsWithIsCultureSpecificAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "StringEndsWithIsCultureSpecificAnalyzer";
		const string Description            = "Warns when a culture-aware 'EndsWith' call is used by default.";
		const string MessageFormat          = "'EndsWith' is culture-aware and missing a StringComparison argument";
		const string Category               = DiagnosticAnalyzerCategories.PracticesAndImprovements;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "'string.EndsWith' is culture-aware");
		// "Add 'StringComparison.Ordinal'" / "Add 'StringComparison.CurrentCulture'
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new StringIndexOfIsCultureSpecificAnalyzer.GatherVisitor<StringEndsWithIsCultureSpecificAnalyzer>(Rule, semanticModel, addDiagnostic, cancellationToken, "EndsWith");
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class StringMethodIsCultureSpecificFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			return ImmutableArray.Create(
				StringEndsWithIsCultureSpecificAnalyzer.DiagnosticId,
				StringIndexOfIsCultureSpecificAnalyzer.DiagnosticId,
				StringLastIndexOfIsCultureSpecificAnalyzer.DiagnosticId,
				StringStartsWithIsCultureSpecificAnalyzer.DiagnosticId);
		}

		public async override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			var cancellationToken = context.CancellationToken;
			var span = context.Span;
			var diagnostics = context.Diagnostics;
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			foreach (var diagnostic in diagnostics) {
				var node = root.FindNode(diagnostic.Location.SourceSpan).SkipArgument () as InvocationExpressionSyntax;
				RegisterFix(context, root, diagnostic, node, "Ordinal", cancellationToken);
				RegisterFix(context, root, diagnostic, node, "CurrentCulture", cancellationToken);
			}
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