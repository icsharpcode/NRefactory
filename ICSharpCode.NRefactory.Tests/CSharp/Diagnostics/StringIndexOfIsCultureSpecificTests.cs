//
// Author:
//       Daniel Grunwald <daniel@danielgrunwald.de>
//
// Copyright (c) 2012 Daniel Grunwald
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
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[TestFixture]
	public class StringIndexOfIsCultureSpecificTests : InspectionActionTestBase
	{
		const string stringIndexOfStringCalls = @"
using System.Collections.Generic;
class Test {
    public void StringIndexOfStringCalls(List<string> list)
    {
        $list[0].IndexOf("".com"")$;
        $list[0].IndexOf("".com"", 0)$;
        $list[0].IndexOf("".com"", 0, 5)$;
        $list[0].IndexOf(list[1], 0, 10)$;
    }
}";
		const string stringIndexOfStringCallsWithComparison = @"
using System.Collections.Generic;
class Test {
    public void StringIndexOfStringCalls(List<string> list)
    {
        list[0].IndexOf("".com"", System.StringComparison.Ordinal);
        list[0].IndexOf("".com"", 0, System.StringComparison.Ordinal);
        list[0].IndexOf("".com"", 0, 5, System.StringComparison.Ordinal);
        list[0].IndexOf(list[1], 0, 10, System.StringComparison.Ordinal);
    }
}";
		
		[Test]
		public void IndexOfStringCalls()
		{
			Analyze<StringIndexOfIsCultureSpecificAnalyzer>(stringIndexOfStringCalls, stringIndexOfStringCallsWithComparison);
		}
		
		[Test]
		public void IndexOfStringCallsAlreadyWithComparison()
		{
			Analyze<StringIndexOfIsCultureSpecificAnalyzer>(stringIndexOfStringCallsWithComparison);
		}
		
		[Test]
		public void StringIndexOfChar()
		{
			string program = @"using System;
class Test {
	void M(string text) {
		text.IndexOf('.');
	}
}";
			Analyze<StringIndexOfIsCultureSpecificAnalyzer>(program);
		}
		
		[Test]
		public void ListIndexOf()
		{
			string program = @"using System.Collections.Generic;
class Test {
	void M(List<string> list) {
		list.IndexOf("".com"");
	}
}";
			Analyze<StringIndexOfIsCultureSpecificAnalyzer>(program);
		}


		[Test]
		public void TestDisable()
		{
			Analyze<StringIndexOfIsCultureSpecificAnalyzer>(@"using System;
using System.Collections.Generic;
class Test {
	public void StringIndexOfStringCalls(List<string> list)
	{
#pragma warning disable " + NRefactoryDiagnosticIDs.StringIndexOfIsCultureSpecificAnalyzerID + @"
		list[0].IndexOf("".com"");
	}
}");
		}


	}
}
