//
// StaticFieldInGenericTypeTests.cs
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
using ICSharpCode.NRefactory6.CSharp.CodeActions;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory6.CSharp.CodeIssues
{

	[TestFixture]
	[Ignore("TODO: Issue not ported yet")]
	public class StaticFieldInGenericTypeTests : InspectionActionTestBase
	{
		
		[Test]
		public void GenericClass()
		{
			TestIssue<StaticFieldInGenericTypeIssue>(@"
class Foo<T>
{
	static string Data;
}");
		}
		
		[Test]
		public void GenericClassWithGenericField()
		{
			Analyze<StaticFieldInGenericTypeIssue>(@"
class Foo<T>
{
	static System.Collections.Generic.IList<T> Cache;
}");
		}
		
		[Test]
		public void GenericClassWithMultipleGenericFields()
		{
			TestIssue<StaticFieldInGenericTypeIssue>(@"
class Foo<T1, T2>
{
	static System.Collections.Generic.IList<T1> Cache;
}");
		}

		[Test]
		public void NestedGenericClassWithGenericField()
		{
			TestIssue<StaticFieldInGenericTypeIssue>(@"
class Foo<T1>
{
	class Bar<T2>
	{
		static System.Collections.Generic.IList<T1> Cache;
	}
}");
		}
		
		[Test]
		public void NonGenericClass()
		{
			Analyze<StaticFieldInGenericTypeIssue>(@"
class Foo
{
	static string Data;
}");
		}
		
		[Test]
		public void NonStaticField()
		{
			Analyze<StaticFieldInGenericTypeIssue>(@"
class Foo<T>
{
	string Data;
}");
		}

		[Test]
		public void TestMicrosoftSuppressMessage()
		{
			TestIssue<StaticFieldInGenericTypeIssue>(@"using System.Diagnostics.CodeAnalysis;

class Foo<T>
{
	[SuppressMessage(""Microsoft.Design"", ""CA1000:DoNotDeclareStaticMembersOnGenericTypes"")]
	static string Data;

	static string OtherData;
}");
		}

		[Test]
		public void TestAssemblyMicrosoftSuppressMessage()
		{
			Analyze<StaticFieldInGenericTypeIssue>(@"using System.Diagnostics.CodeAnalysis;

[assembly:SuppressMessage(""Microsoft.Design"", ""CA1000:DoNotDeclareStaticMembersOnGenericTypes"")]

class Foo<T>
{
	static string Data;

	static string OtherData;
}");
		}

        [Test]
        public void TestDisable()
        {
            var input = @"using System.Diagnostics.CodeAnalysis;

class Foo<T>
{
    // ReSharper disable once StaticFieldInGenericType
	static string Data;
}";
			Analyze<StaticFieldInGenericTypeIssue>(input);
        }

	}
}

