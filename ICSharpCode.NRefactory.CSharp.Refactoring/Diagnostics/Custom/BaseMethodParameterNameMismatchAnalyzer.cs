//
// BaseMethodParameterNameMismatchAnalyzer.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class BaseMethodParameterNameMismatchAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.BaseMethodParameterNameMismatchAnalyzerID,
			GettextCatalog.GetString ("Parameter name differs in base declaration"),
			GettextCatalog.GetString ("Parameter name differs in base declaration"),
			DiagnosticAnalyzerCategories.CodeQualityIssues,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor (NRefactoryDiagnosticIDs.BaseMethodParameterNameMismatchAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize (AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction (
				(nodeContext) => {
					ScanDiagnostic (nodeContext);
				},
				new SyntaxKind [] { SyntaxKind.IndexerDeclaration, SyntaxKind.MethodDeclaration }
			);
		}

		static void ScanDiagnostic (SyntaxNodeAnalysisContext nodeContext)
		{
			if (nodeContext.IsFromGeneratedCode ())
				return;
			var node1 = nodeContext.Node as IndexerDeclarationSyntax;
			if (node1 != null) {
				var rr = nodeContext.SemanticModel.GetDeclaredSymbol (node1);
				var baseProperty = rr.OverriddenProperty;
				if (baseProperty == null)
					return;
				Check (nodeContext, node1.ParameterList.Parameters, rr.Parameters, baseProperty.Parameters);
			}

			var node2 = nodeContext.Node as MethodDeclarationSyntax;
			if (node2 != null) {
				var rr = nodeContext.SemanticModel.GetDeclaredSymbol (node2);
				var baseMethod = rr.OverriddenMethod;
				if (baseMethod == null)
					return;
				Check (nodeContext, node2.ParameterList.Parameters, rr.Parameters, baseMethod.Parameters);
			}
		}

		static void Check (SyntaxNodeAnalysisContext nodeContext, SeparatedSyntaxList<ParameterSyntax> syntaxParams, ImmutableArray<IParameterSymbol> list1, ImmutableArray<IParameterSymbol> list2)
		{
			var upper = Math.Min (list1.Length, list2.Length);
			for (int i = 0; i < upper; i++) {
				var arg = list1 [i];
				var baseArg = list2 [i];

				if (arg.Name != baseArg.Name) {
					nodeContext.ReportDiagnostic (Diagnostic.Create (
						descriptor.Id,
						descriptor.Category,
						descriptor.MessageFormat,
						descriptor.DefaultSeverity,
						descriptor.DefaultSeverity,
						descriptor.IsEnabledByDefault,
						4,
						descriptor.Title,
						descriptor.Description,
						descriptor.HelpLinkUri,
						Location.Create (nodeContext.SemanticModel.SyntaxTree, syntaxParams [i].Identifier.Span),
						null,
						new [] { baseArg.Name }
					));
				}
			}
		}
	}
}