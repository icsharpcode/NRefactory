//
// DelegateSubtractionAnalyzer.cs
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
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class DelegateSubtractionAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.DelegateSubtractionAnalyzerID, 
			GettextCatalog.GetString("Delegate subtraction has unpredictable result"),
			GettextCatalog.GetString("Delegate subtraction has unpredictable result"), 
			DiagnosticAnalyzerCategories.CodeQualityIssues, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.DelegateSubtractionAnalyzerID)
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
				SyntaxKind.SubtractAssignmentExpression,
				SyntaxKind.SubtractExpression
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			var assignment = nodeContext.Node as AssignmentExpressionSyntax;
			if (assignment != null) {
				if (!IsEvent(nodeContext.SemanticModel, assignment.Left) && IsDelegate(nodeContext.SemanticModel, assignment.Right)) {
					diagnostic = Diagnostic.Create (
						descriptor,
						assignment.GetLocation ()
					);
					return true;
				}
			}
			var binex = nodeContext.Node as BinaryExpressionSyntax;
			if (binex != null) {
				if (!IsEvent(nodeContext.SemanticModel, binex.Left) && IsDelegate(nodeContext.SemanticModel, binex.Right)) {
					diagnostic = Diagnostic.Create (
						descriptor,
						binex.GetLocation ()
					);
					return true;
				}
			}
			return false;
		}

		static bool IsEvent(SemanticModel semanticModel, SyntaxNode node)
		{
			var rr = semanticModel.GetSymbolInfo(node);
			return rr.Symbol != null && rr.Symbol.Kind == SymbolKind.Event;
		}

		static bool IsDelegate(SemanticModel semanticModel, SyntaxNode node)
		{
			var rr = semanticModel.GetTypeInfo(node);
			return rr.Type != null && rr.Type.TypeKind == TypeKind.Delegate;
		}
	}
}