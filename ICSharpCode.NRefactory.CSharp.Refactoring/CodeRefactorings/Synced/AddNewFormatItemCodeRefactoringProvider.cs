// 
// IntroduceFormatItem.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	/// <summary>
	/// Introduce format item. Works on strings that contain selections.
	/// "this is <some> string" => string.Format ("this is {0} string", <some>)
	/// </summary>
	[NRefactoryCodeRefactoringProvider(Description = "Creates a string.format call with the selection as parameter")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Introduce format item")]
	public class AddNewFormatItemCodeRefactoringProvider : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
				return;
			var span = context.Span;
			if (span.IsEmpty)
				return;
			var cancellationToken = context.CancellationToken;
			if (cancellationToken.IsCancellationRequested)
				return;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);

			var token = root.FindToken(span.Start);
			if (!token.IsKind(SyntaxKind.StringLiteralToken) || token.Span.End < span.End)
				return;
			//			if (pexpr.LiteralValue.StartsWith("@", StringComparison.Ordinal)) {
			//				if (!(pexpr.StartLocation < new TextLocation(context.Location.Line, context.Location.Column - 1) && new TextLocation(context.Location.Line, context.Location.Column + 1) < pexpr.EndLocation)) {
			//					yield break;
			//				}
			//			} else {
			//				if (!(pexpr.StartLocation < context.Location && context.Location < pexpr.EndLocation)) {
			//					yield break;
			//				}
			//			}

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Info, 
					"Insert format argument", 
					t2 => {
						var parent = token.Parent;
						var tokenText = token.ToString();

						int argumentNumber = 0;
						InvocationExpressionSyntax invocationExpression = null;
						if (parent.Parent.IsKind(SyntaxKind.Argument)) {
							invocationExpression = (InvocationExpressionSyntax)parent.Parent.Parent.Parent;
							var info = model.GetSymbolInfo(invocationExpression);
							var method = info.Symbol as IMethodSymbol;
							if (method.Name == "Format" && method.ContainingType.SpecialType == SpecialType.System_String) {
								argumentNumber = invocationExpression.ArgumentList.Arguments.Count - 1;
							} else {
								invocationExpression = null;
							}
						}

						var endOffset = span.End - token.SpanStart - 2;
						string formatText = tokenText.Substring(1, span.Start - token.SpanStart - 1) + "{" + argumentNumber + "}" + tokenText.Substring(endOffset, tokenText.Length - endOffset - 1);

						string argumentText = tokenText.Substring(span.Start - token.SpanStart, span.Length - 2);

						InvocationExpressionSyntax newInvocation;
						if (invocationExpression != null) {
							parent = invocationExpression;
							var argumentList = new List<ArgumentSyntax> ();
							argumentList.Add(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(formatText))));
							argumentList.AddRange(invocationExpression.ArgumentList.Arguments.Skip(1));
							argumentList.Add(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(argumentText))));
							newInvocation = invocationExpression.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(argumentList)));
						} else {
							newInvocation = SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("string.Format"), SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(new[] {
								SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(formatText))),
								SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(argumentText)))
							})));
						}

						return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode((SyntaxNode)parent, newInvocation.WithAdditionalAnnotations(Formatter.Annotation))));
					}
				)
			);
		}
	}
}