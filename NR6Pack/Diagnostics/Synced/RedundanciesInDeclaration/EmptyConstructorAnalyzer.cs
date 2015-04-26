// 
// EmptyConstructorAnalyzer.cs
// 
// Author:
//      Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013 Ji Kun
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
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SaHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class EmptyConstructorAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.EmptyConstructorAnalyzerID, 
			GettextCatalog.GetString("An empty public constructor without paramaters is redundant."),
			GettextCatalog.GetString("Empty constructor is redundant"), 
			DiagnosticAnalyzerCategories.RedundanciesInDeclarations, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.EmptyConstructorAnalyzerID),
			customTags: DiagnosticCustomTags.Unnecessary
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				nodeContext => {
					Diagnostic diagnostic;
					if (TryGetDiagnostic (nodeContext, out diagnostic)) {
						nodeContext.ReportDiagnostic(diagnostic);
					}
				}, 
				new SyntaxKind[] {  SyntaxKind.ConstructorDeclaration }
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			var constructorDeclaration = nodeContext.Node as ConstructorDeclarationSyntax;
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;

			if (!IsEmpty(constructorDeclaration) || !constructorDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
				return false;

			if ((constructorDeclaration.Parent).GetMembers().OfType<ConstructorDeclarationSyntax>().Count(child => !child.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword))) > 1)
				return false;

			diagnostic = Diagnostic.Create (
				descriptor,
				constructorDeclaration.GetLocation ()
			);
			return true;
		}

		static bool IsEmpty (ConstructorDeclarationSyntax constructorDeclaration)
		{
			if (constructorDeclaration.Initializer != null && constructorDeclaration.Initializer.ArgumentList.Arguments.Count > 0)
				return false;
			
			return constructorDeclaration.ParameterList.Parameters.Count == 0 && 
				EmptyDestructorAnalyzer.IsEmpty (constructorDeclaration.Body);
		}
	}
}