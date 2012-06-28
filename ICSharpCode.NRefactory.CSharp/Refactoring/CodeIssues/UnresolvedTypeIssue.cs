using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using System.Linq;
using System;

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

			public override void VisitMemberReferenceExpression(MemberReferenceExpression expression)
			{
				base.VisitMemberReferenceExpression(expression);

				this.AddIssueIfUnresolvable(expression.Target);
			}

			private void AddIssueIfUnresolvable(AstNode type)
			{
				var result = ctx.Resolve(type);

				if (result is UnknownIdentifierResolveResult)
				{
					var possibleType = GetPossibleTypes((UnknownIdentifierResolveResult)result).FirstOrDefault();
					this.AddIssue(type, ctx.TranslateString("Unknown identifier"), s =>
					              {
						var usingDeclaration = new UsingDeclaration(possibleType.Namespace);
						var existingUsings = ctx.RootNode.Children.OfType<UsingDeclaration>();

						if (existingUsings.Count() > 0)
						{
							AddAfterExistingUsings(s, existingUsings, usingDeclaration);
						}
						else
						{
							AddAtRoot(s, usingDeclaration);
						}
					});
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

			private void AddAfterExistingUsings(Script s, IEnumerable<UsingDeclaration> existingUsings, UsingDeclaration newUsing)
			{
				var lastUsing = existingUsings.Last();
				var nextNode = lastUsing.NextSibling;

				s.InsertAfter(lastUsing, newUsing);
				this.InsertBlankLines(s, lastUsing, nextNode, s.FormattingOptions.BlankLinesAfterUsings);
			}

			private void AddAtRoot(Script s, UsingDeclaration newUsing)
			{
				var rootNode = ctx.RootNode;

				var addNode = rootNode.FirstChild;
				var prevNode = addNode;
				while (addNode is Comment)
				{
					addNode = addNode.NextSibling;
					prevNode = addNode;
				}

				this.InsertBlankLines(s, prevNode, addNode, s.FormattingOptions.BlankLinesBeforeUsings);
				s.InsertBefore(addNode, newUsing);
				this.InsertBlankLines(s, prevNode, addNode, s.FormattingOptions.BlankLinesAfterUsings);
			}

			private IEnumerable<IType> GetPossibleTypes(UnknownIdentifierResolveResult result)
			{
				return ctx.Compilation.GetAllTypeDefinitions()
					.Where(t => t.Name == result.Identifier &&
					       t.TypeParameterCount == result.TypeArgumentCount);
			}

			private void InsertBlankLines(Script s, AstNode insertAfter, AstNode insertBefore, int numberOfLines)
			{
				int linesToAdd = numberOfLines - GetLinesBetween(insertAfter, insertBefore);
				for (int i = 0; i < linesToAdd; i++)
				{
					s.InsertBefore(insertBefore, new TextNode(s.Options.EolMarker));
				}
			}

			private int GetLinesBetween(AstNode startNode, AstNode endNode)
			{
				if (startNode == endNode)
				{
					return 0;
				}

				return endNode.StartLocation.Line - startNode.EndLocation.Line - 1;
			}
		}
	}
}

