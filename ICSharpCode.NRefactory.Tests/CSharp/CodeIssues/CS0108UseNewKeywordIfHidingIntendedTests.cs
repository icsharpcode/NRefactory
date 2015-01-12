// 
// CS0108UseNewKeywordIfHidingIntendedTests.cs
// 
// Author:
//      Mark Garnett 
// 
// Copyright (c) 2014 Mark Garnett <mg10g13@soton.ac.uk>
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

using ICSharpCode.NRefactory6.CSharp.Refactoring;
using ICSharpCode.NRefactory6.CSharp.CodeIssues;
using NUnit.Framework;

namespace ICSharpCode.NRefactory6.CSharp.CodeIssues
{
	[TestFixture]
	public class CS0108UseNewKeywordIfHidingIntendedTests : InspectionActionTestBase
	{
		[Test]
		public void IgnoreNonDerivedClass()
		{
			Analyze<CS0108UseNewKeywordIfHidingIntendedIssue>(@"
class Foo
{
    public void $Bar()
    {
    }
}");
		}

		[Test]
		public void IgnoreNonHidingMethod()
		{
			Analyze<CS0108UseNewKeywordIfHidingIntendedIssue>(@"
class Foo
{
    public void Bar()
    {
    }
}

class Baz : Foo
{
    public void $Bar_()
    {
    }
}");
		}

		[Test]
		public void IgnoreDifferentMethodSignature()
		{
			Analyze<CS0108UseNewKeywordIfHidingIntendedIssue>(@"
class Foo
{
    public void Bar(int foo)
    {
    }
}

class Baz : Foo
{
    public void $Bar(bool foo)
    {
    }
}");
		}

		[Test]
		public void IgnoreOverridingMethod()
		{
			Analyze<CS0108UseNewKeywordIfHidingIntendedIssue>(@"
class Foo
{
    public virtual void Bar()
    {
    }
}

class Baz : Foo
{
    public override void $Bar()
    {
    }
}");
		}

		[Test]
		public void IgnoreAbstractImplementingMethod()
		{
			Analyze<CS0108UseNewKeywordIfHidingIntendedIssue>(@"
class Foo
{
    public abstract void Bar();
}

class Baz : Foo
{
    public void $Bar()
    {
    }
}");
		}

		[Test]
		public void TestMethod()
		{
			Analyze<CS0108UseNewKeywordIfHidingIntendedIssue>(@"
class Foo
{
    public void Bar(int test)
    {
    }
}

class Baz : Foo
{
    public void $Bar$(int test)
    {
    }
}", @"
class Foo
{
    public void Bar(int test)
    {
    }
}

class Baz : Foo
{
    public new void Bar(int test)
    {
    }
}");
		}

		[Test]
		public void TestMethodWithComment()
		{
			Analyze<CS0108UseNewKeywordIfHidingIntendedIssue>(@"
class Foo
{
    public void Bar(int test)
    {
    }
}

class Baz : Foo
{
    /// <summary>
    /// Class description.
    /// </summary>
    public void $Bar$(int test)
    {
    }
}", @"
class Foo
{
    public void Bar(int test)
    {
    }
}

class Baz : Foo
{
    /// <summary>
    /// Class description.
    /// </summary>
    public new void Bar(int test)
    {
    }
}");
		}

		[Test]
		public void TestField()
		{
			Analyze<CS0108UseNewKeywordIfHidingIntendedIssue>(@"
class Foo
{
    public int bar;
}

class Baz : Foo
{
    public int $bar$;
}", @"
class Foo
{
    public int bar;
}

class Baz : Foo
{
    public new int bar;
}");
		}

		[Test]
		public void TestProperty()
		{
			Analyze<CS0108UseNewKeywordIfHidingIntendedIssue>(@"
class Foo
{
    public int Bar { get; set; }
}

class Baz : Foo
{
    public int $Bar$ { get; set; }
}", @"
class Foo
{
    public int Bar { get; set; }
}

class Baz : Foo
{
    public new int Bar { get; set; }
}");
		}

		[Test]
		public void TestType()
		{
			Analyze<CS0108UseNewKeywordIfHidingIntendedIssue>(@"
class Foo
{
    public class Bar
    {
    }
}

class Baz : Foo
{
    public class $Bar$
    {
    }
}", @"
class Foo
{
    public class Bar
    {
    }
}

class Baz : Foo
{
    public new class Bar
    {
    }
}");
		}

		[Test]
		public void TestIndexer()
		{
			Analyze<CS0108UseNewKeywordIfHidingIntendedIssue>(@"
class Foo
{
    protected int thing;

    public int this[int index]
    {
        get
        {
            return thing;
        }
        set
        {
            thing = index;
        }
    }
}

class Baz : Foo
{
    public int $this$[int index]
    {
        get
        {
            return thing;
        }
        set
        {
            thing = index;
        }
    }
}", @"
class Foo
{
    protected int thing;

    public int this[int index]
    {
        get
        {
            return thing;
        }
        set
        {
            thing = index;
        }
    }
}

class Baz : Foo
{
    public new int this[int index]
    {
        get
        {
            return thing;
        }
        set
        {
            thing = index;
        }
    }
}");
		}

		[Test]
		public void TestStruct()
		{
			Analyze<CS0108UseNewKeywordIfHidingIntendedIssue>(@"
class Foo
{
    public struct Bar
    {
    }
}

class Baz : Foo
{
    public struct $Bar$
    {
    }
}", @"
class Foo
{
    public struct Bar
    {
    }
}

class Baz : Foo
{
    public new struct Bar
    {
    }
}");
		}

		[Test]
		public void TestEnum()
		{
			Analyze<CS0108UseNewKeywordIfHidingIntendedIssue>(@"
class Foo
{
    public enum Bar
    {
        a
    }
}

class Baz : Foo
{
    public enum $Bar$
    {
    }
}", @"
class Foo
{
    public enum Bar
    {
        a
    }
}

class Baz : Foo
{
    public new enum Bar
    {
    }
}");
		}

		[Test]
		public void TestInterface()
		{
			Analyze<CS0108UseNewKeywordIfHidingIntendedIssue>(@"
class Foo
{
    public interface Bar
    {
    }
}

class Baz : Foo
{
    public interface $Bar$
    {
    }
}", @"
class Foo
{
    public interface Bar
    {
    }
}

class Baz : Foo
{
    public new interface Bar
    {
    }
}");
		}

		[Test]
		public void TestDelegate()
		{
			Analyze<CS0108UseNewKeywordIfHidingIntendedIssue>(@"
class Foo
{
    public delegate int Bar(int a, bool b);
}

class Baz : Foo
{
    public delegate int $Bar$(int a, bool b);
}", @"
class Foo
{
    public delegate int Bar(int a, bool b);
}

class Baz : Foo
{
    public new delegate int Bar(int a, bool b);
}");
		}

		[Test]
		public void TestEvent()
		{
			Analyze<CS0108UseNewKeywordIfHidingIntendedIssue>(@"
public delegate int Test(bool a);

class Foo
{
    public event Test Bar;
}

class Baz : Foo
{
    public event Test $Bar$;
}", @"
public delegate int Test(bool a);

class Foo
{
    public event Test Bar;
}

class Baz : Foo
{
    public new event Test Bar;
}");
		}
		[Test]
		public void FurtherUpInheritanceTree()
		{
			Analyze<CS0108UseNewKeywordIfHidingIntendedIssue>(@"
class Foo
{
    public void Bar(int testParam)
    {
    }
}

class Bar : Foo
{
}

class Baz : Bar
{
    public void $Bar$(int testParam)
    {
    }
}", @"
class Foo
{
    public void Bar(int testParam)
    {
    }
}

class Bar : Foo
{
}

class Baz : Bar
{
    public new void Bar(int testParam)
    {
    }
}");
		}

	}
}
