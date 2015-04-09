// 
// ReplaceWithSingleCallToLongCount.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2013 Xamarin <http://xamarin.com>
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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ReplaceWithSingleCallToLongCountAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ReplaceWithSingleCallToLongCountAnalyzerID, 
			GettextCatalog.GetString("Redundant Where() call with predicate followed by LongCount()"),
			GettextCatalog.GetString("Replace with single call to 'LongCount()'"), 
			DiagnosticAnalyzerCategories.PracticesAndImprovements, 
			DiagnosticSeverity.Info, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.ReplaceWithSingleCallToLongCountAnalyzerID)
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
				SyntaxKind.InvocationExpression
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			var anyInvoke = nodeContext.Node as InvocationExpressionSyntax;
			var info = nodeContext.SemanticModel.GetSymbolInfo (anyInvoke);

			IMethodSymbol anyResolve = info.Symbol as IMethodSymbol;
			if (anyResolve == null) {
				anyResolve = info.CandidateSymbols.OfType<IMethodSymbol> ().FirstOrDefault (candidate => HasPredicateVersion (candidate));
			}

			if (anyResolve == null || !HasPredicateVersion (anyResolve))
				return false;

			ExpressionSyntax target;
			InvocationExpressionSyntax whereInvoke;
			if (!ReplaceWithSingleCallToAnyAnalyzer.MatchWhere (anyInvoke, out target, out whereInvoke))
				return false;
			info = nodeContext.SemanticModel.GetSymbolInfo (whereInvoke);
			IMethodSymbol whereResolve = info.Symbol as IMethodSymbol;
			if (whereResolve == null) {
				whereResolve = info.CandidateSymbols.OfType<IMethodSymbol> ().FirstOrDefault (candidate => candidate.Name == "Where" && ReplaceWithSingleCallToAnyAnalyzer.IsQueryExtensionClass (candidate.ContainingType));
			}

			if (whereResolve == null || whereResolve.Name != "Where" || !ReplaceWithSingleCallToAnyAnalyzer.IsQueryExtensionClass (whereResolve.ContainingType))
				return false;
			if (whereResolve.Parameters.Length != 1)
				return false;
			var predResolve = whereResolve.Parameters [0];
			if (predResolve.Type.GetTypeParameters ().Length != 2)
				return false;
			diagnostic = Diagnostic.Create (
				descriptor,
				anyInvoke.GetLocation ()
			);
			return true;
		}

		static bool HasPredicateVersion (IMethodSymbol member)
		{
			if (!ReplaceWithSingleCallToAnyAnalyzer.IsQueryExtensionClass (member.ContainingType))
				return false;
			return member.Name == "LongCount";
		}
	}
}