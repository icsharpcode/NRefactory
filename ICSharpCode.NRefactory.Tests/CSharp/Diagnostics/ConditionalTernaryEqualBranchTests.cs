//
// ConditionalTernaryEqualBranchTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using System;
using NUnit.Framework;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using ICSharpCode.NRefactory6.CSharp.CodeRefactorings;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[TestFixture]
	public class ConditionalTernaryEqualBranchTests : InspectionActionTestBase
	{
		[Test]
		public void TestInspectorCase1 ()
		{
			Analyze<ConditionalTernaryEqualBranchAnalyzer>(@"class Foo
{
	void Bar (string str)
	{
		string c = $str != null ? ""default"" : ""default""$;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		string c = ""default"";
	}
}");

		}

		[Test]
		public void TestMoreComplexBranch ()
		{
			Analyze<ConditionalTernaryEqualBranchAnalyzer>(@"class Foo
{
	void Bar (string str)
	{
		var c = $str != null ? 3 + (3 * 4) - 12 * str.Length : 3 + (3 * 4) - 12 * str.Length$;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		var c = 3 + (3 * 4) - 12 * str.Length;
	}
}");

		}

		[Test]
		public void TestNotEqualBranches ()
		{
			Analyze<ConditionalTernaryEqualBranchAnalyzer>(@"class Foo
{
	void Bar (string str)
	{
		string c = str != null ? ""default"" : ""default2"";
	}
}");

		}

		[Test]
		public void TestResharperSuppression ()
		{
			Analyze<ConditionalTernaryEqualBranchAnalyzer>(@"class Foo
{
	void Bar (string str)
	{
// ReSharper disable once ConditionalTernaryEqualBranch
		string c = str != null ? ""default"" : ""default"";
	}
}");

		}

        [Test]
        public void Test()
        {
			Analyze<ConditionalTernaryEqualBranchAnalyzer>(@"
class TestClass
{
	void TestMethod (int i)
	{
		var a = $i > 0 ? 1 + 1 : 1 + 1$;
		var b = i > 1 ? 1 : 2;
	}
}", @"
class TestClass
{
	void TestMethod (int i)
	{
		var a = 1 + 1;
		var b = i > 1 ? 1 : 2;
	}
}");
        }
	}
}

