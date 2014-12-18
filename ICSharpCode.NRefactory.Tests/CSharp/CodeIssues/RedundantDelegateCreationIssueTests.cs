//
// RedundantDelegateCreationIssueTests.cs
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
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory6.CSharp.CodeIssues
{
	[TestFixture]
	[Ignore("TODO: Issue not ported yet")]
	public class RedundantDelegateCreationIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TestAdd ()
		{
			Test<RedundantDelegateCreationIssue>(@"
using System;

public class FooBase
{
	public event EventHandler<EventArgs> Changed;

	FooBase()
	{
		Changed += new EventHandler<EventArgs>(HandleChanged);
	}

	void HandleChanged(object sender, EventArgs e)
	{
	}
}", @"
using System;

public class FooBase
{
	public event EventHandler<EventArgs> Changed;

	FooBase()
	{
		Changed += HandleChanged;
	}

	void HandleChanged(object sender, EventArgs e)
	{
	}
}");
		}

		[Test]
		public void TestRemove ()
		{
			Test<RedundantDelegateCreationIssue>(@"
using System;

public class FooBase
{
	public event EventHandler<EventArgs> Changed;

	FooBase()
	{
		Changed -= new EventHandler<EventArgs>(HandleChanged);
	}

	void HandleChanged(object sender, EventArgs e)
	{
	}
}", @"
using System;

public class FooBase
{
	public event EventHandler<EventArgs> Changed;

	FooBase()
	{
		Changed -= HandleChanged;
	}

	void HandleChanged(object sender, EventArgs e)
	{
	}
}");
		}

		[Test]
		public void TestDisable ()
		{
			Analyze<RedundantDelegateCreationIssue>(@"
using System;

public class FooBase
{
	public event EventHandler<EventArgs> Changed;

	FooBase()
	{
		// ReSharper disable once RedundantDelegateCreation
		Changed += new EventHandler<EventArgs>(HandleChanged);
	}

	void HandleChanged(object sender, EventArgs e)
	{
	}
}");
		}
	}
}

