// 
// RedundantThisQualifierTests.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[TestFixture]
	[Ignore("Not used in NR6Pack!")]
	public class RedundantThisQualifierTests : InspectionActionTestBase
	{
		[Test]
		public void TestInspectorCase1 ()
		{
			Analyze<RedundantThisQualifierAnalyzer>(@"class Foo
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
			AnalyzeWithRule<RedundantThisQualifierAnalyzer>(@"class Foo
{
	public Foo ()
	{
		this.Bar (str);
	}
}", RedundantThisQualifierAnalyzer.EverywhereElse);
		}


		[Test]
		public void TestInsideConstructors ()
		{
			Analyze<RedundantThisQualifierAnalyzer>(@"class Foo
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
			AnalyzeWithRule<RedundantThisQualifierAnalyzer>(@"class Foo
{
	void Bar (string str)
	{
		this.Bar (str);
	}
}", RedundantThisQualifierAnalyzer.InsideConstructors);
		}
		
		[Test]
		public void TestRequiredThisInAssignmentFromFieldToLocal ()
		{
			AnalyzeWithRule<RedundantThisQualifierAnalyzer>(@"class Foo
{
	int a;
	void Bar ()
	{
		var a = this.a;
	}
}", RedundantThisQualifierAnalyzer.EverywhereElse);
		}
		
		[Test]
		public void TestRequiredThisInAssignmentFromDelegateToLocal ()
		{
			AnalyzeWithRule<RedundantThisQualifierAnalyzer>(@"class Foo
{
	int a;
	void Bar ()
	{
		Action a = () => {
			Console.WriteLine (this.a);
		};
	}
}", RedundantThisQualifierAnalyzer.EverywhereElse);
		}
		
		[Test]
		public void TestRedundantThisInAssignmentFromFieldToLocal ()
		{
			AnalyzeWithRule<RedundantThisQualifierAnalyzer>(@"class Foo
{
	int a;
	void Bar ()
	{
		var b = $this.$a;
	}
}", RedundantThisQualifierAnalyzer.EverywhereElse, @"class Foo
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
			AnalyzeWithRule<RedundantThisQualifierAnalyzer>(@"class Foo
{
	int a;
	void Bar ()
	{
		Action b = () => {
			Console.WriteLine ($this.$a);
		};
	}
}", RedundantThisQualifierAnalyzer.EverywhereElse, @"class Foo
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
			AnalyzeWithRule<RedundantThisQualifierAnalyzer>(@"static class Extensions
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
}", RedundantThisQualifierAnalyzer.EverywhereElse);
		}

		[Test]
		public void TestRequiredThisToAvoidCS0135 ()
		{
			AnalyzeWithRule<RedundantThisQualifierAnalyzer>(@"class Foo
{
	int a;
	void Bar ()
	{
		{
			int a = 0;
		}
		this.a = 2;
	}
}", RedundantThisQualifierAnalyzer.EverywhereElse);
		}

		[Test]
		public void TestRequiredThisToAvoidCS0844 ()
		{
			Analyze<RedundantThisQualifierAnalyzer>(@"class Foo
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
			AnalyzeWithRule<RedundantThisQualifierAnalyzer>(@"class Foo
{
	int a;
	void Bar ()
	{
		Action<int> action = (int a) => a.ToString();
		this.a = 2;
	}
}", RedundantThisQualifierAnalyzer.EverywhereElse);
		}
		
		[Test]
		public void TestRequiredThisToAvoidCS0135WithDelegate ()
		{
			AnalyzeWithRule<RedundantThisQualifierAnalyzer>(@"class Foo
{
	int a;
	void Bar ()
	{
		Action<int> action = delegate (int a) { a.ToString(); };
		this.a = 2;
	}
}", RedundantThisQualifierAnalyzer.EverywhereElse);
		}

		[Ignore("Roslyn bug!")]
		[Test]
		public void TestRequiredThisToAvoidCS0135WithForeach ()
		{
			AnalyzeWithRule<RedundantThisQualifierAnalyzer>(@"class Foo
{
	int a;
	void Bar ()
	{
		this.a = 2;
		foreach (var a in ""abc"")
			System.Console.WriteLine (a);
	}
}", RedundantThisQualifierAnalyzer.EverywhereElse);
		}
		
		[Test]
		public void TestRequiredThisToAvoidCS0135WithFor ()
		{
			AnalyzeWithRule<RedundantThisQualifierAnalyzer>(@"class Foo
{
	int a;
	void Bar ()
	{
		this.a = 2;
		for (int a = 0; a < 2; a++)
			System.Console.WriteLine (a);
	}
}", RedundantThisQualifierAnalyzer.EverywhereElse);
		}
		
		[Test]
		public void TestRequiredThisToAvoidCS0135WithUsing ()
		{
			AnalyzeWithRule<RedundantThisQualifierAnalyzer>(@"class Foo
{
	int a;
	void Bar ()
	{
		this.a = 2;
		using (var a = new System.IO.MemoryStream())
			a.Flush();
	}
}", RedundantThisQualifierAnalyzer.EverywhereElse);
		}
		
		[Test]
		public void TestRequiredThisToAvoidCS0135WithFixed ()
		{
			AnalyzeWithRule<RedundantThisQualifierAnalyzer>(@"class Baz
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
}", RedundantThisQualifierAnalyzer.EverywhereElse);
		}
		
		[Test]
		public void TestResharperDisableRestore ()
		{
			Analyze<RedundantThisQualifierAnalyzer> (@"class Foo
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
//			var issues = GetIssuesWithSubIssue (new RedundantThisQualifierIssue (), input, RedundantThisQualifierAnalyzer.EverywhereElse, out context);
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
			AnalyzeWithRule<RedundantThisQualifierAnalyzer>(@"class Foo
{
	int a = this.a;
}", RedundantThisQualifierAnalyzer.EverywhereElse);
		}
	}
}