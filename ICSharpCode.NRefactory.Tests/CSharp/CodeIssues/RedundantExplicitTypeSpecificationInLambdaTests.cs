// 
// RedundantExplicitTypeSpecificationInLambdaTests.cs
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
using ICSharpCode.NRefactory.CSharp.CodeActions;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	[TestFixture]
	public class RedundantExplicitTypeSpecificationInLambdaTests : InspectionActionTestBase
	{

		[Test]
		public void TestInspectorCase1()
		{
			var input = @"using System;
        using System.Collections.Generic;
        using System.Linq;

namespace application
{
    internal class Program
    {
        public delegate int IncreaseByANumber(int j);

        public delegate int MultipleIncreaseByANumber(int i, int j, int l);

        public static void ExecuteCSharp3_0()
        {
            // declare the lambda expression
            IncreaseByANumber increase = (int j) => (j * 42);
            // invoke the method and print 420 to the console
            Console.WriteLine(increase(10));

            MultipleIncreaseByANumber multiple = (int j, int k, int l) => ((j * 42) / k) % l;
            Console.WriteLine(multiple(10, 11, 12));
        }
    }
}";

			TestRefactoringContext context;
			var issues = GetIssues(new RedundantExplicitTypeSpecificationInLambdaIssue(), input, out context);
			Assert.AreEqual(4, issues.Count);
			CheckFix(context, issues, @"using System;
        using System.Collections.Generic;
        using System.Linq;

namespace application
{
    internal class Program
    {
        public delegate int IncreaseByANumber(int j);

        public delegate int MultipleIncreaseByANumber(int i, int j, int l);

        public static void ExecuteCSharp3_0()
        {
            // declare the lambda expression
            IncreaseByANumber increase =  j => (j * 42);
            // invoke the method and print 420 to the console
            Console.WriteLine(increase(10));

            MultipleIncreaseByANumber multiple = ( j,  k,  l) => ((j * 42) / k) % l;
            Console.WriteLine(multiple(10, 11, 12));
        }
    }
}");
		}

		[Test]
		public void TestResharperDisableRestore()
		{
			var input = @"using System;
        using System.Collections.Generic;
        using System.Linq;

namespace application
{
    internal class Program
    {
        public delegate int IncreaseByANumber(int j);

        public delegate int MultipleIncreaseByANumber(int i, int j, int l);

        public static void ExecuteCSharp3_0()
        {
            // declare the lambda expression
			//Resharper disable RedundantExplicitTypeSpecificationInLambda
            IncreaseByANumber increase = (int j) => (j * 42);
			//Resharper restore RedundantExplicitTypeSpecificationInLambda
            // invoke the method and print 420 to the console
            Console.WriteLine(increase(10));

            MultipleIncreaseByANumber multiple = (j, k, l) => ((j * 42) / k) % l;
            Console.WriteLine(multiple(10, 11, 12));
        }
    }
}";

			TestRefactoringContext context;
			var issues = GetIssues(new RedundantExplicitTypeSpecificationInLambdaIssue(), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
	}
}