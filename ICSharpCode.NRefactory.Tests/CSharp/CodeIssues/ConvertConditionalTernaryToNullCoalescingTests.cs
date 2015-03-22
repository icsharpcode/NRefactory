// 
// ConvertConditionalTernaryToNullCoalescingTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory6.CSharp.CodeRefactorings;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[TestFixture]
	public class ConvertConditionalTernaryToNullCoalescingTests : InspectionActionTestBase
	{
		[Test]
		public void TestInspectorCase1 ()
		{
			Analyze<ConvertConditionalTernaryToNullCoalescingAnalyzer>(@"class Foo
{
	void Bar (string str)
	{
		string c = $str != null ? str : ""default""$;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		string c = str ?? ""default"";
	}
}");

		}

		[Test]
		public void TestInspectorCase2 ()
		{
			Analyze<ConvertConditionalTernaryToNullCoalescingAnalyzer>(@"class Foo
{
	void Bar (string str)
	{
		string c = $null != str ? str : ""default""$;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		string c = str ?? ""default"";
	}
}");
		}

		[Test]
		public void TestInspectorCase3 ()
		{
			Analyze<ConvertConditionalTernaryToNullCoalescingAnalyzer>(@"class Foo
{
	void Bar (string str)
	{
		string c = $null == str ? ""default"" : str$;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		string c = str ?? ""default"";
	}
}");
		}

		[Test]
		public void TestInspectorCase4 ()
		{
			Analyze<ConvertConditionalTernaryToNullCoalescingAnalyzer>(@"class Foo
{
	void Bar (string str)
	{
		string c = $str == null ? ""default"" : str$;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		string c = str ?? ""default"";
	}
}");
		}

        [Test]
        public void TestDisable()
        {
			Analyze<ConvertConditionalTernaryToNullCoalescingAnalyzer>(@"class Foo
{
	void Bar (string str)
	{
// ReSharper disable once ConvertConditionalTernaryToNullCoalescing
		string c = str != null ? str : ""default"";
	}
}");
        }
	
		[Test]
		public void TestCastCase ()
		{
			Analyze<ConvertConditionalTernaryToNullCoalescingAnalyzer>(@"class Foo
{
	void Bar (Foo o, Bar b)
	{
		IDisposable c = $o != null ? (IDisposable)o : b$;
	}
}", @"class Foo
{
	void Bar (Foo o, Bar b)
	{
		IDisposable c = (IDisposable)o ?? b;
	}
}");
		}

		[Test]
		public void TestCastCase2 ()
		{
			Analyze<ConvertConditionalTernaryToNullCoalescingAnalyzer>(@"class Foo
{
	void Bar (Foo o, Bar b)
	{
		IDisposable c = $o == null ? (IDisposable)b : o$;
	}
}", @"class Foo
{
	void Bar (Foo o, Bar b)
	{
		IDisposable c = (IDisposable)o ?? b;
	}
}");
		}

		[Test]
		public void TestGenericCastCase()
		{
			Analyze<ConvertConditionalTernaryToNullCoalescingAnalyzer>(@"class Foo
{
	void Bar<T>(object o, T b)
	{
		T c = o != null ? (T)o : b;
	}
}");
		}

		[Test]
		public void TestGenericCastCaseWithRefTypeConstraint()
		{
			Analyze<ConvertConditionalTernaryToNullCoalescingAnalyzer>(@"class Foo
{
	void Bar<T>(object o, T b) where T : class
	{
		T c = $o != null ? (T)o : b$;
	}
}", @"class Foo
{
	void Bar<T>(object o, T b) where T : class
	{
		T c = (T)o ?? b;
	}
}");
		}

		[Test]
		public void TestGenericCastCaseAsNullable()
		{
			Analyze<ConvertConditionalTernaryToNullCoalescingAnalyzer>(@"class Foo
{
	void Bar<T>(object o, T b)
	{
		T? c = $o != null ? (T?)o : b$;
	}
}", @"class Foo
{
	void Bar<T>(object o, T b)
	{
		T? c = (T?)o ?? b;
	}
}");
		}

		[Test]
		public void TestNullableValueCase()
		{
			Analyze<ConvertConditionalTernaryToNullCoalescingAnalyzer>(@"class Test
{
    void TestCase()
    {
		int? x = null;
		int y = $x != null ? x.Value : 0$;
    }
}", @"class Test
{
    void TestCase()
    {
		int? x = null;
		int y = x ?? 0;
    }
}");
		}
	}
}

