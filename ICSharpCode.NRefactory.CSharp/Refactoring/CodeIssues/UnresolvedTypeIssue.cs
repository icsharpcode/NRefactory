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

			public override void VisitAttribute(Attribute attribute)
			{
				base.VisitAttribute(attribute);

				this.AddIssueIfUnresolvable(attribute.Type, true);
			}

			private void AddIssueIfUnresolvable(AstNode type, bool isAttribute = false)
			{
				var result = ctx.Resolve(type);

				if (result is UnknownIdentifierResolveResult)
				{
					var possibleType = GetPossibleTypes((UnknownIdentifierResolveResult)result, isAttribute).FirstOrDefault();
					this.AddIssue(type, ctx.TranslateString("using " + possibleType.Namespace + ";"), s =>
					              {
						var usingDeclaration = new UsingDeclaration(possibleType.Namespace);
						var existingUsings = GetExistingUsings(s, type);

						if (existingUsings.Count() > 0)
						{
							if (s.FormattingOptions.SortUsingsAlphabetically)
							{
								InsertIntoUsingsAlphabetically(s, existingUsings, usingDeclaration);
							}
							else
							{
								AddAfterExistingUsings(s, existingUsings, usingDeclaration);
							}
						}
						else
						{
							AddAtRoot(s, usingDeclaration, type);
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

			IEnumerable<UsingDeclaration> GetExistingUsings(Script s, AstNode currentNode)
			{
				if (s.FormattingOptions.UsingPlacement == UsingPlacement.TopOfFile)
				{
					return ctx.RootNode.Children.OfType<UsingDeclaration>();
				}

				return currentNode.Ancestors.OfType<NamespaceDeclaration>().First().Children.OfType<UsingDeclaration>();
			}

			private void InsertIntoUsingsAlphabetically(Script s, IEnumerable<UsingDeclaration> existingUsings, UsingDeclaration newUsing)
			{
				var nextUsing = existingUsings.FirstOrDefault(u => u.Namespace.CompareTo(newUsing.Namespace) >= 0);

				if (nextUsing != null)
				{
					s.InsertBefore(nextUsing, newUsing);
				}
				else
				{
					var lastUsing = existingUsings.Last();

					s.InsertAfter(lastUsing, newUsing);
					this.InsertBlankLines(s, lastUsing, lastUsing.NextSibling, s.FormattingOptions.BlankLinesAfterUsings);
				}
			}

			private void AddAfterExistingUsings(Script s, IEnumerable<UsingDeclaration> existingUsings, UsingDeclaration newUsing)
			{
				// TODO: Update this to find the using to insert before / after
				var lastUsing = existingUsings.Last();
				var nextNode = lastUsing.NextSibling;

				s.InsertAfter(lastUsing, newUsing);
				this.InsertBlankLines(s, lastUsing, nextNode, s.FormattingOptions.BlankLinesAfterUsings);
			}

			private void AddAtRoot(Script s, UsingDeclaration newUsing, AstNode currentNode)
			{
				if (s.FormattingOptions.UsingPlacement == UsingPlacement.TopOfFile)
				{
					var rootNode = ctx.RootNode;

					var addNode = rootNode.FirstChild;
					var prevNode = addNode;
					while (addNode is Comment) {
						addNode = addNode.NextSibling;
						prevNode = addNode;
					}

					this.InsertBlankLines(s, prevNode, addNode, s.FormattingOptions.BlankLinesBeforeUsings);
					s.InsertBefore(addNode, newUsing);
					this.InsertBlankLines(s, prevNode, addNode, s.FormattingOptions.BlankLinesAfterUsings);
				}
				else
				{
					var declaration = currentNode.Ancestors.OfType<NamespaceDeclaration>().First();
					var type = declaration.Children.OfType<TypeDeclaration>().First();

					this.InsertBlankLines(s, type.PrevSibling, type, s.FormattingOptions.BlankLinesBeforeUsings);
					s.InsertBefore(type, newUsing);
					this.InsertBlankLines(s, type.PrevSibling, type, s.FormattingOptions.BlankLinesAfterUsings);
				}
			}

			private IEnumerable<IType> GetPossibleTypes(UnknownIdentifierResolveResult result, bool isAttribute = false)
			{
				string targetName;

				if (isAttribute && !result.Identifier.EndsWith("Attribute"))
				{
					targetName = result.Identifier + "Attribute";
				}
				else
				{
					targetName = result.Identifier;
				}

				return ctx.Compilation.GetAllTypeDefinitions()
					.Where(t => t.Name == targetName &&
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

