//
// FormatStringProblemIssueTests.cs
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
using NUnit.Framework;
using ICSharpCode.NRefactory6.CSharp.CodeActions;
using ICSharpCode.NRefactory6.CSharp.Refactoring;

namespace ICSharpCode.NRefactory6.CSharp.CodeIssues
{
	[TestFixture]
	[Ignore("TODO: Issue not ported yet")]
	public class FormatStringProblemIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TooFewArguments()
		{
			TestIssue<FormatStringProblemIssue>(@"
class TestClass
{
	void Foo()
	{
		string.Format(""{0}"");
	}
}");
		}


		[Test]
		public void SupportsFixedArguments()
		{
			Analyze<FormatStringProblemIssue>(@"
class TestClass
{
	void Foo()
	{
		Bar(""{0}"", 1);
	}

	void Bar(string format, string arg0)
	{
	}
}");
		}

		[Test]
		public void IgnoresCallWithUnknownNumberOfArguments()
		{
			Analyze<FormatStringProblemIssue>(@"
class TestClass
{
	string Bar(params object[] args)
	{
		return string.Format(""{1}"", args);
	}
}");
		}

		[Test]
		public void FormatItemIndexOutOfRangeOfArguments()
		{
			TestIssue<FormatStringProblemIssue>(@"
class TestClass
{
	void Foo()
	{
		string.Format(""{1}"", 1);
	}
}", 2);
		}
		
		[Test]
		public void FormatItemIndexOutOfRangeOfArguments_ExplicitArrayCreation()
		{
			TestIssue<FormatStringProblemIssue>(@"
class TestClass
{
	void Foo()
	{
		string.Format(""{1}"", new object[] { 1 });
	}
}", 2);
		}
		
		[Test]
		public void FormatItemMissingEndBrace()
		{
			TestIssue<FormatStringProblemIssue>(@"
class TestClass
{
	void Foo()
	{
		string.Format(""Text text text {0 text text text"", 1);
	}
}");
		}
			
		[Test]
		public void UnescapedLeftBrace()
		{
			TestIssue<FormatStringProblemIssue>(@"
class TestClass
{
	void Foo()
	{
		string.Format(""a { a"", 1);
	}
}", 2);
		}

		[Test]
		public void IgnoresStringWithGoodArguments()
		{
			Analyze<FormatStringProblemIssue>(@"
class TestClass
{
	void Foo()
	{
		string.Format(""{0}"", ""arg0"");
	}
}");
		}

		[Test]
		public void IgnoresStringWithGoodArguments_ExplicitArrayCreation()
		{
			Analyze<FormatStringProblemIssue>(@"
class TestClass
{
	void Foo()
	{
		string.Format(""{0} {1}"", new object[] { ""arg0"", ""arg1"" });
	}
}");
		}

		[Test]
		public void IgnoresNonFormattingCall()
		{
			Analyze<FormatStringProblemIssue>(@"
class TestClass
{
	void Foo()
	{
		string lower = string.ToLower(""{0}"");
	}
}");
		}
		
		[Test]
		public void HandlesCallsWithExtraArguments()
		{
			Analyze<FormatStringProblemIssue>(@"
class TestClass
{
	void Foo()
	{
		Foo(1);
	}
}");
		}


		[Test]
		public void TooManyArguments()
		{
			TestIssue<FormatStringProblemIssue>(@"
class TestClass
{
	void Foo()
	{
		string.Format(""{0}"", 1, 2);
	}
}");
		}

		[Test]
		public void UnreferencedArgument()
		{
			TestIssue<FormatStringProblemIssue>(@"
class TestClass
{
	void Foo()
	{
		string.Format(""{0} {2}"", 1, 2, 3);
	}
}");
		}

		/// <summary>
		/// Bug 14405 - Incorrect "argument not used in string format" warning
		/// </summary>
		[Test]
		public void TestBug14405()
		{
			Analyze<FormatStringProblemIssue>(@"
using System;
class TestClass
{
	void Foo()
	{
		DateTime.ParseExact(""expiresString"", ""s"", System.Globalization.CultureInfo.InvariantCulture);
	}
}");
		}
		[Test]
		public void TestDisable()
		{
			Analyze<FormatStringProblemIssue>(@"
class TestClass
{
	void Foo()
	{
		// ReSharper disable once FormatStringProblem
		string.Format(""{0}"", 1, 2);
	}
}");
		}

		/// <summary>
		/// Bug 15867 - Wrong Context for string formatting
		/// </summary>
		[Test]
		public void TestBug15867()
		{
			Analyze<FormatStringProblemIssue>(@"
class TestClass
{
	void Foo()
	{
		double d = 1;
		d.ToString(""G29"", System.Globalization.CultureInfo.InvariantCulture);
	}
}");
		}
	}
}

