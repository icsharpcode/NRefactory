//
// EnumUnderlyingTypeIsIntAnalyzer.cs
//
// Author:
//       Luís Reis <luiscubal@gmail.com>
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class EnumUnderlyingTypeIsIntAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.EnumUnderlyingTypeIsIntAnalyzerID, 
			GettextCatalog.GetString("The default underlying type of enums is int, so defining it explicitly is redundant."),
			GettextCatalog.GetString("Default underlying type of enums is already int"), 
			DiagnosticAnalyzerCategories.RedundanciesInDeclarations, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.EnumUnderlyingTypeIsIntAnalyzerID),
			customTags: DiagnosticCustomTags.Unnecessary
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				nodeContext => {
					Diagnostic diagnostic;
					if (TryAnalyzeEnumDeclaration (nodeContext, out diagnostic)) {
						nodeContext.ReportDiagnostic(diagnostic);
					}
				}, 
				new SyntaxKind[] {  SyntaxKind.EnumDeclaration }
			);
		}

		static bool TryAnalyzeEnumDeclaration (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			var enumDeclaration = nodeContext.Node as EnumDeclarationSyntax;
			diagnostic = default(Diagnostic);
			if (enumDeclaration.BaseList == null || enumDeclaration.BaseList.Types.Count == 0)
				return false;
			var underlyingType = enumDeclaration.BaseList.Types.First ();
            var info = nodeContext.SemanticModel.GetSymbolInfo(underlyingType.Type);
			var type = info.Symbol as ITypeSymbol;
			if (type == null || type.SpecialType != SpecialType.System_Int32)
				return false;

			diagnostic = Diagnostic.Create (
				descriptor,
				enumDeclaration.BaseList.GetLocation ()
			);
			return true;
		}
	}
}