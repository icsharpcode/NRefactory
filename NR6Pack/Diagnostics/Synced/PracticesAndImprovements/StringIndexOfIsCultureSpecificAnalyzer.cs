//
// StringIndexOfIsCultureSpecificAnalyzer.cs
//
// Author:
//       Daniel Grunwald <daniel@danielgrunwald.de>
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Daniel Grunwald
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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class StringIndexOfIsCultureSpecificAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.StringIndexOfIsCultureSpecificAnalyzerID, 
			GettextCatalog.GetString("Warns when a culture-aware 'IndexOf' call is used by default."),
			GettextCatalog.GetString("'IndexOf' is culture-aware and missing a StringComparison argument"), 
			DiagnosticAnalyzerCategories.PracticesAndImprovements, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.StringIndexOfIsCultureSpecificAnalyzerID)
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
				SyntaxKind.InvocationExpression
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;
			var node = nodeContext.Node as InvocationExpressionSyntax;
			MemberAccessExpressionSyntax mre = node.Expression as MemberAccessExpressionSyntax;
			if (mre == null)
				return false;
			if (mre.Name.Identifier.ValueText != "IndexOf")
				return false;

			var rr = nodeContext.SemanticModel.GetSymbolInfo(node, nodeContext.CancellationToken);
			if (rr.Symbol == null)
				return false;
			var symbol = rr.Symbol;
			if (!(symbol.ContainingType != null && symbol.ContainingType.SpecialType == SpecialType.System_String))
				return false;
			var parameters = symbol.GetParameters();
			var firstParameter = parameters.FirstOrDefault();
			if (firstParameter == null || firstParameter.Type.SpecialType != SpecialType.System_String)
				return false;	// First parameter not a string
			var lastParameter = parameters.Last();
			if (lastParameter.Type.Name == "StringComparison")
				return false;	// already specifying a string comparison
			diagnostic = Diagnostic.Create (
				descriptor,
				node.GetLocation ()
			);
			return true;
		}
	}
}