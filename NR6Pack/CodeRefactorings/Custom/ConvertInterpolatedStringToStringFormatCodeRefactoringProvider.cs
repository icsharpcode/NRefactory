//
// ConvertInterpolatedStringToStringFormatCodeRefactoringProvider.cs
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

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "Convert string interpolation to 'string.Format'")]
	public class ConvertInterpolatedStringToStringFormatCodeRefactoringProvider : CodeRefactoringProvider
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
			var node = root.FindToken(span.Start).Parent as InterpolatedStringTextSyntax;
			if (node == null)
				return;
			context.RegisterRefactoring(
				CodeActionFactory.Create(
					node.Span, 
					DiagnosticSeverity.Info, 
					GettextCatalog.GetString ("To format string"), 
					t => {
						var newRoot = root.ReplaceNode (node.Parent, CreateFormatString (node));
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
			);
		}

		SyntaxNode CreateFormatString (InterpolatedStringTextSyntax node)
		{
			var sb = new StringBuilder ();
			sb.Append ("string.Format (\"");
			var stringExpressions = new List<ExpressionSyntax> ();
			var parent = node.Parent as InterpolatedStringExpressionSyntax;
			foreach (var child in parent.Contents) {
				var kind = child.Kind ();
				switch (kind) {
					case SyntaxKind.InterpolatedStringText:
						           sb.Append (((InterpolatedStringTextSyntax)child).TextToken.Value);
					break;
					case SyntaxKind.Interpolation:
						           var interpolation = child as InterpolationSyntax;
						sb.Append ("{");

						int index = -1;
						for (int i = 0; i < stringExpressions.Count; i++) {
							if (stringExpressions[i].IsEquivalentTo (interpolation.Expression)) {
								index = i;
								break;
							}
						}
						if (index < 0) {
							index = stringExpressions.Count;
							stringExpressions.Add (interpolation.Expression);
						}

						sb.Append (index);

						if (interpolation.FormatClause != null)
							sb.Append (interpolation.FormatClause);
						sb.Append ("}");
					break;
				}
			}

			sb.Append ("\"");

			for (int i = 0; i < stringExpressions.Count; i++) {
				sb.Append (", ");
				sb.Append (stringExpressions[i]);
			}
			sb.Append (")");
			return SyntaxFactory.ParseExpression (sb.ToString ()).WithAdditionalAnnotations (Formatter.Annotation);
		}
	}
}

