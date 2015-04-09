//
// ReplaceWithFirstOrDefaultAnalyzer.cs
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
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ReplaceWithFirstOrDefaultAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ReplaceWithFirstOrDefaultAnalyzerID, 
			GettextCatalog.GetString("Replace with call to FirstOrDefault<T>()"),
			GettextCatalog.GetString("Expression can be simlified to 'FirstOrDefault<T>()'"), 
			DiagnosticAnalyzerCategories.PracticesAndImprovements, 
			DiagnosticSeverity.Info, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.ReplaceWithFirstOrDefaultAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryGetDiagnostic(nodeContext, out diagnostic))
						nodeContext.ReportDiagnostic(diagnostic);
				},
				SyntaxKind.ConditionalExpression
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			var node = nodeContext.Node as ConditionalExpressionSyntax;

			//pattern is Any(param) ? First(param) : null/default
			var anyInvocation = node.Condition as InvocationExpressionSyntax;
			var firstInvocation = node.WhenTrue as InvocationExpressionSyntax;
			var nullDefaultWhenFalse = node.WhenFalse;

			if (anyInvocation == null || firstInvocation == null || nullDefaultWhenFalse == null)
				return false;
			var anyExpression = anyInvocation.Expression as MemberAccessExpressionSyntax;
			if (anyExpression == null || anyExpression.Name.Identifier.ValueText != "Any")
				return false;
			var anyParam = anyInvocation.ArgumentList;

			var firstExpression = firstInvocation.Expression as MemberAccessExpressionSyntax;
			if (firstExpression == null || firstExpression.Name.Identifier.ValueText != "First" || !firstInvocation.ArgumentList.IsEquivalentTo(anyParam))
				return false;

			if (!nullDefaultWhenFalse.IsKind(SyntaxKind.NullLiteralExpression) && !nullDefaultWhenFalse.IsKind(SyntaxKind.DefaultExpression))
				return false;

			diagnostic = Diagnostic.Create (
				descriptor,
				node.GetLocation ()
			);
			return true;
		}
	}
}