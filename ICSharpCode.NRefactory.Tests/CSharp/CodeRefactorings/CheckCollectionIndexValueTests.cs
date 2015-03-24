//
// CheckCollectionIndexValueTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[TestFixture]
	public class CheckCollectionIndexValueTests : ContextActionTestBase
	{
		[Test]
		public void TestSimpleCase ()
		{
			
			Test<CheckCollectionIndexValueCodeRefactoringProvider> (@"
using System.Collections.Generic;
class Test
{
    public static void Main (List<int> args)
    {
        Console.WriteLine(args[$5]);
    }
}", @"
using System.Collections.Generic;
class Test
{
    public static void Main (List<int> args)
    {
        if (args.Count > 5)
            Console.WriteLine(args[5]);
    }
}");
		}

		[Test]
		public void TestNestedCase ()
		{
			Test<CheckCollectionIndexValueCodeRefactoringProvider> (@"
using System.Collections.Generic;
class Test
{
    public static void Main (List<int> args)
    {
        if (true)
            if (true)
                Console.WriteLine(args[$5 + 234 - 234]);
    }
}", @"
using System.Collections.Generic;
class Test
{
    public static void Main (List<int> args)
    {
        if (true)
            if (true)
                if (args.Count > 5 + 234 - 234)
                    Console.WriteLine(args[5 + 234 - 234]);
    }
}");
		}
	}
}