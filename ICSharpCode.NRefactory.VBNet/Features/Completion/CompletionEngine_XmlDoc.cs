//
// CompletionEngine_XmlDoc.cs
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
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.VisualBasic;

using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Text;

namespace ICSharpCode.NRefactory6.VisualBasic.Completion
{
	public partial class CompletionEngine
	{
		static readonly List<string> commentTags = new List<string>(new string[] {
			"c",
			"code",
			"example",
			"exception",
			"include",
			"list",
			"listheader",
			"item",
			"term",
			"description",
			"para",
			"param",
			"paramref",
			"permission",
			"remarks",
			"returns",
			"see",
			"seealso",
			"summary",
			"value"
		}
		);

		public static IEnumerable<string> CommentTags {
			get {
				return commentTags;
			}
		}

		string GetLastClosingXmlCommentTag(Document document, SemanticModel semanticModel, int position, SyntaxTrivia trivia)
		{
			var root = semanticModel.SyntaxTree.GetRoot();
			restart:
			string lineText = trivia.ToFullString();
			int startIndex = Math.Min(position - trivia.SpanStart, lineText.Length - 1) - 1;
			while (startIndex > 0 && lineText [startIndex] != '<') {
				--startIndex;
				if (lineText [startIndex] == '/') {
					// already closed.
					startIndex = -1;
					break;
				}
			}
			position = trivia.SpanStart - 1;
			if (startIndex < 0 && position > 0) {
				/*while (position > 0) {
					trivia = root.FindTrivia(position);
					if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)) {
						goto restart;
					} else if (trivia.IsKind(SyntaxKind.WhitespaceTrivia) || trivia.IsKind(SyntaxKind.EndOfLineTrivia)) {
						position--;
					} else {
						break;
					}
				}*/
				return null;
			}

			if (startIndex >= 0) {
				int endIndex = startIndex;
				while (endIndex + 1 < lineText.Length && lineText [endIndex] != '>' && !char.IsWhiteSpace(lineText [endIndex])) {
					endIndex++;
				}
				string tag = endIndex - startIndex - 1 > 0 ? lineText.Substring(
					startIndex + 1,
					endIndex - startIndex - 1
				) : null;
				if (!string.IsNullOrEmpty(tag) && commentTags.IndexOf(tag) >= 0) {
					return tag;
				}
			}
			return null;
		}

		IEnumerable<ICompletionData> GetXmlDocumentationCompletionData(Document document, SemanticModel semanticModel, int position, SyntaxTrivia trivia)
		{
			var closingTag = GetLastClosingXmlCommentTag(document, semanticModel, position, trivia);
			if (closingTag != null) {
				yield return factory.CreateGenericData("/" + closingTag + ">");
			}

			yield return factory.CreateXmlDocCompletionData(
				"c",
				"Set text in a code-like font"
			);
			yield return factory.CreateXmlDocCompletionData(
				"code",
				"Set one or more lines of source code or program output"
			);
			yield return factory.CreateXmlDocCompletionData(
				"example",
				"Indicate an example"
			);
			yield return factory.CreateXmlDocCompletionData(
				"exception",
				"Identifies the exceptions a method can throw",
				"exception cref=\"|\"></exception"
			);
			yield return factory.CreateXmlDocCompletionData(
				"include",
				"Includes comments from a external file",
				"include file=\"|\" path=\"\""
			);
			yield return factory.CreateXmlDocCompletionData(
				"inheritdoc",
				"Inherit documentation from a base class or interface",
				"inheritdoc/"
			);
			yield return factory.CreateXmlDocCompletionData(
				"list",
				"Create a list or table",
				"list type=\"|\""
			);
			yield return factory.CreateXmlDocCompletionData(
				"listheader",
				"Define the heading row"
			);
			yield return factory.CreateXmlDocCompletionData(
				"item",
				"Defines list or table item"
			);

			yield return factory.CreateXmlDocCompletionData("term", "A term to define");
			yield return factory.CreateXmlDocCompletionData(
				"description",
				"Describes a list item"
			);
			yield return factory.CreateXmlDocCompletionData(
				"para",
				"Permit structure to be added to text"
			);

			yield return factory.CreateXmlDocCompletionData(
				"param",
				"Describe a parameter for a method or constructor",
				"param name=\"|\""
			);
			yield return factory.CreateXmlDocCompletionData(
				"paramref",
				"Identify that a word is a parameter name",
				"paramref name=\"|\"/"
			);

			yield return factory.CreateXmlDocCompletionData(
				"permission",
				"Document the security accessibility of a member",
				"permission cref=\"|\""
			);
			yield return factory.CreateXmlDocCompletionData(
				"remarks",
				"Describe a type"
			);
			yield return factory.CreateXmlDocCompletionData(
				"returns",
				"Describe the return value of a method"
			);
			yield return factory.CreateXmlDocCompletionData(
				"see",
				"Specify a link",
				"see cref=\"|\"/"
			);
			yield return factory.CreateXmlDocCompletionData(
				"seealso",
				"Generate a See Also entry",
				"seealso cref=\"|\"/"
			);
			yield return factory.CreateXmlDocCompletionData(
				"summary",
				"Describe a member of a type"
			);
			yield return factory.CreateXmlDocCompletionData(
				"typeparam",
				"Describe a type parameter for a generic type or method"
			);
			yield return factory.CreateXmlDocCompletionData(
				"typeparamref",
				"Identify that a word is a type parameter name"
			);
			yield return factory.CreateXmlDocCompletionData(
				"value",
				"Describe a property"
			);
		}
	}
}