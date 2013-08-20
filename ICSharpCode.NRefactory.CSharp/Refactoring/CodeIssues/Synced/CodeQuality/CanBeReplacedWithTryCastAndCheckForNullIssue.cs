//
// CanBeReplacedWithTryCastAndCheckForNullIssue.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("Type check and casts can be replaced with 'as' and null check",
	                  Description="Type check and casts can be replaced with 'as' and null check",
	                  Category = IssueCategories.CodeQualityIssues,
	                  Severity = Severity.Suggestion,
	                  ResharperDisableKeyword = "CanBeReplacedWithTryCastAndCheckForNull")]
	public class CanBeReplacedWithTryCastAndCheckForNullIssue : GatherVisitorCodeIssueProvider
	{
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
		}

		class GatherVisitor : GatherVisitorBase<CanBeReplacedWithTryCastAndCheckForNullIssue>
		{
			public GatherVisitor (BaseRefactoringContext ctx) : base (ctx)
			{
			}

			static readonly AstNode pattern = 
				new IfElseStatement(
					new NamedNode ("isExpression", PatternHelper.OptionalParentheses(new IsExpression(PatternHelper.OptionalParentheses(new AnyNode()), PatternHelper.AnyType()))),
					new AnyNode("embedded"),
					new AnyNodeOrNull()
				);

			static readonly AstNode negatedPattern = 
				new IfElseStatement(
					new UnaryOperatorExpression (UnaryOperatorType.Not, new NamedNode ("isExpression", PatternHelper.OptionalParentheses(new IsExpression(PatternHelper.OptionalParentheses(new AnyNode()), PatternHelper.AnyType())))),
					PatternHelper.EmbeddedStatement(new Choice { new ReturnStatement (new AnyNodeOrNull()), new BreakStatement (), new ContinueStatement () } ),
					new AnyNodeOrNull()
				);


			static bool IsEmbeddedStatement(AstNode stmt)
			{
				return stmt.Role == Roles.EmbeddedStatement || 
					stmt.Role == IfElseStatement.TrueRole || 
					stmt.Role == IfElseStatement.FalseRole;
			}

			void HandleNegatedCase(IfElseStatement ifElseStatement, Match match)
			{
				var outerIs          = match.Get<Expression>("isExpression").Single();
				var isExpression     = CSharpUtil.GetInnerMostExpression(outerIs) as IsExpression;
				var obj              = CSharpUtil.GetInnerMostExpression(isExpression.Expression);
				var castToType       = isExpression.Type;

				var cast = new Choice {
					PatternHelper.OptionalParentheses(PatternHelper.OptionalParentheses(obj.Clone()).CastTo(castToType.Clone())),
					PatternHelper.OptionalParentheses(PatternHelper.OptionalParentheses(obj.Clone()).CastAs(castToType.Clone()))
				};

				var rr = ctx.Resolve(castToType);
				if (rr == null || rr.IsError)
					return;
				var foundCasts = ifElseStatement.GetParent<BlockStatement>().DescendantNodes(n => n.StartLocation >= ifElseStatement.StartLocation && !cast.IsMatch(n)).Where(n => cast.IsMatch(n)).ToList();
				if (foundCasts.Count == 0)
					return;

				AddIssue(
					isExpression.IsToken,
					ctx.TranslateString("Type check and casts can be replaced with 'as' and null check"),
					ctx.TranslateString("Use 'as' and check for null"),
					script => {
					var varName = ctx.GetNameProposal(CreateMethodDeclarationAction.GuessNameFromType(rr.Type), ifElseStatement.StartLocation);
					var varDec = new VariableDeclarationStatement(
						new PrimitiveType("var"),
						varName,
						new AsExpression(obj.Clone(), castToType.Clone())
					);
					var binaryOperatorIdentifier = new IdentifierExpression(varName);
					var binaryOperatorExpression = new BinaryOperatorExpression(binaryOperatorIdentifier, BinaryOperatorType.Equality, new NullReferenceExpression());

					var linkedNodes = new List<AstNode>();
					linkedNodes.Add(varDec.Variables.First().NameToken);
					linkedNodes.Add(binaryOperatorIdentifier);

					if (IsEmbeddedStatement (ifElseStatement)) {
						var block = new BlockStatement ();
						block.Add(varDec); 
						var newIf = (IfElseStatement)ifElseStatement.Clone();
						newIf.Condition = binaryOperatorExpression;
						foreach (var node in newIf.DescendantNodesAndSelf(n => !cast.IsMatch(n)).Where(n => cast.IsMatch(n))) {
							var id = new IdentifierExpression(varName);
							linkedNodes.Add(id);
							node.ReplaceWith(id);
						}
						block.Add(newIf); 
						script.Replace(ifElseStatement, block);
					} else {
						script.InsertBefore(ifElseStatement, varDec);
						script.Replace(
							ifElseStatement.Condition,
							binaryOperatorExpression
						);
						foreach (var c in foundCasts) {
							var id = new IdentifierExpression(varName);
							linkedNodes.Add(id);
							script.Replace(c, id);
						}
					}
					script.Link(linkedNodes);
				}
				);
			}

			public override void VisitIfElseStatement(IfElseStatement ifElseStatement)
			{
				base.VisitIfElseStatement(ifElseStatement);
				var match = pattern.Match(ifElseStatement);
				if (!match.Success) {
					match = negatedPattern.Match(ifElseStatement);
					if (match.Success)
						HandleNegatedCase(ifElseStatement, match);
					return;
				}


				var outerIs          = match.Get<Expression>("isExpression").Single();
				var isExpression     = CSharpUtil.GetInnerMostExpression(outerIs) as IsExpression;
				var obj              = CSharpUtil.GetInnerMostExpression(isExpression.Expression);
				var castToType       = isExpression.Type;
				var embeddedStatment = match.Get<Statement>("embedded").Single();

				var cast = new Choice {
					PatternHelper.OptionalParentheses(PatternHelper.OptionalParentheses(obj.Clone()).CastTo(castToType.Clone())),
					PatternHelper.OptionalParentheses(PatternHelper.OptionalParentheses(obj.Clone()).CastAs(castToType.Clone()))
				};

				var rr = ctx.Resolve(castToType);
				if (rr == null || rr.IsError)
					return;
				var foundCasts = embeddedStatment.DescendantNodesAndSelf(n => !cast.IsMatch(n)).Where(n => cast.IsMatch(n)).ToList();
				if (foundCasts.Count == 0)
					return;

				AddIssue(
					isExpression.IsToken,
					ctx.TranslateString("Type check and casts can be replaced with 'as' and null check"),
					ctx.TranslateString("Use 'as' and check for null"),
					script => {
						var varName = ctx.GetNameProposal(CreateMethodDeclarationAction.GuessNameFromType(rr.Type), ifElseStatement.StartLocation);
						var varDec = new VariableDeclarationStatement(
							new PrimitiveType("var"),
							varName,
							new AsExpression(obj.Clone(), castToType.Clone())
						);
						var binaryOperatorIdentifier = new IdentifierExpression(varName);
						var binaryOperatorExpression = new BinaryOperatorExpression(binaryOperatorIdentifier, BinaryOperatorType.InEquality, new NullReferenceExpression());

						var linkedNodes = new List<AstNode>();
						linkedNodes.Add(varDec.Variables.First().NameToken);
						linkedNodes.Add(binaryOperatorIdentifier);

						if (IsEmbeddedStatement (ifElseStatement)) {
							var block = new BlockStatement ();
							block.Add(varDec); 
							var newIf = (IfElseStatement)ifElseStatement.Clone();
							newIf.Condition = binaryOperatorExpression;
							foreach (var node in newIf.DescendantNodesAndSelf(n => !cast.IsMatch(n)).Where(n => cast.IsMatch(n))) {
								var id = new IdentifierExpression(varName);
								linkedNodes.Add(id);
								node.ReplaceWith(id);
							}
							block.Add(newIf); 
							script.Replace(ifElseStatement, block);
						} else {
							script.InsertBefore(ifElseStatement, varDec);
							script.Replace(
								outerIs,
								binaryOperatorExpression
							);
							foreach (var c in foundCasts) {
								var id = new IdentifierExpression(varName);
								linkedNodes.Add(id);
								script.Replace(c, id);
							}
						}
						script.Link(linkedNodes);
					}
				);
			}
		}
	}
}