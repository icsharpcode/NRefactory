//
// ConvertNullableToShortFormAnalyzer.cs
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ConvertNullableToShortFormAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ConvertNullableToShortFormAnalyzerID, 
			GettextCatalog.GetString("Convert 'Nullable<T>' to the short form 'T?'"),
			GettextCatalog.GetString("Nullable type can be simplified"), 
			DiagnosticAnalyzerCategories.Opportunities, 
			DiagnosticSeverity.Info, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.ConvertNullableToShortFormAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryGetDiagnostic (nodeContext, out diagnostic)) {
						nodeContext.ReportDiagnostic(diagnostic);
					}
				}, 
				new SyntaxKind[] {  SyntaxKind.QualifiedName, SyntaxKind.GenericName }
			);
		}

		bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			var simpleType = nodeContext.Node;
			var semanticModel = nodeContext.SemanticModel;
			var cancellationToken = nodeContext.CancellationToken;

			diagnostic = default(Diagnostic);
			if (GetTypeArgument(simpleType) == null)
				return false;
			var rr = semanticModel.GetSymbolInfo(simpleType);
			var type = rr.Symbol as ITypeSymbol;
			if (type == null || type.Name != "Nullable" || type.ContainingNamespace.ToDisplayString() != "System")
				return false;

			diagnostic = Diagnostic.Create (
				descriptor,
				simpleType.GetLocation ()
			);
			return true;
		}

		internal static TypeSyntax GetTypeArgument (SyntaxNode node)
		{
			var gns = node as GenericNameSyntax;
			if (gns == null) {
				var qns = node as QualifiedNameSyntax;
				if (qns != null)
					gns = qns.Right as GenericNameSyntax;
			} else {
				var parent = gns.Parent as QualifiedNameSyntax;
				if (parent != null && parent.Right == node)
					return null;
			}

			if (gns != null) {
				if (gns.TypeArgumentList.Arguments.Count == 1) {
					var typeArgument = gns.TypeArgumentList.Arguments[0];
					if (!typeArgument.IsKind(SyntaxKind.OmittedTypeArgument))
						return typeArgument;
				}
			}
			return null;
		}
	}
}