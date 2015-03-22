//
// CopyCommentsFromBase.cs
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
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using System.Xml;


namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Copies comments from base to overriding members/types")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Copy comments from base")]
	public class CopyCommentsFromBaseCodeRefactoringProvider: CodeRefactoringProvider
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
			var root = await document.GetSyntaxRootAsync (cancellationToken).ConfigureAwait (false);
			var token = root.FindToken (span.Start);
			if (!token.IsKind (SyntaxKind.IdentifierToken))
				return;
			
			var node = token.Parent as MemberDeclarationSyntax;
			if (node == null)
				return;
			
			var model = await document.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
			var declaredSymbol = model.GetDeclaredSymbol (node, cancellationToken);
			if (declaredSymbol == null || !string.IsNullOrEmpty (declaredSymbol.GetDocumentationCommentXml (null, false, cancellationToken)))
				return;

			var overriddenMember = declaredSymbol.OverriddenMember ();

			if (overriddenMember == null)
				return;
			
			var documentation = overriddenMember.GetDocumentationCommentXml (null, false, cancellationToken);
			if (string.IsNullOrEmpty (documentation))
				return;
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (documentation);
			if (doc.ChildNodes.Count != 1)
				return;
			var inner = doc.ChildNodes[0].InnerXml.Trim ();
			if (string.IsNullOrEmpty (inner))
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Info, 
					"Copy comments from base", 
					t2 => {
						var triva = node.GetLeadingTrivia ();

						var indentTrivia = triva.FirstOrDefault (t => t.IsKind (SyntaxKind.WhitespaceTrivia));
						var indent = indentTrivia.ToString();

						string[] lines = NewLine.SplitLines (inner);
						for (int i = 0; i < lines.Length; i++) {
							lines[i] = indent + "/// " + lines[i].Trim ();
						}


						var eol = "\r\n";
						int idx = 0;
						while (idx < triva.Count && triva[idx].IsKind (SyntaxKind.EndOfLineTrivia))
							idx++;
						triva = triva.Insert (idx, SyntaxFactory.SyntaxTrivia (SyntaxKind.SingleLineCommentTrivia, string.Join (eol, lines) + eol));
						var newRoot = root.ReplaceNode(node, node.WithLeadingTrivia (triva));
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				) 
			);
		}
	}
}