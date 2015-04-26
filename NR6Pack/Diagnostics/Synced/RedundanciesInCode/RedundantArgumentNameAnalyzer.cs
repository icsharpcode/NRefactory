// 
// RedundantArgumentNameAnalyzer.cs
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class RedundantArgumentNameAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.RedundantArgumentNameAnalyzerID, 
			GettextCatalog.GetString("Redundant explicit argument name specification"),
			GettextCatalog.GetString("Redundant argument name specification"), 
			DiagnosticAnalyzerCategories.RedundanciesInCode, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.RedundantArgumentNameAnalyzerID),
			customTags: DiagnosticCustomTags.Unnecessary
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				nodeContext => {
					GetDiagnostics (nodeContext, ((InvocationExpressionSyntax)nodeContext.Node).ArgumentList?.Arguments);
				}, 
				new SyntaxKind[] {  SyntaxKind.InvocationExpression }
			);
			context.RegisterSyntaxNodeAction(
				nodeContext => {
					GetDiagnostics (nodeContext, ((ElementAccessExpressionSyntax)nodeContext.Node).ArgumentList?.Arguments);
				}, 
				new SyntaxKind[] {  SyntaxKind.ElementAccessExpression }
			);
			context.RegisterSyntaxNodeAction(
				nodeContext => {
					GetDiagnostics (nodeContext, ((ObjectCreationExpressionSyntax)nodeContext.Node).ArgumentList?.Arguments);
				}, 
				new SyntaxKind[] {  SyntaxKind.ObjectCreationExpression }
			);

			context.RegisterSyntaxNodeAction(
				nodeContext => {
					GetDiagnostics (nodeContext, ((AttributeSyntax)nodeContext.Node).ArgumentList?.Arguments);
				}, 
				new SyntaxKind[] {  SyntaxKind.Attribute }
			);
		}

		static void GetDiagnostics (SyntaxNodeAnalysisContext nodeContext, SeparatedSyntaxList<ArgumentSyntax>? arguments)
		{
			if (nodeContext.IsFromGeneratedCode ())
				return;
			
			if (!arguments.HasValue)
				return;

			var node = nodeContext.Node;
			CheckParameters(nodeContext, nodeContext.SemanticModel.GetSymbolInfo (node).Symbol, arguments.Value);
		}

		static void GetDiagnostics (SyntaxNodeAnalysisContext nodeContext, SeparatedSyntaxList<AttributeArgumentSyntax>? arguments)
		{
			if (nodeContext.IsFromGeneratedCode ())
				return;
			
			if (!arguments.HasValue)
				return;

			var node = nodeContext.Node;
			CheckParameters(nodeContext, nodeContext.SemanticModel.GetSymbolInfo (node).Symbol, arguments.Value);
		}

		static void CheckParameters(SyntaxNodeAnalysisContext nodeContext, ISymbol ir, IEnumerable<ArgumentSyntax> arguments)
		{
			if (ir == null)
				return;
			var parameters = ir.GetParameters();
			int i = 0;

			foreach (var arg in arguments) {
				var na = arg.NameColon;
				if (na != null) {
					if (i >= parameters.Length || na.Name.ToString() != parameters[i].Name)
						break;
					nodeContext.ReportDiagnostic (Diagnostic.Create (descriptor, na.GetLocation()));
				}
				i++;
			}
		}

		static void CheckParameters(SyntaxNodeAnalysisContext nodeContext, ISymbol ir, IEnumerable<AttributeArgumentSyntax> arguments)
		{
			if (ir == null)
				return;
			var parameters = ir.GetParameters();
			int i = 0;

			foreach (var arg in arguments) {
				var na = arg.NameColon;
				if (na != null) {
					if (i >= parameters.Length || na.Name.ToString() != parameters[i].Name)
						break;
					nodeContext.ReportDiagnostic (Diagnostic.Create (descriptor, na.GetLocation()));
				}
				i++;
			}
		}
	}
}