// 
// ReplaceWithStringIsNullOrEmptyIssueTests.cs
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
using ICSharpCode.NRefactory6.CSharp.CodeActions;

namespace ICSharpCode.NRefactory6.CSharp.CodeIssues
{
	[TestFixture]
	[Ignore("TODO: Issue not ported yet")]
	public class ReplaceWithStringIsNullOrEmptyIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TestInspectorCaseNS1 ()
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if (str != null && str != """")
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (!string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseNS2 ()
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if (null != str && str != """")
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (!string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorNegatedStringEmpty ()
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if (null != str && str != string.Empty)
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (!string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorStringEmpty ()
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if (null == str || str == string.Empty)
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseNS3 ()
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if (null != str && """" != str)
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (!string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseNS4 ()
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if (str != null && str != """")
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (!string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseSN1 ()
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if (str != """" && str != null)
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (!string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseSN2 ()
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if ("""" != str && str != null)
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (!string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseSN3 ()
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if ("""" != str && null != str)
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (!string.IsNullOrEmpty (str))
			;
	}
}");
		}


		[Test]
		public void TestInspectorCaseSN4 ()
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if (str != """" && null != str)
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (!string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseNS5 ()
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>( @"class Foo
{
	void Bar (string str)
	{
		if (str == null || str == """")
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseNS6 ()
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if (null == str || str == """")
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseNS7 ()
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if (null == str || """" == str)
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseNS8 ()
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if (str == null || """" == str)
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseSN5 ()
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if (str == """" || str == null)
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseSN6 ()
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if ("""" == str || str == null)
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestInspectorCaseSN7 ()
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if ("""" == str || null == str)
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (string.IsNullOrEmpty (str))
			;
	}
}");
		}
		
		[Test]
		public void TestInspectorCaseSN8 ()
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if (str == """" || null == str)
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (string.IsNullOrEmpty (str))
			;
	}
}");
		}
		
		[TestCase("str == null || str.Length == 0")]
		[TestCase("str == null || 0 == str.Length")]
		[TestCase("null == str || str.Length == 0")]
		[TestCase("null == str || 0 == str.Length")]
		public void TestInspectorCaseNL (string expression)
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if (" + expression + @")
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (string.IsNullOrEmpty (str))
			;
	}
}");
		}
	
		[TestCase("str != null && str.Length != 0")]
		[TestCase("str != null && 0 != str.Length")]
		[TestCase("str != null && str.Length > 0")]
		[TestCase("null != str && str.Length != 0")]
		[TestCase("null != str && 0 != str.Length")]
		[TestCase("null != str && str.Length > 0")]
		public void TestInspectorCaseLN (string expression)
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if (" + expression + @")
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (!string.IsNullOrEmpty (str))
			;
	}
}");
		}

		[Test]
		public void TestArrays ()
		{
			Analyze<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar ()
	{
		int[] foo = new int[10];
		if (foo == null || foo.Length == 0) {
		}
	}
}");
		}

		[Test]
		public void TestInspectorCaseNS1WithParentheses ()
		{
			Test<ReplaceWithStringIsNullOrEmptyIssue>(@"class Foo
{
	void Bar (string str)
	{
		if ((str != null) && (str) != """")
			;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		if (!string.IsNullOrEmpty (str))
			;
	}
}");
		}

	}
}
