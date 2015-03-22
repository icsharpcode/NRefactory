//
// SplitIfWithOrConditionInTwoTests.cs
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

namespace ICSharpCode.NRefactory6.CSharp.CodeActions
{
	[TestFixture]
	public class SplitIfWithOrConditionInTwoTests : ContextActionTestBase
	{
		
		[Test]
		public void TestOrSimple()
		{
			Test<SplitIfWithOrConditionInTwoCodeRefactoringProvider>(@"
class Test
{
	void Foo (bool a, bool b)
	{
		if (a $|| b) {
			return;
		}
	}
}
", @"
class Test
{
	void Foo (bool a, bool b)
	{
        if (a)
        {
            return;
        }
        else if (b)
        {
            return;
        }
    }
}
");
		}

		[Test]
		public void TestOrIfElse()
		{
			Test<SplitIfWithOrConditionInTwoCodeRefactoringProvider>(@"
class Test
{
	void Foo (bool a, bool b)
	{
		if (a $|| b) {
			return;
		} else {
			Something ();
		}
	}
}
", @"
class Test
{
	void Foo (bool a, bool b)
	{
        if (a)
        {
            return;
        }
        else if (b)
        {
            return;
        }
        else
        {
            Something();
        }
    }
}
");
		}
			

		[Test]
		public void TestAndOr()
		{
			Test<SplitIfWithOrConditionInTwoCodeRefactoringProvider>(@"
class Test
{
	void Foo (bool a, bool b)
	{
		if (!b $|| a && b) {
			return;
		}
	}
}
", @"
class Test
{
	void Foo (bool a, bool b)
	{
        if (!b)
        {
            return;
        }
        else if (a && b)
        {
            return;
        }
    }
}
");
		}
	}


}

