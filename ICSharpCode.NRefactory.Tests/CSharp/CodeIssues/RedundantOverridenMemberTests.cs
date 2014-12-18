// 
// RedundantOverridenMemberTests.cs
//  
// Author:
//       Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013 Ji Kun<jikun.nus@gmail.com>
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
	public class RedundantOverridenMemberTests : InspectionActionTestBase
	{
		[Test]
		public void TestInspectorCase1()
		{
			Test<RedundantOverridenMemberIssue>(@"namespace Demo
{
	public class BaseClass
	{
		virtual public void run()
		{
			int a = 1+1;
		}
	}
	public class CSharpDemo:BaseClass
	{
		override public void run()
		{
			base.run();
		}
	}
}", @"namespace Demo
{
	public class BaseClass
	{
		virtual public void run()
		{
			int a = 1+1;
		}
	}
	public class CSharpDemo:BaseClass
	{
	}
}");
		}

		[Test]
		public void TestResharperDisable()
		{
			Analyze<RedundantOverridenMemberIssue>(@"namespace Demo
{
	public class BaseClass
	{
		virtual public void run()
		{
			int a = 1+1;
		}
	}
	//Resharper disable RedundantOverridenMember
	public class CSharpDemo:BaseClass
	{
		override public void run()
		{
			base.run();
		}
	}
	//Resharper restore RedundantOverridenMember
}");
		}

		[Test]
		public void TestInspectorCase2()
		{
			Analyze<RedundantOverridenMemberIssue>(@"namespace Demo
{
	public class BaseClass
	{
		virtual public void run()
		{
			int a = 1+1;
		}
	}
	public class CSharpDemo:BaseClass
	{
		override public void run()
		{
			int b = 1+1;
			base.run();
		}
	}
}");
		}

		[Test]
		public void TestTestInspectorCase3()
		{
			Analyze<RedundantOverridenMemberIssue>(@"namespace Demo
{
	public class BaseClass
	{
		virtual public void run()
		{
			int a = 1+1;
		}
	}
	public class CSharpDemo:BaseClass
	{
		public void run1()
		{
			base.run();
		}
	}
}");
		}

		[Test]
		public void TestTestInspectorCase4()
		{
			Test<RedundantOverridenMemberIssue>(
				@"namespace Demo
{
	public class BaseClass
	{
		private int a;
		virtual public int A
		{
			get{ return a; }
			set{ a = value; }
		}
	}
	public class CSharpDemo:BaseClass
	{
		public override int A
		{
			get{ return base.A; }
			set{ base.A = value; }
		}
	}
}", @"namespace Demo
{
	public class BaseClass
	{
		private int a;
		virtual public int A
		{
			get{ return a; }
			set{ a = value; }
		}
	}
	public class CSharpDemo:BaseClass
	{
	}
}");
		}

		[Test]
		public void TestTestInspectorCase5()
		{
			Test<RedundantOverridenMemberIssue>(
				@"namespace Application
{
	public class SampleCollection<T>
	{ 
		private T[] arr = new T[100];
		public virtual T this[int i]
		{
			get{ return arr[i];}
			set{ arr[i] = value;}
		}
	}

	class Class2<T> : SampleCollection<T>
	{
		public override T this[int i]
		{
			get { return base[i];}
			set { base[i] = value; }
		}
	}
}", @"namespace Application
{
	public class SampleCollection<T>
	{ 
		private T[] arr = new T[100];
		public virtual T this[int i]
		{
			get{ return arr[i];}
			set{ arr[i] = value;}
		}
	}

	class Class2<T> : SampleCollection<T>
	{
	}
}");
		}

		[Test]
		public void TestTestInspectorCase6()
		{
			Test<RedundantOverridenMemberIssue>(
				@"using System;
using System.IO;

partial class A
{
	public virtual int AProperty
	{
		get;
		set;
	}

	public virtual int this[int i]
	{
		get { return i; }
		set { Console.WriteLine(value); }
	}
}

class B : A
{
	public override int AProperty
	{
		set
		{
			base.AProperty = value;
		}
	}
	public override int this[int i]
	{
		get
		{
			return base[i];
		}
	}
	public override string ToString()
	{
		return base.ToString();
	}
}

class C : A
{
	public override int AProperty
	{
		get
		{
			return base.AProperty;
		}
	}
}", 4,
			@"using System;
using System.IO;

partial class A
{
	public virtual int AProperty
	{
		get;
		set;
	}

	public virtual int this[int i]
	{
		get { return i; }
		set { Console.WriteLine(value); }
	}
}

class B : A
{
}

class C : A
{
}");
		}

		[Test]
		public void TestRedundantEvent()
		{
			Test<RedundantOverridenMemberIssue>(@"namespace Demo
{
	public class BaseClass
	{
		public virtual event EventHandler FooBar { add {} remove {} }
	}
	public class CSharpDemo:BaseClass
	{
		public override event EventHandler FooBar { add { base.FooBar += value; } remove { base.FooBar -= value; } }
	}
}", @"namespace Demo
{
	public class BaseClass
	{
		public virtual event EventHandler FooBar { add {} remove {} }
	}
	public class CSharpDemo:BaseClass
	{
	}
}");
		}


		[Test]
		public void TestNonRedundantEvent()
		{
			var input = @"namespace Demo
{
	public class BaseClass
	{
		public virtual event EventHandler FooBar { add {} remove {} }
		public virtual event EventHandler FooBar2 { add {} remove {} }
	}
	public class CSharpDemo:BaseClass
	{
		public override event EventHandler FooBar { add { base.FooBar += value; } remove { base.FooBar2 -= value; } }
	}
}";
			Analyze<RedundantOverridenMemberIssue>(input);
		}


		[Test]
		public void TestGetHashCode()
		{
			Analyze<RedundantOverridenMemberIssue>(@"
class Bar
{
	public override bool Equals (object obj)
	{
		return false;
	}

	public override int GetHashCode ()
	{
		return base.GetHashCode ();
	}
}");
		}


		[Test]
		public void TestRedundantGetHashCode()
		{
			TestIssue<RedundantOverridenMemberIssue>(@"
class Bar
{
	public override int GetHashCode ()
	{
		return base.GetHashCode ();
	}
}");
		}


		[Test]
		public void TestPropertyBug()
		{
			Analyze<RedundantOverridenMemberIssue>(@"
class BaseFoo
{
	public virtual int Foo { get; set; }
}

class Bar : BaseFoo
{
	int bar;
	public override int Foo {
		get {
			return base.Foo;
		}
		set {
			base.Foo = bar = value;
		}
	}
}");
		}

	}
}