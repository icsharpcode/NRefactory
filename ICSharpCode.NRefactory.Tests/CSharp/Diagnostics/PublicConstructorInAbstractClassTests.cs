// 
// PublicConstructorInAbstractionClassTests.cs
// 
// Author:
//      Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013 Ji Kun 
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

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[TestFixture]
	public class PublicConstructorInAbstractClassTests : InspectionActionTestBase
	{
		[Test]
		public void TestInspectorCase1()
		{
			Analyze<PublicConstructorInAbstractClassAnalyzer>(@"
abstract class TestClass
{
	public $TestClass$ ()
	{
	}
}", @"
abstract class TestClass
{
	protected TestClass ()
	{
	}
}");
		}
		
		[Test]
		public void TestInspectorCase2()
		{
			Analyze<PublicConstructorInAbstractClassAnalyzer>(@"
abstract class TestClass
{
	static TestClass ()
	{
	}
	void TestMethod ()
	{
		var i = 1;
	}
}");
		}
		
		[Test]
		public void TestInspectorCase3()
		{
			Analyze<PublicConstructorInAbstractClassAnalyzer>(@"
abstract class TestClass
{
	public $TestClass$ ()
	{
	}

	private TestClass ()
	{
	}
	
	public $TestClass$ (string str)
	{
		Console.WriteLine(str);
	}
}", @"
abstract class TestClass
{
	protected TestClass ()
	{
	}

	private TestClass ()
	{
	}
	
	protected TestClass (string str)
	{
		Console.WriteLine(str);
	}
}");
		}

		[Test]
		public void TestDisable()
		{
			Analyze<PublicConstructorInAbstractClassAnalyzer>(@"
#pragma warning disable " + NRefactoryDiagnosticIDs.PublicConstructorInAbstractClassAnalyzerID + @"
abstract class TestClass
{
	public TestClass ()
	{
	}
}
");
		}
	}
}