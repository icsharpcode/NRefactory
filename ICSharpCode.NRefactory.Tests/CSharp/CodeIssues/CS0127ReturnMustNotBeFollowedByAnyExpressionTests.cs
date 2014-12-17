//
// CS0127ReturnMustNotBeFollowedByAnyExpressionTests.cs
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
	[Ignore("Issue not implemented")]
	public class CS0127ReturnMustNotBeFollowedByAnyExpressionTests : InspectionActionTestBase
	{
		[Test]
		public void TestSimpleCase ()
		{
			Test<CS0127ReturnMustNotBeFollowedByAnyExpression>(@"class Foo
{
	void Bar (string str)
	{
		return str;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		return;
	}
}");
		}


		[Test]
		public void TestReturnTypeFix ()
		{
			Test<CS0127ReturnMustNotBeFollowedByAnyExpression>(@"class Foo
{
	void Bar (string str)
	{
		return str;
	}
}", @"class Foo
{
	string Bar (string str)
	{
		return str;
	}
}", 1);
		}

		[Test]
		public void TestSimpleInvalidCase ()
		{
			Analyze<CS0127ReturnMustNotBeFollowedByAnyExpression>(@"class Foo
{
	string Bar (string str)
	{
		return str;
	}
}");
		}

		[Test]
		public void TestProperty ()
		{
			Analyze<CS0127ReturnMustNotBeFollowedByAnyExpression>(@"class Foo {
	string Bar 
	{
		get {
			return ""Hello World "";
		}
	}
}");
		}

		[Test]
		public void TestIndexer ()
		{
			Analyze<CS0127ReturnMustNotBeFollowedByAnyExpression>(@"class Foo {
	string this [int idx]
	{
		get {
			return ""Hello World "";
		}
	}
}");
		}

		[Test]
		public void TestIndexerSetter ()
		{
			TestIssue<CS0127ReturnMustNotBeFollowedByAnyExpression>(@"class Foo {
	string this [int idx]
	{
		set {
			return ""Hello World "";
		}
	}
}");
		}


		[Test]
		public void TestAnonymousMethod ()
		{
			Test<CS0127ReturnMustNotBeFollowedByAnyExpression>(@"class Foo
{
	void Bar (string str)
	{
		System.Action func = delegate {
			return str;
		};
	}
}", @"class Foo
{
	void Bar (string str)
	{
		System.Action func = delegate {
			return;
		};
	}
}");
		}

		[Test]
		public void TestAnonymousMethodReturningValue ()
		{
			Analyze<CS0127ReturnMustNotBeFollowedByAnyExpression>(@"class Foo
{
	void Bar (string str)
	{
		System.Func<string> func = delegate {
			return str;
		};
	}
}");
		}
		
		[Test]
		public void TestLambdaMethod ()
		{
			Analyze<CS0127ReturnMustNotBeFollowedByAnyExpression>(@"class Foo
{
	void Bar (string str)
	{
		System.Func<string> func = () => {
			return str;
		};
	}
}");
		}
		
		[Test]
		public void TestOperatorFalsePositives ()
		{
			Analyze<CS0127ReturnMustNotBeFollowedByAnyExpression>(@"class Foo
{
	public static bool operator == (Foo left, Foo right)
	{
		return true;
	}
}");
		}

		[Test]
		public void TestConstructor ()
		{
			TestIssue<CS0127ReturnMustNotBeFollowedByAnyExpression>(@"class Foo
{
	Foo ()
	{
		return 1;
	}
}");
		}
		
		[Test]
		public void TestDestructor ()
		{
			TestIssue<CS0127ReturnMustNotBeFollowedByAnyExpression>(@"class Foo
{
	~Foo ()
	{
		return 1;
	}
}");
		}
	

		[Test]
		public void TestDontShowUpOnUndecidableCase ()
		{
			Analyze<CS0127ReturnMustNotBeFollowedByAnyExpression>(@"
using System;

class Test
{
	void Foo (Func<int, int> func) {}
	void Foo (Action<int> func) {}

	void Bar (string str)
	{
		Foo(delegate {
			return str;
		});
	}
}");
		}



		/// <summary>
		/// Bug 14843 - CS0127ReturnMustNotBeFollowedByAnyExpression Code Issue false positive
		/// </summary>
		[Test]
		public void TestBug14843 ()
		{
			Analyze<CS0127ReturnMustNotBeFollowedByAnyExpression>(@"
using System;

class Foo {
	public Func<object, object> Func;
}
class Bar
{
	void Test ()
	{
		new Foo {
			Func = o => {
				return o;
			}
		};
	}
}");
		}
		
		[Test]
		public void TestAsyncMethod_Void()
		{
			TestIssue<CS0127ReturnMustNotBeFollowedByAnyExpression>(@"using System;
using System.Threading.Tasks;

class Test
{
	public async void M()
	{
		return 1;
	}
}");
		}
		
		[Test]
		public void TestAsyncMethod_Task()
		{
			TestIssue<CS0127ReturnMustNotBeFollowedByAnyExpression>(@"using System;
using System.Threading.Tasks;

class Test
{
	public async Task M()
	{
		return 1;
	}
}");
		}
		
		[Test]
		public void TestAsyncMethod_TaskOfInt()
		{
			Analyze<CS0127ReturnMustNotBeFollowedByAnyExpression>(@"using System;
using System.Threading.Tasks;

class Test
{
	public async Task<int> M()
	{
		return 1;
	}
}");
		}
	}
}

