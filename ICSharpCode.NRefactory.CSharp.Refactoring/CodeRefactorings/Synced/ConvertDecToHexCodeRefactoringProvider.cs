// 
// ConvertDecToHex.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	/// <summary>
	/// Convert a dec numer to hex. For example: 16 -> 0x10
	/// </summary>
	[NRefactoryCodeRefactoringProvider(Description = "Convert dec to hex.")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Convert dec to hex.")]
	public class ConvertDecToHexCodeRefactoringProvider : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
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
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			var token = root.FindToken(span.Start);
			if (!token.IsKind(SyntaxKind.NumericLiteralToken))
				return;
			var value = token.Value;
			if (!((value is int) || (value is long) || (value is short) || (value is sbyte) ||
				(value is uint) || (value is ulong) || (value is ushort) || (value is byte))) {
				return;
			}
			var literalValue = token.ToString();
			if (literalValue.StartsWith("0X", System.StringComparison.OrdinalIgnoreCase))
				return;
			context.RegisterRefactoring(CodeActionFactory.Create(token.Span, DiagnosticSeverity.Info, "To hex", t2 => Task.FromResult(PerformAction (document, root, token))));
		}

		static Document PerformAction(Document document, SyntaxNode root, SyntaxToken token)
		{
			var node = token.Parent as LiteralExpressionSyntax;

			var newRoot = root.ReplaceToken(
				token,
				SyntaxFactory.ParseToken(string.Format("0x{0:x}", token.Value))
				.WithLeadingTrivia(node.GetLeadingTrivia())
				.WithTrailingTrivia(node.GetTrailingTrivia())
			);
			return document.WithSyntaxRoot(newRoot);
		}
	}
}
