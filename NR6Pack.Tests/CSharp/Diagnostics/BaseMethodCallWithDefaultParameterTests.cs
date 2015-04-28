//
// BaseMethodCallWithDefaultParameterTests.cs
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
using ICSharpCode.NRefactory6.CSharp.CodeRefactorings;
using ICSharpCode.NRefactory6.CSharp.Refactoring;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[TestFixture]
	public class BaseMethodCallWithDefaultParameterTests : InspectionActionTestBase
	{
		[Test]
		public void TestBasicCase ()
		{
			Analyze<BaseMethodCallWithDefaultParameterAnalyzer>(@"
public class MyBase
{
	public virtual void FooBar(int x = 12)
	{
		System.Console.WriteLine(""Foo Bar"" + x);
	}
}

public class MyClass : MyBase
{
	public override void FooBar(int x = 12)
	{
		$base.FooBar()$;
	}
}
");
		}

		[Test]
		public void TestInterfaceCase ()
		{
			Analyze<BaseMethodCallWithDefaultParameterAnalyzer>(@"
public class MyBase
{
	public virtual int this[int x, int y = 12] {
		get {
			return 1;
		}
	}
}

public class MyClass : MyBase
{
	public override int this[int x, int y = 12] {
		get {
			return $base[x]$;
		}
	}
}
");

		}

		[Test]
		public void TestDoNotWarnCase ()
		{
			Analyze<BaseMethodCallWithDefaultParameterAnalyzer>(@"
public class MyBase
{
	public virtual void FooBar(int x = 12)
	{
		System.Console.WriteLine(""Foo Bar"" + x);
	}
}

public class MyClass : MyBase
{
	public override void FooBar(int x = 12)
	{
		base.FooBar(11);
	}
}
");
		}

		[Test]
		public void TestDoNotWarnInParamsCase ()
		{
			Analyze<BaseMethodCallWithDefaultParameterAnalyzer>(@"
public class MyBase
{
	public virtual void FooBar(params int[] x)
	{
		System.Console.WriteLine(""Foo Bar"" + x);
	}
}

public class MyClass : MyBase
{
	public override void FooBar(params int[] x)
	{
		base.FooBar();
	}
}
");
		}

		[Test]
		public void TestDisable ()
		{
			Analyze<BaseMethodCallWithDefaultParameterAnalyzer> (@"
public class MyBase
{
	public virtual void FooBar(int x = 12)
	{
		System.Console.WriteLine(""Foo Bar"" + x);
	}
}

public class MyClass : MyBase
{
	public override void FooBar(int x = 12)
	{
#pragma warning disable " + NRefactoryDiagnosticIDs.BaseMethodCallWithDefaultParameterDiagnosticID  + @"
		base.FooBar();
	}
}
");
		}
	}
}

