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
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[TestFixture]
	public class RedundantArgumentNameTests : InspectionActionTestBase
	{
		[Test]
		public void MethodInvocation1()
		{
			Analyze<RedundantArgumentNameAnalyzer>(@"
class TestClass
{
	public void Foo(int a, int b, double c = 0.1){}
	public void F()
	{
		Foo(1,$b:$ 2);
	}
}
", @"
class TestClass
{
	public void Foo(int a, int b, double c = 0.1){}
	public void F()
	{
		Foo(1, 2);
	}
}
");
		}
		
		[Test]
		public void MethodInvocation2()
		{
			Analyze<RedundantArgumentNameAnalyzer>(@"
class TestClass
{
	public void Foo(int a, int b, double c = 0.1){}
	public void F()
	{
		Foo($a:$ 1, $b:$ 2, $c:$ 0.2);
	}
}
", @"
class TestClass
{
	public void Foo(int a, int b, double c = 0.1){}
	public void F()
	{
		Foo(1, b: 2, c: 0.2);
	}
}
", 0);
		}
		
		[Test]
		public void MethodInvocation3()
		{
			Analyze<RedundantArgumentNameAnalyzer>(@"
class TestClass
{
	public void Foo(int a, int b, double c = 0.1){}
	public void F()
	{
		Foo($a:$ 1, $b:$ 2, $c:$ 0.2);
	}
}
", @"
class TestClass
{
	public void Foo(int a, int b, double c = 0.1){}
	public void F()
	{
		Foo(1, 2, c: 0.2);
	}
}
", 1);
		}


		[Test]
		public void MethodInvocation4()
		{
			Analyze<RedundantArgumentNameAnalyzer>(@"
class TestClass
{
	public void Foo (int a = 2, int b = 3, int c = 4, int d = 5, int e = 5)
	{
	}

	public void F ()
	{
		Foo(1, $b:$ 2, d: 2, c: 3, e:19);
	}
}
", @"
class TestClass
{
	public void Foo (int a = 2, int b = 3, int c = 4, int d = 5, int e = 5)
	{
	}

	public void F ()
	{
		Foo(1, 2, d: 2, c: 3, e:19);
	}
}
");
		}

		[Test]
		public void IndexerExpression() 
		{
			Analyze<RedundantArgumentNameAnalyzer>(@"
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
		int a = TestBases[$i:$ 1, $j:$ 2];
	}
}
", @"
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
		int a = TestBases[1, j: 2];
	}
}
", 0);
		}
		
		[Test]
		public void TestAttributes()
		{
			Analyze<RedundantArgumentNameAnalyzer>(@"using System;
class MyAttribute : Attribute
{
	public MyAttribute(int x, int y) {}
}


[MyAttribute($x:$ 1, $y:$ 2)]
class TestClass
{
}
", @"using System;
class MyAttribute : Attribute
{
	public MyAttribute(int x, int y) {}
}


[MyAttribute(1, 2)]
class TestClass
{
}
", 1);
		}
		
		[Test]
		public void TestObjectCreation()
		{
			Analyze<RedundantArgumentNameAnalyzer>(@"
class TestClass
{
	public TestClass (int x, int y)
	{
	}

	public void Foo ()
	{
		new TestClass (0, $y:$1);
	}
}
", @"
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
");
		}
		
		
		[Test]
		public void Invalid()
		{
			Analyze<RedundantArgumentNameAnalyzer>(@"
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
		int a = TestBases[j: 1, i: 2, k: 3];
	}
}
");
		}

	}
}