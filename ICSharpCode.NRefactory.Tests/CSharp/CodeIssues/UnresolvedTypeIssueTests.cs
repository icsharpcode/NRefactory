using ICSharpCode.NRefactory.CSharp.CodeIssues;
using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.CodeActions;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	[TestFixture]
	public class UnresolvedTypeIssueTests : InspectionActionTestBase
	{
		#region Field Declarations
		[Test]
		public void ShouldReturnAnIssueForUnresolvedFieldDeclarations()
		{
			this.ShouldNotBeAbleToResolve(@"class Foo
{
	private TextWriter textWriter;

	void Bar ()
	{
		textWriter.WriteLine();
	}
}");
		}

		[Test]
		public void ShouldNotReturnAnyIssuesIfFieldTypeIsResolved()
		{
			this.ShouldBeAbleToResolve(@"using System.IO;

class Foo
{
	private TextWriter textWriter;

	void Bar ()
	{
		textWriter.WriteLine();
	}
}");
		}

		[Test]
		public void ShouldReturnAnIssueIfFieldTypeArgumentIsNotResolvable()
		{
			this.ShouldNotBeAbleToResolve(
@"using System.Collections.Generic;

class Foo
{
	private List<INotifyPropertyChanged> notifiers;
}");
		}

		[Test]
		public void ShouldNotReturnAnIssueIfFieldTypeArgumentIsResolvable()
		{
			this.ShouldBeAbleToResolve(
@"using System.Collections.Generic;

class Foo
{
	private List<string> notifiers;
}");
		}
		#endregion

		#region Method Return Types
		[Test]
		public void ShouldReturnIssueForUnresolvedReturnType()
		{
			this.ShouldNotBeAbleToResolve(
@"class Foo
{
	TextWriter Bar ()
	{
		return null;
	}
}");
		}

		[Test]
		public void ShouldNotReturnIssueForResolvedReturnType()
		{
			this.ShouldBeAbleToResolve(
@"using System.IO;

class Foo
{
	TextWriter Bar ()
	{
		return null;
	}
}");
		}

		[Test]
		public void ShouldReturnIssueForUnresolvedReturnTypeArguments()
		{
			this.ShouldNotBeAbleToResolve(
@"using System.Collections.Generic;
class Foo
{
	List<INotifyPropertyChanged> GetNotifiers()
	{
		return null;
	}
}");
		}

		[Test]
		public void ShouldNotReturnIssueForResolvedReturnTypeArguments()
		{
			this.ShouldBeAbleToResolve(
@"using System.Collections.Generic;
class Foo
{
	List<string> GetStrings()
	{
		return null;
	}
}");
		}
		#endregion

		#region Local Variables
		[Test]
		public void ShouldReturnIssueForUnresolvedLocalVariableDeclaration()
		{
			this.ShouldNotBeAbleToResolve(
@"class Foo
{
	void Bar ()
	{
		TextWriter writer;
	}
}");
		}

		[Test]
		public void ShouldNotReturnIssueForResolvedLocalVariableDeclaration()
		{
			this.ShouldBeAbleToResolve(
@"using System.IO;

class Foo
{
	void Bar ()
	{
		TextWriter writer;
	}
}");
		}

		[Test]
		public void ShouldReturnIssueForUnresolvedLocalVariableTypeArguments()
		{
			this.ShouldNotBeAbleToResolve(
@"using System.Collections.Generic;

class Foo
{
	void Bar ()
	{
		List<INotifyPropertyChanged> notifiers;
	}
}");
		}
		#endregion

		#region Method Parameters
		[Test]
		public void ShouldReturnIssueIfMethodParameterIsNotResolvable()
		{
			this.ShouldNotBeAbleToResolve(
@"class Foo
{
	void Bar (TextWriter writer)
	{
	}
}"
			);
		}

		[Test]
		public void ShouldNotReturnAnIssueIfMethodParameterIsResolvable()
		{
			this.ShouldBeAbleToResolve(
@"using System.IO;

class Foo
{
	void Bar (TextWriter writer)
	{
	}
}");
		}

		[Test]
		public void ShouldReturnIssueIfMethodParameterTypeArgumentIsNotResolvable()
		{
			this.ShouldNotBeAbleToResolve(
@"using System.Collections.Generic;
class Foo
{
	void Bar (List<INotifyPropertyChanged> notifiers)
	{
	}
}");
		}

		[Test]
		public void ShouldNotReturnIssueIfMethodParameterTypeArgumentsAreResolvable()
		{
			this.ShouldBeAbleToResolve(
@"using System.Collections.Generic;
class Foo
{
	void Bar (List<string> strings)
	{
	}
}");
		}
		#endregion

		#region Base Types
		[Test]
		public void ShouldReturnIssueIfBaseClassIsNotResolvable()
		{
			this.ShouldNotBeAbleToResolve(
@"class Foo : List<string>
{
}");
		}

		[Test]
		public void ShouldNotReturnIssueIfBaseClassIsResolvable()
		{
			this.ShouldBeAbleToResolve(
@"using System.Collections.Generic;

class Foo : List<string>
{
}");
		}

		[Test]
		public void ShouldReturnIssueIfBaseClassTypeArgumentIsNotResolvable()
		{
			this.ShouldNotBeAbleToResolve(
@"using System.Collections.Generic;
class Foo : List<INotifyPropertyChanged>
{
}");
		}
		#endregion

		#region Type Casting
		[Test]
		public void ShouldReturnIssueIfTypeCastIsNotResolvable()
		{
			this.ShouldNotBeAbleToResolve(
@"class Foo
{
	void Bar (object bigObject)
	{
		var notifier = (INotifyPropertyChanged)bigObject;
	}
}");
		}

		[Test]
		public void ShouldNotReturnIssueIfTypeCastIsResolvable()
		{
			this.ShouldBeAbleToResolve(
@"class Foo
{
	void Bar (object bigObject)
	{
		var notifier = (string)bigObject;
	}
}");
		}

		[Test]
		public void ShouldReturnIssueIfAsExpressionIsNotResolvable()
		{
			this.ShouldNotBeAbleToResolve(
@"class Foo
{
	void Bar (object bigObject)
	{
		var notifier = bigObject as INotifyPropertyChanged;
	}
}");
		}

		[Test]
		public void ShouldNotReturnIssueIfTypeInAsExpressionIsResolvable()
		{
			this.ShouldBeAbleToResolve(
@"class Foo
{
	void Bar (object bigObject)
	{
		var str = bigObject as string;
	}
}");
		}
		#endregion

		private void ShouldNotBeAbleToResolve(string testInput)
		{
			// Arrange
			TestRefactoringContext context;

			// Act
			var issue = GetIssues(new UnresolvedTypeIssue(), testInput, out context).Single();

			// Assert
			Assert.AreEqual("Unknown identifier", issue.Description);
		}

		private void ShouldBeAbleToResolve(string testInput)
		{
			// Arrange
			TestRefactoringContext context;

			// Act
			var issues = GetIssues(new UnresolvedTypeIssue(), testInput, out context);

			// Assert
			Assert.IsEmpty(issues);
		}
	}
}
