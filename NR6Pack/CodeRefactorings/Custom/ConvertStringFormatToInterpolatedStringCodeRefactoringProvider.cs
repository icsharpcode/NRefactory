//
// ConvertStringFormatToInterpolatedStringCodeRefactoringProvider.cs
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
using ICSharpCode.NRefactory6.CSharp.Diagnostics;
using System.Diagnostics;
using System.Text;
using System.Linq.Expressions;

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "Convert 'string.Format' to string interpolation")]
	public class ConvertStringFormatToInterpolatedStringCodeRefactoringProvider : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync (CodeRefactoringContext context)
		{
			var document = context.Document;
			if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
				return;
			var span = context.Span;
			if (!span.IsEmpty)
				return;
			var cancellationToken = context.CancellationToken;
			if (cancellationToken.IsCancellationRequested)
				return;
			var model = await document.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
			if (model.IsFromGeneratedCode (cancellationToken))
				return;
			var root = await model.SyntaxTree.GetRootAsync (cancellationToken).ConfigureAwait (false);
			var node = root.FindToken(span.Start).Parent;

			var invocation = node?.Parent?.Parent as InvocationExpressionSyntax;
			if (invocation == null)
				return;
			var target = model.GetSymbolInfo (invocation.Expression).Symbol;
			if (target == null || target.Name != "Format" || target.ContainingType.SpecialType != SpecialType.System_String)
				return;
			context.RegisterRefactoring(
				CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Info, 
					GettextCatalog.GetString ("To interpolated string"), 
					t => {
						var newRoot = root.ReplaceNode (invocation, CreateInterpolatedString (invocation));
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
			);
		}

		SyntaxNode CreateInterpolatedString (InvocationExpressionSyntax invocation)
		{
			var expr = invocation.ArgumentList.Arguments [0].Expression as LiteralExpressionSyntax;

			var str = expr.Token.Value.ToString ();
			var stringFormatDigits = new StringBuilder ();
			var sb = new StringBuilder ();
			sb.Append ("$\"");

			bool inStringFormat = false;
			for (int i = 0; i < str.Length; i++) {
				var ch = str [i];

				if (ch == '{') {
					inStringFormat = true;
					stringFormatDigits.Length = 0;
				} else if (ch == '}' || ch == ':') {
					if (inStringFormat) {
						if (stringFormatDigits.Length > 0) {
							try {
								var argNum = int.Parse (stringFormatDigits.ToString ());
								if (argNum + 1 < invocation.ArgumentList.Arguments.Count) {
									sb.Append (invocation.ArgumentList.Arguments [argNum + 1].Expression);
								} else {
									sb.Append (stringFormatDigits.ToString ());
								}
							} catch (Exception) {
							}
						}
						inStringFormat = false;
					}
				} else if (inStringFormat && char.IsDigit (ch)) {
					stringFormatDigits.Append (ch);
					continue;
				}

				sb.Append (ch);
			}

			sb.Append ("\"");
			return SyntaxFactory.ParseExpression (sb.ToString ());
		}
	}
}