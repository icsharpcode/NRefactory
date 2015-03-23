// 
// RedundantUsingDirectiveTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
	[Ignore("TODO: Issue not ported yet")]
	public class RedundantUsingDirectiveTests : InspectionActionTestBase
	{
		[Test]
		public void TestInspectorCase1 ()
		{
			Test<RedundantUsingDirectiveAnalyzer>(@"using System;

class Foo
{
	void Bar (string str)
	{
	}
}", @"
class Foo
{
	void Bar (string str)
	{
	}
}");
		}
		
		[Test]
		public void TestInspectorCase2 ()
		{
			Analyze<RedundantUsingDirectiveAnalyzer>(@"using System;

class Foo
{
	void Bar (string str)
	{
	}
}");
		}
		
		[Test]
		public void TestInspectorCase3 ()
		{
			Analyze<RedundantUsingDirectiveAnalyzer>(@"using System;
using System.Collections.Generic;

namespace Foo
{
	class Bar
	{
		List<String> list;
	}
}");
		}
		
		[Test]
		public void Linq1 ()
		{
			Analyze<RedundantUsingDirectiveAnalyzer>(@"using System;
using System.Collections.Generic;
using System.Linq;

class Bar
{
	public object M(List<String> list)
	{
		return list.Where(t => !String.IsNullOrEmpty(t));
	}
}");
		}
		
		[Test]
		public void Linq2 ()
		{
			Analyze<RedundantUsingDirectiveAnalyzer>(@"using System;
using System.Collections.Generic;
using System.Linq;

class Bar
{
	public object M(List<String> list)
	{
		return from t in list where !String.IsNullOrEmpty(t) select t;
	}
}");
		}

		[Test]
		public void TestResharperDisableRestore ()
		{
			TestIssue<RedundantUsingDirectiveAnalyzer>(@"// ReSharper disable RedundantUsingDirective
using System;
// ReSharper restore RedundantUsingDirective
using System.IO;

class Foo
{
}");
		}

		[Test]
		public void TestResharperDisableOnce ()
		{
			TestIssue<RedundantUsingDirectiveAnalyzer>(@"using System;
// ReSharper disable once RedundantUsingDirective
using System.IO;
using System.Text;

class Foo
{
}", 2);
		}

		[Test]
		public void TestSubnamespace ()
		{
			Test<RedundantUsingDirectiveAnalyzer>(@"namespace Foo
{
	using System;
	using System.Collections.Generic;

	class Bar
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ();
		}
	}
}", @"namespace Foo
{
	using System;

	class Bar
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ();
		}
	}
}");
		}

		[Test]
		public void TestSubnamespaceCase1 ()
		{
			Test<RedundantUsingDirectiveAnalyzer>(@"namespace Foo
{
	using System;
	namespace Sub {
		using System;
	}
	
	namespace Sub2 {
		class Bar
		{
			public static void Main (string[] args)
			{
				Console.WriteLine ();
			}
		}
	}
}", @"namespace Foo
{
	using System;
	namespace Sub {
	}
	
	namespace Sub2 {
		class Bar
		{
			public static void Main (string[] args)
			{
				Console.WriteLine ();
			}
		}
	}
}");
		}

		[Test]
		public void TestSubnamespaceCase3 ()
		{
			Test<RedundantUsingDirectiveAnalyzer>(@"namespace Foo
{
	using System;
	
	namespace Sub2 {
		using System;
		class Bar
		{
			public static void Main (string[] args)
			{
				Console.WriteLine ();
			}
		}
	}
}", @"namespace Foo
{
	
	namespace Sub2 {
		using System;
		class Bar
		{
			public static void Main (string[] args)
			{
				Console.WriteLine ();
			}
		}
	}
}");
		}



		[Test]
		public void TestKeywordsInNamespace ()
		{
			Analyze<RedundantUsingDirectiveAnalyzer>(@"

namespace org.eclipse.jgit.@internal.storage.file
{
	public class File {}
}

namespace Foo
{
	using org.eclipse.jgit.@internal.storage.file;
	
	class Bar 
	{
		public static void Main (string[] args)
		{
			File file;
		}
	}
}


");
		}
	}

}
