// 
// NotImplementedExceptionInspector.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
	/// <summary>
	/// This inspector just shows that there is a not implemented exception. It doesn't offer a fix.
	/// Should only be shown in overview bar, no underlining.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NotImplementedExceptionAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.NotImplementedExceptionAnalyzerID,
			GettextCatalog.GetString ("Shows NotImplementedException throws in the quick task bar"),
			GettextCatalog.GetString ("Not implemented"),
			DiagnosticAnalyzerCategories.Notifications,
			DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor (NRefactoryDiagnosticIDs.NotImplementedExceptionAnalyzerID)
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
				new SyntaxKind [] { SyntaxKind.ThrowStatement }
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;
			var node = nodeContext.Node as ThrowStatementSyntax;
			var result = nodeContext.SemanticModel.GetTypeInfo (node.Expression).Type;
			if (result == null || result.Name != "NotImplementedException" || result.ContainingNamespace.ToDisplayString () != "System")
				return false;
			diagnostic = Diagnostic.Create (descriptor, node.Expression.GetLocation ());
			return true;
		}
	}
}