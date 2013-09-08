//
// AlignmentTests.cs
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
using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.NRefactory.IndentationTests
{
	[TestFixture]
	public class AlignmentTests
	{
		[Test]
		public void MethodCallAlignment()
		{
			CSharpFormattingOptions fmt = FormattingOptionsFactory.CreateMono();
			fmt.AlignToFirstMethodCallArgument = false;
			var indent = Helper.CreateEngine(@"
class Foo
{
	void Test ()
	{
		Call(A,$", fmt);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void IndexerAlignment()
		{
			CSharpFormattingOptions fmt = FormattingOptionsFactory.CreateMono();
			fmt.AlignToFirstIndexerArgument = false;
			var indent = Helper.CreateEngine(@"
class Foo
{
void Test ()
{
Call[A,$", fmt);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void BinaryExpressionAlignment()
		{
			CSharpFormattingOptions fmt = FormattingOptionsFactory.CreateMono();
			fmt.AlignToFirstIndexerArgument = false;
			var indent = Helper.CreateEngine(@"
class Foo
{
	void Test ()
	{
		public static bool IsComplexExpression(AstNode expr)
		{
			return expr.StartLocation.Line != expr.EndLocation.Line ||
				expr is ConditionalExpression ||$", fmt);
			Assert.AreEqual("\t\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void MethodContinuation()
		{
			CSharpFormattingOptions fmt = FormattingOptionsFactory.CreateMono();
			fmt.AlignToFirstMethodCallArgument = false;
			var indent = Helper.CreateEngine(@"
class Foo
{
	void Test ()
	{
		var a = Call(A)
			.Foo ()$", fmt);
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
		}

		[Test]
		public void MethodContinuationDeep()
		{
			CSharpFormattingOptions fmt = FormattingOptionsFactory.CreateMono();
			fmt.AlignToFirstMethodCallArgument = false;
			var indent = Helper.CreateEngine(@"
class Foo
{
	void Test ()
	{
		var a = Call(A)
			.Foo ()
			.Foo ()
			.Foo ()$", fmt);
			Assert.AreEqual("\t\t\t", indent.ThisLineIndent);
		}

		[Test]
		public void AlignEmbeddedIfStatements()
		{
			CSharpFormattingOptions fmt = FormattingOptionsFactory.CreateMono();
			fmt.AlignEmbeddedIfStatements = true;
			var indent = Helper.CreateEngine(@"
class Foo
{
	void Test ()
	{
		if (true)
		if (true)
		if (true) $", fmt);
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void UnalignEmbeddedIfStatements()
		{
			CSharpFormattingOptions fmt = FormattingOptionsFactory.CreateMono();
			fmt.AlignEmbeddedIfStatements = false;
			var indent = Helper.CreateEngine(@"
class Foo
{
	void Test ()
	{
		if (true)
			if (true)
				if (true) $", fmt);
			Assert.AreEqual("\t\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t\t", indent.NextLineIndent);
		}

		[Test]
		public void AlignEmbeddedUsingStatements()
		{
			CSharpFormattingOptions fmt = FormattingOptionsFactory.CreateMono();
			fmt.AlignEmbeddedUsingStatements = true;
			var indent = Helper.CreateEngine(@"
class Foo
{
	void Test (IDisposable a, IDisposable b)
	{
		using (a)
		using (a)
		using (b) $", fmt);
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t", indent.NextLineIndent);
		}
	
		[Test]
		public void UnalignEmbeddedUsingStatements()
		{
			CSharpFormattingOptions fmt = FormattingOptionsFactory.CreateMono();
			fmt.AlignEmbeddedUsingStatements = false;
			var indent = Helper.CreateEngine(@"
class Foo
{
	void Test (IDisposable a, IDisposable b)
	{
		using (a)
			using (a)
				using (b) $", fmt);
			Assert.AreEqual("\t\t\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t\t\t\t", indent.NextLineIndent);
		}

		[Ignore("fixme")]
		[Test]
		public void AlignNamedAttributeArgument()
		{
			CSharpFormattingOptions fmt = FormattingOptionsFactory.CreateMono();
			fmt.AlignToFirstMethodCallArgument = true;
			var indent = Helper.CreateEngine(@"
[Attr (1,
       Foo = 2,$
       Bar = 3", fmt);
			Assert.AreEqual("       ", indent.ThisLineIndent, "this line indent doesn't match");
			Assert.AreEqual("       ", indent.NextLineIndent, "next line indent doesn't match");
		}

		[Test]
		public void UnalignNamedAttributeArguments()
		{
			CSharpFormattingOptions fmt = FormattingOptionsFactory.CreateMono();
			fmt.AlignToFirstMethodCallArgument = false;
			var indent = Helper.CreateEngine(@"
[Attr (1,
	Foo = 2,$
	Bar = 3", fmt);
			Assert.AreEqual("\t", indent.ThisLineIndent, "this line indent doesn't match");
			Assert.AreEqual("\t", indent.NextLineIndent, "next line indent doesn't match");
		}

		[Test]
		public void TestFormatFirstLineKeepFalse()
		{
			var fmt = FormattingOptionsFactory.CreateMono();
			fmt.KeepCommentsAtFirstColumn = false;
			var indent = Helper.CreateEngine(@"
class Foo 
{
 // Hello World$", fmt);
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestFormatFirstLineKeepTrue()
		{
			var fmt = FormattingOptionsFactory.CreateMono();
			fmt.KeepCommentsAtFirstColumn = true;
			var indent = Helper.CreateEngine(@"
class Foo 
{
// Hello World$", fmt);
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}
	}
}

