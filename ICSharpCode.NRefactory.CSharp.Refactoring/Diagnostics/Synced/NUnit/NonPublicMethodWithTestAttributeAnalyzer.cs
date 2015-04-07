//
// NonPublicMethodWithTestAttributeAnalyzer.cs
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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NonPublicMethodWithTestAttributeAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.NonPublicMethodWithTestAttributeAnalyzerID, 
			GettextCatalog.GetString("Non public methods are not found by NUnit"),
			GettextCatalog.GetString("NUnit test methods should be public"), 
			DiagnosticAnalyzerCategories.NUnit, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.NonPublicMethodWithTestAttributeAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				nodeContext => {
					Diagnostic diagnostic;
					if (TryAnalyzeMethod (nodeContext, out diagnostic)) {
						nodeContext.ReportDiagnostic(diagnostic);
					}
				}, 
				new SyntaxKind[] {  SyntaxKind.MethodDeclaration }
			);
		}

		static bool TryAnalyzeMethod (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			var methodDeclaration = nodeContext.Node as MethodDeclarationSyntax;
			diagnostic = default(Diagnostic);

			var methodSymbol = nodeContext.SemanticModel.GetDeclaredSymbol (methodDeclaration);
			if (methodSymbol == null || methodSymbol.IsOverride || methodSymbol.IsStatic || methodDeclaration.Modifiers.Any (m => m.IsKind (SyntaxKind.PublicKeyword)))
				return false;

			if (!methodSymbol.GetAttributes ().Any (a => a.AttributeClass.Name == "TestAttribute" && a.AttributeClass.ContainingNamespace.ToDisplayString () == "NUnit.Framework"))
				return false;

			diagnostic = Diagnostic.Create (
				descriptor,
				methodDeclaration.Identifier.GetLocation ()
			);
			return true;
		}
	}
}