// 
// RedundantInternalInspectorTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory6.CSharp.CodeActions;

namespace ICSharpCode.NRefactory6.CSharp.CodeIssues
{
	[TestFixture]
	public class RedundantInternalInspectorTests : InspectionActionTestBase
	{
		[Test]
		public void TestInspectorCase1()
		{
			Analyze<RedundantInternalIssue>(@"
namespace Test
{
    $internal$ class Foo
    {
        internal void Bar(string str)
        {
        }
    }
}", @"
namespace Test
{
    class Foo
    {
        internal void Bar(string str)
        {
        }
    }
}");
		}

		[Test]
		public void TestInspectorCase1WithComment()
		{
			Analyze<RedundantInternalIssue>(@"
namespace Test
{
    /// <summary>
    /// Class description.
    /// </summary>
    $internal$ class Foo
    {
        internal void Bar(string str)
        {
        }
    }
}", @"
namespace Test
{
    /// <summary>
    /// Class description.
    /// </summary>
    class Foo
    {
        internal void Bar(string str)
        {
        }
    }
}");
		}

		[Test]
		public void TestNestedClass()
		{
			Analyze<RedundantInternalIssue>(@"
namespace Test
{
    class Foo
    {
        internal class Nested
        {
        }
    }
}");
		}

		[Test]
		public void TestNestedInPublicClass()
		{
			Analyze<RedundantInternalIssue>(@"
namespace Test
{
    public class Foo
    {
        internal class Nested
        {
        }
    }
}");
		}
	}
}
