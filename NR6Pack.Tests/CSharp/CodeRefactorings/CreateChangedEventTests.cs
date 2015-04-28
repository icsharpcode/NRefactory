//
// CreateChangedEventTests.cs
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
using NUnit.Framework;
using ICSharpCode.NRefactory6.CSharp.Refactoring;

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[TestFixture]
	public class CreateChangedEventTests : ContextActionTestBase
	{
		[Test]
		public void TestSimpleCase()
		{
			Test<CreateChangedEventCodeRefactoringProvider>(@"class TestClass
{
    string test;
    public string $Test {
        get {
            return test;
        }
        set {
            test = value;
        }
    }
}", @"class TestClass
{
    string test;
    public string Test {
        get {
            return test;
        }
        set {
            test = value;
            OnTestChanged(System.EventArgs.Empty);
        }
    }

    protected virtual void OnTestChanged(System.EventArgs e)
    {
        TestChanged?.Invoke(this, e);
    }

    public event System.EventHandler TestChanged;
}");
		}

		[Test]
		public void TestSimplify()
		{
			Test<CreateChangedEventCodeRefactoringProvider>(@"using System;
class TestClass
{
    string test;
    public string $Test {
        get {
            return test;
        }
        set {
            test = value;
        }
    }
}", @"using System;
class TestClass
{
    string test;
    public string Test {
        get {
            return test;
        }
        set {
            test = value;
            OnTestChanged(EventArgs.Empty);
        }
    }

    protected virtual void OnTestChanged(EventArgs e)
    {
        TestChanged?.Invoke(this, e);
    }

    public event EventHandler TestChanged;
}");
		}

		[Test]
		public void TestStaticClassCase()
		{
			Test<CreateChangedEventCodeRefactoringProvider>(@"static class TestClass
{
    static string test;
    public static string $Test {
        get {
            return test;
        }
        set {
            test = value;
        }
    }
}", @"static class TestClass
{
    static string test;
    public static string Test {
        get {
            return test;
        }
        set {
            test = value;
            OnTestChanged(System.EventArgs.Empty);
        }
    }

    static void OnTestChanged(System.EventArgs e)
    {
        TestChanged?.Invoke(null, e);
    }

    public static event System.EventHandler TestChanged;
}");
		}

		[Test]
		public void TestSealedCase()
		{
			Test<CreateChangedEventCodeRefactoringProvider>(@"sealed class TestClass
{
    string test;
    public string $Test {
        get {
            return test;
        }
        set {
            test = value;
        }
    }
}", @"sealed class TestClass
{
    string test;
    public string Test {
        get {
            return test;
        }
        set {
            test = value;
            OnTestChanged(System.EventArgs.Empty);
        }
    }

    void OnTestChanged(System.EventArgs e)
    {
        TestChanged?.Invoke(this, e);
    }

    public event System.EventHandler TestChanged;
}");
		}

		[Test]
		public void TestWrongLocation()
		{
			TestWrongContext<CreateChangedEventCodeRefactoringProvider>(@"class TestClass
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

			TestWrongContext<CreateChangedEventCodeRefactoringProvider>(@"class TestClass
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

			TestWrongContext<CreateChangedEventCodeRefactoringProvider>(@"class TestClass
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

	}
}

