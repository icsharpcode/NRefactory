// 
// ConvertImplicitToExplicittImplementationTests.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang <lightyang0@gmail.com>
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
	public class ConvertImplicitToExplicittImplementationTests : ContextActionTestBase
	{
		[Test]
		public void Test()
		{
			Test<ConvertImplicitToExplicitImplementationAction>(@"
interface ITest
{
    void Method ();
}
class TestClass : ITest
{
    public void $Method()
    {
    }
}", @"
interface ITest
{
    void Method ();
}
class TestClass : ITest
{
    void ITest.Method()
    {
    }
}");
		}

		[Test]
		public void TestWithXmlDoc()
		{
			Test<ConvertImplicitToExplicitImplementationAction>(@"
interface ITest
{
    void Method ();
}
class TestClass : ITest
{
    /// <summary>
    /// Some method description.
    /// </summary>
    public void $Method()
    {
    }
}", @"
interface ITest
{
    void Method ();
}
class TestClass : ITest
{
    /// <summary>
    /// Some method description.
    /// </summary>
    void ITest.Method()
    {
    }
}");
		}

		[Test]
		public void TestWithInlineComment()
		{
			Test<ConvertImplicitToExplicitImplementationAction>(@"
interface ITest
{
    void Method ();
}
class TestClass : ITest
{
    public void $Method() // Some comment
    {
    }
}", @"
interface ITest
{
    void Method ();
}
class TestClass : ITest
{
    void ITest.Method() // Some comment
    {
    }
}");
		}

		[Test]
		public void TestProperty()
		{
			Test<ConvertImplicitToExplicitImplementationAction>(@"
interface ITest
{
    int Prop { get; set; }
}
class TestClass : ITest
{
    public int $Prop
    {
        get { }
        set { }
    }
}", @"
interface ITest
{
    int Prop { get; set; }
}
class TestClass : ITest
{
    int ITest.Prop
    {
        get { }
        set { }
    }
}");
		}

		[Test]
		public void TestEvent()
		{
			Test<ConvertImplicitToExplicitImplementationAction>(@"
using System;

interface ITest
{
    event EventHandler Evt;
}
class TestClass : ITest
{
    public event EventHandler $Evt
    {
        add { }
        remove { }
    }
}", @"
using System;

interface ITest
{
    event EventHandler Evt;
}
class TestClass : ITest
{
    event EventHandler ITest.Evt
    {
        add { }
        remove { }
    }
}");
		}

		[Test]
		public void TestIndexer()
		{
			Test<ConvertImplicitToExplicitImplementationAction>(@"
interface ITest
{
    int this[int i] { get; }
}
class TestClass : ITest
{
    public int $this[int i]
    {
        get { }
    }
}", @"
interface ITest
{
    int this[int i] { get; }
}
class TestClass : ITest
{
    int ITest.this[int i]
    {
        get { }
    }
}");
		}

		[Test]
		public void TestMultipleInterfaces()
		{
			TestWrongContext<ConvertImplicitToExplicitImplementationAction>(@"
interface ITest
{
    void Method ();
}
interface ITest2
{
    void Method ();
}
class TestClass : ITest, ITest2
{
    void $Method ()
    {
    }
}");
		}

		[Test]
		public void TestNonImplicitImplementation()
		{
			TestWrongContext<ConvertImplicitToExplicitImplementationAction>(@"
class TestClass
{
    void $Method ()
    {
    }
}");
		}

		[Test]
		public void TestInterfaceMethod()
		{
			TestWrongContext<ConvertImplicitToExplicitImplementationAction>(@"
interface ITest
{
    void Method ();
}
interface ITest2 : ITest
{
    void $Method ();
}");
		}

	}
}
