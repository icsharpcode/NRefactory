//
// NameOfSuggestionAnalyzer.cs
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NameOfSuggestionAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.NameOfSuggestionAnalyzerID, 
			GettextCatalog.GetString("Suggest the usage of the nameof operator"),
			GettextCatalog.GetString("Use 'nameof({0})' expression instead."), 
			DiagnosticAnalyzerCategories.CodeQualityIssues, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.NameOfSuggestionAnalyzerID)
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
				new SyntaxKind[] { SyntaxKind.ObjectCreationExpression }
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;

			var options = nodeContext.SemanticModel.SyntaxTree.Options as CSharpParseOptions;
			if (options != null && options.LanguageVersion < LanguageVersion.CSharp6)
				return false;

			var objectCreateExpression = nodeContext.Node as ObjectCreationExpressionSyntax;

			ExpressionSyntax paramNode;
			if (!CheckExceptionType (nodeContext.SemanticModel, objectCreateExpression, out paramNode))
				return false;
			var paramName = NotResolvedInTextAnalyzer.GetArgumentParameterName (paramNode);
			if (paramName == null)
				return false;

			var validNames = NotResolvedInTextAnalyzer.GetValidParameterNames (objectCreateExpression);

			if (!validNames.Contains (paramName))
				return false;
			
			diagnostic = Diagnostic.Create (descriptor, paramNode.GetLocation (), paramName);
			return true;
		}

		internal static bool CheckExceptionType(SemanticModel model, ObjectCreationExpressionSyntax objectCreateExpression, out ExpressionSyntax paramNode)
		{
			paramNode = null;
			var type = model.GetTypeInfo(objectCreateExpression).Type;
			if (type == null)
				return false;
			if (type.Name == typeof(ArgumentException).Name) {
				if (objectCreateExpression.ArgumentList.Arguments.Count >= 2) {
					paramNode = objectCreateExpression.ArgumentList.Arguments[1].Expression;
				}
				return paramNode != null;
			}
			if (type.Name == typeof(ArgumentNullException).Name ||
			    type.Name == typeof(ArgumentOutOfRangeException).Name ||
			    type.Name == typeof(DuplicateWaitObjectException).Name) {
				if (objectCreateExpression.ArgumentList.Arguments.Count >= 1) {
					paramNode = objectCreateExpression.ArgumentList.Arguments[0].Expression;
				}
				return paramNode != null;
			}
			return false;
		}
	}
}

