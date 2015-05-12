//
// StringCompareIsCultureSpecificTests.cs
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
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[TestFixture]
	public class StringCompareIsCultureSpecificTests : InspectionActionTestBase
	{
		[Test]
		public void TestCase1()
		{
			Analyze<StringCompareIsCultureSpecificAnalyzer>(@"
class Test
{
	void Foo ()
	{
		Console.WriteLine ($string.Compare(""Foo"", ""Bar"")$);
	}
}", @"
class Test
{
	void Foo ()
	{
		Console.WriteLine (string.Compare(""Foo"", ""Bar"", System.StringComparison.Ordinal));
	}
}");
		}

		[Test]
		public void TestInvalidCase1()
		{
			Analyze<StringCompareIsCultureSpecificAnalyzer>(@"
class Test
{
	void Foo ()
	{
		Console.WriteLine (string.Compare(""Foo"", ""Bar"", System.StringComparison.Ordinal));
	}
}");
		}

		[Test]
		public void TestCase2()
		{
			Analyze<StringCompareIsCultureSpecificAnalyzer>(@"
class Test
{
	void Foo ()
	{
		Console.WriteLine ($System.String.Compare(""Foo"", ""Bar"", true)$);
	}
}", @"
class Test
{
	void Foo ()
	{
		Console.WriteLine (System.String.Compare(""Foo"", ""Bar"", System.StringComparison.OrdinalIgnoreCase));
	}
}");
		}

		[Test]
		public void TestInvalidCase2()
		{
			Analyze<StringCompareIsCultureSpecificAnalyzer>(@"
class Test
{
	void Foo ()
	{
		Console.WriteLine (string.Compare(""Foo"", ""Bar"", System.StringComparison.OrdinalIgnoreCase));
	}
}");
		}

		[Test]
		public void TestCase3()
		{
			Analyze<StringCompareIsCultureSpecificAnalyzer>(@"
class Test
{
	void Foo ()
	{
		Console.WriteLine ($string.Compare(""Foo"", ""Bar"", false)$);
	}
}", @"
class Test
{
	void Foo ()
	{
		Console.WriteLine (string.Compare(""Foo"", ""Bar"", System.StringComparison.Ordinal));
	}
}");
		}

		[Test]
		public void TestCase4()
		{
			Analyze<StringCompareIsCultureSpecificAnalyzer>(@"
class Test
{
	void Foo ()
	{
		Console.WriteLine ($string.Compare(""Foo"", 0, ""Bar"", 1, 1)$);
	}
}", @"
class Test
{
	void Foo ()
	{
		Console.WriteLine (string.Compare(""Foo"", 0, ""Bar"", 1, 1, System.StringComparison.Ordinal));
	}
}");
		}

		[Test]
		public void TestInvalidCase4()
		{
			Analyze<StringCompareIsCultureSpecificAnalyzer>(@"
class Test
{
	void Foo ()
	{
		Console.WriteLine (string.Compare(""Foo"", 0, ""Bar"", 1, 1, System.StringComparison.Ordinal));
	}
}");
		}

		[Test]
		public void TestCase5()
		{
			Analyze<StringCompareIsCultureSpecificAnalyzer>(@"
class Test
{
	void Foo ()
	{
		Console.WriteLine ($string.Compare(""Foo"", 0, ""Bar"", 1, 1, true)$);
	}
}", @"
class Test
{
	void Foo ()
	{
		Console.WriteLine (string.Compare(""Foo"", 0, ""Bar"", 1, 1, System.StringComparison.OrdinalIgnoreCase));
	}
}");
		}

		[Test]
		public void TestCase6()
		{
			Analyze<StringCompareIsCultureSpecificAnalyzer>(@"
class Test
{
	void Foo ()
	{
		Console.WriteLine ($string.Compare(""Foo"", 0, ""Bar"", 1, 1, false)$);
	}
}", @"
class Test
{
	void Foo ()
	{
		Console.WriteLine (string.Compare(""Foo"", 0, ""Bar"", 1, 1, System.StringComparison.Ordinal));
	}
}");
		}

		[Test]
		public void TestInvalid()
		{
			Analyze<StringCompareIsCultureSpecificAnalyzer>(@"
class Test
{
	void Foo ()
	{
		Console.WriteLine (string.Compare(""a"", ""b"", true, System.Globalization.CultureInfo.CurrentCulture));
	}
}");
		}

		[Test]
		public void TestComplex()
		{
			Analyze<StringCompareIsCultureSpecificAnalyzer>(@"
class Test
{
	void Foo (bool b)
	{
		Console.WriteLine ($string.Compare(""Foo"", ""Bar"", b)$);
	}
}", @"
class Test
{
	void Foo (bool b)
	{
		Console.WriteLine (string.Compare(""Foo"", ""Bar"", b ? System.StringComparison.OrdinalIgnoreCase : System.StringComparison.Ordinal));
	}
}");
		}

		[Test]
		public void TestDisable()
		{
			Analyze<StringCompareIsCultureSpecificAnalyzer>(@"
class Test
{
	void Foo()
	{
#pragma warning disable " + NRefactoryDiagnosticIDs.StringCompareIsCultureSpecificAnalyzerID + @"
		Console.WriteLine(string.Compare(""Foo"", ""Bar""));
	}
}");
		}
	}
}
