using ICSharpCode.NRefactory.CSharp.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	[TestFixture]
	public class RedundantBlockInDifferentBranchesTests : InspectionActionTestBase
	{
		[Test]
		public void TestConditionalExpression1()
		{
			var input = @"
class TestClass
{
	void TestMethod (int foo)
	{
		if (foo > 5)
		{
			foo = foo +  1;
			foo = foo + foo;
		}
		else
		{
			foo = foo + 1;
			foo = foo + foo;
		}
	}
}";
			var output = @"
class TestClass
{
	void TestMethod (int foo)
	{
		if (foo > 5) {
			foo = foo + 1;
			foo = foo + foo;
		}
	}
}";
			Test<RedundantBlockInDifferentBranchesIssue>(input, 1, output);
		}

		[Test]
		public void TestConditionalExpression2()
		{
			var input = @"
class TestClass
{
	void TestMethod (int foo)
	{
		if (foo > 5)
			foo = foo + 1;
		else
			foo = foo + 1;
	}
}";
			var output = @"
class TestClass
{
	void TestMethod (int foo)
	{
		if (foo > 5)
			foo = foo + 1;
	}
}";
			Test<RedundantBlockInDifferentBranchesIssue>(input, 1, output);
		}

		[Test]
		public void TestConditionalExpression3()
		{
			var input = @"
class TestClass
{
	void TestMethod (int foo)
	{
		if (foo > 5)
			foo = foo + 1;
		else
		{
			foo = foo + 1;
		}
	}
}";
			var output = @"
class TestClass
{
	void TestMethod (int foo)
	{
		if (foo > 5)
			foo = foo + 1;
	}
}";
			Test<RedundantBlockInDifferentBranchesIssue>(input, 1, output);
		}

		[Test]
		public void TestConditionalExpression4()
		{
			var input = @"
class TestClass
{
	void TestMethod (int foo)
	{
		if (foo > 5)
		{
		}
		else
		{}
	}
}";
			var output = @"
class TestClass
{
	void TestMethod (int foo)
	{
		if (foo > 5) {
		}
	}
}";
			Test<RedundantBlockInDifferentBranchesIssue>(input, 1 , output);
		}

		[Test]
		public void TestNoProblem2()
		{
			var input = @"
class TestClass
{
	void TestMethod (int foo)
	{
		if (foo > 5)
		{
			foo = foo + 1;
			return 2;
		}
		else
			return 5;
	}
}";
			Test<RedundantBlockInDifferentBranchesIssue>(input, 0);
		}

		[Test]
		public void TestResharperDisableRestore()
		{
			var input = @"
class TestClass
{
	void TestMethod (int foo)
	{
//Resharper disable RedundantBlockInDifferentBranches
		if (foo > 5)
			return 2;
		else
			return 5;
//Resharper restore RedundantBlockInDifferentBranches
	}
}";
			Test<RedundantBlockInDifferentBranchesIssue>(input, 0);
		}
	}
}
