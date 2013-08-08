// 
// MethodNeverReturnsIssueTests.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang <lightyang0@gmail.com>
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

using ICSharpCode.NRefactory.CSharp.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	[TestFixture]
	public class FunctionNeverReturnsIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TestEnd ()
		{
			var input = @"
class TestClass
{
	void TestMethod () 
	{
		int i = 1;
	}
}";
			Test<FunctionNeverReturnsIssue> (input, 0);
		}

		[Test]
		public void TestReturn ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		return;
	}
}";
			Test<FunctionNeverReturnsIssue> (input, 0);
		}

		[Test]
		public void TestThrow ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		throw new System.NotImplementedException();	
	}
}";
			Test<FunctionNeverReturnsIssue> (input, 0);
		}

		[Test]
		public void TestNeverReturns ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		while (true) ;
	}
}";
			Test<FunctionNeverReturnsIssue> (input, 1);
		}

		[Test]
		public void TestRecursive ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		TestMethod ();
	}
}";
			Test<FunctionNeverReturnsIssue> (input, 1);
		}

		[Test]
		public void TestNonRecursive ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		TestMethod (0);
	}
	void TestMethod (int i)
	{
	}
}";
			Test<FunctionNeverReturnsIssue> (input, 0);
		}

		[Test]
		public void TestNonRecursiveProperty ()
		{
			var input = @"
class TestClass
{
	int foo;
	int Foo
	{
		get { return foo; }
		set
		{
			if (Foo != value)
				foo = value;
		}
	}
}";
			Test<FunctionNeverReturnsIssue> (input, 0);
		}


		[Test]
		public void TestGetterNeverReturns ()
		{
			var input = @"
class TestClass
{
	int TestProperty
	{
		get {
			while (true) ;
		}
	}
}";
			Test<FunctionNeverReturnsIssue> (input, 1);
		}

		[Test]
		public void TestRecursiveGetter ()
		{
			var input = @"
class TestClass
{
	int TestProperty
	{
		get {
			return TestProperty;
		}
	}
}";
			Test<FunctionNeverReturnsIssue> (input, 1);
		}

		[Test]
		public void TestRecursiveSetter ()
		{
			var input = @"
class TestClass
{
	int TestProperty
	{
		set {
			TestProperty = value;
		}
	}
}";
			Test<FunctionNeverReturnsIssue> (input, 1);
		}

		[Test]
		public void TestMethodGroupNeverReturns ()
		{
			var input = @"
class TestClass
{
	int TestMethod()
	{
		return TestMethod();
	}
	int TestMethod(object o)
	{
		return TestMethod();
	}
}";
			Test<FunctionNeverReturnsIssue> (input, 1);
		}

		[Test]
		public void TestIncrementProperty()
		{
			var input = @"
class TestClass
{
	int TestProperty
	{
		get { return TestProperty++; }
		set { TestProperty++; }
	}
}";
			Test<FunctionNeverReturnsIssue> (input, 2);
		}

		[Test]
		public void TestLambdaNeverReturns ()
		{
			var input = @"
class TestClass
{
	void TestMethod()
	{
		System.Action action = () => { while (true) ; };
	}
}";
			Test<FunctionNeverReturnsIssue> (input, 1);
		}

		[Test]
		public void TestDelegateNeverReturns ()
		{
			var input = @"
class TestClass
{
	void TestMethod()
	{
		System.Action action = delegate() { while (true) ; };
	}
}";

			Test<FunctionNeverReturnsIssue> (input, 1);
		}
		[Test]
		public void YieldBreak ()
		{
			var input = @"
class TestClass
{
	System.Collections.Generic.IEnumerable<string> TestMethod ()
	{
		yield break;
	}
}";
			Test<FunctionNeverReturnsIssue> (input, 0);
		}

		[Test]
		public void TestDisable ()
		{
			var input = @"
class TestClass
{
	// ReSharper disable once FunctionNeverReturns
	void TestMethod ()
	{
		while (true) ;
	}
}";
			TestWrongContext<FunctionNeverReturnsIssue> (input);
		}
	}
}
