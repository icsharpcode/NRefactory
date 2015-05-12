//
// StringCompareIsCultureSpecificAnalyzer.cs
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
    [NotPortedYet]
    public class StringCompareIsCultureSpecificAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.StringCompareIsCultureSpecificAnalyzerID, 
			GettextCatalog.GetString("Warns when a culture-aware 'Compare' call is used by default"),
			GettextCatalog.GetString("'string.Compare' is culture-aware"), 
			DiagnosticAnalyzerCategories.PracticesAndImprovements, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.StringCompareIsCultureSpecificAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		//static readonly DiagnosticDescriptor Rule1 = new DiagnosticDescriptor (DiagnosticId, Description, "Use ordinal comparison", Category, DiagnosticSeverity.Warning, true, "'string.Compare' is culture-aware");
		//static readonly DiagnosticDescriptor Rule2 = new DiagnosticDescriptor (DiagnosticId, Description, "Use culture-aware comparison", Category, DiagnosticSeverity.Warning, true, "'string.Compare' is culture-aware");


		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryGetDiagnostic (nodeContext, out diagnostic)) {
						nodeContext.ReportDiagnostic(diagnostic);
					}
				}, 
				new SyntaxKind[] { SyntaxKind.InvocationExpression }
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
			if (mre.Name.Identifier.ValueText != "Compare")
				return false;
			if (node.ArgumentList.Arguments.Count != 2 &&
			    node.ArgumentList.Arguments.Count != 3 &&
			    node.ArgumentList.Arguments.Count != 5 &&
			    node.ArgumentList.Arguments.Count != 6)
				return false;
			
			var rr = nodeContext.SemanticModel.GetSymbolInfo (node, nodeContext.CancellationToken);
			if (rr.Symbol == null)
				return false;
			var symbol = rr.Symbol;
			if (!(symbol.ContainingType != null && symbol.ContainingType.SpecialType == SpecialType.System_String))
				return false;
			if (!symbol.IsStatic)
				return false;
			var parameters = symbol.GetParameters ();
			var firstParameter = parameters.FirstOrDefault ();
			if (firstParameter == null || firstParameter.Type.SpecialType != SpecialType.System_String)
				return false;   // First parameter not a string

			var lastParameter = parameters.Last();
			if (lastParameter.Type.Name == "StringComparison")
				return false; // already specifying a string comparison

			diagnostic = Diagnostic.Create (
				descriptor,
				node.GetLocation ()
			);
			return true;
		}
	}
}