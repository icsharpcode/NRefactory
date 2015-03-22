//
// SealedMemberInSealedClassTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
	public class SealedMemberInSealedClassTests : InspectionActionTestBase
	{
		[Test]
		public void TestBasicCase()
		{
			Analyze<SealedMemberInSealedClassAnalyzer>(@"
sealed class Foo
{
	public sealed override string $ToString$()
	{
		return ""''"";
	}
}
", @"
sealed class Foo
{
	public override string ToString()
	{
		return ""''"";
	}
}
");
		}

		[Test]
		public void TestFieldDeclaration()
		{
			Analyze<SealedMemberInSealedClassAnalyzer>(@"
public sealed class Foo
{
	private int field;
}
");
		}

		[Test]
		public void TestValid()
		{
			Analyze<SealedMemberInSealedClassAnalyzer>(@"
class Foo
{
	public sealed override string ToString()
	{
		return ""''"";
	}
}
");
		}


		[Test]
		public void TestDisable()
		{
			Analyze<SealedMemberInSealedClassAnalyzer>(@"
sealed class Foo
{
	// ReSharper disable once SealedMemberInSealedClass
	public sealed override string ToString()
	{
		return ""''"";
	}
}
");
		}
	}
}

