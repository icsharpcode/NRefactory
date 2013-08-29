// RedundantArgumentNameTests.cs
//
// Author:
//      Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013 Ji Kun <jikun.nus@gmail.com>
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
	public class RedundantArgumentNameTests : InspectionActionTestBase
	{
		[Test]
		public void MethodInvocation1()
		{
			var input = @"
class TestClass
{
	public void Foo(int a, int b, double c = 0.1){}
	public void F()
	{
		Foo(1,b: 2);
	}
}
";
			var output = @"
class TestClass
{
	public void Foo(int a, int b, double c = 0.1){}
	public void F()
	{
		Foo (1, 2);
	}
}
";
			Test<RedundantArgumentNameIssue>(input, 1, output);
		}

		[Test]
		public void MethodInvocation2()
		{
			var input = @"
class TestClass
{
	public void Foo(int a, int b, double c = 0.1){}
	public void F()
	{
		Foo(
//Resharper disable RedundantArgumentName
a: 1, b: 2,
//Resharper restore RedundantArgumentName
 c: 0.2);
	}
}
";
			var output = @"
class TestClass
{
	public void Foo(int a, int b, double c = 0.1){}
	public void F()
	{
		Foo (
//Resharper disable RedundantArgumentName
			1, 2,
//Resharper restore RedundantArgumentName
			0.2);
	}
}
";
			Test<RedundantArgumentNameIssue>(input, 1, output);
		}

		[Test]
		public void MethodInvocation3()
		{
			var input = @"
class TestClass
{
	public void Foo(int a, int b, double c = 0.1){}
	public void F()
	{
		Foo (
//Resharper disable RedundantArgumentName
a: 1,
//Resharper restore RedundantArgumentName
 b: 2,
//Resharper disable RedundantArgumentName
 c: 0.2);
	}
}
";
			var output = @"
class TestClass
{
	public void Foo(int a, int b, double c = 0.1){}
	public void F()
	{
		Foo (
//Resharper disable RedundantArgumentName
			1,
//Resharper restore RedundantArgumentName
			2,
//Resharper disable RedundantArgumentName
			c: 0.2);
	}
}
";
			Test<RedundantArgumentNameIssue>(input, 1, output);
		}

		[Test]
		public void IndexerExpression()
		{
			var input = @"
public class TestClass
{
	public int this[int i, int j]
	{
		set { }
		get { return 0; }
	}
}
internal class Test
{
	private void Foo()
	{
		var TestBases = new TestClass();
		int a = TestBases[i: 1, j: 2];
	}
}
";
			var output = @"
public class TestClass
{
	public int this[int i, int j]
	{
		set { }
		get { return 0; }
	}
}
internal class Test
{
	private void Foo()
	{
		var TestBases = new TestClass();
		int a = TestBases [1, j: 2];
	}
}
";
			Test<RedundantArgumentNameIssue>(input, 2, output, 0);
		}

		[Ignore("Fixme")]
		[Test]
		public void TestAttributes()
		{
			var input = @"using System;
class MyAttribute : Attribute
{
	public MyAttribute(int x, int y) {}
}


[MyAttribute(x: 1, y: 2)]
class TestClass
{
}
"
;
			var output = @"using System;
class MyAttribute : Attribute
{
	public MyAttribute(int x, int y) {}
}


[MyAttribute(x: 1, 2)]
class TestClass
{
}
";
			Test<RedundantArgumentNameIssue>(input, 2, output, 1);
		}

		[Test]
		public void TestObjectCreation()
		{
			var input = @"
class TestClass
{
	public TestClass (int x, int y)
	{
	}

	public void Foo ()
	{
		new TestClass (0, y:1);
	}
}
"
;
			var output = @"
class TestClass
{
	public TestClass (int x, int y)
	{
	}

	public void Foo ()
	{
		new TestClass (0, 1);
	}
}
";
			Test<RedundantArgumentNameIssue>(input, 1, output);
		}


		[Test]
		public void Invalid()
		{
			var input = @"
public class TestClass
{
	public int this[int i, int j , int k]
	{
		set { }
		get { return 0; }
	}
}
internal class Test
{
	private void Foo()
	{
		var TestBases = new TestClass();
		int a = TestBases[ j: 1, i: 2, k: 3];
	}
}
";
			Test<RedundantArgumentNameIssue>(input, 0);
		}
		
		[Test]
		public void ResharperDisable()
		{
			var input = @"
public class TestClass
{
	public int this[int i, int j]
	{
		set { }
		get { return 0; }
	}
}
internal class Test
{
	private void Foo()
	{
		var TestBases = new TestClass();
//Resharper disable RedundantArgumentName
		int a = TestBases[ i: 1, j: 2];
//Resharper restore RedundantArgumentName
	}
}
";
			Test<RedundantArgumentNameIssue>(input, 0);
		}
	}
}