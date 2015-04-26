//
// InvokeAsExtensionMethodAnalyzer.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Simon Lindgren
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
	public class InvokeAsExtensionMethodAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.InvokeAsExtensionMethodAnalyzerID,
			GettextCatalog.GetString ("If an extension method is called as static method convert it to method syntax"),
			GettextCatalog.GetString ("Convert static method call to extension method call"),
			DiagnosticAnalyzerCategories.Opportunities,
			DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor (NRefactoryDiagnosticIDs.InvokeAsExtensionMethodAnalyzerID)
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
				new SyntaxKind [] { SyntaxKind.InvocationExpression }
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			var node = nodeContext.Node as InvocationExpressionSyntax;
			var semanticModel = nodeContext.SemanticModel;
			var cancellationToken = nodeContext.CancellationToken;

			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;
			var memberReference = node.Expression as MemberAccessExpressionSyntax;
			if (memberReference == null)
				return false;
			var firstArgument = node.ArgumentList.Arguments.FirstOrDefault();
			if (firstArgument == null || firstArgument.Expression.IsKind(SyntaxKind.NullLiteralExpression))
				return false;
			var expressionSymbol = semanticModel.GetSymbolInfo(node.Expression).Symbol as IMethodSymbol;
			//ignore non-extensions and reduced extensions (so a.Ext, as opposed to B.Ext(a))
			if (expressionSymbol == null || !expressionSymbol.IsExtensionMethod || expressionSymbol.MethodKind == MethodKind.ReducedExtension)
				return false;

			diagnostic = Diagnostic.Create (
				descriptor,
				memberReference.Name.GetLocation ()
			);
			return true;
		}
	}
}