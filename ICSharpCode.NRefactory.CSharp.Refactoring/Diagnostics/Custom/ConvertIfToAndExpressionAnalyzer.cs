//
// ConvertIfToAndExpressionAnalyzer.cs
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
	public class ConvertIfToAndExpressionAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ConvertIfToAndExpressionAnalyzerID,
			GettextCatalog.GetString ("Convert 'if' to '&&' expression"),
			"{0}",
			DiagnosticAnalyzerCategories.PracticesAndImprovements,
			DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor (NRefactoryDiagnosticIDs.ConvertIfToAndExpressionAnalyzerID)
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
				new SyntaxKind [] { SyntaxKind.IfStatement }
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;
			var node = nodeContext.Node as IfStatementSyntax;
			ExpressionSyntax target;
			SyntaxTriviaList assignmentTrailingTriviaList;
			if (ConvertIfToOrExpressionAnalyzer.MatchIfElseStatement (node, SyntaxKind.FalseLiteralExpression, out target, out assignmentTrailingTriviaList)) {
				var varDeclaration = ConvertIfToOrExpressionAnalyzer.FindPreviousVarDeclaration (node);
				if (varDeclaration != null) {
					var targetIdentifier = target as IdentifierNameSyntax;
					if (targetIdentifier == null)
						return false;
					var declaredVarName = varDeclaration.Declaration.Variables.First ().Identifier.Value;
					var assignedVarName = targetIdentifier.Identifier.Value;
					if (declaredVarName != assignedVarName)
						return false;
					if (!ConvertIfToOrExpressionAnalyzer.CheckTarget (targetIdentifier, node.Condition))
						return false;
					diagnostic = Diagnostic.Create (descriptor, node.IfKeyword.GetLocation (), GettextCatalog.GetString ("Convert to '&&' expression"));
					return true;
				} else {
					if (!ConvertIfToOrExpressionAnalyzer.CheckTarget (target, node.Condition))
						return false;
					diagnostic = Diagnostic.Create (descriptor, node.IfKeyword.GetLocation (), GettextCatalog.GetString ("Replace with '&='"));
					return true;
				}
			}
			return false;
		}
	}

}