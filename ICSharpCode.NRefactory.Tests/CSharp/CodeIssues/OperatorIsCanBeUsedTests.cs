// 
// ReplaceWithIsOperatorTests.cs
//  
// Author:
//       Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013 Ji Kun
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
	public class OperatorIsCanBeUsedTests : InspectionActionTestBase
	{
		[Test]
		public void TestInspectorCase1()
		{
			Analyze<OperatorIsCanBeUsedAnalyzer>(@"
using System;
using System.Linq;
using System.Reflection;

namespace Demo
{
	public class BaseClass
	{
		public static void main(string[] args)
		{
			int a = 1;
			if ($typeof(int) == a.GetType()$) {
			}
		}
	}
}", @"
using System;
using System.Linq;
using System.Reflection;

namespace Demo
{
	public class BaseClass
	{
		public static void main(string[] args)
		{
			int a = 1;
			if (a is int) {
			}
		}
	}
}");
		}

		[Test]
		public void TestInspectorCase2()
		{
			Analyze<OperatorIsCanBeUsedAnalyzer>(@"
using System;
using System.Linq;
using System.Reflection;

namespace Demo
{
	public class BaseClass
	{
		public static void main(string[] args)
		{
			int a = 1;
			if ($a.GetType() == typeof(int)$) {
			}
		}
	}
}", @"
using System;
using System.Linq;
using System.Reflection;

namespace Demo
{
	public class BaseClass
	{
		public static void main(string[] args)
		{
			int a = 1;
			if (a is int) {
			}
		}
	}
}");
		}

		[Test]
		public void TestInspectorCase3()
		{
			Analyze<OperatorIsCanBeUsedAnalyzer>(@"
using System;
using System.Linq;
using System.Reflection;

namespace Demo
{
	public class BaseClass
	{
		static public int a;
		public static void main(string[] args)
		{
			if ($BaseClass.a.GetType() == typeof(int)$) {
			}
		}
	}
}", @"
using System;
using System.Linq;
using System.Reflection;

namespace Demo
{
	public class BaseClass
	{
		static public int a;
		public static void main(string[] args)
		{
			if (BaseClass.a is int) {
			}
		}
	}
}");
		}

		[Test]
		public void TestInspectorCase4()
		{
			Analyze<OperatorIsCanBeUsedAnalyzer>(@"
using System;
using System.Reflection;

namespace Demo
{
	public sealed class TestClass
	{
	}

	public class BaseClass 
	{
		public static void main(string[] args)
		{
			BaseClass b = new BaseClass();
			if ($typeof(TestClass) == b.GetType()$) {
			}
		}
	}
}", @"
using System;
using System.Reflection;

namespace Demo
{
	public sealed class TestClass
	{
	}

	public class BaseClass 
	{
		public static void main(string[] args)
		{
			BaseClass b = new BaseClass();
			if (b is TestClass) {
			}
		}
	}
}");
		}

		[Test]
		public void TestInspectorCase5()
		{
			Analyze<OperatorIsCanBeUsedAnalyzer>(@"
using System;
using System.Reflection;

namespace Demo
{
	public class TestClass
	{
	}

	public class BaseClass : TestClass
	{
		public static void main(string[] args)
		{
			BaseClass b = new BaseClass();
			if ((typeof(TestClass) == b.GetType())) {

			}
		}
	}
}");
		}

		[Test]
		public void TestResharperDisable()
		{
			Analyze<OperatorIsCanBeUsedAnalyzer>(@"using System;
using System.Linq;
using System.Reflection;

namespace Demo
{
	public class BaseClass
	{
		public static void main(string[] args)
		{
			int a = 1;
			//Resharper disable OperatorIsCanBeUsed
			if ((typeof (int) == a.GetType())) {
			}
			//Resharper restore OperatorIsCanBeUsed
		}
	}
}");
		}
	}
}