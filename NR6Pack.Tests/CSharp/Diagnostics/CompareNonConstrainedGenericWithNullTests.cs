//
// CompareNonConstrainedGenericWithNullTests.cs
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
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[TestFixture]
	public class CompareNonConstrainedGenericWithNullTests : InspectionActionTestBase
	{
		[Test]
		public void TestLocal ()
		{
			Analyze<CompareNonConstrainedGenericWithNullAnalyzer>(@"public class Bar
{
	public void Foo<T> (T t)
	{
		if (t == $null$) {
		}
	}
}", @"public class Bar
{
	public void Foo<T> (T t)
	{
		if (t == default(T)) {
		}
	}
}");
		}

		[Test]
		public void TestField ()
		{
			Analyze<CompareNonConstrainedGenericWithNullAnalyzer>(@"public class Bar<T>
{
	T t;
	public void Foo ()
	{
		if (t == $null$) {
		}
	}
}", @"public class Bar<T>
{
	T t;
	public void Foo ()
	{
		if (t == default(T)) {
		}
	}
}");
		}

		[Test]
		public void TestInvalid ()
		{
			Analyze<CompareNonConstrainedGenericWithNullAnalyzer>(@"public class Bar
{
	public void Foo<T> (T t) where T : class
	{
		if (t == null) {
		}
	}
}");
		}

		[Test]
		public void TestDisable ()
		{
			Analyze<CompareNonConstrainedGenericWithNullAnalyzer>(@"public class Bar
{
	public void Foo<T> (T t)
	{
#pragma warning disable " + NRefactoryDiagnosticIDs.CompareNonConstrainedGenericWithNullAnalyzerID + @"
		if (t == null) {
		}
	}
}");
		}
	}
}

