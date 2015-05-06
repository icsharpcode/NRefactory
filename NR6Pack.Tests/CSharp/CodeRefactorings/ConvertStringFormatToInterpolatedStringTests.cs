//
// ConvertStringFormatToInterpolatedStringTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[TestFixture]
	public class ConvertStringFormatToInterpolatedStringTests : ContextActionTestBase
	{
		[Test]
		public void TestSimpleStringFormat ()
		{
			Test<ConvertStringFormatToInterpolatedStringCodeRefactoringProvider> (@"
class TestClass
{
    void Foo ()
    {
        var world = ""World"";
        var str = $string.Format (""Hello {0}"", world);
    }
}", @"
class TestClass
{
    void Foo ()
    {
        var world = ""World"";
        var str = $""Hello {world}"";
    }
}");
		}

		[Test]
		public void TestComplexStringFormat ()
		{
			Test<ConvertStringFormatToInterpolatedStringCodeRefactoringProvider> (@"
class TestClass
{
    void Foo ()
    {
        var str = $string.Format (""Hello {0:0.0} {1:0X}"", 0.5d,2134);
    }
}", @"
class TestClass
{
    void Foo ()
    {
        var str = $""Hello {0.5d:0.0} {2134:0X}"";
    }
}");
		}

	}
}

