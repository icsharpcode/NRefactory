//
// ConvertIfStatementToConditionalTernaryExpressionTests.cs
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
	public class ConvertIfStatementToConditionalTernaryExpressionTests : InspectionActionTestBase
	{
		[Test]
		public void TestIfElse ()
		{
			Analyze<ConvertIfStatementToConditionalTernaryExpressionAnalyzer>(@"class Foo
{
	static int Bar (int x)
	{
		int result;
		$if$ (x > 10)
			result = 10;
		else
			result = 20;
		return result;
	}
}");
		}

		[Test]
		public void TestSkipComplexCondition ()
		{
			Analyze<ConvertIfStatementToConditionalTernaryExpressionAnalyzer>(@"class Foo
{
	static int Bar (int x)
	{
		int result;
		if (x > 10 ||
		    x < -10)
			result = 10;
		else
			result = 20;
		return result;
	}
}");
		}

		[Test]
		public void TestSkipIfElseIf ()
		{
			Analyze<ConvertIfStatementToConditionalTernaryExpressionAnalyzer>(@"class Foo
{
	static int Bar (int x)
	{
		int result;
		if (x < 10)
			result = -10;
		else $if$ (x > 10)
			result = 10;
		else
			result = 20;
		return result;
	}
}");
		}

		[Test]
		public void TestSkipComplexTrueExpression ()
		{
			Analyze<ConvertIfStatementToConditionalTernaryExpressionAnalyzer>(@"class Foo
{
	static int Bar (int x)
	{
		int result;
		if (x > 10)
			result = 10 +
					 12;
		else
			result = 20;
		return result;
	}
}");
		}

		[Test]
		public void TestSkipComplexFalseExpression ()
		{
			Analyze<ConvertIfStatementToConditionalTernaryExpressionAnalyzer>(@"class Foo
{
	static int Bar (int x)
	{
		int result;
		if (x > 10)
			result = 10;
		else
			result = 20 +

12;
		return result;
	}
}");
		}

		[Test]
		public void TestDisable ()
		{
			Analyze<ConvertIfStatementToConditionalTernaryExpressionAnalyzer>(@"class Foo
{
	static int Bar (int x)
	{
		int result;
#pragma warning disable " + NRefactoryDiagnosticIDs.ConvertIfStatementToConditionalTernaryExpressionAnalyzerID + @"
		if (x > 10)
			result = 10;
		else
			result = 20;
		return result;
	}
}");
		}

		[Test]
		public void TestSkipAnnoyingCase1 ()
		{
			Analyze<ConvertIfStatementToConditionalTernaryExpressionAnalyzer>(@"class Foo
{
	int Bar(string example)
	{
		if (!string.IsNullOrEmpty (example)) {
			text = Environment.NewLine != ""\n"" ? example.Replace (""\n"", Environment.NewLine) : example;
		} else {
			text = """";
		}
	}
}");
		}
		

	}
}

