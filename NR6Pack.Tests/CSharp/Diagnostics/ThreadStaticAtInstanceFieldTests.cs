//
// ThreadStaticAtInstanceFieldTests.cs
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
using ICSharpCode.NRefactory6.CSharp.CodeRefactorings;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[TestFixture]
	public class ThreadStaticAtInstanceFieldTests : InspectionActionTestBase
	{
		[Test]
		public void TestInspectorCase1()
		{
			Analyze<ThreadStaticAtInstanceFieldAnalyzer>(@"using System;
class Foo
{
	[$ThreadStatic$]
	int bar;
}", @"using System;
class Foo
{
	int bar;
}");
		}

		[Test]
		public void TestInspectorCase2()
		{
			Analyze<ThreadStaticAtInstanceFieldAnalyzer>(@"using System;
class Foo
{
	[Serializable, $ThreadStatic$]
	int bar;
}", @"using System;
class Foo
{
	[Serializable]
	int bar;
}");
		}

		[Test]
		public void TestInspectorCase3()
		{
			Analyze<ThreadStaticAtInstanceFieldAnalyzer>(@"class Foo
{
	[$System.ThreadStatic$, System.Serializable]
	int bar;
}", @"class Foo
{
	[System.Serializable]
	int bar;
}");
		}



		[Test]
		public void TestDisable()
		{
			Analyze<ThreadStaticAtInstanceFieldAnalyzer>(@"using System;
class Foo
{
#pragma warning disable " + NRefactoryDiagnosticIDs.ThreadStaticAtInstanceFieldAnalyzerID + @"

	[ThreadStatic]
	int bar;
}");

		}


		[Test]
		public void InstanceField()
		{
			Analyze<ThreadStaticAtInstanceFieldAnalyzer>(@"
using System;
class TestClass
{
	[$ThreadStatic$]
	string field;
}", @"
using System;
class TestClass
{
	string field;
}");
		}

		[Test]
		public void InstanceFieldWithMultiAttributeSection()
		{
			Analyze<ThreadStaticAtInstanceFieldAnalyzer>(@"
using System;
class TestClass
{
	[field: $ThreadStatic$, ContextStatic]
	string field;
}", @"
using System;
class TestClass
{
	[field: ContextStatic]
	string field;
}");
		}

		[Test]
		public void StaticField()
		{
			Analyze<ThreadStaticAtInstanceFieldAnalyzer>(@"
using System;
class TestClass
{
	[ThreadStatic]
	static string field;
}");
		}
	}
}

