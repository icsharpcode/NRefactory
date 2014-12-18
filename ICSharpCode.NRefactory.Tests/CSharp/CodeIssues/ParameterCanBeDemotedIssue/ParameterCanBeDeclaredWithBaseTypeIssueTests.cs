//
// ParameterCouldBeDeclaredWithBaseTypeTests.cs
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
	public class ParameterCanBeDeclaredWithBaseTypeIssueTests : InspectionActionTestBase
	{
		[Test]
		public void BasicTest()
		{
			Test<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
class A
{
	public virtual void Foo() {}
}
class B : A
{
	public virtual void Bar() {}
}
class C
{
	void F(B b)
	{
		b.Foo();
	}
}", @"
class A
{
	public virtual void Foo() {}
}
class B : A
{
	public virtual void Bar() {}
}
class C
{
	void F(A b)
	{
		b.Foo();
	}
}");
		}
		
		[Test]
		public void IgnoresUnusedParameters()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
class A
{
	void F(A a1)
	{
	}
}");
		}
		
		[Test]
		public void IgnoresDirectionalParameters()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
interface IA
{
}
class A : IA
{
	void F(out A a1)
	{
		object.Equals(a1, null);
	}
}");
		}

		[Test]
		public void IgnoresOverrides()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
interface IA
{
	void Foo();
}
class B : IA
{
	public virtual void Foo() {}
	public virtual void Bar() {}
}
class TestBase
{
	public void F(B b) {
		b.Foo();
		b.Bar();
	}
}
class TestClass : TestBase
{
	public override void F(B b)
	{
		b.Foo();
	}
}");
		}
		
		[Test]
		public void IgnoresOverridables()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
interface IA
{
	void Foo();
}
class B : IA
{
	public virtual void Foo() {}
	public virtual void Bar() {}
}
class TestClass
{
	public virtual void F(B b)
	{
		b.Foo();
	}
}");
		}
		
		[Test]
		public void HandlesNeededProperties()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
interface IA
{
	void Foo(string s);
}
class B : IA
{
	public virtual void Foo(string s) {}
	public string Property { get; }
}
class TestClass
{
	public void F(B b)
	{
		b.Foo(b.Property);
	}
}");
		}
		
		[Test]
		public void InterfaceTest()
		{
			Test<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
interface IA
{
	void Foo();
}
class B : IA
{
	public virtual void Foo() {}
	public virtual void Bar() {}
}
class C
{
	void F(B b)
	{
		b.Foo();
	}
}", @"
interface IA
{
	void Foo();
}
class B : IA
{
	public virtual void Foo() {}
	public virtual void Bar() {}
}
class C
{
	void F(IA b)
	{
		b.Foo();
	}
}");
		}
		
		[Test]
		public void RespectsExpectedTypeInIfStatement()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
class C
{
	void F (bool b, bool c)
	{
		if (b && c)
			return;
	}
}");
		}
		
		[Test]
		public void MultipleInterfaceTest()
		{
			Test<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
interface IA1
{
	void Foo();
}
interface IA2
{
	void Bar();
}
class B : IA1, IA2
{
	public virtual void Foo() {}
	public virtual void Bar() {}
}
class C : B {}
class Test
{
	void F(C c)
	{
		c.Foo();
		c.Bar();
	}
}", @"
interface IA1
{
	void Foo();
}
interface IA2
{
	void Bar();
}
class B : IA1, IA2
{
	public virtual void Foo() {}
	public virtual void Bar() {}
}
class C : B {}
class Test
{
	void F(B c)
	{
		c.Foo();
		c.Bar();
	}
}");
		}

		string baseInput = @"
interface IA
{
	void Foo();
}
interface IB : IA
{
	void Bar();
}
interface IC : IA
{
	new void Foo();
	void Baz();
}
class D : IB
{
	public void Foo() {}
	public void Bar() {}
}
class E : D, IC
{
	public void Baz() {}
	void IC.Foo() {}
}";
		
		[Ignore]
		[Test]
		public void FindsTopInterface()
		{
			Test<ParameterCanBeDeclaredWithBaseTypeIssue>(baseInput + @"
class Test
{
	void F(E e)
	{
		e.Foo();
	}
}", baseInput + @"
class Test
{
	void F(IA e)
	{
		e.Foo();
	}
}");
		}
		
		[Test]
		public void DoesNotChangeOverload()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(baseInput + @"
class Test
{
	void F(IB b)
	{
		Bar (b);
	}
	
	void Bar (IA a)
	{
	}

	void Bar (IB b)
	{
	}
}");
		}
		
		[Test]
		public void AssignmentToExplicitlyTypedVariable()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(baseInput + @"
class Test
{
	void F(IB b)
	{
		IB b2;
		b2 = b;
		object.Equals(b, b2);
	}
}");
		}
		
		[Test]
		public void GenericMethod()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(baseInput + @"
class Test
{
	void F(IB b)
	{
		Generic (b);
	}

	void Generic<T> (T arg) where T : IA
	{
	}
}");
		}

		[Test]
		public void VariableDeclarationWithTypeInference()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(baseInput + @"
class Test
{
	void Foo (IB b)
	{
		var b2 = b;
		Foo (b2);
	}

	void Foo (IA a)
	{
	}
}");
		}

		[Test]
		public void RespectsOutgoingCallsTypeRestrictions()
		{
			Test<ParameterCanBeDeclaredWithBaseTypeIssue>(baseInput + @"
class Test
{
	void F(E e)
	{
		e.Foo();
		DemandType(e);
	}

	void DemandType(D d)
	{
	}
}", baseInput + @"
class Test
{
	void F(D e)
	{
		e.Foo();
		DemandType(e);
	}

	void DemandType(D d)
	{
	}
}");
		}
		
		[Test]
		public void AccountsForNonInvocationMethodGroupUsageInMethodCall()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
delegate void FooDelegate (string s);
interface IBase
{
	void Bar();
}
interface IDerived : IBase
{
	void Foo(string s);
}
class TestClass
{
	public void Bar (IDerived derived)
	{
		derived.Bar();
		Baz (derived.Foo);
	}

	void Baz (FooDelegate fd)
	{
	}
}");
		}
		
		[Test]
		public void AccountsForNonInvocationMethodGroupUsageInVariableDeclaration()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
delegate void FooDelegate (string s);
interface IBase
{
	void Bar();
}
interface IDerived : IBase
{
	void Foo(string s);
}
class TestClass
{
	public void Bar (IDerived derived)
	{
		derived.Bar();
		FooDelegate d = derived.Foo;
	}
}");
		}
		
		[Test]
		public void AccountsForNonInvocationMethodGroupUsageInAssignmentExpression()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
delegate void FooDelegate (string s);
interface IBase
{
	void Bar();
}
interface IDerived : IBase
{
	void Foo(string s);
}
class TestClass
{
	public void Bar (IDerived derived)
	{
		derived.Bar();
		FooDelegate d;
		d = derived.Foo;
	}
}");
		}
		
		[Ignore]
		[Test]
		public void AccountsForIndexers()
		{
			Test<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
class TestClass
{
	void Write(string[] s)
	{
		object.Equals(s, s);
		var element = s[1];
	}
}",1 , @"
class TestClass
{
	void Write(System.Collections.Generic.IList<string> s)
	{
		object.Equals(s, s);
		var element = s[1];
	}
}", 1, 1);
		}
		
		[Test]
		public void AccountsForArrays()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
class TestClass
{
	void Write(string[] s)
	{
		var i = s.Length;
		SetValue (out s[1]);
	}

	void SetValue (out string s)
	{
	} 
}");
		}
		
		[Test]
		public void LimitsParamsParametersToArrays()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
class TestClass
{
	void Write(params string[] s)
	{
		System.Console.WriteLine (s);
	}
}");
		}
		
		[Test]
		public void DoesNotSuggestProgramEntryPointChanges()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
class TestClass
{
	public static void Main (string[] args)
	{
		if (args.Length > 2) {
		}
	}
}");
		}
		
		[Test]
		public void IgnoresImplicitInterfaceImplementations()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
interface IHasFoo
{
	void Foo (string s);
}
class TestClass : IHasFoo
{
	public void Foo(string s)
	{
		object o = s;
	} 
}");
		}

		[Test]
		public void IgnoresEnumParameters()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
enum ApplicableValues
{
	None,
	Some
}
class TestClass
{
	public void Foo(ApplicableValues av)
	{
		object o = av;
	} 
}");
		}
		
		[Test]
		public void CallToOverriddenMember()
		{
			Test<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
class TestBase
{
	public virtual void Foo()
	{
	}
}
class Test : TestBase
{
	void F (Test t)
	{
		t.Foo();
	}
	
	public override void Foo()
	{
	}
}", @"
class TestBase
{
	public virtual void Foo()
	{
	}
}
class Test : TestBase
{
	void F (TestBase t)
	{
		t.Foo();
	}
	
	public override void Foo()
	{
	}
}");
		}
		
		[Test]
		public void CallToShadowingMember()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
class TestBase
{
	public virtual void Foo()
	{
	}
}
class Test : TestBase
{
	void F (Test t)
	{
		t.Foo();
	}
	
	public new void Foo()
	{
	}
}");
		}
		
		[Test]
		public void CallToShadowingMember2()
		{
			Test<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
class TestBaseBase
{
	public virtual void Foo()
	{
	}
}
class TestBase : TestBaseBase
{
	protected virtual new void Foo()
	{
	}
}
class Test : TestBase
{
	void F (Test t)
	{
		t.Foo();
	}
	
	public override void Foo()
	{
	}
}", @"
class TestBaseBase
{
	public virtual void Foo()
	{
	}
}
class TestBase : TestBaseBase
{
	protected virtual new void Foo()
	{
	}
}
class Test : TestBase
{
	void F (TestBase t)
	{
		t.Foo();
	}
	
	public override void Foo()
	{
	}
}");
		}
		
		[Test]
		public void CallToShadowingMemberWithBaseInterface()
		{
			Test<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
interface IFoo
{
	void Foo();
}
class TestBaseBase : IFoo
{
	public virtual void Foo()
	{
	}
}
class TestBase : TestBaseBase
{
	protected virtual new void Foo()
	{
	}
}
class Test : TestBase
{
	void F (Test t)
	{
		t.Foo();
	}
	
	protected override void Foo()
	{
	}
}", @"
interface IFoo
{
	void Foo();
}
class TestBaseBase : IFoo
{
	public virtual void Foo()
	{
	}
}
class TestBase : TestBaseBase
{
	protected virtual new void Foo()
	{
	}
}
class Test : TestBase
{
	void F (TestBase t)
	{
		t.Foo();
	}
	
	protected override void Foo()
	{
	}
}");
		}

		/// <summary>
		/// Bug 9617 - Incorrect "parameter can be demoted to base class" warning for arrays
		/// </summary>
		[Test]
		public void TestBug9617()
		{
			Test<ParameterCanBeDeclaredWithBaseTypeIssue>(@"class Test
{
	object Foo (object[] arr)
	{
	    return arr [0];
	}
}", 1, @"class Test
{
	object Foo (System.Collections.IList arr)
	{
	    return arr [0];
	}
}");
		}

		[Test]
		public void TestBug9617Case2()
		{
			Test<ParameterCanBeDeclaredWithBaseTypeIssue>(@"class Test
{
	int Foo (int[] arr)
	{
	    return arr [0];
	}
}", 1, @"class Test
{
	int Foo (System.Collections.Generic.IList<int> arr)
	{
	    return arr [0];
	}
}");
		}
		
		[Test]
		public void DoNotDemoteStringComparisonToReferenceComparison_WithinLambda()
		{
			Test<ParameterCanBeDeclaredWithBaseTypeIssue>(@"using System; using System.Linq; using System.Collections.Generic;
class Test
{
	IEnumerable<User> users;
	User GetUser (String id)
	{
		return users.Where(u => u.Name == id).SingleOrDefault();
	}
}
class User {
	public string Name;
}
", 0);
		}

		[Test]
		public void TestMicrosoftSuppressMessage()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
class A
{
	public virtual void Foo() {}
}
class B : A
{
	public virtual void Bar() {}
}
class C
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage(""Microsoft.Design"", ""CA1011:ConsiderPassingBaseTypesAsParameters"")]
	void F(B b)
	{
		b.Foo();
	}
}");
		}

		[Test]
		public void TestDisableAll()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(@"// ReSharper disable All

class A
{
	public virtual void Foo() {}
}
class B : A
{
	public virtual void Bar() {}
}
class C
{
	void F(B b)
	{
		b.Foo();
	}
}");
		}


		/// <summary>
		/// Bug 14099 - Do not suggest demoting Exception to _Exception
		/// </summary>
		[Test]
		public void TestBug14099()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(@"
using System;

public class Test
{
	public void Foo (Exception ex)
	{
		System.Console.WriteLine (ex.HelpLink);
	}
}
");
		}


		[Test]
		public void TestPreferGenerics()
		{
			Analyze<ParameterCanBeDeclaredWithBaseTypeIssue>(@"using System.Collections.Generic;

class Test
{
	int Foo (ICollection<object> arr)
	{
		return arr.Count;
	}
}");
		}


	}
}

