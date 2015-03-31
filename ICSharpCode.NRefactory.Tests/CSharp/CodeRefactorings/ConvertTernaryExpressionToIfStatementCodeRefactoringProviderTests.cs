//
// ConvertTernaryExpressionToIfStatementCodeRefactoringProviderTests.cs
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

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[TestFixture]
	public class ConvertTernaryExpressionToIfStatementCodeRefactoringProviderTests : ContextActionTestBase
	{
		[Test]
		public void TestConditionalOperator()
		{
			Test<ConvertTernaryExpressionToIfStatementCodeRefactoringProvider>(@"
class TestClass
{
    int TestMethod (int o, int p)
    {
        int z;
        z $= i > 0 ? o : p;
        return z;
    }
}", @"
class TestClass
{
    int TestMethod (int o, int p)
    {
        int z;
        if (i > 0)
            z = o;
        else
            z = p;
        return z;
    }
}");
		}

		[Test]
		public void TestNullCoalescingOperator()
		{
			Test<ConvertTernaryExpressionToIfStatementCodeRefactoringProvider>(@"
class Test
{
    object TestMethod(object o, object p)
    {
        object z;
        z $= o ?? p;
        return z;
    }
}", @"
class Test
{
    object TestMethod(object o, object p)
    {
        object z;
        if (o != null)
            z = o;
        else
            z = p;
        return z;
    }
}");
		}

		[Test]
		public void TestEmbeddedStatement()
		{
			Test<ConvertTernaryExpressionToIfStatementCodeRefactoringProvider>(@"
class TestClass
{
    void TestMethod(int i)
    {
        int a;
        if (i < 10)
            a $= i > 0 ? 0 : 1;
    }
}", @"
class TestClass
{
    void TestMethod(int i)
    {
        int a;
        if (i < 10)
            if (i > 0)
                a = 0;
            else
                a = 1;
    }
}");
		}


		[Test]
		public void TestAssignment()
		{
			Test<ConvertTernaryExpressionToIfStatementCodeRefactoringProvider>(@"
class TestClass
{
    void TestMethod (int i)
    {
        int a;
        a $= i > 0 ? 0 : 1;
    }
}", @"
class TestClass
{
    void TestMethod (int i)
    {
        int a;
        if (i > 0)
            a = 0;
        else
            a = 1;
    }
}");
			Test<ConvertTernaryExpressionToIfStatementCodeRefactoringProvider>(@"
class TestClass
{
    void TestMethod (int i)
    {
        int a;
        a $+= i > 0 ? 0 : 1;
    }
}", @"
class TestClass
{
    void TestMethod (int i)
    {
        int a;
        if (i > 0)
            a += 0;
        else
            a += 1;
    }
}");
		}

		[Test]
		public void TestReturnConditionalOperator()
		{
			Test<ConvertTernaryExpressionToIfStatementCodeRefactoringProvider>(@"
class TestClass
{
    int TestMethod(int i)
    {
        $return i > 0 ? 1 : 0;
    }
}", @"
class TestClass
{
    int TestMethod(int i)
    {
        if (i > 0)
            return 1;
        return 0;
    }
}");
		}

		[Test]
		public void TestReturnConditionalOperatorWithComment()
		{
			Test<ConvertTernaryExpressionToIfStatementCodeRefactoringProvider>(@"
class TestClass
{
    int TestMethod(int i)
    {
        // Some comment
        $return i > 0 ? 1 : 0;
    }
}", @"
class TestClass
{
    int TestMethod(int i)
    {
        // Some comment
        if (i > 0)
            return 1;
        return 0;
    }
}");
		}

		[Test]
		public void TestReturnNullCoalescingOperator()
		{
			Test<ConvertTernaryExpressionToIfStatementCodeRefactoringProvider>(@"
class Test
{
    object Foo(object o, object p)
    {
        $return o ?? p;
    }
}", @"
class Test
{
    object Foo(object o, object p)
    {
        if (o != null)
            return o;
        return p;
    }
}");
		}
	}
}