//
// CallToStaticMemberViaDerivedTypeAnalyzer.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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
	public class AccessToStaticMemberViaDerivedTypeAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.AccessToStaticMemberViaDerivedTypeAnalyzerID, 
			GettextCatalog.GetString("Suggests using the class declaring a static function when calling it"),
			GettextCatalog.GetString("Call to static member via a derived class"), 
			DiagnosticAnalyzerCategories.PracticesAndImprovements, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.AccessToStaticMemberViaDerivedTypeAnalyzerID)
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
				SyntaxKind.SimpleMemberAccessExpression
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			var node = nodeContext.Node as MemberAccessExpressionSyntax;

			if (node.Expression.IsKind(SyntaxKind.ThisExpression) || node.Expression.IsKind(SyntaxKind.BaseExpression))
				// Call within current class scope using 'this' or 'base'
				return false;
			var memberResolveResult = nodeContext.SemanticModel.GetSymbolInfo(node);
			if (memberResolveResult.Symbol == null)
				return false;
			if (!memberResolveResult.Symbol.IsStatic)
				return false;

			var typeInfo = nodeContext.SemanticModel.GetTypeInfo (node.Expression);
			if (memberResolveResult.Symbol == null || typeInfo.Type == null)
				return false;
			if (!memberResolveResult.Symbol.IsStatic)
				return false;

			if (memberResolveResult.Symbol.ContainingType.Equals(typeInfo.Type))
				return false;

			// check whether member.DeclaringType contains the original type
			// (curiously recurring template pattern)
			if (CheckCuriouslyRecurringTemplatePattern(memberResolveResult.Symbol.ContainingType, typeInfo.Type))
				return false;

			diagnostic = Diagnostic.Create (
				descriptor,
				node.Expression.GetLocation ()
			);
			return true;
		}

		static bool CheckCuriouslyRecurringTemplatePattern(ITypeSymbol containingType, ITypeSymbol type)
		{
			if (containingType.Equals(type))
				return true;
			var nt = containingType as INamedTypeSymbol;
			if (nt == null)
				return false;
			foreach (var typeArg in nt.TypeArguments) {
				if (CheckCuriouslyRecurringTemplatePattern(typeArg, type))
					return true;
			}
			return false;
		}
	}
}