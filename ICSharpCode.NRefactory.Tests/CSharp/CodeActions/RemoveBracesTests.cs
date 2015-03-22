// 
// RemoveBracesTests.cs
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

namespace ICSharpCode.NRefactory6.CSharp.CodeActions
{
	[TestFixture]
	public class RemoveBracesTests : ContextActionTestBase
	{
		[Test]
		public void TestSimpleBraces()
		{
			string result = RunContextAction(
				                         new RemoveBracesCodeRefactoringProvider(),
				                         "class TestClass" + Environment.NewLine +
				                         "{" + Environment.NewLine +
				                         "    void Test()" + Environment.NewLine +
				                         "    {" + Environment.NewLine +
				                         "        if (true) ${" + Environment.NewLine +
				                         "            ;" + Environment.NewLine +
				                         "        }" + Environment.NewLine +
				                         "    }" + Environment.NewLine +
				                         "}"
			                         );
            
			Assert.AreEqual(
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"    void Test()" + Environment.NewLine +
				"    {" + Environment.NewLine +
				"        if (true) ;" + Environment.NewLine +
				"    }" + Environment.NewLine +
				"}", result);
		}

		[Test]
		public void TestSimpleBracesWithComment()
		{
			string result = RunContextAction(
										 new RemoveBracesCodeRefactoringProvider(),
										 "class TestClass" + Environment.NewLine +
										 "{" + Environment.NewLine +
										 "    void Test()" + Environment.NewLine +
										 "    {" + Environment.NewLine +
										 "        // Some comment" + Environment.NewLine +
										 "        if (true) ${" + Environment.NewLine +
										 "            ;" + Environment.NewLine +
										 "        }" + Environment.NewLine +
										 "    }" + Environment.NewLine +
										 "}"
									 );

			Assert.AreEqual(
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"    void Test()" + Environment.NewLine +
				"    {" + Environment.NewLine +
				"        // Some comment" + Environment.NewLine +
				"        if (true) ;" + Environment.NewLine +
				"    }" + Environment.NewLine +
				"}", result);
		}

		[Test]
		public void TestTryCatch()
		{
			TestWrongContext<RemoveBracesCodeRefactoringProvider>(@"class TestClass
{
    void Test()
    {
        try ${ ; } catch (Exception) { ; }
    }
}");
		}

		[Test]
		public void TestTryCatchCatch()
		{
			TestWrongContext<RemoveBracesCodeRefactoringProvider>(@"class TestClass
{
    void Test()
    {
        try { ; } catch (Exception) ${ ; }
    }
}");
		}

		[Test]
		public void TestTryCatchFinally()
		{
			TestWrongContext<RemoveBracesCodeRefactoringProvider>(@"class TestClass
{
    void Test()
    {
        try { ; } catch (Exception) { ; } finally ${ ; }
    }
}");
		}


		[Test]
		public void TestSwitchCatch()
		{
			TestWrongContext<RemoveBracesCodeRefactoringProvider>(@"class TestClass
{
    void Test()
    {
        switch (foo    ) ${ default: break;}
    }
}");
		}

		[Test]
		public void TestMethodDeclaration()
		{
			TestWrongContext<RemoveBracesCodeRefactoringProvider>(@"class TestClass
{
    void Test()
    ${
        ;
    }
}");
		}

		[Test]
		public void TestRemoveBracesFromIf()
		{
			Test<RemoveBracesCodeRefactoringProvider>(@"class TestClass
{
    void Test()
    {
        $if (true) {
            Console.WriteLine(""Hello"");
        }
    }
}", @"class TestClass
{
    void Test()
    {
        if (true) Console.WriteLine(""Hello"");
    }
}");
		}

		[Test]
		public void TestRemoveBracesFromElse()
		{
			Test<RemoveBracesCodeRefactoringProvider>(@"class TestClass
{
    void Test()
    {
        if (true) {
            Console.WriteLine(""Hello"");
        } $else {
            Console.WriteLine(""World"");
        }
    }
}", @"class TestClass
{
    void Test()
    {
        if (true) {
            Console.WriteLine(""Hello"");
        }
        else Console.WriteLine(""World"");
    }
}");
		}

		[Test]
		public void TestRemoveBracesFromDoWhile()
		{
			Test<RemoveBracesCodeRefactoringProvider>(@"class TestClass
{
    void Test()
    {
        $do {
            Console.WriteLine(""Hello"");
        } while (true);
    }
}", @"class TestClass
{
    void Test()
    {
        do Console.WriteLine(""Hello"");
        while (true);
    }
}");
		}

		[Test]
		public void TestRemoveBracesFromForeach()
		{
			Test<RemoveBracesCodeRefactoringProvider>(@"class TestClass
{
    void Test()
    {
        $foreach (var a in b) {
            Console.WriteLine(""Hello"");
        }
    }
}", @"class TestClass
{
    void Test()
    {
        foreach (var a in b) Console.WriteLine(""Hello"");
    }
}");
		}

		[Test]
		public void TestRemoveBracesFromFor()
		{
			Test<RemoveBracesCodeRefactoringProvider>(@"class TestClass
{
    void Test()
    {
        $for (; ;) {
            Console.WriteLine(""Hello"");
        }
    }
}", @"class TestClass
{
    void Test()
    {
        for (; ;) Console.WriteLine(""Hello"");
    }
}");
		}

		[Test]
		public void TestRemoveBracesFromLock()
		{
			Test<RemoveBracesCodeRefactoringProvider>(@"class TestClass
{
    void Test()
    {
        $lock (this) {
            Console.WriteLine(""Hello"");
        }
    }
}", @"class TestClass
{
    void Test()
    {
        lock (this) Console.WriteLine(""Hello"");
    }
}");
		}

		[Test]
		public void TestRemoveBracesFromUsing()
		{
			Test<RemoveBracesCodeRefactoringProvider>(@"class TestClass
{
    void Test()
    {
        $using (var a = new A()) {
            Console.WriteLine(""Hello"");
        }
    }
}", @"class TestClass
{
    void Test()
    {
        using (var a = new A()) Console.WriteLine(""Hello"");
    }
}");
		}

		[Test]
		public void TestRemoveBracesFromWhile()
		{
			Test<RemoveBracesCodeRefactoringProvider>(@"class TestClass
{
    void Test()
    {
        $while (true) {
            Console.WriteLine(""Hello"");
        }
    }
}", @"class TestClass
{
    void Test()
    {
        while (true) Console.WriteLine(""Hello"");
    }
}");
		}

		[Test]
		public void TestDoNotRemoveBracesFromBlockWithVariable()
		{
			TestWrongContext<RemoveBracesCodeRefactoringProvider>(@"class TestClass
{
    void Test()
    {
        $if (true) {
            double PI = Math.PI;
        }
    }
}");
		}

		[Test]
		public void TestDoNotRemoveBracesFromBlockWithLabel()
		{
			TestWrongContext<RemoveBracesCodeRefactoringProvider>(@"class TestClass
{
    void Test()
    {
        $if (true) {
            here: Console.WriteLine(""Hello"");
        }
    }
}");
		}

		[Test]
		public void TestNullNode()
		{
			TestWrongContext<RemoveBracesCodeRefactoringProvider>(@"class TestClass
{
    void Test()
    {
        if (true) {
            Console.WriteLine(""Hello"");
        }
    }
}

        $          
");
		}
	}
}

