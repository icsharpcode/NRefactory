// 
// RemoveBackingStoreTests.cs
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

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[TestFixture]
	public class ReplacePropertyWithBackingFieldWithAutoPropertyTests : ContextActionTestBase
	{
		[Test]
		public void TestSimpleStore()
		{
			Test<ReplacePropertyWithBackingFieldWithAutoPropertyCodeRefactoringProvider>(@"
class TestClass
{
    int field;
    public int $Field
    {
        get { return field; }
        set { field = value; }
    }
}
", @"
class TestClass
{
    public int Field { get; set; }
}
");
		}

		[Test]
		public void TestSimpleStoreWithXmlDoc()
		{
			Test<ReplacePropertyWithBackingFieldWithAutoPropertyCodeRefactoringProvider>(@"
class TestClass
{
    int field;

    /// <summery>
    /// Description of this field.
    /// </summary>
    public int $Field
    {
        get { return field; }
        set { field = value; }
    }
}
", @"
class TestClass
{

    /// <summery>
    /// Description of this field.
    /// </summary>
    public int Field { get; set; }
}
");
		}

		/// <summary>
		/// Bug 3292 -Error in analysis service
		/// </summary>
		[Test]
		public void TestBug3292()
		{
			TestWrongContext<ReplacePropertyWithBackingFieldWithAutoPropertyCodeRefactoringProvider>(
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	int field;" + Environment.NewLine +
				"	public int $Field {" + Environment.NewLine +
				"		get { " +
				"			Console.WriteLine(field);" +
				"		}" + Environment.NewLine +
				"		set { field = value; }" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}"
			);
		}

		[Test()]
		public void TestBug3292Case2()
		{
			TestWrongContext<ReplacePropertyWithBackingFieldWithAutoPropertyCodeRefactoringProvider>(
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	int field;" + Environment.NewLine +
				"	public int $Field {" + Environment.NewLine +
				"		get { " +
				"			return field;" +
				"		}" + Environment.NewLine +
				"		set { Console.WriteLine(field); }" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}"
			);
		}


		[Test]
		public void TestWrongLocation()
		{
			TestWrongContext<ReplacePropertyWithBackingFieldWithAutoPropertyCodeRefactoringProvider>(@"class TestClass
{
	string test;
	public $string Test {
		get {
			return test;
		}
		set {
			test = value;
		}
	}
}");

			TestWrongContext<ReplacePropertyWithBackingFieldWithAutoPropertyCodeRefactoringProvider>(@"class TestClass
{
	string test;
	public string $FooBar.Test {
		get {
			return test;
		}
		set {
			test = value;
		}
	}
}");

			TestWrongContext<ReplacePropertyWithBackingFieldWithAutoPropertyCodeRefactoringProvider>(@"class TestClass
{
	string test;
	public string Test ${
		get {
			return test;
		}
		set {
			test = value;
		}
	}
}");
		}

		/// <summary>
		/// Bug 16108 - Convert to autoproperty issues
		/// </summary>
		[Test]
		public void TestBug16108Case1()
		{
			TestWrongContext<ReplacePropertyWithBackingFieldWithAutoPropertyCodeRefactoringProvider>(@"
class MyClass
{
    [DebuggerHiddenAttribute]
    int a;
    int $A {
        get { return a; }
        set { a = value; }
    }
}
");
		}

		/// <summary>
		/// Bug 16108 - Convert to autoproperty issues
		/// </summary>
		[Test]
		public void TestBug16108Case2()
		{
			TestWrongContext<ReplacePropertyWithBackingFieldWithAutoPropertyCodeRefactoringProvider>(@"
class MyClass
{
    int a = 4;
    int $A {
        get { return a; }
        set { a = value; }
    }
}
");
		}


		/// <summary>
		/// Bug 16447 - Convert to Auto Property removes multiple variable if declared inline
		/// </summary>
		[Test]
		public void TestBug16447()
		{
			Test<ReplacePropertyWithBackingFieldWithAutoPropertyCodeRefactoringProvider>(@"
public class Foo
{
	int _bpm = 120, _index = 1, _count;
	int $Count {
		get { return _count; }
		set { _count = value; }
	}
}
", @"
public class Foo
{
    int _bpm = 120, _index = 1;
    int Count { get; set; }
}
");
		}


		[Test]
		public void TestUnimplementedComputedProperty()
		{
			Test<ReplacePropertyWithBackingFieldWithAutoPropertyCodeRefactoringProvider>(@"
class TestClass
{
    public int $Field
    {
        get
        {
            throw new System.NotImplementedException();
        }

        set
        {
            throw new System.NotImplementedException();
        }
    }
}
", @"
class TestClass
{
    public int Field { get; set; }
}
");
		}
	}
}

