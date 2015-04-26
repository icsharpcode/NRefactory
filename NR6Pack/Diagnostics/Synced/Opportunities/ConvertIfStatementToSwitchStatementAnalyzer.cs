//
// ConvertIfStatementToSwitchStatementAnalyzer.cs
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ICSharpCode.NRefactory6.CSharp.CodeRefactorings;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ConvertIfStatementToSwitchStatementAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ConvertIfStatementToSwitchStatementAnalyzerID, 
			GettextCatalog.GetString("'if' statement can be re-written as 'switch' statement"),
			GettextCatalog.GetString("Convert to 'switch' statement"), 
			DiagnosticAnalyzerCategories.Opportunities, 
			DiagnosticSeverity.Info, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.ConvertIfStatementToSwitchStatementAnalyzerID)
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
				new SyntaxKind[] {  SyntaxKind.IfStatement }
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			var node = nodeContext.Node as IfStatementSyntax;
			var semanticModel = nodeContext.SemanticModel;
			var cancellationToken = nodeContext.CancellationToken;

			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;
			if (node.Parent is IfStatementSyntax || node.Parent is ElseClauseSyntax)
				return false;

			var switchExpr = ConvertIfStatementToSwitchStatementCodeRefactoringProvider.GetSwitchExpression(semanticModel, node.Condition);
			if (switchExpr == null)
				return false;
			var switchSections = new List<SwitchSectionSyntax>();
			if (!ConvertIfStatementToSwitchStatementCodeRefactoringProvider.CollectSwitchSections(switchSections, semanticModel, node, switchExpr))
				return false;
			if (switchSections.Count(s => !s.Labels.OfType<DefaultSwitchLabelSyntax>().Any()) <= 2)
				return false;

			diagnostic = Diagnostic.Create (
				descriptor,
				node.IfKeyword.GetLocation ()
			);
			return true;
		}
	}
}