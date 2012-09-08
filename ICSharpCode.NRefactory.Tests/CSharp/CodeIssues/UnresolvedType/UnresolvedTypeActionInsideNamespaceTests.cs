using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.CodeIssues;
using ICSharpCode.NRefactory.CSharp.CodeActions;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues.UnresolvedType
{
	[TestFixture]
	public class UnresolvedTypeActionInsideNamespaceTests : InspectionActionTestBase
	{
		[Test]
		public void ShouldInsertUsingStatement()
		{
			TestRefactoringContext context;
			string testCode =
@"namespace TestNamespace
{
	class TestClass
	{
		private List<string> stringList;
	}
}";

			string expectedOutput = 
@"namespace TestNamespace
{
	using System.Collections.Generic;
	class TestClass
	{
		private List<string> stringList;
	}
}";

			var issue = GetIssues(new UnresolvedTypeIssue(), testCode, out context).Single();

			CheckFix(
				context,
				issue,
				expectedOutput,
				s =>
				{
				s.FormattingOptions.UsingPlacement = UsingPlacement.InsideNamespace;
				s.FormattingOptions.BlankLinesAfterUsings = 0;
			});
		}

		[Test]
		public void ShouldAddBlankLinesBeforeUsingStatement()
		{
			TestRefactoringContext context;
			string testCode =
@"namespace TestNamespace
{
	class TestClass
	{
		private List<string> stringList;
	}
}";

			string expectedOutput = 
@"namespace TestNamespace
{


	using System.Collections.Generic;
	class TestClass
	{
		private List<string> stringList;
	}
}";

			var issue = GetIssues(new UnresolvedTypeIssue(), testCode, out context).Single();

			CheckFix(
				context,
				issue,
				expectedOutput,
				s =>
				{
				s.FormattingOptions.UsingPlacement = UsingPlacement.InsideNamespace;
				s.FormattingOptions.BlankLinesBeforeUsings = 2;
				s.FormattingOptions.BlankLinesAfterUsings = 0;
			});
		}

		[Test]
		public void ShouldAddBlankLinesAfterUsingStatements()
		{
			TestRefactoringContext context;
			string testCode =
@"namespace TestNamespace
{
	class TestClass
	{
		private List<string> stringList;
	}
}";

			string expectedOutput = 
@"namespace TestNamespace
{
	using System.Collections.Generic;


	class TestClass
	{
		private List<string> stringList;
	}
}";

			var issue = GetIssues(new UnresolvedTypeIssue(), testCode, out context).Single();

			CheckFix(
				context,
				issue,
				expectedOutput,
				s =>
				{
				s.FormattingOptions.UsingPlacement = UsingPlacement.InsideNamespace;
				s.FormattingOptions.BlankLinesAfterUsings = 2;
			});
		}

		[Test]
		public void ShouldAddUsingAfterExistingUsings()
		{
			TestRefactoringContext context;
			string testCode =
@"namespace TestNamespace
{
	using System;

	class TestClass
	{
		private List<string> stringList;
	}
}";

			string expectedOutput = 
@"namespace TestNamespace
{
	using System;
	using System.Collections.Generic;

	class TestClass
	{
		private List<string> stringList;
	}
}";

			var issue = GetIssues(new UnresolvedTypeIssue(), testCode, out context).Single();

			CheckFix(
				context,
				issue,
				expectedOutput,
				s =>
				{
				s.FormattingOptions.UsingPlacement = UsingPlacement.InsideNamespace;
			});
		}

		[Test]
		public void ShouldAddUsingInMostNestedNamespace()
		{
			TestRefactoringContext context;
			string testCode =
@"namespace OuterNamespace
{
	namespace InnerNamespace
	{
		class TestClass
		{
			private List<string> stringList;
		}
	}
}";

			string expectedOutput = 
@"namespace OuterNamespace
{
	namespace InnerNamespace
	{
		using System.Collections.Generic;

		class TestClass
		{
			private List<string> stringList;
		}
	}
}";

			var issue = GetIssues(new UnresolvedTypeIssue(), testCode, out context).Single();

			CheckFix(
				context,
				issue,
				expectedOutput,
				s =>
				{
				s.FormattingOptions.UsingPlacement = UsingPlacement.InsideNamespace;
			});
		}

		[Test]
		public void ShouldAddUsingAfterExistingUsingsInMostNestedNamespace()
		{
			TestRefactoringContext context;
			string testCode =
@"namespace OuterNamespace
{
	namespace InnerNamespace
	{
		using System;

		class TestClass
		{
			private List<string> stringList;
		}
	}
}";

			string expectedOutput = 
@"namespace OuterNamespace
{
	namespace InnerNamespace
	{
		using System;
		using System.Collections.Generic;

		class TestClass
		{
			private List<string> stringList;
		}
	}
}";

			var issue = GetIssues(new UnresolvedTypeIssue(), testCode, out context).Single();

			CheckFix(
				context,
				issue,
				expectedOutput,
				s =>
				{
				s.FormattingOptions.UsingPlacement = UsingPlacement.InsideNamespace;
			});
		}
	}
}

