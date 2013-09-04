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
			var policy = formatOptions ?? FormattingOptionsFactory.CreateMono();

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
