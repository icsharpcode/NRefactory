// 
// MetaTests.cs
// 
// Author:
//      Luís Reis <luiscubal@gmail.com>
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

using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.CodeActions;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	/// <summary>
	/// Tests for the test APIs.
	/// </summary>
	[TestFixture]
	public class MetaTests
	{
		[Test]
		public void TestGlobalOperation ()
		{
			List<string> contents = new List<string>() {
				@"class Test1
{
	public int $x;
}", @"class Test2
{
	public void Foo(Test1 test1)
	{
		test1.x = 1;
		test1.x = 2;
	}
}"
			};
			var context = TestRefactoringContext.Create(contents, 0);
			using (var script = context.StartScript()) {
				var variable = context.GetNode<VariableInitializer>();
				script.DoGlobalOperationOn(new List<IEntity>() {
					((MemberResolveResult) context.Resolve(variable)).Member
				}, (rCtx, rScript, nodes) => {
					foreach (var node in nodes) {
						rScript.Replace(node, new IdentifierExpression("replacement"));
					}
				});
			}

			Assert.AreEqual(@"class Test1
{
	public int replacement;
}", context.GetSideDocumentText(0));

			Assert.AreEqual(@"class Test2
{
	public void Foo(Test1 test1)
	{
		replacement = 1;
		replacement = 2;
	}
}", context.GetSideDocumentText(1));
		}

		[Test]
		public void TestRename ()
		{
			List<string> contents = new List<string>() {
				@"class Test1
{
	public int $x;
}", @"class Test2
{
	public void Foo(Test1 test1)
	{
		test1.x = 1;
		test1.x = 2;
	}
}"
			};
			var context = TestRefactoringContext.Create(contents, 0);
			using (var script = context.StartScript()) {
				var variable = context.GetNode<VariableInitializer>();
				script.Rename(((MemberResolveResult)context.Resolve(variable)).Member, "newName");
			}

			Assert.AreEqual(@"class Test1
{
	public int newName;
}", context.GetSideDocumentText(0));

			Assert.AreEqual(@"class Test2
{
	public void Foo(Test1 test1)
	{
		test1.newName = 1;
		test1.newName = 2;
	}
}", context.GetSideDocumentText(1));
		}
	}
}

