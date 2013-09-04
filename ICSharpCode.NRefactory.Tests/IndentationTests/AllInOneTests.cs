using ICSharpCode.NRefactory.CSharp;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.IndentationTests
{
	[TestFixture]
	public class AllInOneTests
	{
		const string ProjectDir = "../../";
		const string TestFilesPath = "ICSharpCode.NRefactory.Tests/IndentationTests/TestFiles";

		public void BeginFileTest(string fileName, CSharpFormattingOptions policy = null, TextEditorOptions options = null)
		{
			Helper.ReadAndTest(System.IO.Path.Combine(ProjectDir, TestFilesPath, fileName), policy, options);
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

		[Test]
		public void TestAllInOne_SwitchCase()
		{
			var policy = FormattingOptionsFactory.CreateMono();
			policy.IndentSwitchBody = true;
			policy.IndentCaseBody = true;
			policy.IndentBreakStatements = false;

			BeginFileTest("SwitchCase.cs", policy);
		}
	}
}
