using System;
using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	[TestFixture]
	public class SomeOfEnumValuesWasNotHandledInSwitchStatementIssueTests: InspectionActionTestBase
	{
		[Test]
		public void TestNoComment()
		{
			TestWrongContext<SomeOfEnumValuesWasNotHandledInSwitchStatementIssue>(@"
enum Foo { First, Second, Third }

class TestClass
{
    void TestMethod(Foo foo)
    {
        $switch (foo)
		{
			case Foo.First: return;
			default: return;
		}
    }
}");
		}

		[Test]
		public void TestNotEnumSwitchExpression()
		{
			TestWrongContext<SomeOfEnumValuesWasNotHandledInSwitchStatementIssue>(@"
enum Foo { First, Second, Third }

class TestClass
{
    void TestMethod(object obj)
    {
		// Check exhaustiveness
        $switch (obj)
		{
			case Foo.First: return;
			default: return;
		}
    }
}");
		}

		[Test]
		public void TestEmptySwitch()
		{
			TestWrongContext<SomeOfEnumValuesWasNotHandledInSwitchStatementIssue>(@"
enum Foo { First, Second, Third }

class TestClass
{
    void TestMethod(Foo foo)
    {
		// Check exhaustiveness
        $switch (foo)
		{
		}
    }
}");
		}

		[Test]
		public void TestOnlyDefaultLabel()
		{
			TestWrongContext<SomeOfEnumValuesWasNotHandledInSwitchStatementIssue>(@"
enum Foo { First, Second, Third }

class TestClass
{
    void TestMethod(Foo foo)
    {
		// Check exhaustiveness
        $switch (foo)
		{
			default: return;
		}
    }
}");
		}

		[Test]
		public void TestNotEnumSwitchLabelsExpressions()
		{
			TestWrongContext<SomeOfEnumValuesWasNotHandledInSwitchStatementIssue>(@"
enum Foo { First, Second, Third }

class TestClass
{
    void TestMethod(Foo foo)
    {
		// Check exhaustiveness
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
		public void TestWrongEnumSwitchLabelsExpressions()
		{
			TestWrongContext<SomeOfEnumValuesWasNotHandledInSwitchStatementIssue>(@"
enum Foo { First, Second, Third }
enum Bar { First, Second, Third }

class TestClass
{
    void TestMethod(Foo foo)
    {
		// Check exhaustiveness
        $switch (foo)
		{
			case Bar.First: return;
			case Bar.Second: return;
			default: return;
		}
    }
}");
		}

		[Test]
		public void TestEnumExhaustive()
		{
			TestWrongContext<SomeOfEnumValuesWasNotHandledInSwitchStatementIssue>(@"
enum Foo { First, Second, Third }

class TestClass
{
    void TestMethod(Foo foo)
    {
		// Check exhaustiveness
        $switch (foo)
		{
			case Foo.First: return;
			case Foo.Second: return;
			case Foo.Third: return;
			default: return;
		}
    }
}");
		}

		[Test]
		public void TestEnumNonExhaustiveWithoutDefaultCase()
		{
			Test<SomeOfEnumValuesWasNotHandledInSwitchStatementIssue>(
				"enum Foo { First, Second, Third }" + Environment.NewLine +
				"" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"\tvoid TestMethod(Foo foo)" + Environment.NewLine +
				"\t{" + Environment.NewLine +
				"\t\t// Check exhaustiveness" + Environment.NewLine +
				"\t\tswitch (foo)" + Environment.NewLine +
				"\t\t{" + Environment.NewLine +
				"\t\t\tcase Foo.Second: return;" + Environment.NewLine +
				"\t\t}" + Environment.NewLine +
				"\t}" + Environment.NewLine +
				"}" + Environment.NewLine,
				"enum Foo { First, Second, Third }" + Environment.NewLine +
				"" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"\tvoid TestMethod(Foo foo)" + Environment.NewLine +
				"\t{" + Environment.NewLine +
				"\t\t// Check exhaustiveness" + Environment.NewLine +
				"\t\tswitch (foo) {" + Environment.NewLine +
				"\t\tcase Foo.Second:" + Environment.NewLine +
				"\t\t\treturn;" + Environment.NewLine +
				"\t\tcase Foo.First:" + Environment.NewLine +
				"\t\t\tthrow new System.NotImplementedException ();" + Environment.NewLine +
				"\t\tcase Foo.Third:" + Environment.NewLine +
				"\t\t\tthrow new System.NotImplementedException ();" + Environment.NewLine +
				"\t\t}" + Environment.NewLine +
				"\t}" + Environment.NewLine +
				"}" + Environment.NewLine
			);
		}

		[Test]
		public void TestEnumNonExhaustiveWithDefaultCase()
		{
			Test<SomeOfEnumValuesWasNotHandledInSwitchStatementIssue>(
				"enum Foo { First, Second, Third }" + Environment.NewLine +
				"" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"\tvoid TestMethod(Foo foo)" + Environment.NewLine +
				"\t{" + Environment.NewLine +
				"\t\t// Check exhaustiveness" + Environment.NewLine +
				"\t\tswitch (foo)" + Environment.NewLine +
				"\t\t{" + Environment.NewLine +
				"\t\t\tcase Foo.Second: return;" + Environment.NewLine +
				"\t\t\tdefault: return;" + Environment.NewLine +
				"\t\t}" + Environment.NewLine +
				"\t}" + Environment.NewLine +
				"}" + Environment.NewLine,
				"enum Foo { First, Second, Third }" + Environment.NewLine +
				"" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"\tvoid TestMethod(Foo foo)" + Environment.NewLine +
				"\t{" + Environment.NewLine +
				"\t\t// Check exhaustiveness" + Environment.NewLine +
				"\t\tswitch (foo) {" + Environment.NewLine +
				"\t\tcase Foo.Second:" + Environment.NewLine +
				"\t\t\treturn;" + Environment.NewLine +
				"\t\tcase Foo.First:" + Environment.NewLine +
				"\t\t\tthrow new System.NotImplementedException ();" + Environment.NewLine +
				"\t\tcase Foo.Third:" + Environment.NewLine +
				"\t\t\tthrow new System.NotImplementedException ();" + Environment.NewLine +
				"\t\tdefault:" + Environment.NewLine +
				"\t\t\treturn;" + Environment.NewLine +
				"\t\t}" + Environment.NewLine +
				"\t}" + Environment.NewLine +
				"}" + Environment.NewLine
			);
		}

		[Test]
		public void TestEnumNonExhaustiveWithDefaultCaseGroupedLabels()
		{
			Test<SomeOfEnumValuesWasNotHandledInSwitchStatementIssue>(
				"enum Foo { First, Second, Third }" + Environment.NewLine +
				"" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"\tvoid TestMethod(Foo foo)" + Environment.NewLine +
				"\t{" + Environment.NewLine +
				"\t\t// Check exhaustiveness" + Environment.NewLine +
				"\t\tswitch (foo)" + Environment.NewLine +
				"\t\t{" + Environment.NewLine +
				"\t\t\tcase Foo.Second:" + Environment.NewLine +
				"\t\t\tcase Foo.First: return;" + Environment.NewLine +
				"\t\t\tdefault: return;" + Environment.NewLine +
				"\t\t}" + Environment.NewLine +
				"\t}" + Environment.NewLine +
				"}" + Environment.NewLine,
				"enum Foo { First, Second, Third }" + Environment.NewLine +
				"" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"\tvoid TestMethod(Foo foo)" + Environment.NewLine +
				"\t{" + Environment.NewLine +
				"\t\t// Check exhaustiveness" + Environment.NewLine +
				"\t\tswitch (foo) {" + Environment.NewLine +
				"\t\tcase Foo.Second:" + Environment.NewLine +
				"\t\tcase Foo.First:" + Environment.NewLine +
				"\t\t\treturn;" + Environment.NewLine +
				"\t\tcase Foo.Third:" + Environment.NewLine +
				"\t\t\tthrow new System.NotImplementedException ();" + Environment.NewLine +
				"\t\tdefault:" + Environment.NewLine +
				"\t\t\treturn;" + Environment.NewLine +
				"\t\t}" + Environment.NewLine +
				"\t}" + Environment.NewLine +
				"}" + Environment.NewLine
				);
		}
	}
}
