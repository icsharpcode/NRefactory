// 
// DisposeMethodInNonIDisposableTypeTests.cs
// 
// Author:
//      Luís Reis <luiscubal@gmail.com>
// 
// Copyright (c) 2013 Luís Reis <luiscubal@gmail.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using ICSharpCode.NRefactory.CSharp.CodeActions;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	public class DisposeMethodInNonIDisposableTypeTests : InspectionActionTestBase
	{
		void Test (string input, int issueCount)
		{
			TestRefactoringContext context;
			var issues = GetIssues (new DisposeMethodInNonIDisposableTypeIssue(), input, out context);
			Assert.AreEqual (issueCount, issues.Count);
		}

		void TestNoFix (string input, int issueCount)
		{
			TestRefactoringContext context;
			var issues = GetIssues (new DisposeMethodInNonIDisposableTypeIssue(), input, out context);
			Assert.AreEqual (issueCount, issues.Count);
			CheckFix (context, issues, input);
		}

		void TestFix (string input, int issueCount, string output)
		{
			TestRefactoringContext context;
			var issues = GetIssues (new DisposeMethodInNonIDisposableTypeIssue (), input, out context);
			Assert.AreEqual (issueCount, issues.Count);
			CheckFix (context, issues, output);
		}

		[Test]
		public void IgnoreNonDisposeMethodsTest()
		{
			string input = @"
class TestClass
{
	void TestMethod ()
	{
	}
}
";

			Test(input, 0);
		}

		[Test]
		public void IgnoreProperImplementationMethodsTest()
		{
			string input = @"
class TestClass : System.IDisposable
{
	public void Dispose ()
	{
	}
}
";

			Test(input, 0);
		}

		[Test]
		public void IgnoreExplicitDisposeTest()
		{
			string input = @"
interface Foo {
	void Dispose ();
}
class TestClass : Foo
{
	void Foo.Dispose ()
	{
	}
}
";
			Test(input, 0);
		}

		[Test]
		public void IgnoreIndirectImplementationTest()
		{
			string input = @"
interface Bar : System.IDisposable {}
class TestClass : Bar
{
	public void Dispose ()
	{
	}
}
";

			Test(input, 0);
		}

		[Test]
		public void DisposeWithoutIDisposableTest()
		{
			string input = @"
class TestClass
{
	public void Dispose ()
	{
	}
}
";

			string output = @"
class TestClass : System.IDisposable
{
	public void Dispose ()
	{
	}
}
";

			TestFix(input, 1, output);
		}

		[Test]
		public void DisposeWithUsingTest()
		{
			string input = @"
using System;
class TestClass
{
	public void Dispose ()
	{
	}
}
";

			string output = @"
using System;
class TestClass : IDisposable
{
	public void Dispose ()
	{
	}
}
";

			TestFix(input, 1, output);
		}

		[Test]
		public void NonPublicDisposeTest()
		{
			string input = @"
using System;
class TestClass
{
	protected void Dispose ()
	{
	}
}
";

			string output = @"
using System;
class TestClass : IDisposable
{
	public void Dispose ()
	{
	}
}
";

			TestFix(input, 1, output);
		}

		[Test]
		public void DisabledForInterfaceTest()
		{
			string input = @"
using System;
interface TestInterface
{
	void Dispose ();
}
";

			Test(input, 0);
		}

		[Test]
		public void DisabledForStaticTest()
		{
			string input = @"
class TestType
{
	public static void Dispose() {}
}
";

			Test(input, 0);
		}

		[Test]
		public void DisabledForArgumentsTest()
		{
			string input = @"
class TestClass
{
	public void Dispose (object arg)
	{
	}
}
";

			Test(input, 0);
		}

		[Test]
		public void DisabledForNonVoidTest()
		{
			string input = @"
class TestClass
{
	public int Dispose ()
	{
		return 0;
	}
}
";

			Test(input, 0);
		}
	}
}

