// 
// CopyCommentsFromBaseTests.cs
//  
// Author:
//       Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013 Ji Kun <jikun.nus@gmail.com>
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

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[TestFixture]
	public class CopyCommentsFromBaseTest : ContextActionTestBase
	{
		[Test]
		public void TestCopyMethodMultiString ()
		{
            
			Test<CopyCommentsFromBaseCodeRefactoringProvider> (@"
namespace TestNS
{
    class TestClass
    {
        ///<summary>ssss
        ///ssss</summary>
        public virtual void Test()
        {
            int a;
        }
    }
    class DerivdedClass : TestClass
    {
        public override void $Test()
        {
            string str = string.Empty;
        }
    }
}", @"
namespace TestNS
{
    class TestClass
    {
        ///<summary>ssss
        ///ssss</summary>
        public virtual void Test()
        {
            int a;
        }
    }
    class DerivdedClass : TestClass
    {
        /// <summary>ssss
        /// ssss</summary>
        public override void Test()
        {
            string str = string.Empty;
        }
    }
}");
		}

		[Test]
		public void TestCopyMethodSingleString ()
		{

			Test<CopyCommentsFromBaseCodeRefactoringProvider> (@"
namespace TestNS
{
    class TestClass
    {
        ///ssss
        public virtual void Test()
        {
            int a;
        }
    }
    class DerivdedClass : TestClass
    {
        public override void $Test()
        {
            string str = string.Empty;
        }
    }
}", @"
namespace TestNS
{
    class TestClass
    {
        ///ssss
        public virtual void Test()
        {
            int a;
        }
    }
    class DerivdedClass : TestClass
    {
        /// ssss
        public override void Test()
        {
            string str = string.Empty;
        }
    }
}");
		}

		[Test]
		public void TestCopyMethodAbstractClassString ()
		{
            
			Test<CopyCommentsFromBaseCodeRefactoringProvider> (@"
namespace TestNS
{
    abstract class TestClass
    {
        ///ssss
        ///ssss
        public abstract void Test();
    }
    class DerivdedClass : TestClass
    {
        public override void $Test()
        {
            string str = string.Empty;
        }
    }
}", @"
namespace TestNS
{
    abstract class TestClass
    {
        ///ssss
        ///ssss
        public abstract void Test();
    }
    class DerivdedClass : TestClass
    {
        /// ssss
        /// ssss
        public override void Test()
        {
            string str = string.Empty;
        }
    }
}");
		}
	
	
		[Test]
		public void TestCopyProperty ()
		{

			Test<CopyCommentsFromBaseCodeRefactoringProvider> (@"
namespace TestNS
{
    class TestClass
    {
        /// <summary>
        /// FooBar
        /// </summary>
        public virtual int Test { get; set; }
    }
    class DerivdedClass : TestClass
    {
        public override int $Test { get; set; }
    }
}", @"
namespace TestNS
{
    class TestClass
    {
        /// <summary>
        /// FooBar
        /// </summary>
        public virtual int Test { get; set; }
    }
    class DerivdedClass : TestClass
    {
        /// <summary>
        /// FooBar
        /// </summary>
        public override int Test { get; set; }
    }
}");
		}

		[Test]
		public void TestCopyType ()
		{

			Test<CopyCommentsFromBaseCodeRefactoringProvider> (@"
/// <summary>
/// FooBar
/// </summary>
class Base 
{
}

class $TestClass : Base
{
}
", @"
/// <summary>
/// FooBar
/// </summary>
class Base 
{
}

/// <summary>
/// FooBar
/// </summary>
class TestClass : Base
{
}
");
		}


		[Test]
		public void TestSkipExisting ()
		{
			TestWrongContext <CopyCommentsFromBaseCodeRefactoringProvider> (@"
/// <summary>
/// FooBar
/// </summary>
class Base 
{
}

/// <summary>
/// FooBar
/// </summary>
class $TestClass : Base
{
}
");
		}

		[Test]
		public void TestSkipEmpty ()
		{
			TestWrongContext <CopyCommentsFromBaseCodeRefactoringProvider> (@"
class Base 
{
}

class $TestClass : Base
{
}
");
		}



		[Test]
		public void TestInterfaceSimpleCase()
		{
			Test<CopyCommentsFromBaseCodeRefactoringProvider>(@"
interface ITest
{
	///sssss
	void Method ();
}
class TestClass : ITest
{
	public void $Method ()
	{
	}
}", @"
interface ITest
{
	///sssss
	void Method ();
}
class TestClass : ITest
{
	///sssss
	public void Method ()
	{
	}
}");
		}

		[Test]
		public void TestInterfaceMultiCase()
		{
			Test<CopyCommentsFromBaseCodeRefactoringProvider>(@"
interface ITest
{
	///sssss
	///sssss
	void Method ();
}
class TestClass : ITest
{
	public void $Method ()
	{
	}
}", @"
interface ITest
{
	///sssss
	///sssss
	void Method ();
}
class TestClass : ITest
{
	///sssss
	///sssss
	public void Method ()
	{
	}
}");
		}	

		[Ignore]
		public void TestInterfaceNoProblem()
		{
			Test<CopyCommentsFromBaseCodeRefactoringProvider>(@"
interface ITest
{
	void Method ();
}
class TestClass : ITest
{
	public void $Method ()
	{
	}
}", @"
interface ITest
{
	void Method ();
}
class TestClass : ITest
{
	public void Method ()
	{
	}
}");
		}

	}
}