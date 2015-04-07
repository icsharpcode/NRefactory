// 
// ConvertToStaticTypeAnalyzer.cs
//  
// Author:
//       Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013  Ji Kun <jikun.nus@gmail.com>
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

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ConvertToStaticTypeAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ConvertToStaticTypeAnalyzerID,
			GettextCatalog.GetString ("If all fields, properties and methods members are static, the class can be made static."),
			GettextCatalog.GetString ("This class is recommended to be defined as static"),
			DiagnosticAnalyzerCategories.Opportunities,
			DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor (NRefactoryDiagnosticIDs.ConvertToStaticTypeAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize (AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction (
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryGetDiagnostic (nodeContext, out diagnostic)) {
						nodeContext.ReportDiagnostic (diagnostic);
					}
				},
				new SyntaxKind [] { SyntaxKind.ClassDeclaration }
			);
		}

		bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			var node = nodeContext.Node as ClassDeclarationSyntax;
			var semanticModel = nodeContext.SemanticModel;
			var cancellationToken = nodeContext.CancellationToken;

			diagnostic = default(Diagnostic);
			ITypeSymbol classType = semanticModel.GetDeclaredSymbol (node);
			if (!node.Modifiers.Any () || node.Modifiers.Any (m => m.IsKind (SyntaxKind.PartialKeyword)) || classType.IsAbstract || classType.IsStatic)
				return false;
			//ignore implicitly declared (e.g. default ctor)
			IEnumerable<ISymbol> enumerable = classType.GetMembers ().Where (m => !(m is ITypeSymbol));
			if (Enumerable.Any(enumerable, f => (!f.IsStatic && !f.IsImplicitlyDeclared) || (f is IMethodSymbol && IsMainMethod ((IMethodSymbol)f))))
				return false;

			diagnostic = Diagnostic.Create (
				descriptor,
				node.Identifier.GetLocation ()
			);
			return true;
		}

		internal static bool IsMainMethod(IMethodSymbol m)
		{
			return (m.ReturnType.SpecialType == SpecialType.System_Int32 || m.ReturnType.SpecialType == SpecialType.System_Void) && m.IsStatic && m.Name.Equals("Main");
		}
	}
}