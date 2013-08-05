using NUnit.Framework;

namespace ICSharpCode.NRefactory.IndentationTests
{
	[TestFixture]
	public class AllInOneTests
	{
		const string ProjectDir = "../../";
		const string TestFilesPath = "ICSharpCode.NRefactory.Tests/IndentationTests/TestFiles";

		public void BeginFileTest(string fileName)
		{
			Helper.ReadAndTest(System.IO.Path.Combine(ProjectDir, TestFilesPath, fileName));
		}

		[Test]
		public void TestAllInOne_Simple()
		{
			BeginFileTest("Simple.cs");   
		}

		[Test]
		public void TestAllInOne_PreProcessorDirectives()
		{
			BeginFileTest("PreProcessorDirectives.cs");
		}

		[Test]
		public void TestAllInOne_Comments()
		{
			BeginFileTest("Comments.cs");
		}

		[Test]
		public void TestAllInOne_Strings()
		{
			BeginFileTest("Strings.cs");
		}

		[Test]
		public void TestAllInOne_IndentEngine()
		{
			BeginFileTest("IndentEngine.cs");
		}

		[Test]
		public void TestAllInOne_IndentState()
		{
			BeginFileTest("IndentState.cs");
		}
	}
}
