// 
// ConvertToNewLineTests.cs
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
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	[TestFixture]
	public class CovertToNewLineTests : ContextActionTestBase
	{

		[Test]
		public void TestSimpleString()
		{
			Test<ConvertToNewLineAction>(@"class TestClass
			{
				void Test ()
				{
					string str = $""\n""; 
				} 
			}",
			@"class TestClass
			{
				void Test ()
				{
					string str = System.Environment.NewLine; 
				} 
			}"
			);
		}

		[Test]
		public void TestSimpleString2()
		{
			Test<ConvertToNewLineAction>(@"class TestClass
			{
				void Test ()
				{
					string str = $""\r""; 
				} 
			}",
			@"class TestClass
			{
				void Test ()
				{
					string str = System.Environment.NewLine; 
				} 
			}"
			);
		}

		[Test]
		public void TestSimpleString3()
		{
			Test<ConvertToNewLineAction>(@"class TestClass
			{
				void Test ()
				{
					string str = $""\r\n""; 
				} 
			}",
			@"class TestClass
			{
				void Test ()
				{
					string str = System.Environment.NewLine; 
				} 
			}"
			);
		}
	}
}