//
// OverrideContextHandler.cs
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


using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	class OverrideContextHandler : CompletionContextHandler
	{
		public override bool IsTriggerCharacter (SourceText text, int position)
		{
			return IsTriggerAfterSpaceOrStartOfWordCharacter (text, position);
		}

		public async override Task<IEnumerable<ICompletionData>> GetCompletionDataAsync (CompletionResult completionResult, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, CancellationToken cancellationToken)
		{
			var ctx = await completionContext.GetSyntaxContextAsync (engine.Workspace, cancellationToken).ConfigureAwait (false);
			var result = new List<ICompletionData> ();

			var incompleteMemberSyntax = ctx.TargetToken.Parent as IncompleteMemberSyntax;
			if (incompleteMemberSyntax != null) {
				var mod = incompleteMemberSyntax.Modifiers.LastOrDefault();
				if (!mod.IsKind(SyntaxKind.OverrideKeyword))
					return result;
			} else {
				return result;
			}

			if (ctx.ContainingTypeDeclaration == null)
				return result;
			var curType = ctx.GetCurrentType(await completionContext.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false));
			if (curType == null)
				return result;
			
			foreach (var m in curType.BaseType.GetMembers ()) {
				if (!m.IsOverride && !m.IsVirtual || m.ContainingType == curType)
					continue;
				// filter out the "Finalize" methods, because finalizers should be done with destructors.
				if (m.Kind == SymbolKind.Method && m.Name == "Finalize") {
					continue;
				}

				// check if the member is already implemented
				bool foundMember = curType.GetMembers().Any(cm => HasOverridden (m, cm));
				if (foundMember)
					continue;

				/*				if (alreadyInserted.Contains(m))
					continue;
				alreadyInserted.Add(m);*/

				var data = engine.Factory.CreateNewOverrideCompletionData(
					this,
					incompleteMemberSyntax.SpanStart,
					curType,
					m
				);
				//data.CompletionCategory = col.GetCompletionCategory(m.DeclaringTypeDefinition);
				result.Add (data); 
			}
			return result;
		}

		static bool HasOverridden(ISymbol original, ISymbol testSymbol)
		{
			if (original.Kind != testSymbol.Kind)
				return false;
			switch (testSymbol.Kind) {
			case SymbolKind.Method:
				return ((IMethodSymbol)testSymbol).OverriddenMethod == original;
			case SymbolKind.Property:
				return ((IPropertySymbol)testSymbol).OverriddenProperty == original;
			case SymbolKind.Event:
				return ((IEventSymbol)testSymbol).OverriddenEvent == original;
			}
			return false;
		}

	}
}

