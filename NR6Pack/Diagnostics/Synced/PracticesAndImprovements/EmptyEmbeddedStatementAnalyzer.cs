//
// Analyzer.cs
//
// Author:
//       Ciprian Khlud <ciprian.mustiata@yahoo.com>
//
// Copyright (c) 2013 Ciprian Khlud
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
	public class EmptyEmbeddedStatementAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.EmptyEmbeddedStatementAnalyzerID, 
			GettextCatalog.GetString("Empty control statement body"),
			GettextCatalog.GetString("';' should be avoided. Use '{}' instead"), 
			DiagnosticAnalyzerCategories.PracticesAndImprovements, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.EmptyEmbeddedStatementAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryGetDiagnosticForWhile(nodeContext, out diagnostic))
						nodeContext.ReportDiagnostic(diagnostic);
				},
				SyntaxKind.WhileStatement
			);

			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryGetDiagnosticForForeach(nodeContext, out diagnostic))
						nodeContext.ReportDiagnostic(diagnostic);
				},
				SyntaxKind.ForEachStatement
			);

			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryGetDiagnosticForIf(nodeContext, out diagnostic))
						nodeContext.ReportDiagnostic(diagnostic);
				},
				SyntaxKind.IfStatement
			);

			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryGetDiagnosticForFor(nodeContext, out diagnostic))
						nodeContext.ReportDiagnostic(diagnostic);
				},
				SyntaxKind.ForStatement
			);

		}

		static bool TryGetDiagnosticForWhile (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;
			var node = nodeContext.Node as WhileStatementSyntax;

			if (!Check(node.Statement))
				return false;

			diagnostic = Diagnostic.Create (
				descriptor,
				node.Statement.GetLocation ()
			);
			return true;
		}

		static bool TryGetDiagnosticForForeach (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;
			var node = nodeContext.Node as ForEachStatementSyntax;

			if (!Check(node.Statement))
				return false;

			diagnostic = Diagnostic.Create (
				descriptor,
				node.Statement.GetLocation ()
			);
			return true;
		}

		static bool TryGetDiagnosticForIf (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;
			var node = nodeContext.Node as IfStatementSyntax;

			if (Check (node.Statement)) {
				diagnostic = Diagnostic.Create (
					descriptor,
					node.Statement.GetLocation ()
				);
				return true;
			}

			if (node.Else != null && Check (node.Else.Statement)) {
				diagnostic = Diagnostic.Create (
					descriptor,
					node.Else.Statement.GetLocation ()
				);
				return true;
			}

			return false;
		}

		static bool TryGetDiagnosticForFor (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;
			var node = nodeContext.Node as ForStatementSyntax;

			if (!Check(node.Statement))
				return false;

			diagnostic = Diagnostic.Create (
				descriptor,
				node.Statement.GetLocation ()
			);
			return true;
		}


		static bool Check(SyntaxNode body)
		{
			if (body == null || !body.IsKind(SyntaxKind.EmptyStatement))
				return false;
			return true;
		}
	}
}