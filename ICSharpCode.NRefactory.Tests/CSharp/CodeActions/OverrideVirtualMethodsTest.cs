﻿// 
// OverrideVirtualMethodsTest.cs
//
// Author:
//       Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013 Ji Kun <jikun.nus@gmail.com>
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

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	[TestFixture]
	public class OverrideVirtualMethodsTest : ContextActionTestBase
	{
		[Test]
		public void TestSimpleBaseType()
		{
			Test<OverrideVirtualMemberAction>(@"class Simple {
	public virtual void FooBar (string foo, int bar){}
}

class Foo : $Simple
{
}
", @"class Simple {
	public virtual void FooBar (string foo, int bar){}
}

class Foo : Simple
{
	#region override virtual methods
	public override void FooBar (string foo, int bar)
	{
		throw new System.NotImplementedException ();
	}
	#endregion
}
");
		}

		[Test]
		public void TestProtectedMembers()
		{
			Test<OverrideVirtualMemberAction>(@"class Simple {
	protected virtual string ServiceName() {}
}

class Foo : $Simple
{
}
", @"class Simple {
	protected virtual string ServiceName() {}
}

class Foo : Simple
{
	#region override virtual methods
	protected override string ServiceName ()
	{
		throw new System.NotImplementedException ();
	}
	#endregion
}
");
		}

		[Test]
		public void TestProtectedInternalMembers()
		{
			Test<OverrideVirtualMemberAction>(@"class Simple {
	protected internal virtual string ServiceName() {}
}

class Foo : $Simple
{
}
", @"class Simple {
	protected internal virtual string ServiceName() {}
}

class Foo : Simple
{
	#region override virtual methods
	protected internal override string ServiceName ()
	{
		throw new System.NotImplementedException ();
	}
	#endregion
}
");
		}

		[Test]
		public void TestAlreadyImplemented()
		{
			Test<OverrideVirtualMemberAction>(@"class Simple {
	public virtual void Foo() {}
	public virtual void FooBar() {}
}

class FooA : $Simple {
	public override void Foo() {}
}

", @"class Simple {
	public virtual void Foo() {}
	public virtual void FooBar() {}
}

class FooA : Simple {
	public override void Foo() {}
	#region override virtual methods
	public override void FooBar ()
	{
		throw new System.NotImplementedException ();
	}
	#endregion
}
");
		}
	}
}

