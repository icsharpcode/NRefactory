// 
// RedundantNullCoalescingExpressionTests.cs
// 
// Author:
//      Luís Reis <luiscubal@gmail.com>
// 
// Copyright (c) 2013 Luís Reis
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

using ICSharpCode.NRefactory6.CSharp.CodeRefactorings;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using NUnit.Framework;
using ICSharpCode.NRefactory6.CSharp.CodeCompletion;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[Ignore("TODO roslyn port.")]
	[TestFixture]
	public class ConstantNullCoalescingConditionTests : InspectionActionTestBase
	{
		[Test]
		public void TestNullRightSide()
		{
			Test<ConstantNullCoalescingConditionAnalyzer>(@"
class TestClass
{
	void Foo()
	{
		object o = new object () ?? null;
	}
}", @"
class TestClass
{
	void Foo()
	{
		object o = new object ();
	}
}");
		}

		[Test]
		public void TestNullLeftSide()
		{
			Test<ConstantNullCoalescingConditionAnalyzer>(@"
class TestClass
{
	void Foo()
	{
		object o = null ?? new object ();
	}
}", @"
class TestClass
{
	void Foo()
	{
		object o = new object ();
	}
}");
		}

		[Test]
		public void TestEqualExpressions()
		{
			Test<ConstantNullCoalescingConditionAnalyzer>(@"
class TestClass
{
	void Foo()
	{
		object o = new object () ?? new object ();
	}
}", @"
class TestClass
{
	void Foo()
	{
		object o = new object ();
	}
}");
		}

		[Test]
		public void TestSmartUsage()
		{
			//Previously, this was a "TestWrongContext".
			//However, since smart null coallescing was introduced, this can now be
			//detected as redundant
			Test<ConstantNullCoalescingConditionAnalyzer>(@"
class TestClass
{
	void Foo()
	{
		object o = new object () ?? """";
	}
}", @"
class TestClass
{
	void Foo()
	{
		object o = new object ();
	}
}");
		}

		[Test]
		public void TestSmartUsageInParam()
		{
			Analyze<ConstantNullCoalescingConditionAnalyzer>(@"
class TestClass
{
	void Foo(object o)
	{
		object p = o ?? """";
	}
}");
		}

		[Ignore("enable again")]
		[Test]
		public void TestDisable()
		{
			Analyze<ConstantNullCoalescingConditionAnalyzer>(@"
class TestClass
{
	void Foo()
	{
		// ReSharper disable once ConstantNullCoalescingCondition
		object o = new object () ?? null;
	}
}");
		}
	}
}