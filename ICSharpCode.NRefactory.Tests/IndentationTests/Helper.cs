//
// Helper.cs
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

using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Editor;
using NUnit.Framework;
using System.IO;
using System.Text;

namespace ICSharpCode.NRefactory.IndentationTests
{
	internal static class Helper
	{
		public static IDocumentIndentEngine CreateEngine(string text, CSharpFormattingOptions formatOptions = null)
		{
			var policy = formatOptions;
			if ( policy == null) {
				policy = FormattingOptionsFactory.CreateMono();
				policy.IndentPreprocessorDirectives = false;
			}

			var sb = new StringBuilder();
			int offset = 0;
			for (int i = 0; i < text.Length; i++)
			{
				var ch = text[i];
				if (ch == '$')
				{
					offset = i;
					continue;
				}
				sb.Append(ch);
			}

			var document = new ReadOnlyDocument(sb.ToString());
			var options = new TextEditorOptions();

			var result = new CacheIndentEngine(new CSharpIndentEngine(document, options, policy));
			result.Update(offset);
			return result;
		}


		public static void ReadAndTest(string filePath, CSharpFormattingOptions policy = null, TextEditorOptions options = null)
		{
			if (File.Exists(filePath))
			{
				var code = File.ReadAllText(filePath);
				var document = new ReadOnlyDocument(code);
				policy = policy ?? FormattingOptionsFactory.CreateMono();
				options = options ?? new TextEditorOptions { IndentBlankLines = false };

				var engine = new CacheIndentEngine(new CSharpIndentEngine(document, options, policy));

				foreach (var ch in code)
				{
					if (options.EolMarker[0] == ch)
					{
						Assert.IsFalse(engine.NeedsReindent,
								string.Format("Line: {0}, Indent: {1}, Current indent: {2}",
								engine.Location.Line.ToString(), engine.ThisLineIndent.Length, engine.CurrentIndent.Length));
					}

					engine.Push(ch);
				}
			}
			else
			{
				Assert.Fail("File " + filePath + " doesn't exist.");
			}
		}
	}
}
