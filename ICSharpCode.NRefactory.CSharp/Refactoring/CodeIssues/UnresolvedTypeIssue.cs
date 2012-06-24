using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("Add namespaces for unresolved types",
	                  Description = "Adds required using statements that are missing from the file.",
	                  Category = IssueCategories.CompilerErrors,
	                  Severity = Severity.Error,
	                  IssueMarker = IssueMarker.Underline)]
	public class UnresolvedTypeIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context).GetIssues();
		}

		private class GatherVisitor : GatherVisitorBase
		{
			public GatherVisitor(BaseRefactoringContext ctx)
				: base(ctx)
			{
			}

			public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
			{
				base.VisitFieldDeclaration(fieldDeclaration);

				this.AddIssueIfUnresolvable(fieldDeclaration.ReturnType);
			}

			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
			{
				base.VisitMethodDeclaration(methodDeclaration);

				this.AddIssueIfUnresolvable(methodDeclaration.ReturnType);
			}

			public override void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
			{
				base.VisitVariableDeclarationStatement(variableDeclarationStatement);

				this.AddIssueIfUnresolvable(variableDeclarationStatement.Type);
			}

			public override void VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
			{
				base.VisitParameterDeclaration(parameterDeclaration);

				this.AddIssueIfUnresolvable(parameterDeclaration.Type);
			}

			public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
			{
				base.VisitTypeDeclaration(typeDeclaration);

				foreach (var node in typeDeclaration.BaseTypes)
				{
					this.AddIssueIfUnresolvable(node);
				}
			}

			public override void VisitCastExpression(CastExpression castExpression)
			{
				base.VisitCastExpression(castExpression);

				this.AddIssueIfUnresolvable(castExpression.Type);
			}

			public override void VisitAsExpression(AsExpression asExpression)
			{
				base.VisitAsExpression(asExpression);

				this.AddIssueIfUnresolvable(asExpression.Type);
			}

			private void AddIssueIfUnresolvable(AstType type)
			{
				var result = ctx.Resolve(type);
				if (result is UnknownIdentifierResolveResult)
				{
					this.AddIssue(type, ctx.TranslateString("Unknown identifier"), s =>  { });
				}

				var simpleType = type as SimpleType;
				if (simpleType != null)
				{
					foreach (var typeArgument in simpleType.TypeArguments)
					{
						this.AddIssueIfUnresolvable(typeArgument);
					}
				}
			}
		}
	}
}

