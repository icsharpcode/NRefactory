// ArrayCreationCanBeReplacedWithArrayInitializerTests.cs
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
	public class ArrayCreationCanBeReplacedWithArrayInitializerTests : InspectionActionTestBase
	{
		[Test]
		public void TestVariableDeclarationCase1()
		{
			Analyze<ArrayCreationCanBeReplacedWithArrayInitializerAnalyzer>(@"
class TestClass
{
	void TestMethod ()
	{
		int[] foo = $new int[] ${1, 2, 3};
	}
}", @"
class TestClass
{
	void TestMethod ()
	{
		int[] foo = {1, 2, 3};
	}
}");
		}

		[Test]
		public void TestFieldCase1()
		{
			Analyze<ArrayCreationCanBeReplacedWithArrayInitializerAnalyzer>(@"
class TestClass
{
	int[] foo = $new int[] ${1, 2, 3};
}", @"
class TestClass
{
	int[] foo = {1, 2, 3};
}");
		}


		[Test]
		public void TestVariableDeclarationCase2()
		{
			Analyze<ArrayCreationCanBeReplacedWithArrayInitializerAnalyzer>(@"
class TestClass
{
	void TestMethod ()
	{
		int[] foo = $new [] ${1, 2, 3};
	}
}", @"
class TestClass
{
	void TestMethod ()
	{
		int[] foo = {1, 2, 3};
	}
}");
		}

		[Test]
		public void TestFieldCase2()
		{
			Analyze<ArrayCreationCanBeReplacedWithArrayInitializerAnalyzer>(@"
class TestClass
{
	public int[] filed = $new [] ${1,2,3};
}", @"
class TestClass
{
	public int[] filed = {1,2,3};
}");
		}

		[Test]
		public void TestNoProblem1()
		{
			Analyze<ArrayCreationCanBeReplacedWithArrayInitializerAnalyzer>(@"
class TestClass
{
	void TestMethod ()
	{
		var foo = new[] {1, 2, 3};
	}
}");
		}

		[Test]
		public void TestNoProblem2()
		{
			Analyze<ArrayCreationCanBeReplacedWithArrayInitializerAnalyzer>(@"
class TestClass
{
	void TestMethod ()
	{
		var foo = new int[] {1, 2, 3};
	}
}");
		}

		[Test]
		public void TestNoProblem3()
		{
			Analyze<ArrayCreationCanBeReplacedWithArrayInitializerAnalyzer>(@"
class TestClass
{
	Void Foo(int[] a)
	{}
	void TestMethod ()
	{
		Foo(new int[]{1,2,3});
	}
}");
		}
	}
}
