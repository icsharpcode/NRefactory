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
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using System.Linq;

namespace ICSharpCode.NRefactory6.CSharp.CodeIssues
{
	[TestFixture]
	[Ignore("Not used in NR6Pack!")]
	public class RedundantThisQualifierIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TestInspectorCase1 ()
		{
			Analyze<RedundantThisQualifierIssue>(@"class Foo
{
	void Bar (string str)
	{
		$this.$Bar (str);
	}
}", @"class Foo
{
	void Bar (string str)
	{
		Bar (str);
	}
}");
		}
		
		[Test]
		public void TestSkipConstructors ()
		{
			AnalyzeWithRule<RedundantThisQualifierIssue>(@"class Foo
{
	public Foo ()
	{
		this.Bar (str);
	}
}", RedundantThisQualifierIssue.EverywhereElse);
		}


		[Test]
		public void TestInsideConstructors ()
		{
			Analyze<RedundantThisQualifierIssue>(@"class Foo
{
	public Foo(string str)
	{
		$this.$Bar(str);
	}
	void Bar(string str)
	{
	}
}", @"class Foo
{
	public Foo(string str)
	{
		Bar(str);
	}
	void Bar(string str)
	{
	}
}");
		}

		[Test]
		public void TestInsideConstructorsSkipMembers ()
		{
			AnalyzeWithRule<RedundantThisQualifierIssue>(@"class Foo
{
	void Bar (string str)
	{
		this.Bar (str);
	}
}", RedundantThisQualifierIssue.InsideConstructors);
		}
		
		[Test]
		public void TestRequiredThisInAssignmentFromFieldToLocal ()
		{
			AnalyzeWithRule<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		var a = this.a;
	}
}", RedundantThisQualifierIssue.EverywhereElse);
		}
		
		[Test]
		public void TestRequiredThisInAssignmentFromDelegateToLocal ()
		{
			AnalyzeWithRule<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		Action a = () => {
			Console.WriteLine (this.a);
		};
	}
}", RedundantThisQualifierIssue.EverywhereElse);
		}
		
		[Test]
		public void TestRedundantThisInAssignmentFromFieldToLocal ()
		{
			AnalyzeWithRule<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		var b = $this.$a;
	}
}", RedundantThisQualifierIssue.EverywhereElse, @"class Foo
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
			AnalyzeWithRule<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		Action b = () => {
			Console.WriteLine ($this.$a);
		};
	}
}", RedundantThisQualifierIssue.EverywhereElse, @"class Foo
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
			AnalyzeWithRule<RedundantThisQualifierIssue>(@"static class Extensions
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
}", RedundantThisQualifierIssue.EverywhereElse);
		}

		[Test]
		public void TestRequiredThisToAvoidCS0135 ()
		{
			AnalyzeWithRule<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		{
			int a = 0;
		}
		this.a = 2;
	}
}", RedundantThisQualifierIssue.EverywhereElse);
		}

		[Test]
		public void TestRequiredThisToAvoidCS0844 ()
		{
			Analyze<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		{
			this.a = 0;
		}
		var a = 2;
	}
}");
		}
		
		[Test]
		public void TestRequiredThisToAvoidCS0135WithLambda ()
		{
			AnalyzeWithRule<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		Action<int> action = (int a) => a.ToString();
		this.a = 2;
	}
}", RedundantThisQualifierIssue.EverywhereElse);
		}
		
		[Test]
		public void TestRequiredThisToAvoidCS0135WithDelegate ()
		{
			AnalyzeWithRule<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		Action<int> action = delegate (int a) { a.ToString(); };
		this.a = 2;
	}
}", RedundantThisQualifierIssue.EverywhereElse);
		}

		[Ignore("Roslyn bug!")]
		[Test]
		public void TestRequiredThisToAvoidCS0135WithForeach ()
		{
			AnalyzeWithRule<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		this.a = 2;
		foreach (var a in ""abc"")
			System.Console.WriteLine (a);
	}
}", RedundantThisQualifierIssue.EverywhereElse);
		}
		
		[Test]
		public void TestRequiredThisToAvoidCS0135WithFor ()
		{
			AnalyzeWithRule<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		this.a = 2;
		for (int a = 0; a < 2; a++)
			System.Console.WriteLine (a);
	}
}", RedundantThisQualifierIssue.EverywhereElse);
		}
		
		[Test]
		public void TestRequiredThisToAvoidCS0135WithUsing ()
		{
			AnalyzeWithRule<RedundantThisQualifierIssue>(@"class Foo
{
	int a;
	void Bar ()
	{
		this.a = 2;
		using (var a = new System.IO.MemoryStream())
			a.Flush();
	}
}", RedundantThisQualifierIssue.EverywhereElse);
		}
		
		[Test]
		public void TestRequiredThisToAvoidCS0135WithFixed ()
		{
			AnalyzeWithRule<RedundantThisQualifierIssue>(@"class Baz
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
}", RedundantThisQualifierIssue.EverywhereElse);
		}
		
		[Test]
		public void TestResharperDisableRestore ()
		{
			Analyze<RedundantThisQualifierIssue> (@"class Foo
{
	void Bar (string str)
	{
		// ReSharper disable RedundantThisQualifier
		this.Bar (str);
		// ReSharper restore RedundantThisQualifier
		$this.$Bar (str);
	}
}");
		}

//		[Test]
//		public void TestBatchFix ()
//		{
//			var input = @"class Foo
//{
//	void Bar (string str)
//	{
//		this.Bar (str);
//		this.Bar (str);
//	}
//}";
//
//			TestRefactoringContext context;
//			var issues = GetIssuesWithSubIssue (new RedundantThisQualifierIssue (), input, RedundantThisQualifierIssue.EverywhereElse, out context);
//			Assert.AreEqual (2, issues.Count);
//			CheckBatchFix (context, issues, issues[0].Actions.First().SiblingKey, @"class Foo
//{
//	void Bar (string str)
//	{
//		Bar (str);
//		Bar (str);
//	}
//}");
//		}
		
		[Test]
		public void InvalidUseOfThisInFieldInitializer()
		{
			AnalyzeWithRule<RedundantThisQualifierIssue>(@"class Foo
{
	int a = this.a;
}", RedundantThisQualifierIssue.EverywhereElse);
		}
	}
}