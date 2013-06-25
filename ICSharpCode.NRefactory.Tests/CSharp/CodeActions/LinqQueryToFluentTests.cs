// 
// LinqQueryToFluentTests.cs
//  
// Author:
//       Luís Reis <luiscubal@gmail.com>
// 
// Copyright (c) 2013 Luís Reis
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
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	[TestFixture]
	public class LinqQueryToFluentTests : ContextActionTestBase
	{
		[Test]
		public void TestSimpleQuery()
		{
			string input = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = $from x in System.Enumerable.Empty<int> ()
                   select x;
	}
}
";

			string output = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = System.Enumerable.Empty<int> ().Select (x => x);
	}
}
";

			Assert.AreEqual(output, RunContextAction(new LinqQueryToFluentAction(), input));
		}

		[Test]
		public void TestWhereQuery()
		{
			string input = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = $from x in System.Enumerable.Empty<int> ()
                   where x > 1
                   select x;
	}
}
";

			string output = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = System.Enumerable.Empty<int> ().Where (x => x > 1).Select (x => x);
	}
}
";

			Assert.AreEqual(output, RunContextAction(new LinqQueryToFluentAction(), input));
		}

		[Test]
		public void TestOrderByQuery()
		{
			string input = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = $from x in System.Enumerable.Empty<int> ()
                   orderby x, x * 2 descending
                   select x;
	}
}
";

			string output = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = System.Enumerable.Empty<int> ().OrderBy (x => x).ThenByDescending (x => x * 2).Select (x => x);
	}
}
";

			Assert.AreEqual(output, RunContextAction(new LinqQueryToFluentAction(), input));
		}

		[Test]
		public void TestDoubleFromWithSelectQuery()
		{
			string input = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var newEnumerable = System.Enumerable.Empty<int> ();
		var data = $from x in System.Enumerable.Empty<int> ()
                   from y in newEnumerable
                   select x * y;
	}
}
";

			string output = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var newEnumerable = System.Enumerable.Empty<int> ();
		var data = System.Enumerable.Empty<int> ().SelectMany (x => newEnumerable, (x, y) => x * y);
	}
}
";

			Assert.AreEqual(output, RunContextAction(new LinqQueryToFluentAction(), input));
		}

		[Test]
		public void TestDoubleFromWithIntermediateQuery()
		{
			string input = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var newEnumerable = System.Enumerable.Empty<int> ();
		var data = $from x in System.Enumerable.Empty<int> ()
                   from y in newEnumerable
                   where x > y
                   select x * y;
	}
}
";

			string output = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var newEnumerable = System.Enumerable.Empty<int> ();
		var data = System.Enumerable.Empty<int> ().SelectMany (x => newEnumerable, (x, y) => new {
	x,
	y
}).Where (_ => _.x > _.y).Select (_ => _.x * _.y);
	}
}
";

			Assert.AreEqual(output, RunContextAction(new LinqQueryToFluentAction(), input));
		}

		[Test]
		public void TestLetQuery()
		{
			string input = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = $from x in System.Enumerable.Empty<int> ()
                   let y = x * 2
                   select x * y;
	}
}
";

			string output = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = System.Enumerable.Empty<int> ().Select (x => new {
	x,
	y = x * 2
}).Select (_ => _.x * _.y);
	}
}
";

			Assert.AreEqual(output, RunContextAction(new LinqQueryToFluentAction(), input));
		}

		[Test]
		public void TestCastQuery()
		{
			string input = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = $from float x in System.Enumerable.Empty<int> ()
                   select x;
	}
}
";

			string output = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = System.Enumerable.Empty<int> ().Cast<float> ().Select (x => x);
	}
}
";

			Assert.AreEqual(output, RunContextAction(new LinqQueryToFluentAction(), input));
		}

		[Test]
		public void TestJoinQuery()
		{
			string input = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = $from x in System.Enumerable.Empty<int> ()
                   join float y in new int[] { 4, 5, 6 } on x * 2 equals y
                   select x * y;
	}
}
";

			string output = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = System.Enumerable.Empty<int> ().Join (new int[] { 4, 5, 6 }.Cast<float> (), x => x * 2, y => y, (x, y) => x * y);
	}
}
";

			Assert.AreEqual(output, RunContextAction(new LinqQueryToFluentAction(), input));
		}

		[Test]
		public void TestJoinWithIntermediateQuery()
		{
			string input = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = $from x in System.Enumerable.Empty<int> ()
                   join float y in new int[] { 4, 5, 6 } on x * 2 equals y
                   where x == 2
                   select x * y;
	}
}
";

			string output = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = System.Enumerable.Empty<int> ().Join (new int[] { 4, 5, 6 }.Cast<float> (), x => x * 2, y => y, (x, y) => new { x, y }).Where (_ => _.x == 2).Select (_ => _.x * _.y);
	}
}
";

			Assert.AreEqual(output, RunContextAction(new LinqQueryToFluentAction(), input));
		}

		[Test]
		public void TestJoinWithIntoQuery()
		{
			string input = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = $from x in System.Enumerable.Empty<int> ()
                   join y in new int[] { 4, 5, 6 } on x * 2 equals y
                   into g
                   select g;
	}
}
";

			string output = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = System.Enumerable.Empty<int> ().GroupJoin (new int[] { 4, 5, 6 }, x => x * 2, y => y, (x, g) => g);
	}
}
";

			Assert.AreEqual(output, RunContextAction(new LinqQueryToFluentAction(), input));
		}

		[Test]
		public void TestSimpleGroup()
		{
			string input = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = $from x in System.Enumerable.Empty<int> ()
                   group x by x % 10;
	}
}
";

			string output = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = System.Enumerable.Empty<int> ().GroupBy (x => x % 10);
	}
}
";

			Assert.AreEqual(output, RunContextAction(new LinqQueryToFluentAction(), input));
		}

		[Test]
		public void TestDifferentGroup()
		{
			string input = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = $from x in System.Enumerable.Empty<int> ()
                   group x / 10 by x % 10;
	}
}
";

			string output = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = System.Enumerable.Empty<int> ().GroupBy (x => x % 10, x => x / 10);
	}
}
";

			Assert.AreEqual(output, RunContextAction(new LinqQueryToFluentAction(), input));
		}

		[Test]
		public void TestInto()
		{
			string input = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = $from x in System.Enumerable.Empty<int> ()
                   select x * 2 into y
                   select y * 3;
	}
}
";

			string output = @"
using System.Linq;
public class TestClass
{
	public void TestMethod()
	{
		var data = System.Enumerable.Empty<int> ().Select (x => x * 2).Select (y => y * 3);
	}
}
";

			Assert.AreEqual(output, RunContextAction(new LinqQueryToFluentAction(), input));
		}
	}
}

