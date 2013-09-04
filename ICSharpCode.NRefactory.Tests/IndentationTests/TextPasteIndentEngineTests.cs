//
// TextPasteIndentEngineTests.cs
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
using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Editor;
using System.Text;

namespace ICSharpCode.NRefactory.IndentationTests
{
	[TestFixture]
	public class TextPasteIndentEngineTests
	{
		public static CacheIndentEngine CreateEngine(string text, CSharpFormattingOptions formatOptions = null)
		{
			var policy = formatOptions ?? FormattingOptionsFactory.CreateMono();
			
			var sb = new StringBuilder();
			int offset = 0;
			for (int i = 0; i < text.Length; i++) {
				var ch = text [i];
				if (ch == '$') {
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

		[Test]
		public void TestSimplePaste()
		{
			var indent = CreateEngine(@"
class Foo
{
	void Bar ()
	{
		System.Console.WriteLine ($);
	}
}");
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, new TextEditorOptions());
			var text = handler.FormatPlainText(indent.Offset, "Foo", null);
			Assert.AreEqual("Foo", text);
		}

		[Test]
		public void TestMultiLinePaste()
		{
			var indent = CreateEngine(@"
namespace FooBar
{
	class Foo
	{
		void Bar ()
		{
			System.Console.WriteLine ();
		}
		$
	}
}
");
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, new TextEditorOptions());
			
			var text = handler.FormatPlainText(indent.Offset, "void Bar ()\n{\nSystem.Console.WriteLine ();\n}", null);
			Assert.AreEqual("void Bar ()\n\t\t{\n\t\t\tSystem.Console.WriteLine ();\n\t\t}", text);
		}

		[Test]
		public void TestMultiplePastes()
		{
			var indent = CreateEngine(@"
class Foo
{
void Bar ()
{
System.Console.WriteLine ();
}
$
}


");
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, new TextEditorOptions());
			
			for (int i = 0; i < 2; i++) {
				var text = handler.FormatPlainText(indent.Offset, "void Bar ()\n{\nSystem.Console.WriteLine ();\n}", null);
				Assert.AreEqual("void Bar ()\n\t{\n\t\tSystem.Console.WriteLine ();\n\t}", text);
			}
		}
		

		[Test]
		public void TestPasteNewLine()
		{
			var indent = CreateEngine(@"
class Foo
{
	$void Bar ()
	{
	}
}");
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, new TextEditorOptions());
			var text = handler.FormatPlainText(indent.Offset, "int i;\n", null);
			Assert.AreEqual("int i;\n\t", text);
		}

		[Test]
		public void PasteVerbatimString()
		{
			var indent = CreateEngine(@"
class Foo
{
void Bar ()
{
	
}
}");
			ITextPasteHandler handler = new TextPasteIndentEngine(indent, new TextEditorOptions());
			var str = "string str = @\"\n1\n\t2 \n\t\t3\n\";";
			var text = handler.FormatPlainText(indent.Offset, str, null);
			Assert.AreEqual(str, text);
		}

	}
}

