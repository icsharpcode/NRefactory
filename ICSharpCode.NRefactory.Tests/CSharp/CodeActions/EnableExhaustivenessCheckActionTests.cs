using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.Refactoring.CodeActions;

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	[TestFixture]
	public class EnableExhaustivenessCheckActionTests: ContextActionTestBase
	{
		[Test]
		public void TestNotEnumSwitchLabelsExpressions()
		{
			TestWrongContext<EnableExhaustivenessCheckAction>(@"
enum Foo { First, Second, Third }

class TestClass
{
	void TestMethod(Foo foo)
	{
		$switch (foo)
		{
			case 0: return;
			case Foo.Second: return;
			default: return;
		}
	}
}");
		}

		[Test]
		public void TestCheckAlreadyEnabled()
		{
			TestWrongContext<EnableExhaustivenessCheckAction>(@"
enum Foo { First, Second, Third }

class TestClass
{
	void TestMethod(Foo foo)
	{
		// Check exhaustiveness
		$switch (foo)
		{
			case Foo.Second: return;
			default: return;
		}
	}
}");
		}

		[Test]
		public void TestCheckDisabled()
		{
			Test<EnableExhaustivenessCheckAction>(@"
enum Foo { First, Second, Third }

class TestClass
{
	void TestMethod(Foo foo)
	{
		$switch (foo)
		{
			case Foo.Second: return;
			default: return;
		}
	}
}", @"
enum Foo { First, Second, Third }

class TestClass
{
	void TestMethod(Foo foo)
	{
		// Check exhaustiveness
		switch (foo)
		{
			case Foo.Second: return;
			default: return;
		}
	}
}");
		}
	}
}
