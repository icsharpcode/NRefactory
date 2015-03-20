// 
// AddAnotherAccessorTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc.
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

namespace ICSharpCode.NRefactory6.CSharp.CodeActions
{
	[TestFixture]
	public class AddAnotherAccessorTests : ContextActionTestBase
	{
		[Test]
		public void TestAddSet()
		{
			Test<AddAnotherAccessorCodeRefactoringProvider>(@"
class TestClass
{
    int field;
	public int $Field 
    {
        get 
        {
            return field;
        }
	}
}", @"
class TestClass
{
    int field;
    public int Field
    {
        get
        {
            return field;
        }

        set
        {
            field = value;
        }
    }
}");
		}

		[Test]
		public void TestAddSet_ReadOnlyField()
		{
			Test<AddAnotherAccessorCodeRefactoringProvider>(@"
class TestClass
{
    readonly int field;
	public int $Field
    {
		get
        {
            return field;
        }
	}
}", @"
class TestClass
{
    readonly int field;
    public int Field
    {
        get
        {
            return field;
        }

        set
        {
            throw new System.NotImplementedException();
        }
    }
}");
		}

		[Test]
		public void TestAddGet()
		{
			Test<AddAnotherAccessorCodeRefactoringProvider>(@"
class TestClass
{
    int field;
    public int $Field {
        set 
        {
            field = value;
        }
    }
}", @"
class TestClass
{
    int field;
    public int Field
    {
        get
        {
            return field;
        }

        set
        {
            field = value;
        }
    }
}");
		}

		[Test]
		public void TestAddGetWithComment()
		{
			Test<AddAnotherAccessorCodeRefactoringProvider>(@"
class TestClass
{
    int field;
    public int $Field {
        // Some comment
        set 
        {
            field = value;
        }
    }
}", @"
class TestClass
{
    int field;
    public int Field
    {
        get
        {
            return field;
        }
        // Some comment
        set
        {
            field = value;
        }
    }
}");
		}

		[Test]
		public void TestAutoProperty()
		{
			Test<AddAnotherAccessorCodeRefactoringProvider>(@"
class TestClass
{
    string $Test 
    {
        get;
    }
}", @"
class TestClass
{
    string Test
    {
        get; set;
    }
}");
		}

		[Test]
		public void TestAutoPropertyWithComment()
		{
			Test<AddAnotherAccessorCodeRefactoringProvider>(@"
class TestClass
{
    string $Test 
    {
        // Some comment
        get;
    }
}", @"
class TestClass
{
    string Test
    {
        // Some comment
        get; set;
    }
}");
		}
	}
}
