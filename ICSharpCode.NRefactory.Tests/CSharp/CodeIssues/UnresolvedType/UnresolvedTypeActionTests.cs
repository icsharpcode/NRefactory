using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.CodeIssues;
using ICSharpCode.NRefactory.CSharp.CodeActions;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues.UnresolvedType
{
	[TestFixture]
	public class UnresolvedTypeActionTests : InspectionActionTestBase
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
@"using System.Collections.Generic;

namespace TestNamespace
{
	class TestClass
	{
		private List<string> stringList;
	}
}";

			var issue = GetIssues(new UnresolvedTypeIssue(), testCode, out context).Single();

			CheckFix(context, issue, expectedOutput);
		}

		[Test]
		public void ShouldAddBlankLinesAfterUsings()
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
@"using System.Collections.Generic;


namespace TestNamespace
{
	class TestClass
	{
		private List<string> stringList;
	}
}";

			var issue = GetIssues(new UnresolvedTypeIssue(), testCode, out context).Single();

			CheckFix(context, issue, expectedOutput, s => s.FormattingOptions.BlankLinesAfterUsings = 2);
		}

		[Test]
		public void ShouldAddBlankLinesBeforeUsing()
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
@"

using System.Collections.Generic;

namespace TestNamespace
{
	class TestClass
	{
		private List<string> stringList;
	}
}";

			var issue = GetIssues(new UnresolvedTypeIssue(), testCode, out context).Single();

			CheckFix(context, issue, expectedOutput, s => s.FormattingOptions.BlankLinesBeforeUsings = 2);
		}

		[Test]
		public void ShouldAddAfterExistingUsingStatements()
		{
			TestRefactoringContext context;
			string testCode =
@"using System;
namespace TestNamespace
{
	class TestClass
	{
		private List<string> stringList;
	}
}";

			string expectedOutput = 
@"using System;
using System.Collections.Generic;

namespace TestNamespace
{
	class TestClass
	{
		private List<string> stringList;
	}
}";

			var issue = GetIssues(new UnresolvedTypeIssue(), testCode, out context).Single();

			CheckFix(context, issue, expectedOutput);
		}

		[Test]
		public void ShouldNotAddBlankLinesAfterIfTheyAreAlreadyThere()
		{
			TestRefactoringContext context;
			string testCode =
@"using System;

namespace TestNamespace
{
	class TestClass
	{
		private List<string> stringList;
	}
}";

			string expectedOutput = 
@"using System;
using System.Collections.Generic;

namespace TestNamespace
{
	class TestClass
	{
		private List<string> stringList;
	}
}";

			var issue = GetIssues(new UnresolvedTypeIssue(), testCode, out context).Single();

			CheckFix(context, issue, expectedOutput);
		}

		[Test]
		public void ShouldLeaveAdditionalBlankLinesThatAlreadyExist()
		{
			TestRefactoringContext context;
			string testCode =
@"using System;


namespace TestNamespace
{
	class TestClass
	{
		private List<string> stringList;
	}
}";

			string expectedOutput = 
@"using System;
using System.Collections.Generic;


namespace TestNamespace
{
	class TestClass
	{
		private List<string> stringList;
	}
}";

			var issue = GetIssues(new UnresolvedTypeIssue(), testCode, out context).Single();

			CheckFix(context, issue, expectedOutput);
		}

		[Test]
		public void ShouldAddFirstUsingAfterComments()
		{
			TestRefactoringContext context;
			string testCode =
@"// This is the file header.
// It contains any copyright information.
namespace TestNamespace
{
	class TestClass
	{
		private List<string> stringList;
	}
}";

			string expectedOutput = 
@"// This is the file header.
// It contains any copyright information.
using System.Collections.Generic;

namespace TestNamespace
{
	class TestClass
	{
		private List<string> stringList;
	}
}";

			var issue = GetIssues(new UnresolvedTypeIssue(), testCode, out context).Single();

			CheckFix(context, issue, expectedOutput);
		}
	}
}

