// 
// RedundantComparisonWithNullIssueTests.cs
//  
// Author:
//       Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013 Ji Kun
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
	[Ignore("TODO: Issue not ported yet")]
	public class RedundantComparisonWithNullIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TestInspectorCase1()
		{
			Test<RedundantComparisonWithNullIssue> (@"using System;class Test {public void test(){int a = 0;if(a is int && a != null){a = 1;}}}", @"using System;class Test {public void test(){int a = 0;
		if (a is int) {
			a = 1;
		}}}");
		}

		[Test]
		public void TestResharperDisable()
		{
			Analyze<RedundantComparisonWithNullIssue> (@"using System;
class Test {
	public void test(){
	int a = 0;
	//Resharper disable RedundantComparisonWithNull
	if(a is int && a != null)
	{a = 1;}
	//Resharper restore RedundantComparisonWithNull
	}	
	}");
		}

		[Test]
		public void TestInspectorCase2()
		{
			Test<RedundantComparisonWithNullIssue> (@"using System;class Test {public void test(){int a = 0;while(a != null && a is int){a = 1;}}}", @"using System;class Test {public void test(){int a = 0;
		while (a is int) {
			a = 1;
		}}}");
		}

		[Test]
		public void TestCaseWithFullParens()
		{
			Test<RedundantComparisonWithNullIssue> (@"using System;
class TestClass
{
	public void Test(object o)
	{
		if (!((o is int) && (o != null))) {
		}
	}
}", @"using System;
class TestClass
{
	public void Test(object o)
	{
		if (!(o is int)) {
		}
	}
}");
		}

        [Test]
        public void TestDisable()
        {
			Analyze<RedundantComparisonWithNullIssue> (
                @"using System;
class TestClass
{
	public void Test(object o)
	{
// ReSharper disable once RedundantComparisonWithNull
		if (!((o is int) && (o != null))) {
		}
	}
}");
        }


		[Ignore("Extended version")]
		[Test]
		public void TestNegatedCase()
		{
			Test<RedundantComparisonWithNullIssue> (@"using System;
class TestClass
{
	public void Test(object o)
	{
		if (null == o || !(o is int)) {
		}
	}
}", @"using System;
class TestClass
{
	public void Test(object o)
	{
		if (!(o is int)) {
		}
	}
}");
		}
	}
}