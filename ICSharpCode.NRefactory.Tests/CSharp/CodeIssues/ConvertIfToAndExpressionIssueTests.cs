//
// ConvertIfToAndExpressionIssueTests.cs
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
using ICSharpCode.NRefactory6.CSharp.CodeActions;

namespace ICSharpCode.NRefactory6.CSharp.CodeIssues
{
	[TestFixture]
	public class ConvertIfToAndExpressionIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TestVariableDeclarationCase ()
		{
			Analyze<ConvertIfToAndExpressionIssue>(@"class Foo
{
	int Bar(int o)
	{
		bool b = o > 10;
		$if$ (o < 10)
			b = false;
	}
}", @"class Foo
{
	int Bar(int o)
	{
		bool b = o > 10 && o >= 10;
	}
}");
		}

		[Test]
		public void TestVariableDeclarationCaseAndComment()
		{
			Analyze<ConvertIfToAndExpressionIssue>(@"class Foo
{
	int Bar(int o)
	{
		// Some comment
		bool b = o > 10;
		$if$ (o < 10)
			b = false;
	}
}", @"class Foo
{
	int Bar(int o)
	{
		// Some comment
		bool b = o > 10 && o >= 10;
	}
}");
		}

		[Test]
		public void TestVariableDeclarationCaseWithBlock()
		{
			Analyze<ConvertIfToAndExpressionIssue>(@"class Foo
{
	int Bar(int o)
	{
		bool b = o > 10;
		$if$ (o < 10)
		{
			b = false;
		}
	}
}", @"class Foo
{
	int Bar(int o)
	{
		bool b = o > 10 && o >= 10;
	}
}");
		}

		[Test]
		public void TestComplexVariableDeclarationCase ()
		{
			Analyze<ConvertIfToAndExpressionIssue>(@"class Foo
{
	int Bar(int o)
	{
		bool b = o > 10 || o < 10;
		$if$ (o < 10)
			b = false;
	}
}", @"class Foo
{
	int Bar(int o)
	{
		bool b = (o > 10 || o < 10) && o >= 10;
	}
}");
		}

		[Test]
		public void TestConversionBug ()
		{
			Analyze<ConvertIfToAndExpressionIssue>(@"class Foo
{
	public override void VisitComposedType (ComposedType composedType)
	{
		$if$ (composedType.PointerRank > 0)
			unsafeStateStack.Peek ().UseUnsafeConstructs = false;
	}
}", @"class Foo
{
	public override void VisitComposedType (ComposedType composedType)
	{
		unsafeStateStack.Peek ().UseUnsafeConstructs &= composedType.PointerRank <= 0;
	}
}");
		}

		[Test]
		public void TestCommonCase ()
		{
			Analyze<ConvertIfToAndExpressionIssue>(@"class Foo
{
	int Bar(int o)
	{
		bool b = o > 10;
		Console.WriteLine ();
		$if$ (o < 10)
			b = false;
	}
}", @"class Foo
{
	int Bar(int o)
	{
		bool b = o > 10;
		Console.WriteLine ();
		b &= o >= 10;
	}
}");
		}

		[Test]
		public void TestCommonCaseWithComment()
		{
			Analyze<ConvertIfToAndExpressionIssue>(@"class Foo
{
	int Bar(int o)
	{
		bool b = o > 10;
		Console.WriteLine ();
		// Some comment
		$if$ (o < 10)
			b = false;
	}
}", @"class Foo
{
	int Bar(int o)
	{
		bool b = o > 10;
		Console.WriteLine ();
		// Some comment
		b &= o >= 10;
	}
}");
		}

		[Test]
		public void TestCommonCaseWithBlock()
		{
			Analyze<ConvertIfToAndExpressionIssue>(@"class Foo
{
	int Bar(int o)
	{
		bool b = o > 10;
		Console.WriteLine ();
		$if$ (o < 10)
		{
			b = false;
		}
	}
}", @"class Foo
{
	int Bar(int o)
	{
		bool b = o > 10;
		Console.WriteLine ();
		b &= o >= 10;
	}
}");
		}

		[Test]
		public void TestCommonCaseWithElse()
		{
			Analyze<ConvertIfToAndExpressionIssue>(@"class Foo
{
	int Bar(int o)
	{
		bool b = o > 10;
		Console.WriteLine ();
		if (o < 10)
		{
			b = false;
		}
		else
		{
			return 42;
		}
	}
}");
		}

		[Test]
		public void TestNullCheckBug()
		{
			Analyze<ConvertIfToAndExpressionIssue>(@"class Foo
{
	public bool Enabled { get; set; }

	int Bar(Foo fileChangeWatcher)
	{
		if (fileChangeWatcher != null)
			fileChangeWatcher.Enabled = true;
	}
}");
		}
	}
}

