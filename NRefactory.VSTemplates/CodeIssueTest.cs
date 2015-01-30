using NUnit.Framework;

namespace ICSharpCode.NRefactory6.CSharp.CodeIssues
{
	[TestFixture]
	public class $safeitemrootname$ : InspectionActionTestBase
	{
		[Test]
		public void Test()
		{
			Analyze</*NAME_OF_YOUR_ISSUE_CLASS*/>(@"
class Foo
{
}
");
		}
	}
}