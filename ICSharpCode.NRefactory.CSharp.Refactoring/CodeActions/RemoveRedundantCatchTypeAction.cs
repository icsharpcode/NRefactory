//
// RemoveRedundantCatchTypeAction.cs
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
using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Removes a redundant exception type specifier from catch clauses")]
	[ExportCodeRefactoringProvider("Remove redundant type", LanguageNames.CSharp)]
	public class RemoveRedundantCatchTypeAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
		}
//		public override System.Collections.Generic.IEnumerable<CodeAction> GetActions(SemanticModel context)
//		{
//			var catchClause = context.GetNode<CatchClause>();
//			if (catchClause == null)
//				yield break;
//			if (catchClause.Type.IsNull)
//				yield break;
//			var exceptionType = context.ResolveType(catchClause.Type);
//			if (exceptionType != context.Compilation.FindType(typeof(Exception)))
//				yield break;
//			var syntaxTree = context.RootNode as SyntaxTree;
//			if (syntaxTree == null)
//				yield break;
//			var exceptionIdentifierRR = context.Resolve(catchClause.VariableNameToken) as LocalResolveResult;
//			if (exceptionIdentifierRR != null &&
//				IsReferenced(exceptionIdentifierRR.Variable, catchClause.Body, syntaxTree, context))
//				yield break;
//			yield return new CodeAction(context.TranslateString("Remove type specifier"), script => {
//				script.Replace(catchClause, new CatchClause() {
//					Body = catchClause.Body.Clone() as BlockStatement
//				});
//			}, catchClause.Type);
//		}
//
//		bool IsReferenced(IVariable variable, AstNode node, SyntaxTree syntaxTree, SemanticModel context)
//		{
//			int referencesFound = 0;
//			var findRef = new FindReferences();
//			findRef.FindLocalReferences(variable, context.UnresolvedFile, syntaxTree, context.Compilation, (n, entity) => {
//				referencesFound++;
//			}, CancellationToken.None);
//
//			// One reference is the declaration, and that does not count
//			return referencesFound > 1;
//		}
	}
}

