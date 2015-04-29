//
// NonReadonlyReferencedInGetHashCodeAnalyzer.cs
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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
    [NotPortedYet]
    public class NonReadonlyReferencedInGetHashCodeAnalyzer : DiagnosticAnalyzer
	{	
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.NonReadonlyReferencedInGetHashCodeAnalyzerID, 
			GettextCatalog.GetString("Non-readonly field referenced in 'GetHashCode()'"),
			GettextCatalog.GetString("Non-readonly field referenced in 'GetHashCode()'"), 
			DiagnosticAnalyzerCategories.CodeQualityIssues, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.NonReadonlyReferencedInGetHashCodeAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					IEnumerable<Diagnostic> diagnostics;
					if (TryGetDiagnostic(nodeContext, out diagnostics))
						foreach (var diagnostic in diagnostics)
							nodeContext.ReportDiagnostic(diagnostic);
				},
				SyntaxKind.MethodDeclaration
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out IEnumerable<Diagnostic> diagnostic)
		{
			diagnostic = default(IEnumerable<Diagnostic>);
			var node = nodeContext.Node as MethodDeclarationSyntax;
			IMethodSymbol method = nodeContext.SemanticModel.GetDeclaredSymbol(node);
			if (method == null || method.Name != "GetHashCode" || !method.IsOverride || method.Parameters.Count() > 0)
				return false;
			if (method.ReturnType.SpecialType != SpecialType.System_Int32)
				return false;

			diagnostic = node
				.DescendantNodes ()
				.OfType<IdentifierNameSyntax> ()
				.Where (n => IsNonReadonlyField (nodeContext.SemanticModel, n))
				.Select (n => Diagnostic.Create (descriptor, n.GetLocation ())	);
			return true;
		}

		static bool IsNonReadonlyField(SemanticModel semanticModel, SyntaxNode node)
		{
			var symbol = semanticModel.GetSymbolInfo(node).Symbol as IFieldSymbol;
			return symbol != null && !symbol.IsReadOnly && !symbol.IsConst;
		}
	}
}