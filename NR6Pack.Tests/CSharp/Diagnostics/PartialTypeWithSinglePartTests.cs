// 
// RedundantPartialTypeTests.cs
// 
// Author:
//      Luís Reis <luiscubal@gmail.com>
// 
// Copyright (c) 2013 Luís Reis
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
using NUnit.Framework;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[TestFixture]
	public class PartialTypeWithSinglePartTests : InspectionActionTestBase
	{
		[Test]
		public void TestRedundantModifier ()
		{
			Analyze<PartialTypeWithSinglePartAnalyzer> (
@"$partial$ class TestClass
{
}", @"class TestClass
{
}");
		}

		[Test]
		public void TestNecessaryModifier ()
		{
			Analyze<PartialTypeWithSinglePartAnalyzer> ((string)@"
partial class TestClass
{
}
partial class TestClass
{
}");
		}

		[Test]
		public void TestDisable ()
		{
			Analyze<PartialTypeWithSinglePartAnalyzer> (@"
#pragma warning disable " + NRefactoryDiagnosticIDs.PartialTypeWithSinglePartDiagnosticID + @"
partial class TestClass
{
}");
		}

		[Test]
		public void TestRedundantNestedPartial ()
		{
			Analyze<PartialTypeWithSinglePartAnalyzer> (@"
partial class TestClass
{
	$partial$ class Nested
	{
	}
}
partial class TestClass
{
}", @"
partial class TestClass
{
	class Nested
	{
	}
}
partial class TestClass
{
}");
		}

		[Test]
		public void TestRedundantNestedPartialInNonPartialOuterClass ()
		{
			Analyze<PartialTypeWithSinglePartAnalyzer> (@"
class TestClass
{
	$partial$ class Nested
	{
	}
}", @"
class TestClass
{
	class Nested
	{
	}
}");
		}

		[Test]
		public void TestRedundantNestedPartialDisable ()
		{
			Analyze<PartialTypeWithSinglePartAnalyzer> (@"
#pragma warning disable " + NRefactoryDiagnosticIDs.PartialTypeWithSinglePartDiagnosticID + @"
partial class TestClass
{
	#pragma warning restore " + NRefactoryDiagnosticIDs.PartialTypeWithSinglePartDiagnosticID + @"
	$partial$ class Nested
	{
	}
}
", @"
#pragma warning disable " + NRefactoryDiagnosticIDs.PartialTypeWithSinglePartDiagnosticID + @"
partial class TestClass
{
	#pragma warning restore " + NRefactoryDiagnosticIDs.PartialTypeWithSinglePartDiagnosticID + @"
	class Nested
	{
	}
}
");
		}


		[Test]
		public void TestNeededNestedPartial ()
		{
			Analyze<PartialTypeWithSinglePartAnalyzer>(@"
partial class TestClass
{
	partial class Nested
	{
	}
}
partial class TestClass
{
	partial class Nested
	{
	}
}");
		}


	}
}