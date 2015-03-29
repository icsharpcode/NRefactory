//
// AddExceptionDescriptionAction.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
	[NRefactoryCodeRefactoringProvider(Description = "Add an exception description to the xml documentation")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Add an exception description to the xml documentation")]
	public class AddExceptionDescriptionCodeRefactoringProvider : SpecializedCodeRefactoringProvider<ThrowStatementSyntax>
	{
		protected override IEnumerable<CodeAction> GetActions (Document document, SemanticModel semanticModel, SyntaxNode root, TextSpan span, ThrowStatementSyntax node, CancellationToken cancellationToken)
		{
			var entity = node.FirstAncestorOrSelf<MemberDeclarationSyntax> ();
			if (entity == null)
				yield break;
			
			var rr = semanticModel.GetDeclaredSymbol (entity);
			if (rr == null)
				yield break;
			var expr = semanticModel.GetTypeInfo (node.Expression);
			if (expr.Type == null)
				yield break;

			bool hadDescription = false;
			foreach (var trivia in entity.GetLeadingTrivia ()) {
				if (!trivia.IsKind (SyntaxKind.SingleLineDocumentationCommentTrivia))
					continue;
				hadDescription = true;
				if (trivia.HasStructure) {
					var structure = trivia.GetStructure ();
					foreach (var d in structure.DescendantNodesAndSelf ().OfType<XmlElementSyntax> ()) {
						if (d.StartTag.Name.LocalName.ToString () == "exception") {
							foreach (var n in d.StartTag.Attributes) {
								if (n.Name.LocalName.ToString () == "cref") {
									// TODO: That's not a correct cref matching.
									if (n.ToString ().Contains (expr.Type.Name))
										yield break;
								}
							}
						}
					}
				}
			}
			if (!hadDescription)
				yield break;
			yield return CodeActionFactory.Create(node.Span, DiagnosticSeverity.Info, GettextCatalog.GetString ("Add exception description"), 
				t2 => {
					var newComment = SyntaxFactory.ParseLeadingTrivia (string.Format ("/// <exception cref=\"{0}\"></exception>\r\n", expr.Type.GetDocumentationCommentId ()));
					var list = entity.GetLeadingTrivia ();
					list = list.Add (newComment.First ());
					var newRoot = root.ReplaceNode ((SyntaxNode)entity, entity.WithLeadingTrivia (list).WithAdditionalAnnotations (Formatter.Annotation));
					return Task.FromResult(document.WithSyntaxRoot (newRoot));
				}
			);
		}
	}
}

