// 
// RedundantExplicitSizeSpeicificationInArrayCreationTests.cs
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
	public class RedundantExplicitSizeSpeicificationInArrayCreationTests : InspectionActionTestBase
	{

		[Test]
		public void TestInspectorCase1()
		{
			var input = @"using System;
	namespace Application
	{
		public class BaseClass
		{
			public void method()
			{
				int[] array = new int[5]{1,2,3,4,5};
			}
		}
	}
";

			TestRefactoringContext context;
			var issues = GetIssues(new RedundantSizeSpecificationInArrayCreationIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			CheckFix(context, issues, @"using System;
	namespace Application
	{
		public class BaseClass
		{
			public void method()
			{
				int[] array = new int[]{1,2,3,4,5};
			}
		}
	}
");
		}

		[Test]
		public void TestInspectorCase2()
		{
			var input = @"using System;
	namespace Application
	{
		public class BaseClass
		{
			public void method()
			{
				int[] array = new int[]{1,2,3,4,5};
				int[] array1= new int[4]{1,2,3,4,5};
			}
		}
	}
";
			TestRefactoringContext context;
			var issues = GetIssues(new RedundantSizeSpecificationInArrayCreationIssue(), input, out context);
			Assert.AreEqual(0, issues.Count);
		}

		[Test]
		public void TestResharperDisableRestore()
		{
			var input = @"using System;
	namespace Application
	{
		public class BaseClass
		{
			public void method()
			{
//Resharper disable RedundantSizeSpecificationInCreatingArray
				int[] array = new int[5]{1,2,3,4,5};
//Resharper restore RedundantSizeSpecificationInCreatingArray
			}
		}
	}
";

			TestRefactoringContext context;
			var issues = GetIssues(new RedundantSizeSpecificationInArrayCreationIssue(), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
	}
}