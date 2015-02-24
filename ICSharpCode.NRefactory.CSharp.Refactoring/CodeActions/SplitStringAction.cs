// 
// SplitString.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
	[NRefactoryCodeRefactoringProvider(Description = "Splits string literal into two")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Split string literal")]
	public class SplitStringAction : SpecializedCodeAction<LiteralExpressionSyntax>
	{
		protected override IEnumerable<CodeAction> GetActions(Document document, SemanticModel semanticModel, SyntaxNode root, TextSpan span, LiteralExpressionSyntax node, CancellationToken cancellationToken)
		{
			if (!node.IsKind(SyntaxKind.StringLiteralExpression))
				yield break;
//			yield return new CodeActionFactory.Create(
//				node.Span,
//				DiagnosticSeverity.Info,
//				"Split string literal",
//				t2 => {
//					var parent = block.Parent.ReplaceNode((SyntaxNode)block, block.Statements.First())
//						.WithAdditionalAnnotations(Formatter.Annotation);
//
//					var newRoot = root.ReplaceNode((SyntaxNode)node, parent);
//					return Task.FromResult(document.WithSyntaxRoot(newRoot));
//				}
//			);

			//			if (pexpr.LiteralValue.StartsWith("@", StringComparison.Ordinal)) {
			//				if (!(pexpr.StartLocation < new TextLocation(context.Location.Line, context.Location.Column - 2) &&
			//					new TextLocation(context.Location.Line, context.Location.Column + 2) < pexpr.EndLocation)) {
			//					yield break;
			//				}
			//			} else {
			//				if (!(pexpr.StartLocation < new TextLocation(context.Location.Line, context.Location.Column - 1) && new TextLocation(context.Location.Line, context.Location.Column + 1) < pexpr.EndLocation)) {
			//					yield break;
			//				}
			//			}
			//
			//			yield return new CodeAction(context.TranslateString(""), script => {
			//				int offset = context.GetOffset (context.Location);
			//				script.InsertText (offset, pexpr.LiteralValue.StartsWith("@", StringComparison.Ordinal) ? "\" + @\"" : "\" + \"");
			//			}, pexpr);
			//		}

		}
	}
}
