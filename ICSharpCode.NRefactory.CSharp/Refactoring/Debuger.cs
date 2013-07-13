using System;
using System.IO;

namespace ICSharpCode.NRefactory.CSharp
{
	public class TestClass
		{
			
			public static void WriteFile(string content)
			{
				StreamWriter sw = File.AppendText("/Users/leoji/test");
				sw.WriteLine(content);
				sw.Flush();
				sw.Close();
			}
			
			public static void WriteFileInt(int content)
			{
				StreamWriter sw = File.AppendText("/Users/leoji/test");
				sw.WriteLine(content);
				sw.Flush();
				sw.Close();
			}
		}
	
}

