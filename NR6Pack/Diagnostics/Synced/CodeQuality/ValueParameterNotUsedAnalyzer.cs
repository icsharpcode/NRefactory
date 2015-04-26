//
// SetterDoesNotUseValueParameterTests.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ValueParameterNotUsedAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ValueParameterNotUsedAnalyzerID, 
			GettextCatalog.GetString("Warns about property or indexer setters and event adders or removers that do not use the value parameter"),
			GettextCatalog.GetString("The {0} does not use the 'value' parameter"), 
			DiagnosticAnalyzerCategories.CodeQualityIssues, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.ValueParameterNotUsedAnalyzerID)
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
				SyntaxKind.SetAccessorDeclaration,
				SyntaxKind.AddAccessorDeclaration,
				SyntaxKind.RemoveAccessorDeclaration
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;
			var node = nodeContext.Node as AccessorDeclarationSyntax;
			var evt = node.Parent.Parent as EventDeclarationSyntax;
			if (evt != null) {
				if (evt.AccessorList.Accessors.Any (a => a.IsKind (SyntaxKind.AddAccessorDeclaration) && a.Body.Statements.Count == 0) &&
					(evt.AccessorList.Accessors.Any (a => a.IsKind (SyntaxKind.RemoveAccessorDeclaration) && a.Body.Statements.Count == 0)))
					return false;
			}
			if (!FindIssuesInAccessor (nodeContext.SemanticModel, node))
				return false;
			diagnostic = Diagnostic.Create (
				descriptor,
				node.Keyword.GetLocation (),
				GetMessageArgument (node)
			);
			return true;
		}

		static string GetMessageArgument (AccessorDeclarationSyntax node)
		{
			switch (node.Kind ()) {
			case SyntaxKind.SetAccessorDeclaration:
				return GettextCatalog.GetString ("setter");
			case SyntaxKind.AddAccessorDeclaration:
				return GettextCatalog.GetString ("add accessor");
			case SyntaxKind.RemoveAccessorDeclaration:
				return GettextCatalog.GetString ("remove accessor");
			}
			return null;
		}

		static bool FindIssuesInAccessor(SemanticModel semanticModel, AccessorDeclarationSyntax accessor)
		{
			var body = accessor.Body;
			if (!IsEligible(body))
				return false;

			if (body.Statements.Any()) {
				var foundValueSymbol = semanticModel.LookupSymbols(body.Statements.First().SpanStart, null, "value").FirstOrDefault();
				if (foundValueSymbol == null)
					return false;

				foreach (var valueRef in body.DescendantNodes().OfType<IdentifierNameSyntax>().Where(ins => ins.Identifier.ValueText == "value")) {
					var valueRefSymbol = semanticModel.GetSymbolInfo(valueRef).Symbol;
					if (foundValueSymbol.Equals(valueRefSymbol))
						return false;
				}
			}

			return true;
		}

		static bool IsEligible(BlockSyntax body)
		{
			if (body == null)
				return false;
			if (body.Statements.Any(s => s is ThrowStatementSyntax))
				return false;
			return true;
		}
	}
}