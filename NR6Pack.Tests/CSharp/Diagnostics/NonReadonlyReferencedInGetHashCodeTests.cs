// 
// NonReadonlyReferencedInGetHashCodeTetsts.cs
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
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using ICSharpCode.NRefactory6.CSharp.CodeRefactorings;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[TestFixture]
	public class NonReadonlyReferencedInGetHashCodeTests : InspectionActionTestBase
	{
		
		[Test]
		public void TestInspectorCase1()
		{
			Analyze<NonReadonlyReferencedInGetHashCodeAnalyzer>(@"using System;
public class TestClass1
{
	public int a = 1;
}

public class TestClass2
{
	public override int GetHashCode()
	{
		TestClass1 c = new TestClass1();
		int b = 1;
		b++;
		return c.$a$;
	}
}
");
		}
		
		[Test]
		public void TestInspectorCase2()
		{
			Analyze<NonReadonlyReferencedInGetHashCodeAnalyzer>(@"using System;
public class TestClass1
{
	public int a = 1;
}

public class TestClass2
{
	private int b;
	public override int GetHashCode()
	{
		TestClass1 c = new TestClass1();
		$b$++;
		return c.$a$;
	}
}");
		}
		
		[Test]
		public void TestInspectorCase3()
		{
			Analyze<NonReadonlyReferencedInGetHashCodeAnalyzer>(@"using System;
public class TestClass1
{
	public int a = 1;
}

public class TestClass2
{
	public override int GetHashCode()
	{
		TestClass1 c = new TestClass1();
		int b = 1;
		b++;
		return c.GetHashCode();
	}
}");
		}
		
		[Test]
		public void TestInspectorCase4()
		{
			Analyze<NonReadonlyReferencedInGetHashCodeAnalyzer>(@"
public class Test1
{
	public int a = 1;
}

public class Test2
{
	private int q;
	public Test1 r;
	public override int GetHashCode()
	{
		Test1 c = new Test1();
		$q$ = 1 + $q$ + $r$.$a$;
		return c.$a$;
	}
}


public class Test3
{
	private int q;
	public Test2 r;
	public override int GetHashCode()
	{
		Test1 c = new Test1();
		c.GetHashCode();
		$q$ = 1 + $q$ + $r$.$r$.$a$;
		return c.$a$;
	}
}");
		}
		
		
		[Test]
		public void TestDisable()
		{
			Analyze<NonReadonlyReferencedInGetHashCodeAnalyzer>(@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace resharper_test
{
	class Foo
	{
		private readonly int fooval;
		private int tmpval;
#pragma warning disable " + NRefactoryDiagnosticIDs.NonReadonlyReferencedInGetHashCodeAnalyzerID + @"
		public override int GetHashCode()
		{
			int a = 6;
			tmpval = a + 3;
			a = tmpval + 5;
			return fooval;
		}
	}
}
");
		}



		[Test]
		public void TestConst()
		{
			Analyze<NonReadonlyReferencedInGetHashCodeAnalyzer>(@"using System;
public class TestClass1
{
	public const int a = 1;
	
	public override int GetHashCode()
	{
		return a;
	}
}
");
		}

		[Test]
		public void TestReadOnly()
		{
			Analyze<NonReadonlyReferencedInGetHashCodeAnalyzer>(@"using System;
public class TestClass1
{
	public readonly int a = 1;
	
	public override int GetHashCode()
	{
		return a;
	}
}
");
		}
	}
}