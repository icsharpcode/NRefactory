//
// DocumentChangeAction.cs
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
using System.Threading;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.CaseCorrection;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CodeActions;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	public sealed class DocumentChangeAction : NRefactoryCodeAction
	{
		readonly string title;
		readonly Func<CancellationToken, Task<Document>> createChangedDocument;

		public override string Title {
			get {
				return title;
			}
		}

		public DocumentChangeAction(TextSpan textSpan, DiagnosticSeverity severity, string title, Func<CancellationToken, Task<Document>> createChangedDocument) : base(textSpan, severity)
		{
			this.title = title;
			this.createChangedDocument = createChangedDocument;
		}

		protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
		{
			var task = createChangedDocument.Invoke (cancellationToken);
			return task;
		}

		protected override async Task<Document> PostProcessChangesAsync(Document document, CancellationToken cancellationToken)
		{
			document = await Simplifier.ReduceAsync(document, Simplifier.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);

			var options = document.Project.Solution.Workspace.Options;

			options = options.WithChangedOption(CSharpFormattingOptions.SpaceWithinSquareBrackets, false);
			options = options.WithChangedOption(CSharpFormattingOptions.SpaceBetweenEmptySquareBrackets, false);
			options = options.WithChangedOption(CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses, false);
			options = options.WithChangedOption(CSharpFormattingOptions.SpaceBetweenEmptyMethodDeclarationParentheses, false);
			options = options.WithChangedOption(CSharpFormattingOptions.SpaceWithinOtherParentheses, false);

			document = await Formatter.FormatAsync(document, Formatter.Annotation, options: options, cancellationToken: cancellationToken).ConfigureAwait(false);

			document = await CaseCorrector.CaseCorrectAsync(document, CaseCorrector.Annotation, cancellationToken).ConfigureAwait(false);
			return document;
		}
	}
}