using NUnit.Framework;

namespace ICSharpCode.NRefactory6.CSharp.CodeActions
{
	[TestFixture]
	public class $safeitemrootname$ : ContextActionTestBase
	{
		[Test]
		public void Test()
		{
			Analyze</*NAME_OF_YOUR_ACTION_CLASS*/>(@"
class Foo
{
}
");
		}
	}
}