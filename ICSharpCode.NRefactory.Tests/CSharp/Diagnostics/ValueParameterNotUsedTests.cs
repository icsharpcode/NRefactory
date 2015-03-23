//
// SetterDoesNotUseValueParameterTests.cs
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
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using ICSharpCode.NRefactory6.CSharp.CodeRefactorings;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[TestFixture]
	public class ValueParameterNotUsedTests : InspectionActionTestBase
	{
		[Test]
		public void TestPropertySetter()
		{
			Analyze<ValueParameterNotUsedAnalyzer>(@"class A
{
	int Property1
	{
		set {
			int val = value;
		}
	}
	int Property2
	{
		$set$ {
		}
	}
}");
		}


		[Test]
		public void TestDisable()
		{
			Analyze<ValueParameterNotUsedAnalyzer>(@"class A
{
    int Property1
    {
        set
        {
            int val = value;
        }
    }
    int Property2
    {
// ReSharper disable once ValueParameterNotUsed
        set
        {
        }
    }
}");
		}

		[Test]
		public void TestMatchingIndexerSetter()
		{
			Analyze<ValueParameterNotUsedAnalyzer>(@"class A
{
	A this[int index]
	{
		$set$ {
		}
	}
}");
		}

		[Test]
		public void TestMatchingEventAdder()
		{
			Analyze<ValueParameterNotUsedAnalyzer>(@"class A	
{
	delegate void TestEventHandler ();
	TestEventHandler eventTested;
	event TestEventHandler EventTested
	{
		add {
			eventTested += value;
		}
		$remove$ {
		}
	}
}");
		}

		[Test]
		public void TestNonMatchingIndexerSetter()
		{
			Analyze<ValueParameterNotUsedAnalyzer>(@"class A
{
	A this[int index]
	{
		set {
			A a = value;
		}
	}
}");
		}

		[Test]
		public void IgnoresAutoSetter()
		{
			Analyze<ValueParameterNotUsedAnalyzer>(@"class A
{
	string  Property { set; }
}");
		}

		[Test]
		public void IgnoreReadOnlyProperty()
		{
			Analyze<ValueParameterNotUsedAnalyzer>(@"class A
{
	string  Property { get; }
}");
		}

		[Test]
		public void DoesNotCrashOnNullIndexerAccessorBody()
		{
			Analyze<ValueParameterNotUsedAnalyzer>(@"abstract class A
{
	public abstract string this[int i] { get; set; }
}");
		}

		[Test]
		public void DoesNotWarnOnExceptionThrowingAccessor()
		{
			Analyze<ValueParameterNotUsedAnalyzer>(@"abstract class A
{
	public string Property
	{
		set {
			throw new Exception();
		}
	}
}");
		}

		[Test]
		public void DoesNotWarnOnEmptyCustomEvent()
		{
			// Empty custom events are often used when the event can never be raised
			// by a class (but the event is required e.g. due to an interface).
			Analyze<ValueParameterNotUsedAnalyzer>(@"class A	
{
	delegate void TestEventHandler ();
	event TestEventHandler EventTested
	{
		add { }
		remove { }
	}
}");
		}

		[Test]
		public void DoesNotWarnOnNotImplementedCustomEvent()
		{
			Analyze<ValueParameterNotUsedAnalyzer>(@"class A	
{
	delegate void TestEventHandler ();
	event TestEventHandler EventTested
	{
		add { throw new System.NotImplementedException(); }
		remove { throw new System.NotImplementedException(); }
	}
}");
		}
	}
}

