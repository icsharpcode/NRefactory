//
// RoslynRecommendationsCompletionContextHandler.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Threading.Tasks;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{

	//	public class CompletionEngineCache
	//	{
	//		public List<INamespace>  namespaces;
	//		public ICompletionData[] importCompletion;
	//	}

	class RoslynRecommendationsCompletionContextHandler : CompletionContextHandler
	{
		static bool IsAttribute (ITypeSymbol type)
		{
			// todo: better attribute recognition test.
			return type.Name.EndsWith ("Attribute", StringComparison.Ordinal);
		}

		bool IsException (ITypeSymbol type)
		{
			if (type == null)
				return false;
			if (type.Name == "Exception" && type.ContainingNamespace.Name == "System")
				return true;
			return IsException (type.BaseType);
		}

		public async override Task<IEnumerable<ICompletionData>> GetCompletionDataAsync (CompletionResult completionResult, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, CancellationToken cancellationToken)
		{
			var ctx = await completionContext.GetSyntaxContextAsync (engine.Workspace, cancellationToken).ConfigureAwait (false);
			var semanticModel = await completionContext.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
			var result = new List<ICompletionData> ();

			var parent = ctx.TargetToken.Parent;
			bool isInAttribute = parent != null && (parent.IsKind (SyntaxKind.AttributeList) ||
								 parent.Parent != null && parent.IsKind (SyntaxKind.QualifiedName) && parent.Parent.IsKind (SyntaxKind.Attribute));
			bool isInBaseList = parent != null && parent.IsKind (SyntaxKind.BaseList);
			bool isInUsingDirective = parent != null && parent.Parent != null && parent.Parent.IsKind (SyntaxKind.UsingDirective);
			var completionDataLookup = new Dictionary<Tuple<string, SymbolKind>, ISymbolCompletionData> ();
			bool isInCatchTypeExpression = parent.IsKind (SyntaxKind.CatchDeclaration) ||
				parent.IsKind (SyntaxKind.QualifiedName) && parent != null && parent.Parent.IsKind (SyntaxKind.CatchDeclaration);

			Action<ISymbolCompletionData> addData = d => {
				var key = Tuple.Create (d.DisplayText, d.Symbol.Kind);
				ISymbolCompletionData data;
				if (completionDataLookup.TryGetValue (key, out data)) {
					data.AddOverload (d);
					return;
				}
				completionDataLookup.Add (key, d);
				result.Add (d);
			};

			var completionCategoryLookup = new Dictionary<ITypeSymbol, ICompletionCategory> ();
			foreach (var symbol in Recommender.GetRecommendedSymbolsAtPosition (semanticModel, completionContext.Position, engine.Workspace, null, cancellationToken)) {
				if (symbol.Kind == SymbolKind.NamedType) {
					if (isInAttribute) {
						var type = (ITypeSymbol)symbol;
						if (IsAttribute (type)) {
							addData (engine.Factory.CreateSymbolCompletionData (this, symbol, type.Name.Substring (0, type.Name.Length - "Attribute".Length)));
						}
					}
					if (isInBaseList) {
						var type = (ITypeSymbol)symbol;
						if (type.IsSealed || type.IsStatic)
							continue;
					}
					if (isInCatchTypeExpression) {
						var type = (ITypeSymbol)symbol;
						if (!IsException (type))
							continue;
					}
				}

				if (isInUsingDirective && symbol.Kind != SymbolKind.Namespace)
					continue;
				var newData = engine.Factory.CreateSymbolCompletionData (this, symbol);
				var categorySymbol = (ISymbol)symbol.ContainingType ?? symbol.ContainingNamespace;
				if (categorySymbol != null) {
					ICompletionCategory category;
					if (completionCategoryLookup.TryGetValue (symbol.ContainingType, out category)) {
						completionCategoryLookup [symbol.ContainingType] = category = engine.Factory.CreateCompletionDataCategory (categorySymbol);
					}
					newData.CompletionCategory = category;
				}
				addData (newData);
			}
			return result;
		}
	}
}