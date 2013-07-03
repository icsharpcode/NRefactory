// 
// ConvertToEnumTests.cs
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
	public class ConvertToEnumTests : ContextActionTestBase
	{
		[Test]
		public void TestBasic()
		{
			Test<ConvertToEnumAction>(@"class TestClass
{
	const int $X_Y = 1;
	const int X_Z = 2;
	const int X_W = X_Y;
}", @"class TestClass
{
	enum X : int
	{
		Y = 1,
		Z = 2,
		W = Y
	}
}");
		}

		[Test]
		public void TestMixedTypes()
		{
			Test<ConvertToEnumAction>(@"class TestClass
{
	const System.Int64 $X_Y = 1;
	const int X_Z = 2;
	const System.Int64 X_W = X_Y;
}", @"class TestClass
{
	enum X : long
	{
		Y = 1,
		W = Y
	}
	const int X_Z = 2;
}");
		}

		[Test]
		public void TestCombinedFields()
		{
			Test<ConvertToEnumAction>(@"class TestClass
{
	const int $X_Y = 1, Z = 2;
	const int X_K = 1;
}", @"class TestClass
{
	enum X : int
	{
		Y = 1,
		K = 1
	}
	const int Z = 2;
}");
		}

		[Test]
		public void TestSingleFile()
		{
			Test<ConvertToEnumAction>(@"class TestClass
{
	const int $X_Y = 1;
	const int X_Z = 2;
	const int X_W = X_Y;

	void Foo ()
	{
		int x = X_Y;
	}
	void Foo (int x)
	{
		int y = X_Z;
	}
	void Foo (char x)
	{
		int y = X_W;
	}
}", @"class TestClass
{
	enum X : int
	{
		Y = 1,
		Z = 2,
		W = Y
	}
	void Foo ()
	{
		int x = ((int)X.Y);
	}
	void Foo (int x)
	{
		int y = ((int)X.Z);
	}
	void Foo (char x)
	{
		int y = ((int)X.W);
	}
}");
		}

		[Test]
		public void TestMethodReferences()
		{
			Test<ConvertToEnumAction>(@"class TestClass
{
	class Nested
	{
		public const int $X_Y = 1;
		public const int X_Z = 2;
	}
	void Method ()
	{
		int x = Nested.X_Y;
	}
}", @"class TestClass
{
	class Nested
	{
		public enum X : int
		{
			Y = 1,
			Z = 2
		}
	}
	void Method ()
	{
		int x = ((int)Nested.X.Y);
	}
}");
		}

		[Test]
		public void TestParams()
		{
			Test<ConvertToEnumAction>(@"class TestClass
{
	class Nested
	{
		public const int $X_Y = 1;
		public const int X_Z = 2;
	}
	void Method (string[] args)
	{
		int x = Nested.X_Z;
	}
}", @"class TestClass
{
	class Nested
	{
		public enum X : int
		{
			Y = 1,
			Z = 2
		}
	}
	void Method (string[] args)
	{
		int x = ((int)Nested.X.Z);
	}
}");
		}
	}
}
