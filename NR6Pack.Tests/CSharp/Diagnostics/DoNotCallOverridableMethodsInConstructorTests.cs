//
// DoNotCallOverridableMethodsInConstructorTests.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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
using ICSharpCode.NRefactory6.CSharp.CodeRefactorings;
using ICSharpCode.NRefactory6.CSharp.Refactoring;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[TestFixture]
	public class DoNotCallOverridableMethodsInConstructorTests : InspectionActionTestBase
	{ 
		[Test]
		public void CatchesBadCase()
		{
			Analyze<DoNotCallOverridableMethodsInConstructorAnalyzer>(@"class Foo
{
	Foo()
	{
		$Bar()$;
		$this.Bar()$;
	}

	virtual void Bar ()
	{
	}
}");
		}

		[Test]
		public void TestDisable()
		{
            Analyze<DoNotCallOverridableMethodsInConstructorAnalyzer>(@"class Foo
{
	Foo()
	{
#pragma warning disable " + NRefactoryDiagnosticIDs.DoNotCallOverridableMethodsInConstructorAnalyzerID + @"
		Bar();
	}

	virtual void Bar ()
	{
	}
}");
		}



		[Test]
		public void IgnoresGoodCase()
		{
			Analyze<DoNotCallOverridableMethodsInConstructorAnalyzer>(@"class Foo
{
	Foo()
	{
		Bar();
		Bar();
	}

	void Bar ()
	{
	}
}");
		}
		
		[Test]
		public void IgnoresSealedClasses()
		{
			Analyze<DoNotCallOverridableMethodsInConstructorAnalyzer>(@"sealed class Foo
{
	Foo()
	{
		Bar();
		Bar();
	}

	virtual void Bar ()
	{
	}
}");
		}
		
		[Test]
		public void IgnoresNonLocalCalls()
		{
			Analyze<DoNotCallOverridableMethodsInConstructorAnalyzer>(@"class Foo
{
	Foo()
	{
		Foo f = new Foo();
		f.Bar();
	}

	virtual void Bar ()
	{
	}
}");
		}
		
		[Test]
		public void IgnoresEventHandlers()
		{
			Analyze<DoNotCallOverridableMethodsInConstructorAnalyzer>(@"class Foo
{
	Foo()
	{
		SomeEvent += delegate { Bar(); };
	}

	virtual void Bar ()
	{
	}
}");
		}


		/// <summary>
		/// Bug 14450 - False positive of "Virtual member call in constructor"
		/// </summary>
		[Test]
		public void TestBug14450()
		{
			Analyze<DoNotCallOverridableMethodsInConstructorAnalyzer>(@"
using System;

public class Test {
    public Test(Action action) {
        action();
    }
}
");
		}
		
		[Test]
		public void SetVirtualProperty()
		{
			Analyze<DoNotCallOverridableMethodsInConstructorAnalyzer>(@"class Foo
{
	Foo()
	{
		$this.AutoProperty$ = 1;
	}

	public virtual int AutoProperty { get; set; }
}");
		}
	}
}
