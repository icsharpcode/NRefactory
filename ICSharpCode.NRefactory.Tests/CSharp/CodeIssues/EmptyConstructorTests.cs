// 
// RedundantConstructorTest.cs
//  
// Author:
//       Ji Kun <jikun.nus@gmail.com>
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
using ICSharpCode.NRefactory6.CSharp.Refactoring;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[TestFixture]
	public class EmptyConstructorTests : InspectionActionTestBase
	{
		[Test]
		public void TestInspectorCase1()
		{
			Analyze<EmptyConstructorAnalyzer>(@"using System;class Test {private int member; $public Test(){}$}", @"using System;class Test {private int member; }");
		}

		[Test]
		public void TestInspectorCase2()
		{
			Analyze<EmptyConstructorAnalyzer>(@"using System;class Test {private int member;$public Test(){}$ static Test(){}}", @"using System;class Test {private int member;static Test(){}}");
		}

		[Test]
		public void TestResharperDisable()
		{
			Analyze<EmptyConstructorAnalyzer>(@"using System;
	//Resharper disable EmptyConstructor
class Test {
	public Test(){
	}
	//Resharper restore EmptyConstructor	
	}");
		}

		[Test]
		public void TestNegateCase1()
		{
			Analyze<EmptyConstructorAnalyzer>(@"using System;class Test {public Test(){Foo();}}");
		}

		[Test]
		public void TestNegateCase2()
		{
			Analyze<EmptyConstructorAnalyzer>(@"using System;class Test {public Test(){Bar();} private Test(){}}");
		}

		[Test]
		public void TestNegateCase3()
		{
			Analyze<EmptyConstructorAnalyzer>(@"using System;class Test {public Test() : base(4) {}}");
		}
	}
}