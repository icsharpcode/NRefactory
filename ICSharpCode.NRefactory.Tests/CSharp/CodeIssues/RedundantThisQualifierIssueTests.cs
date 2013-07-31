// 
// RedundantThisQualifierIssueTests.cs
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
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.CSharp.CodeActions;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	[TestFixture]
	public class RedundantThisQualifierIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TestInspectorCase1 ()
		{
			var input = @"class Foo
{
	void Bar (string str)
	{
		this.Bar (str);
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new RedundantThisQualifierIssue (), input, out context);
			Assert.AreEqual (1, issues.Count);
			CheckFix (context, issues, @"class Foo
{
	void Bar (string str)
	{
		Bar (str);
	}
}");
		}
		
		[Test]
		public void TestRequiredThisInAssignmentFromFieldToLocal ()
		{
			Test<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		var a = this.a;
	}
}", 0);
		}
		
		[Test]
		public void TestRequiredThisInAssignmentFromDelegateToLocal ()
		{
			Test<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		Action a = () => {
			Console.WriteLine (this.a);
		};
	}
}", 0);
		}
		
		[Test]
		public void TestRedundantThisInAssignmentFromFieldToLocal ()
		{
			Test<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		var b = this.a;
	}
}", @"class Foo
{
	int a;
	void Bar ()
	{
		var b = a;
	}
}");
		}
		
		[Test]
		public void TestRedundantThisInAssignmentFromDelegateToLocal ()
		{
			Test<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		Action b = () => {
			Console.WriteLine (this.a);
		};
	}
}", @"class Foo
{
	int a;
	void Bar ()
	{
		Action b = () => {
			Console.WriteLine (a);
		};
	}
}");
		}
		
		[Test]
		public void TestRequiredThisInExtensionMethodCall ()
		{
			Test<RedundantThisQualifierIssue>(@"static class Extensions
{
	public static void Ext (this Foo foo)
	{
	}
}

class Foo
{
	void Bar ()
	{
		this.Ext ();
	}
}", 0);
		}
		
		[Test]
		public void TestRequiredThisToAvoidCS0135 ()
		{
			Test<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		{
			int a = 0;
		}
		this.a = 2;
	}
}", 0);
		}
		
		[Test]
		public void TestRequiredThisToAvoidCS0135WithLambda ()
		{
			Test<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		Action<int> action = (int a) => a.ToString();
		this.a = 2;
	}
}", 0);
		}
		
		[Test]
		public void TestRequiredThisToAvoidCS0135WithDelegate ()
		{
			Test<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		Action<int> action = delegate (int a) { a.ToString(); };
		this.a = 2;
	}
}", 0);
		}
		
		[Test]
		public void TestRequiredThisToAvoidCS0135WithForeach ()
		{
			Test<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		this.a = 2;
		foreach (var a in ""abc"")
			System.Console.WriteLine (a);
	}
}", 0);
		}
		
		[Test]
		public void TestRequiredThisToAvoidCS0135WithFor ()
		{
			Test<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		this.a = 2;
		for (int a = 0; a < 2; a++)
			System.Console.WriteLine (a);
	}
}", 0);
		}
		
		[Test]
		public void TestRequiredThisToAvoidCS0135WithUsing ()
		{
			Test<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		this.a = 2;
		using (var a = new System.IO.MemoryStream())
			a.Flush();
	}
}", 0);
		}
		
		[Test]
		public void TestRequiredThisToAvoidCS0135WithFixed ()
		{
			Test<RedundantThisQualifierIssue>(@"class Baz
{
	public int c;
}
class Foo
{
	int a;
	unsafe void Bar ()
	{
		this.a = 2;
		var b = new Baz();
		fixed (int* a = &b.c)
			Console.WriteLine(a == null);
	}
}", 0);
		}
		
		[Test]
		public void TestResharperDisableRestore ()
		{
			var input = @"class Foo
{
	void Bar (string str)
	{
		// ReSharper disable RedundantThisQualifier
		this.Bar (str);
		// ReSharper restore RedundantThisQualifier
		this.Bar (str);
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new RedundantThisQualifierIssue (), input, out context);
			Assert.AreEqual (1, issues.Count);
		}

		[Test]
		public void TestBatchFix ()
		{
			var input = @"class Foo
{
	void Bar (string str)
	{
		this.Bar (str);
		this.Bar (str);
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new RedundantThisQualifierIssue (), input, out context);
			Assert.AreEqual (2, issues.Count);
			CheckBatchFix (context, issues, issues[0].Actions.First().SiblingKey, @"class Foo
{
	void Bar (string str)
	{
		Bar (str);
		Bar (str);
	}
}");
		}
	}
}