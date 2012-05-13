using System;
using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	[TestFixture]
	public class ToProtectedActionTest : ContextActionTestBase
	{
		[Test]
		public void TestPublicClass ()
		{
			string result = RunContextAction (
				new ToProtectedAction (),
				"using System;" + Environment.NewLine +
				"$public class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	" + Environment.NewLine +
				"}"
			);
			Console.WriteLine (result);
			Assert.AreEqual (
				"using System;" + Environment.NewLine +
				"protected class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	" + Environment.NewLine +
				"}", result);
		}
		
		[Test]
		public void TestPrivateClass ()
		{
			string result = RunContextAction (
				new ToProtectedAction (),
				"using System;" + Environment.NewLine +
				"$class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	" + Environment.NewLine +
				"}"
			);
			Console.WriteLine (result);
			Assert.AreEqual (
				"using System;" + Environment.NewLine +
				"protected class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	" + Environment.NewLine +
				"}", result);
		}
		
		[Test]
		public void TestInternalClass ()
		{
			string result = RunContextAction (
				new ToProtectedAction (),
				"using System;" + Environment.NewLine +
				"$internal class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	" + Environment.NewLine +
				"}"
			);
			Console.WriteLine (result);
			Assert.AreEqual (
				"using System;" + Environment.NewLine +
				"protected class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	" + Environment.NewLine +
				"}", result);
		}
		
		[Test]
		public void TestExplicitPrivateClass ()
		{
			string result = RunContextAction (
				new ToProtectedAction (),
				"using System;" + Environment.NewLine +
				"public class SomeClass{" + Environment.NewLine +
				"	$private class TestClass" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}"
			);
			Console.WriteLine (result);
			Assert.AreEqual (
				"using System;" + Environment.NewLine +
				"public class SomeClass{" + Environment.NewLine +
				"	protected class TestClass" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}

		[Test]
		public void TestWrongContext ()
		{
			TestWrongContext<ToProtectedAction> (
				"using System;" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	$public void TestMethod(){}" + Environment.NewLine +
				"}"
				);
		}
		
		[Test]
		public void TestWrongContext1 ()
		{
			TestWrongContext<ToProtectedAction> (
				"using System;" + Environment.NewLine +
				"public class SomeClass{" + Environment.NewLine +
				"	$protected class TestClass" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}"
			);
		}
		
		[Test]
		public void TestWrongContext2 ()
		{
			TestWrongContext<ToProtectedAction> (
				"using System;" + Environment.NewLine +
				"public class SomeClass{" + Environment.NewLine +
				"	$protected internal class TestClass" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}"
			);
		}
		
		[Test]
		public void TestWrongContext3 ()
		{
			TestWrongContext<ToProtectedAction> (
				"using System;" + Environment.NewLine +
				"public $class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	" + Environment.NewLine +
				"}"
			);
		}
	}
}

