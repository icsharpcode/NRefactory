using NUnit.Framework;

namespace ICSharpCode.NRefactory.IndentationTests
{
	[TestFixture]
	public class StringTests
	{
		[Test]
		public void TestString_Simple()
		{
			var indent = Helper.CreateEngine(@"""some string""$");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestString_Escaped()
		{
			var indent = Helper.CreateEngine(@"""some escaped \"" string "" { $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestString_NotEscaped()
		{
			var indent = Helper.CreateEngine(@"""some not escaped "" string "" { $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestString_NotEnded()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	""some string {
#if true $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestChar_Simple()
		{
			var indent = Helper.CreateEngine(@"'X'$");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestChar_Escaped()
		{
			var indent = Helper.CreateEngine(@"'\'' { $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestChar_NotEscaped()
		{
			var indent = Helper.CreateEngine(@"''' { $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestChar_NotEnded()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	' { 
#if true $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestVerbatim_Simple()
		{
			var indent = Helper.CreateEngine(@"@"" verbatim string "" $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestVerbatim_MultiLine()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo 
	{
		@"" verbatim string $");
			Assert.AreEqual("\t\t", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestVerbatim_MultiLine2()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	class Foo 
	{
		@"" verbatim string 
{ $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestVerbatim_Escaped()
		{
			var indent = Helper.CreateEngine(@"@"" some """"string { """" in a verbatim string "" $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestVerbatim_NotEscaped()
		{
			var indent = Helper.CreateEngine(@"@"" some ""string { "" in a verbatim string "" $");
			Assert.AreEqual("", indent.ThisLineIndent);
			Assert.AreEqual("\t", indent.NextLineIndent);
		}

		[Test]
		public void TestVerbatim_EscapedMultiLine()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	@"" some verbatim string """" { $");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("", indent.NextLineIndent);
		}

		[Test]
		public void TestVerbatim_EscapedMultiLine2()
		{
			var indent = Helper.CreateEngine(@"
namespace Foo {
	@"" some verbatim string """""" { $");
			Assert.AreEqual("\t", indent.ThisLineIndent);
			Assert.AreEqual("\t\t", indent.NextLineIndent);
		}
	}
}
