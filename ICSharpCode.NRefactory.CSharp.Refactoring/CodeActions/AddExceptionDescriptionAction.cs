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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Add an exception description to the xml documentation")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Add an exception description to the xml documentation")]
	public class AddExceptionDescriptionAction : SpecializedCodeAction<ThrowStatementSyntax>
	{
//		static AstNode SearchInsertionNode (AstNode entity)
//		{
//			AstNode result = entity;
//			while (result != null && (result.Role == Roles.Comment || result.Role == Roles.NewLine || result.Role == Roles.Whitespace))
//				result = result.NextSibling;
//			return result;
//		}
		protected override IEnumerable<CodeAction> GetActions(Document document, SemanticModel semanticModel, SyntaxNode root, TextSpan span, ThrowStatementSyntax node, CancellationToken cancellationToken)
		{
			yield break;
		}
//		protected override CodeAction GetAction (SemanticModel context, ThrowStatement node)
//		{
//			var entity = node.GetParent<EntityDeclaration> ();
//			if (entity == null)
//				return null;
//			var rr = context.Resolve (entity) as MemberResolveResult;
//			if (rr == null || rr.IsError)
//				return null;
//			var expr = context.Resolve (node.Expression);
//			if (expr == null || expr.IsError || expr.Type.GetDefinition () == null)
//				return null;
//
//			var docElement = XmlDocumentationElement.Get (rr.Member);
//			if (docElement == null || docElement.Children.Count == 0)
//				return null;
//			foreach (var de in docElement.Children) {
//				if (de.Name == "exception") {
//					if (de.ReferencedEntity == expr.Type)
//						return null;
//				}
//			}
//
//			return new CodeAction (
//				context.TranslateString ("Add exception to xml documentation"),
//				script => {
//					var startText = string.Format (" <exception cref=\"{0}\">", expr.Type.GetDefinition ().GetIdString ());
//					var comment = new Comment (startText +"</exception>", CommentType.Documentation);
//					script.InsertBefore (
//						SearchInsertionNode (entity.FirstChild) ?? entity, 
//						comment
//					);
//					script.Select (script.GetSegment (comment).Offset + ("///" + startText).Length, 0);
//				},
//				node
//			);
//		}
	}
}

