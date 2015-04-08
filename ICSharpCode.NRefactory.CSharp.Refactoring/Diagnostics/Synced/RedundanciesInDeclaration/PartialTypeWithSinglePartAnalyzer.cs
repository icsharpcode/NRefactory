// PartialTypeWithSinglePartAnalyzer.cs
// 
// Author:
//      Luís Reis <luiscubal@gmail.com>
// 
// Copyright (c) 2013 Luís Reis
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "PartialTypeWithSinglePart")]
	public class PartialTypeWithSinglePartAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.PartialTypeWithSinglePartDiagnosticID, 
			GettextCatalog.GetString("Class is declared partial but has only one part"), 
			GettextCatalog.GetString("Partial class with single part"), 
			DiagnosticAnalyzerCategories.RedundanciesInDeclarations, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.PartialTypeWithSinglePartDiagnosticID),
			customTags: DiagnosticCustomTags.Unnecessary
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryRemovePartialModifier (nodeContext, out diagnostic)) {
						nodeContext.ReportDiagnostic(diagnostic);
					}
				}, 
				new SyntaxKind[] {  SyntaxKind.ClassDeclaration }
			);
		}

		static bool TryRemovePartialModifier (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			var classDeclaration = nodeContext.Node as ClassDeclarationSyntax;
			diagnostic = default(Diagnostic);
			if (classDeclaration == null)
				return false;
			var modifier = 
				classDeclaration
					.Modifiers
					.Cast<SyntaxToken?> ()
					.FirstOrDefault (m => m.Value.IsKind (SyntaxKind.PartialKeyword));
			if (!modifier.HasValue)
				return false;
			var symbol = nodeContext.SemanticModel.GetDeclaredSymbol (classDeclaration, nodeContext.CancellationToken);
			if (symbol == null || symbol.Locations.Length != 1)
				return false;

			diagnostic = Diagnostic.Create (descriptor, modifier.Value.GetLocation ());
			return true;
		}
	}
}