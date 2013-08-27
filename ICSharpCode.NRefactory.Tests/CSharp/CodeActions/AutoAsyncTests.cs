// 
// AutoAsyncTests.cs
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
	public class AutoAsyncTests : ContextActionTestBase
	{
		[Test]
		public void TestSimple() {
			Test<AutoAsyncAction>(@"
using System.Threading.Tasks;
class TestClass
{
	public Task<int> $TestMethod ()
	{
		var tcs = new TaskCompletionSource<int> ();
		tcs.SetResult (1);
		return tcs.Task;
	}
}", @"
using System.Threading.Tasks;
class TestClass
{
	public async Task<int> TestMethod ()
	{
		return 1;
	}
}");
		}

		[Test]
		public void TestTaskWithoutResult() {
			Test<AutoAsyncAction>(@"
using System.Threading.Tasks;
class TestClass
{
	public Task $TestMethod ()
	{
		var tcs = new TaskCompletionSource<int> ();
		tcs.SetResult (1);
		return tcs.Task;
	}
}", @"
using System.Threading.Tasks;
class TestClass
{
	public async Task TestMethod ()
	{
		int result = 1;
		return;
	}
}");
		}

		[Test]
		public void TestBasicContinueWith() {
			Test<AutoAsyncAction>(@"
using System.Threading.Tasks;
class TestClass
{
	public Task<int> Foo ()
	{
		return null;
	}
	public Task<int> $TestMethod ()
	{
		var tcs = new TaskCompletionSource<int> ();
		Foo ().ContinueWith (precedent => {
			tcs.SetResult (precedent.Result);
		});
		return tcs.Task;
	}
}", @"
using System.Threading.Tasks;
class TestClass
{
	public Task<int> Foo ()
	{
		return null;
	}
	public async Task<int> TestMethod ()
	{
		int precedentResult = await Foo ();
		return precedentResult;
	}
}");
		}

		[Test]
		public void TestDelegate() {
			Test<AutoAsyncAction>(@"
using System.Threading.Tasks;
class TestClass
{
	public Task<int> Foo ()
	{
		return null;
	}
	public Task<int> $TestMethod ()
	{
		var tcs = new TaskCompletionSource<int> ();
		Foo ().ContinueWith (delegate {
			tcs.SetResult (42);
		});
		return tcs.Task;
	}
}", @"
using System.Threading.Tasks;
class TestClass
{
	public Task<int> Foo ()
	{
		return null;
	}
	public async Task<int> TestMethod ()
	{
		int taskResult = await Foo ();
		return 42;
	}
}");
		}

		[Test]
		public void TestTwoContinueWith() {
			Test<AutoAsyncAction>(@"
using System.Threading.Tasks;
class TestClass
{
	public Task<int> Foo ()
	{
		return null;
	}
	public Task<int> $TestMethod ()
	{
		var tcs = new TaskCompletionSource<int> ();
		Foo ().ContinueWith (precedent => {
			Foo ().ContinueWith (precedent2 => {
				tcs.SetResult (precedent.Result + precedent2.Result);
			});
		});
		return tcs.Task;
	}
}", @"
using System.Threading.Tasks;
class TestClass
{
	public Task<int> Foo ()
	{
		return null;
	}
	public async Task<int> TestMethod ()
	{
		int precedentResult = await Foo ();
		int precedent2Result = await Foo ();
		return precedentResult + precedent2Result;
	}
}");
		}

		[Test]
		public void TestChainedContinueWith() {
			Test<AutoAsyncAction>(@"
using System.Threading.Tasks;
class TestClass
{
	public Task<int> Foo ()
	{
		return null;
	}
	public Task<int> $TestMethod ()
	{
		var tcs = new TaskCompletionSource<int> ();
		Foo ().ContinueWith (precedent => {
			Foo ();
		}).ContinueWith (precedent => {
			tcs.SetResult (1);
		});
		return tcs.Task;
	}
}", @"
using System.Threading.Tasks;
class TestClass
{
	public Task<int> Foo ()
	{
		return null;
	}
	public async Task<int> TestMethod ()
	{
		int precedentResult = await Foo ();
		Foo ();
		return 1;
	}
}");
		}

		[Test]
		public void TestRedundantReturn() {
			Test<AutoAsyncAction>(@"
using System.Threading.Tasks;
class TestClass
{
	public Task $TestMethod ()
	{
		var tcs = new TaskCompletionSource<object> ();
		tcs.SetResult (null);
		return tcs.Task;
	}
}", @"
using System.Threading.Tasks;
class TestClass
{
	public async Task TestMethod ()
	{
		object result = null;
		return;
	}
}");
		}

		[Test]
		public void TestDisabledForBadReturn() {
			TestWrongContext<AutoAsyncAction>(@"
using System.Threading.Tasks;
class TestClass
{
	public Task $TestMethod ()
	{
		var tcs = new TaskCompletionSource<object> ();
		tcs.SetResult (null);
		return null;
	}
}");
		}
	}
}

