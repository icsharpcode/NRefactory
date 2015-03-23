// 
// CompareBooleanWithTrueOrFalseTests.cs
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

using ICSharpCode.NRefactory6.CSharp.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[TestFixture]
	[Ignore("TODO: Issue not ported yet")]
	public class RedundantBoolCompareTests : InspectionActionTestBase
	{
		[Test]
		public void Test ()
		{
			var input = @"
class TestClass
{
	void TestMethod (bool x)
	{
		bool y;
		y = x == true;
		y = x == false;
		y = x != false;
		y = x != true;
	}
}";
			var output = @"
class TestClass
{
	void TestMethod (bool x)
	{
		bool y;
		y = x;
		y = !x;
		y = x;
		y = !x;
	}
}";
			Test<RedundantBoolCompareAnalyzer> (input, 4, output);
		}

		[Test]
		public void TestInsertParentheses ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		bool y = 2 > 1 == false;
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		bool y = !(2 > 1);
	}
}";
			Test<RedundantBoolCompareAnalyzer> (input, 1, output);
		}

		[Test]
		public void TestInvalid ()
		{
			Analyze<RedundantBoolCompareAnalyzer> (@"
class TestClass
{
	void TestMethod (bool? x)
	{
		bool y;
		y = x == true;
		y = x == false;
		y = x != false;
		y = x != true;
	}
}");
		}

		[Test]
		public void TestNullable ()
		{
			var input = @"
class TestClass
{
	void TestMethod (bool? x)
	{
		var y = x == false;
	}
}";
			Test<RedundantBoolCompareAnalyzer> (input, 0);
		}
	}
}
