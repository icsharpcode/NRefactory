//
// RedundantToStringCallIssueTests.cs
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
using ICSharpCode.NRefactory6.CSharp.CodeActions;
using ICSharpCode.NRefactory6.CSharp.Refactoring;

namespace ICSharpCode.NRefactory6.CSharp.CodeIssues
{
	[TestFixture]
	[Ignore("TODO: Issue not ported yet")]
	public class RedundantToStringCallIssueTests : InspectionActionTestBase
	{
		
		[Test]
		public void ConcatenationOperator ()
		{
			Test<RedundantToStringCallIssue>(@"
class Foo
{
	void Bar (object i)
	{
		string s = """" + i.ToString() + """" + i.ToString();
	}
}", 2, @"
class Foo
{
	void Bar (object i)
	{
		string s = """" + i + """" + i;
	}
}");
		}

		[Test]
		public void TestValueTypes ()
		{
			Analyze<RedundantToStringCallIssue>(@"
class Foo
{
	void Bar (int i)
	{
		string s = """" + i.ToString() + """" + i.ToString();
	}
}");
		}


		[Test]
		public void ConcatenationOperatorWithToStringAsOnlyString ()
		{
			Analyze<RedundantToStringCallIssue>(@"
class Foo
{
	void Bar (int i)
	{
		string s = i.ToString() + i + i + i + 1.3;
	}
}");
		}
		
		[Test]
		public void IgnoresCallsToIFormattableToString ()
		{
			Analyze<RedundantToStringCallIssue>(@"
class Foo
{
	void Bar (System.DateTime dt)
	{
		string s = dt.ToString("""", CultureInfo.InvariantCulture) + string.Empty;
	}
}");
		}
		
		[Test]
		public void StringTarget ()
		{
			Test<RedundantToStringCallIssue>(@"
class Foo
{
	void Bar (string str)
	{
		string s = str.ToString();
		string inOperator = """" + str.ToString();
	}
}", 2, @"
class Foo
{
	void Bar (string str)
	{
		string s = str;
		string inOperator = """" + str;
	}
}");
		}
		
		[Test]
		public void FormatStringTests ()
		{
			Test<RedundantToStringCallIssue>(@"
class Foo
{
	void Bar (object i)
	{
		string s = string.Format(""{0}"", i.ToString());
	}
}", @"
class Foo
{
	void Bar (object i)
	{
		string s = string.Format (""{0}"", i);
	}
}");
		}
		
		[Test]
		public void HandlesNonLiteralFormatParameter ()
		{
			Test<RedundantToStringCallIssue>(@"
class Foo
{
	void Bar (object i)
	{
		string format = ""{0}"";
		string s = string.Format(format, i.ToString());
	}
}", @"
class Foo
{
	void Bar (object i)
	{
		string format = ""{0}"";
		string s = string.Format (format, i);
	}
}");
		}
		
		[Test]
		public void FormatStringWithNonObjectParameterTests ()
		{
			Test<RedundantToStringCallIssue>(@"
class Foo
{
	void Bar (object i)
	{
		string s = FakeFormat(""{0} {1}"", i.ToString(), i.ToString());
	}

	void FakeFormat(string format, string arg0, object arg1)
	{
	}
	void FakeFormat(string format, params object[] arg1)
	{
	}
}", @"
class Foo
{
	void Bar (object i)
	{
		string s = FakeFormat (""{0} {1}"", i.ToString (), i);
	}

	void FakeFormat(string format, string arg0, object arg1)
	{
	}
	void FakeFormat(string format, params object[] arg1)
	{
	}
}");
		}
		
		[Test]
		public void FormatMethodWithObjectParamsArray ()
		{
			Test<RedundantToStringCallIssue>(@"
class Foo
{
	void Bar (object i)
	{
		string s = FakeFormat(""{0} {1}"", i.ToString(), i.ToString());
	}

	void FakeFormat(string format, params object[] args)
	{
	}
}", 2, @"
class Foo
{
	void Bar (object i)
	{
		string s = FakeFormat (""{0} {1}"", i, i);
	}

	void FakeFormat(string format, params object[] args)
	{
	}
}");
		}
		
		[Test, Ignore("broken")]
		public void DetectsBlacklistedCalls ()
		{
			Test<RedundantToStringCallIssue>(@"
class Foo
{
	void Bar (object i)
	{
		var w = new System.IO.StringWriter ();
		w.Write (i.ToString());
		w.WriteLine (i.ToString());
	}
}", 2, @"
class Foo
{
	void Bar (object i)
	{
		var w = new System.IO.StringWriter ();
		w.Write (i);
		w.WriteLine (i);
	}
}");
		}
	}
}

