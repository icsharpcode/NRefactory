using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.CodeIssues;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.CSharp.CodeActions;
using ICSharpCode.NRefactory.CSharp;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues.UnresolvedType
{
	[TestFixture]
	public class UnresolvedTypeActionAlphabeticalTests : InspectionActionTestBase
	{
		[Test]
		public void ShouldAddUsingAtStartIfItIsTheFirstAlphabetically()
		{
			TestRefactoringContext context;
			string testCode =
@"namespace OuterNamespace
{
	using System.IO;

	class TestClass
	{
		private List<TextWriter> writerList;
	}
}";

			string expectedOutput = 
@"namespace OuterNamespace
{
	using System.Collections.Generic;
	using System.IO;

	class TestClass
	{
		private List<TextWriter> writerList;
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
				s.FormattingOptions.SortUsingsAlphabetically = true;
			});
		}
		
		[Test]
		public void ShouldInsertUsingBetweenExistingUsings()
		{
			TestRefactoringContext context;
			string testCode =
@"namespace OuterNamespace
{
	using System;
	using System.IO;

	class TestClass
	{
		private List<TextWriter> writerList;
	}
}";

			string expectedOutput = 
@"namespace OuterNamespace
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	class TestClass
	{
		private List<TextWriter> writerList;
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
				s.FormattingOptions.SortUsingsAlphabetically = true;
			});
		}
		
		[Test]
		public void ShouldInsertUsingAfterExistingUsings()
		{
			TestRefactoringContext context;
			string testCode =
@"namespace OuterNamespace
{
	using System;
	using System.Collections.Generic;

	class TestClass
	{
		private List<TextWriter> writerList;
	}
}";

			string expectedOutput = 
@"namespace OuterNamespace
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	class TestClass
	{
		private List<TextWriter> writerList;
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
				s.FormattingOptions.SortUsingsAlphabetically = true;
			});
		}
		
		[Test]
		public void ShouldAddBlankLinesAfterUsingsWhenAddingAtEnd()
		{
			TestRefactoringContext context;
			string testCode =
@"namespace OuterNamespace
{
	using System;
	using System.Collections.Generic;
	class TestClass
	{
		private List<TextWriter> writerList;
	}
}";

			string expectedOutput = 
@"namespace OuterNamespace
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	class TestClass
	{
		private List<TextWriter> writerList;
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
				s.FormattingOptions.SortUsingsAlphabetically = true;
				s.FormattingOptions.BlankLinesAfterUsings = 1;
			});
		}
	}
}

