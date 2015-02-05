//
// InsertionAction.cs
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
using System.Threading;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.CaseCorrection;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	public sealed class InsertionAction : NRefactoryCodeAction
	{
		readonly string title;
		readonly Func<CancellationToken, Task<InsertionResult>> createInsertion;

		public override string Title {
			get {
				return title;
			}
		}

		public InsertionAction(TextSpan textSpan, DiagnosticSeverity severity, string title, Func<CancellationToken, Task<InsertionResult>> createInsertion) : base(textSpan, severity)
		{
			this.title = title;
			this.createInsertion = createInsertion;
		}

		protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
		{
			return createInsertion.Invoke (cancellationToken).ContinueWith (t => CreateChangedDocument (t, cancellationToken).Result);
		}

		static async Task<Document> CreateChangedDocument (Task<InsertionResult> task, CancellationToken cancellationToken)
		{
			var insertionResult = task.Result;
			var document = insertionResult.Context.Document;
			var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait (false);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait (false);
			var node = root.FindNode (task.Result.Context.Span).AncestorsAndSelf ().OfType<BaseTypeDeclarationSyntax> ().FirstOrDefault ();

			var newRoot = root.InsertNodesBefore (node.ChildNodes ().First (n => n is MemberDeclarationSyntax && n.Span.End > insertionResult.Context.Span.Start), new [] { insertionResult.Node.WithAdditionalAnnotations (Formatter.Annotation) });

			return document.WithSyntaxRoot (newRoot);
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

