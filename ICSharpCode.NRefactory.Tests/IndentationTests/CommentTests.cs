using NUnit.Framework;

namespace ICSharpCode.NRefactory.IndentationTests
{
	[TestFixture]
	public class CommentTests
	{
		[Test]
		public void TestLineComment_Simple()
		{
			var indent = Helper.CreateEngine("// comment $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestLineComment_PreProcessor()
		{
			var indent = Helper.CreateEngine(@"
#if NOTTHERE
	// comment $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestLineComment_Class()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	// comment $");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestLineComment_For()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	for (;;)
		// comment 
		$");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}

		[Test]
		public void TestLineComment_For2()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	for (;;)
		// comment 
		Test();
	$");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestLineComment_For3()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	for (;;) ;
	// comment $");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestMultiLineComment_Simple()
		{
			var indent = Helper.CreateEngine(@"/* comment */$");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestMultiLineComment_ExtraSpaces()
		{
			var indent = Helper.CreateEngine(@"/* comment $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("   ", indent.NextLineIndent);
		}

		[Test]
		public void TestMultiLineComment_MultiLines()
		{
			var indent = Helper.CreateEngine(@"
class Foo {
	/* line 1 
		* line 2
		**/$");
			Assert.AreEqual("\t   ", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestMultiLineComment_MultiLinesExtraSpaces()
		{
			var indent = Helper.CreateEngine(@"
class Foo { /* line 1 
				* line 2
				* $");
			Assert.AreEqual("               ", indent.ThisLineIndent);
			Assert.AreEqual("               ", indent.NextLineIndent);
		}
	}
}
