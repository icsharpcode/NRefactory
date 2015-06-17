using ICSharpCode.NRefactory6.CSharp.Diagnostics;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using NUnit.Framework;

namespace NR6Pack.Tests.CSharp.Diagnostics
{
    [TestFixture]
    public class RewriteIfReturnToReturnTests : InspectionActionTestBase
    {
        [Test]
        public void When_Return_In_IfStatement()
        {
            var input = @"
class TestClass
{
	object TestMethod (object obj)
	{
		if(obj != null)
            return obj;
        return new object();
	}
}";

            Analyze<RewriteIfReturnToReturnAnalyzer>(input, null, 1);
        }


        [Test]
        public void When_Return_Value_Correctly()
        {
            var input = @"
class TestClass
{
	bool TestMethod (object obj)
	{
        return obj!= null
	}
}";

            Analyze<RewriteIfReturnToReturnAnalyzer>(input, null, 0);
        }

        [Test]
        public void When_Return_Statement_Corrected()
        {
            var input = @"
class TestClass
{
	bool TestMethod (object obj)
	{
        if (obj != null)
            return true;
        return false;
	}
}";

            var output = @"
class TestClass
{
	bool TestMethod (object obj)
	{
        return obj!= null
	}
}";

            Analyze<RewriteIfReturnToReturnAnalyzer>(input, output,1,1);
        }        

    }

}
