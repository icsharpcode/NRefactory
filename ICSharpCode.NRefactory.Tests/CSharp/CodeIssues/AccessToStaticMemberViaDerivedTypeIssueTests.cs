//
// CallToStaticMemberViaDerivedType.cs
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
	public class AccessToStaticMemberViaDerivedTypeIssueTests : InspectionActionTestBase
	{
		[Test]
		public void MemberInvocation()
		{
			Analyze<AccessToStaticMemberViaDerivedTypeIssue>(@"
class A
{
	public static void F() { }
}
class B : A { }
class C
{
	void Main()
	{
		$B$.F ();
	}
}", @"
class A
{
	public static void F() { }
}
class B : A { }
class C
{
	void Main()
	{
		A.F ();
	}
}"
			);
		}

		[Test]
		public void MemberInvocationWithComment()
		{
			Analyze<AccessToStaticMemberViaDerivedTypeIssue>(@"
class A
{
	public static void F() { }
}
class B : A { }
class C
{
	void Main()
	{
		// Some comment
		$B$.F ();
	}
}", @"
class A
{
	public static void F() { }
}
class B : A { }
class C
{
	void Main()
	{
		// Some comment
		A.F ();
	}
}"
			);
		}

		[Test]
		public void TestDisable()
		{
			Analyze<AccessToStaticMemberViaDerivedTypeIssue>(@"
class A
{
	public static void F() { }
}
class B : A { }
class C
{
	void Main()
	{
        // ReSharper disable once AccessToStaticMemberViaDerivedType
		B.F ();
	}
}");
		}

		[Test]
		public void PropertyAccess()
		{
			Analyze<AccessToStaticMemberViaDerivedTypeIssue>(@"
class A
{
	public static string Property { get; set; }
}
class B : A { }
class C
{
	void Main()
	{
		System.Console.WriteLine($B$.Property);
	}
}", @"
class A
{
	public static string Property { get; set; }
}
class B : A { }
class C
{
	void Main()
	{
		System.Console.WriteLine(A.Property);
	}
}"
			);
		}

		[Test]
		public void FieldAccess()
		{
			Analyze<AccessToStaticMemberViaDerivedTypeIssue>(@"class A
{
	public static string Property;
}
class B : A { }
class C
{
	void Main()
	{
		System.Console.WriteLine($B$.Property);
	}
}", @"class A
{
	public static string Property;
}
class B : A { }
class C
{
	void Main()
	{
		System.Console.WriteLine(A.Property);
	}
}"
			);
		}

		[Test]
		public void NestedClass()
		{
			Analyze<AccessToStaticMemberViaDerivedTypeIssue>(@"
class A
{
	public class B
	{
		public static void F() { }
	}
	public class C : B { }
}
class D
{
	void Main()
	{
		$A.C$.F ();
	}
}", @"
class A
{
	public class B
	{
		public static void F() { }
	}
	public class C : B { }
}
class D
{
	void Main()
	{
		A.B.F ();
	}
}"
			);
		}

		[Test]
		public void ExpandsTypeWithNamespaceIfNeccessary()
		{
			Analyze<AccessToStaticMemberViaDerivedTypeIssue>(@"namespace First
{
	class A
	{
		public static void F() { }
	}
}
namespace Second
{
	public class B : First.A { }
	class C
	{
		void Main()
		{
			$B$.F ();
		}
	}
}", @"namespace First
{
	class A
	{
		public static void F() { }
	}
}
namespace Second
{
	public class B : First.A { }
	class C
	{
		void Main()
		{
			First.A.F ();
		}
	}
}"
			);
		}

		[Test]
		public void IgnoresCorrectCalls()
		{
			Analyze<AccessToStaticMemberViaDerivedTypeIssue>(@"
class A
{
	public static void F() { }
}
class B
{
	void Main()
	{
		A.F();
	}
}");
		}

		[Test]
		public void IgnoresNonStaticCalls()
		{
			Analyze<AccessToStaticMemberViaDerivedTypeIssue>(@"
class A
{
	public void F() { }
}
class B : A { }
class C
{
	void Main()
	{
		B b = new B();
		b.F();
	}
}");
		}

		[Test]
		public void IgnoresOwnMemberFunctions()
		{
			Analyze<AccessToStaticMemberViaDerivedTypeIssue>(@"
class A
{
	protected static void F() { }
}
class B : A
{
	void Main()
	{
		F();
		this.F();
		base.F();
	}
}");
		}

		[Test]
		public void IgnoresCuriouslyRecurringTemplatePattern()
		{
			Analyze<AccessToStaticMemberViaDerivedTypeIssue>(@"
class Base<T>
{
	public static void F() { }
}
class Derived : Base<Derived> {}
class Test
{
	void Main()
	{
		Derived.F();
	}
}");
		}
	}
}

