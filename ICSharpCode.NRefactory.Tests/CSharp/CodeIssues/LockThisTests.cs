// LockThisTests.cs
// 
// Author:
//      Luís Reis <luiscubal@gmail.com>
// 
// Copyright (c) 2013 Luís Reis <luiscubal@gmail.com>
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

using ICSharpCode.NRefactory.CSharp.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	[TestFixture]
	public class LockThisTests : InspectionActionTestBase
	{
		[Test]
		public void TestLockThisInMethod ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		lock (this) {
		}
	}
}";

			Test<LockThisIssue> (input, 1);
		}

		[Test]
		public void TestLockThisInGetter ()
		{
			var input = @"
class TestClass
{
	int MyProperty {
		get {
			lock (this) {
				return 0;
			}
		}
	}
}";

			Test<LockThisIssue> (input, 1);
		}

		[Test]
		public void TestLockThisInSetter ()
		{
			var input = @"
class TestClass
{
	int MyProperty {
		set {
			lock (this) {
			}
		}
	}
}";

			Test<LockThisIssue> (input, 1);
		}

		[Test]
		public void TestLockThisInConstructor ()
		{
			var input = @"
class TestClass
{
	TestClass()
	{
		lock (this) {
		}
	}
}";

			Test<LockThisIssue> (input, 1);
		}

		[Test]
		public void TestLockThisInDelegate ()
		{
			var input = @"
class TestClass
{
	TestClass()
	{
		Action lockThis = delegate ()
		{
			lock (this) {
			}
		};
	}
}";

			Test<LockThisIssue> (input, 1);
		}

		[Test]
		public void TestLockThisInLambda ()
		{
			var input = @"
class TestClass
{
	TestClass()
	{
		Action lockThis = () =>
		{
			lock (this) {
			}
		};
	}
}";

			Test<LockThisIssue> (input, 1);
		}

		[Test]
		public void TestLockParenthesizedThis ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		lock ((this)) {
		}
	}
}";

			Test<LockThisIssue> (input, 1);
		}

		[Test]
		public void TestLockNonThis ()
		{
			var input = @"
class TestClass
{
	object locker = new object();

	TestClass()
	{
		lock (locker) {
		}
	}
}";

			Test<LockThisIssue> (input, 0);
		}
	}
}
