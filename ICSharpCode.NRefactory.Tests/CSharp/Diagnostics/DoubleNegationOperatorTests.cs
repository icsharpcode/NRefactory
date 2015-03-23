// 
// DoubleNegationOperatorTests.cs
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
	public class DoubleNegationOperatorTests : InspectionActionTestBase
	{
		[Ignore("Roslyn bug in FindNode")]
		[Test]
		public void TestLogicalNot ()
		{
			Analyze<DoubleNegationOperatorAnalyzer> (@"
class TestClass
{
	bool GetBool () { }

	void TestMethod ()
	{
		var x = $!!GetBool ()$;
		x = $!(!(GetBool ()))$;
	}
}", @"
class TestClass
{
	bool GetBool () { }

	void TestMethod ()
	{
		var x = GetBool ();
		x = GetBool ();
	}
}");
		}

		[Test]
		public void TestBitwiseNot ()
		{
			Analyze<DoubleNegationOperatorAnalyzer> (@"
class TestClass
{
	void TestMethod ()
	{
		var x = $~(~(123))$;
	}
}", @"
class TestClass
{
	void TestMethod ()
	{
		var x = 123;
	}
}");
		}

		[Test]
		public void TestDisable ()
		{
			Analyze<DoubleNegationOperatorAnalyzer> (@"
class TestClass
{
	void TestMethod ()
	{
		// disable once DoubleNegationOperator
		var x = ~(~(123));
	}
}");
		}
	}
}
