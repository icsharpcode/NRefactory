//
// CS0126ReturnMustBeFollowedByAnyExpressionTests.cs
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
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory6.CSharp.CodeIssues
{
	[TestFixture]
	[Ignore("TODO: Issue not ported yet")]
	public class CS0126ReturnMustBeFollowedByAnyExpressionTests : InspectionActionTestBase
	{
		static void Test(string type, string defaultValue)
		{
			Test<CS0126ReturnMustBeFollowedByAnyExpression>(@"class Foo
{
	" + type + @" Bar (string str)
	{
		return;
	}
}", @"class Foo
{
	" + type + @" Bar (string str)
	{
		return " + defaultValue + @";
	}
}");
		}

		[Test]
		public void TestSimpleCase ()
		{
			Test("int", "0");
			Test("string", "\"\"");
			Test("char", "' '");
			Test("bool", "false");
			Test("object", "null");
			Test("System.DateTime", "default(System.DateTime)");
		}

		[Test]
		public void TestReturnTypeFix ()
		{
			Test<CS0126ReturnMustBeFollowedByAnyExpression>(@"class Foo
{
	int Bar (string str)
	{
		return;
	}
}", @"class Foo
{
	void Bar (string str)
	{
		return;
	}
}", 1);
		}


		[Test]
		public void TestProperty ()
		{
			TestIssue<CS0126ReturnMustBeFollowedByAnyExpression>(@"class Foo {
	string Bar 
	{
		get {
			return;
		}
	}
}");
		}

		
		[Test]
		public void TestPropertySetter ()
		{
			Analyze<CS0126ReturnMustBeFollowedByAnyExpression>(@"class Foo {
	string Bar 
	{
		set {
			return;
		}
	}
}");
		}


		[Test]
		public void TestIndexer ()
		{
			TestIssue<CS0126ReturnMustBeFollowedByAnyExpression>(@"class Foo {
	string this [int idx]
	{
		get {
			return;
		}
	}
}");
		}

		[Test]
		public void TestAnonymousMethod ()
		{
			TestIssue<CS0126ReturnMustBeFollowedByAnyExpression>(@"
using System;
class Foo
{
	void Bar (string str)
	{
		System.Func<string> func = delegate {
			return;
		};
	}
}");
		}


		[Test]
		public void TestAnonymousMethodReturnTypeFix ()
		{
			Test<CS0126ReturnMustBeFollowedByAnyExpression>(@"
using System;
class Foo
{
	int Bar (string str)
	{
		System.Func<string> func = delegate {
			return;
		};
	}
}", @"
using System;
class Foo
{
	int Bar (string str)
	{
		System.Func<string> func = delegate {
			return """";
		};
	}
}");
		}

		[Test]
		public void TestAnonymousMethodReturningVoid ()
		{
			Analyze<CS0126ReturnMustBeFollowedByAnyExpression>(@"using System;

class Foo
{
	void Bar (string str)
	{
		Action func = delegate {
			return;
		};
	}
}");
		}


		[Test]
		public void TestLambdaMethod ()
		{
			TestIssue<CS0126ReturnMustBeFollowedByAnyExpression>(@"class Foo
{
	void Bar (string str)
	{
		System.Func<string> func = () => {
			return;
		};
	}
}");
		}

		[Test]
		public void TestOperatorFalsePositives ()
		{
			TestIssue<CS0126ReturnMustBeFollowedByAnyExpression>(@"class Foo
{
	public static bool operator == (Foo left, Foo right)
	{
		return;
	}
}");
		}

		[Test]
		public void TestConstructor ()
		{
			Analyze<CS0126ReturnMustBeFollowedByAnyExpression>(@"class Foo
{
	Foo ()
	{
		return;
	}
}");
		}

		[Test]
		public void TestDestructor ()
		{
			Analyze<CS0126ReturnMustBeFollowedByAnyExpression>(@"class Foo
{
	~Foo ()
	{
		return;
	}
}");
		}

		[Test]
		public void TestDontShowUpOnUndecidableCase ()
		{
			Analyze<CS0126ReturnMustBeFollowedByAnyExpression>(@"
using System;

class Test
{
	void Foo (Func<int, int> func) {}
	void Foo (Action<int> func) {}

	void Bar (string str)
	{
		Foo(delegate {
			return;
		});
	}
}");
		}

		[Test]
		public void TestParallelForBug ()
		{
			Analyze<CS0126ReturnMustBeFollowedByAnyExpression>(@"
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

class Test
{
	void FooBar(IEnumerable<string> str)
	{
		Parallel.ForEach(str, p => {
			return;
		});
	}
}");
		}


		[Test]
		public void TestConstructorInitializer ()
		{
			Analyze<CS0126ReturnMustBeFollowedByAnyExpression>(@"
using System;

class Test
{
	Test (Func<int, int> func) {}
	Test (Action<int> func) {}
	
	Test () : this (delegate { return; }) 
	{
	}
}");
		}
		
		[Test]
		public void TestAsyncMethod_Void()
		{
			Analyze<CS0126ReturnMustBeFollowedByAnyExpression>(@"using System;
using System.Threading.Tasks;

class Test
{
	public async void M()
	{
		return;
	}
}");
		}
		
		[Test]
		public void TestAsyncMethod_Task()
		{
			Analyze<CS0126ReturnMustBeFollowedByAnyExpression>(@"using System;
using System.Threading.Tasks;

class Test
{
	public async Task M()
	{
		return;
	}
}");
		}
		
		[Test]
		public void TestAsyncMethod_TaskOfInt()
		{
			TestIssue<CS0126ReturnMustBeFollowedByAnyExpression>(@"using System;
using System.Threading.Tasks;

class Test
{
	public async Task<int> M()
	{
		return;
	}
}");
		}
	}
}

