// 
// CheckIfParameterIsNullTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc.
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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[TestFixture]
	public class CheckIfParameterIsNullTests : ContextActionTestBase
	{
		[Test]
		public void Test()
		{
			string result = RunContextAction(
				                         new CheckIfParameterIsNullCodeRefactoringProvider(),
				                         "using System;" + Environment.NewLine +
				                         "class TestClass" + Environment.NewLine +
				                         "{" + Environment.NewLine +
				                         "    void Test (string $param)" + Environment.NewLine +
				                         "    {" + Environment.NewLine +
				                         "        Console.WriteLine (param);" + Environment.NewLine +
				                         "    }" + Environment.NewLine +
				                         "}"
			                         );
            
			Assert.AreEqual(
				"using System;" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"    void Test (string param)" + Environment.NewLine +
				"    {" + Environment.NewLine +
				"        if (param == null)" + Environment.NewLine +
				"            throw new ArgumentNullException(nameof(param));" + Environment.NewLine +
				"        Console.WriteLine (param);" + Environment.NewLine +
				"    }" + Environment.NewLine +
				"}", result);
		}

		[Ignore("broken")]
		[Test]
		public void TestWithComment()
		{
			string result = RunContextAction(
										 new CheckIfParameterIsNullCodeRefactoringProvider(),
										 "using System;" + Environment.NewLine +
										 "class TestClass" + Environment.NewLine +
										 "{" + Environment.NewLine +
										 "    void Test (string $param)" + Environment.NewLine +
										 "    {" + Environment.NewLine +
										 "        // Some comment" + Environment.NewLine +
										 "        Console.WriteLine (param);" + Environment.NewLine +
										 "    }" + Environment.NewLine +
										 "}"
									 );

			Assert.AreEqual(
				"using System;" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"    void Test (string param)" + Environment.NewLine +
				"    {" + Environment.NewLine +
				"        if (param == null)" + Environment.NewLine +
				"            throw new ArgumentNullException(\"param\");" + Environment.NewLine +
				"        // Some comment" + Environment.NewLine +
				"        Console.WriteLine (param);" + Environment.NewLine +
				"    }" + Environment.NewLine +
				"}", result);
		}

		[Test]
		public void TestLambda()
		{
			Test<CheckIfParameterIsNullCodeRefactoringProvider>(@"class Foo
{
    void Test ()
    {
        var lambda = ($sender, e) => {
        };
    }
}", @"class Foo
{
    void Test ()
    {
        var lambda = (sender, e) => {
            if (sender == null)
                throw new System.ArgumentNullException(nameof(sender));
        };
    }
}");
		}

		[Test]
		public void TestAnonymousMethod()
		{
			Test<CheckIfParameterIsNullCodeRefactoringProvider>(@"class Foo
{
    void Test ()
    {
        var lambda = delegate(object $-[sender]-, object e) {
        };
    }
}", @"class Foo
{
    void Test ()
    {
        var lambda = delegate(object sender, object e) {
            if (sender == null)
                throw new System.ArgumentNullException(nameof(sender));
        };
    }
}");
		}

		[Test]
		public void TestNullCheckAlreadyThere_StringName()
		{
			TestWrongContext<CheckIfParameterIsNullCodeRefactoringProvider>(@"class Foo
{
    void Test ()
    {
        var lambda = ($sender, e) => {
            if (sender == null)
                throw new System.ArgumentNullException(""sender"");
        };
    }
}");
		}

		[Test]
		public void TestNullCheckAlreadyThere_NameOf()
		{
			TestWrongContext<CheckIfParameterIsNullCodeRefactoringProvider>(@"class Foo
{
    void Test ()
    {
        var lambda = ($sender, e) => {
            if (sender == null)
                throw new System.ArgumentNullException(nameof(sender));
        };
    }
}");
		}

		[Test]
		public void TestPopupOnlyOnName()
		{
			TestWrongContext<CheckIfParameterIsNullCodeRefactoringProvider>(@"class Foo
{
	void Test ($string param)
	{
	}
}");
		}


		[Test]
		public void Test_OldCSharp()
		{
			var parseOptions = new CSharpParseOptions (
				LanguageVersion.CSharp5,
				DocumentationMode.Diagnose | DocumentationMode.Parse,
				SourceCodeKind.Regular,
				ImmutableArray.Create ("DEBUG", "TEST")
			);

			Test<CheckIfParameterIsNullCodeRefactoringProvider>(@"class Foo
{
    void Test (string $test)
    {
    }
}", @"class Foo
{
    void Test (string test)
    {
        if (test == null)
            throw new System.ArgumentNullException(""test"");
    }
}", parseOptions: parseOptions);
		}
	}
}
