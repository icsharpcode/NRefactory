//
// StringCompareToIsCultureSpecificIssueTests.cs
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
using ICSharpCode.NRefactory.PlayScript.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.PlayScript
{
	[TestFixture]
	public class StringCompareToIsCultureSpecificIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TestSimpleCase()
		{
			Test<StringCompareToIsCultureSpecificIssue>(@"
public class Test
{
	void Foo (string b)
	{
		Console.WriteLine (""Foo"".CompareTo(b));
	}
}
", @"
public class Test
{
	void Foo (string b)
	{
		Console.WriteLine (string.Compare (""Foo"", b, System.StringComparison.Ordinal));
	}
}
");
		}

		[Test]
		public void TestInvalidCase()
		{
			TestWrongContext<StringCompareToIsCultureSpecificIssue>(@"
public class Test
{
	void Foo (object b)
	{
		Console.WriteLine (""Foo"".CompareTo(b));
	}
}
");
		}

		
		[Test]
		public void TestDisable()
		{
			TestWrongContext<StringCompareToIsCultureSpecificIssue>(@"
public class Test
{
	void Foo (string b)
	{
		// ReSharper disable once StringCompareToIsCultureSpecific
		Console.WriteLine (""Foo"".CompareTo(b));
	}
}
");
		}
	}
}

