//
// EmptyGeneralCatchClauseAnalyzer.cs
//
// Author:
//       Ji Kun <jikun.nus@gmail.com>
//
// Copyright (c) 2013 Ji Kun <jikun.nus@gmail.com>
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
	/// A catch clause that catches System.Exception and has an empty body
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class EmptyGeneralCatchClauseAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.EmptyGeneralCatchClauseAnalyzerID, 
			GettextCatalog.GetString("A catch clause that catches System.Exception and has an empty body"),
			GettextCatalog.GetString("Empty general catch clause suppresses any error"), 
			DiagnosticAnalyzerCategories.CodeQualityIssues, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.EmptyGeneralCatchClauseAnalyzerID)
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
				SyntaxKind.CatchClause
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;
			var node = nodeContext.Node as CatchClauseSyntax;
			if (node.Declaration == null) {
				diagnostic = Diagnostic.Create (
					descriptor,
					node.CatchKeyword.GetLocation ()
				);
				return true;
			} else {
				var type = node.Declaration.Type;
				if (type != null) {
					var typeSymbol = nodeContext.SemanticModel.GetTypeInfo (type).Type;
					if (typeSymbol == null || typeSymbol.TypeKind == TypeKind.Error || !typeSymbol.GetFullName ().Equals ("System.Exception"))
						return false;

					BlockSyntax body = node.Block;
					if (body.Statements.Any ())
						return false;

					diagnostic = Diagnostic.Create (
						descriptor,
						node.CatchKeyword.GetLocation ()
					);
					return true;
				}
			}
			return false;
		}
	}
}