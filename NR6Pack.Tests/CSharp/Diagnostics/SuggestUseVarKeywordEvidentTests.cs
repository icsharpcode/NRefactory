//
// SuggestUseVarKeywordEvidentTests.cs
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

using NUnit.Framework;
using Microsoft.CodeAnalysis;


namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
    [TestFixture]
    public class SuggestUseVarKeywordEvidentTests : InspectionActionTestBase
    {
        [Test]
        public void TestInspectorCase1()
        {
			Analyze<SuggestUseVarKeywordEvidentAnalyzer>(@"class Foo
{
	void Bar (object o)
	{
		$Foo$ foo = (Foo)o;
	}
}", @"class Foo
{
	void Bar (object o)
	{
		var foo = (Foo)o;
	}
}");
        }

        [Test]
        public void TestV2()
        {
			Analyze<SuggestUseVarKeywordEvidentAnalyzer>(@"class Foo
{
	void Bar (object o)
	{
		$Foo$ foo = (Foo)o;
	}
}", @"class Foo
{
	void Bar (object o)
	{
		var foo = (Foo)o;
	}
}");
        }

        [Test]
        public void When_Creating_An_Object()
        {
			Analyze<SuggestUseVarKeywordEvidentAnalyzer>(@"class Foo
{
	void Bar (object o)
	{
		$Foo$ foo = new Foo();
	}
}", @"class Foo
{
	void Bar (object o)
	{
		var foo = new Foo();
	}
}");
        }

        [Test]
        public void When_Explicitely_Initializing_An_Array()
        {
			Analyze<SuggestUseVarKeywordEvidentAnalyzer>(@"class Foo
{
	void Bar (object o)
	{
	    $int[]$ foo = new int[] { 1, 2, 3 };
	}
}", @"class Foo
{
	void Bar (object o)
	{
	    var foo = new int[] { 1, 2, 3 };
	}
}");

        }

        [Test]
        public void When_Implicitely_Initializing_An_Array()
        {
            Analyze<SuggestUseVarKeywordEvidentAnalyzer>(@"class Foo
{
	void Bar (object o)
	{
	    int[] foo = new[] { 1, 2, 3 };
	}
}");
        }

        [Test]
        public void When_Retrieving_Object_By_Property()
        {
			Analyze<SuggestUseVarKeywordEvidentAnalyzer>(@"
   
public class SomeClass
{
     public SomeClass MyProperty { get; set; }
 }

public class Foo
{
    public void SomeMethod(object o)
    {
        $SomeClass$ someObject = (SomeClass)o;
        SomeClass retrievedObject = someObject.MyProperty;
    }
}
", @"
   
public class SomeClass
{
     public SomeClass MyProperty { get; set; }
 }

public class Foo
{
    public void SomeMethod(object o)
    {
        var someObject = (SomeClass)o;
        SomeClass retrievedObject = someObject.MyProperty;
    }
}
");
        }
        [Test]
        public void When_Casting_Objects()
        {
			Analyze<SuggestUseVarKeywordEvidentAnalyzer>(@"
public class MyClass
{
}

public class Foo
{
    public void SomeMethod(object o)
    {
        $MyClass$ someObject = (MyClass)o;
        if(someObject is MyClass)
            $MyClass$ castedObject = o as MyClass;
    }
}
", @"
public class MyClass
{
}

public class Foo
{
    public void SomeMethod(object o)
    {
        var someObject = (MyClass)o;
        if(someObject is MyClass)
            var castedObject = o as MyClass;
    }
}
");
        }

    }
}