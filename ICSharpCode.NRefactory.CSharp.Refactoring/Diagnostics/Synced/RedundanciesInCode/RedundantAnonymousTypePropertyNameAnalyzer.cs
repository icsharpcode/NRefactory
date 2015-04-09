//
// RedundantAnonymousTypePropertyNameAnalyzer.cs
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
	public class RedundantAnonymousTypePropertyNameAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.RedundantAnonymousTypePropertyNameAnalyzerID, 
			GettextCatalog.GetString("Redundant explicit property name"),
			GettextCatalog.GetString("The name can be inferred from the initializer expression"), 
			DiagnosticAnalyzerCategories.RedundanciesInCode, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.RedundantAnonymousTypePropertyNameAnalyzerID),
			customTags: DiagnosticCustomTags.Unnecessary
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				nodeContext => {
					GetDiagnostics (nodeContext);
				}, 
				new SyntaxKind[] {  SyntaxKind.AnonymousObjectCreationExpression }
			);
		}

		static void GetDiagnostics (SyntaxNodeAnalysisContext nodeContext)
		{
			var node = nodeContext.Node as AnonymousObjectCreationExpressionSyntax;

			foreach (var expr in node.Initializers) {
				if (expr.NameEquals == null || expr.NameEquals.Name == null)
					continue;

				if (expr.NameEquals.Name.ToString() == GetAnonymousTypePropertyName(expr.Expression)) {
					nodeContext.ReportDiagnostic (Diagnostic.Create(descriptor, expr.NameEquals.GetLocation()));
				}
			}
		}

		static string GetAnonymousTypePropertyName(SyntaxNode expr)
		{
			var mAccess = expr as MemberAccessExpressionSyntax;
			return mAccess != null ? mAccess.Name.ToString() : expr.ToString();
		}
	}
}