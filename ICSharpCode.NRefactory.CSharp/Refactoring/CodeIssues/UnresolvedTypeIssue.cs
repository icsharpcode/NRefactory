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

			public override void VisitMemberReferenceExpression(MemberReferenceExpression expression)
			{
				base.VisitMemberReferenceExpression(expression);

				this.AddIssueIfUnresolvable(expression.Target);
			}

			public override void VisitSimpleType(SimpleType simpleType)
			{
				base.VisitSimpleType(simpleType);

				if (simpleType.Role == Roles.Type || simpleType.Role == Roles.TypeArgument ||
				    simpleType.Role == Roles.BaseType)
				{
					if (simpleType.Parent.Parent is AttributeSection)
					{
						this.AddIssueIfUnresolvable(simpleType, true);
					}
					else
					{
						this.AddIssueIfUnresolvable(simpleType);
					}
				}
			}

			private static string GetIdentifier(AstNode node)
			{
				if (node is SimpleType) {
					return ((SimpleType)node).Identifier;
				}

				if (node is IdentifierExpression) {
					return ((IdentifierExpression)node).Identifier;
				}

				return null;
			}

			private void AddIssueIfUnresolvable(AstNode type, bool isAttribute = false)
			{
				var result = ctx.Resolve(type);
				var unknownIdResult = result as UnknownIdentifierResolveResult;

				if (unknownIdResult != null && unknownIdResult.Identifier == GetIdentifier(type))
				{
					var possibleType = GetPossibleTypes(unknownIdResult, isAttribute).FirstOrDefault();
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

