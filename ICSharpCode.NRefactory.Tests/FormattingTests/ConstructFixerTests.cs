//
// ConstructFixerTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Editor;

namespace ICSharpCode.NRefactory.CSharp.FormattingTests
{
	[TestFixture]
	public class ConstructFixerTests
	{
		static void Test (string input, string expectedOutput)
		{
			int caretPositon = input.IndexOf('$');
			if (caretPositon > 0)
				input = input.Substring(0, caretPositon) + input.Substring(caretPositon + 1);

			var document1 = new StringBuilderDocument(input);

			int expectedCaretPosition = expectedOutput.IndexOf('$');
			if (expectedCaretPosition > 0)
				expectedOutput = expectedOutput.Substring(0, expectedCaretPosition) + expectedOutput.Substring(expectedCaretPosition + 1);

			var fixer = new ConstructFixer(FormattingOptionsFactory.CreateMono ());
			int newCaretPosition;
			Assert.IsTrue(fixer.TryFix(document1, caretPositon, out newCaretPosition));   
			if (expectedOutput != document1.Text) {
				System.Console.WriteLine("expected:");
				System.Console.WriteLine(expectedOutput);
				System.Console.WriteLine("was:");
				System.Console.WriteLine(document1.Text);
			}
			Assert.AreEqual(expectedOutput, document1.Text); 
			Assert.AreEqual(expectedCaretPosition, newCaretPosition); 
		}

		[Test]
		public void TestMethodCallCase1()
		{
			Test(
				@"
class Foo
{
	void Bar(int i)
	{
		Bar (42$
	}
}", @"
class Foo
{
	void Bar(int i)
	{
		Bar (42);$
	}
}");
		}

		[Test]
		public void TestMethodCallCase2()
		{
			Test(
				@"
class Foo
{
	void Bar()
	{
		Bar$ (;
	}
}", @"
class Foo
{
	void Bar()
	{
		Bar ();$
	}
}");
		}

		[Test]
		public void TestMethodCallCase3()
		{
			Test(
				@"
class Foo
{
	void Bar()
	{
		Bar ($)
	}
}", @"
class Foo
{
	void Bar()
	{
		Bar ();$
	}
}");
		}
	
		[Test]
		public void TestClassDeclaration()
		{
			Test(@"class Foo$", @"class Foo
{
	$
}");
		}

		[Ignore("Fixme - parser error")]
		[Test]
		public void TestClassDeclaration_TestErrorRecovery()
		{
			Test(@"class Foo$
// Foo
", @"class Foo
{
	$
}

// Foo");
		}

		[Ignore("Fixme - parser error")]
		[Test]
		public void TestDelegateDeclaration()
		{
			Test(
@"delegate void FooHandler (object foo$", 
@"delegate void FooHandler (object foo);
$");
		}

		[Ignore("Fixme - parser error")]
		[Test]
		public void TestMethodDeclaration()
		{
			Test(
				@"
class Foo
{
	void Bar(int i$
}
", @"
class Foo
{
	void Bar(int i)
	{
		$
	}
}");
		}

		[Test]
		public void TestIfStatement()
		{
			Test(
				@"
class Foo
{
	void Bar(int i)
	{
		if (true$
	}
}", @"
class Foo
{
	void Bar(int i)
	{
		if (true) {
			$
		}
	}
}");
		}

		[Test]
		public void TestSwitch()
		{
			Test(
				@"
class Foo
{
	void Bar(int i)
	{
		switch (i$
	}
}
", @"
class Foo
{
	void Bar(int i)
	{
		switch (i) {
			$
		}
	}
}
");
		}

		[Ignore("Fixme - parser error")]
		[Test]
		public void TestCase()
		{
			Test(
				@"
class Foo
{
	void Bar(int i)
	{
		switch (i) {
			case 1$
		}
	}
}
", @"
class Foo
{
	void Bar(int i)
	{
		switch (i) {
			case 1:
				$
		}
	}
}");
		}

	}
}

