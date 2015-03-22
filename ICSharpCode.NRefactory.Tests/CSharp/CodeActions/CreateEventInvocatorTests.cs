// 
// CreateEventInvocatorTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[TestFixture]
	public class CreateEventInvocatorTests : ContextActionTestBase
	{
		[Test]
		public void TestSimpleCase ()
		{
			Test<CreateEventInvocatorCodeRefactoringProvider> (@"using System;
class TestClass
{
    public event EventHandler $Tested;
}", @"using System;
class TestClass
{
    protected virtual void OnTested(EventArgs e)
    {
        Tested?.Invoke(this, e);
    }

    public event EventHandler Tested;
}");
		}

		[Test]
		public void Test_CSharp5_SimpleCase ()
		{
			var parseOptions = new CSharpParseOptions (
				                            LanguageVersion.CSharp5,
				                            DocumentationMode.Diagnose | DocumentationMode.Parse,
				                            SourceCodeKind.Regular,
				                            ImmutableArray.Create ("DEBUG", "TEST")
			                            );
			Test<CreateEventInvocatorCodeRefactoringProvider> (@"using System;
class TestClass
{
    public event EventHandler $Tested;
}", @"using System;
class TestClass
{
    protected virtual void OnTested(EventArgs e)
    {
        var handler = Tested;
        if (handler != null)
            handler(this, e);
    }

    public event EventHandler Tested;
}", parseOptions: parseOptions);
		}

		[Test]
		public void Test_CSharp5_NameClash ()
		{
			var parseOptions = new CSharpParseOptions (
				                            LanguageVersion.CSharp5,
				                            DocumentationMode.Diagnose | DocumentationMode.Parse,
				                            SourceCodeKind.Regular,
				                            ImmutableArray.Create ("DEBUG", "TEST")
			                            );
			Test<CreateEventInvocatorCodeRefactoringProvider> (@"using System;
class TestClass
{
    public event EventHandler $e;
}", @"using System;
class TestClass
{
    protected virtual void OnE(EventArgs e)
    {
        var handler = this.e;
        if (handler != null)
            handler(this, e);
    }

    public event EventHandler e;
}", parseOptions: parseOptions);
		}

		[Test]
		public void TestNameClash ()
		{
			Test<CreateEventInvocatorCodeRefactoringProvider> (@"using System;
class TestClass
{
    public event EventHandler $e;
}", @"using System;
class TestClass
{
    protected virtual void OnE(EventArgs e)
    {
        this.e?.Invoke(this, e);
    }

    public event EventHandler e;
}");
		}

		[Test]
		public void TestStaticEvent ()
		{
			Test<CreateEventInvocatorCodeRefactoringProvider> (@"using System;
class TestClass
{
    public static event EventHandler $Tested;
}", @"using System;
class TestClass
{
    static void OnTested(EventArgs e)
    {
        Tested?.Invoke(null, e);
    }

    public static event EventHandler Tested;
}");
		}


		[Test]
		public void TestStaticNameClash ()
		{
			Test<CreateEventInvocatorCodeRefactoringProvider> (@"using System;
class TestClass
{
    public static event EventHandler $e;
}", @"using System;
class TestClass
{
    static void OnE(EventArgs e)
    {
        TestClass.e?.Invoke(null, e);
    }

    public static event EventHandler e;
}");
		}

	}
}

