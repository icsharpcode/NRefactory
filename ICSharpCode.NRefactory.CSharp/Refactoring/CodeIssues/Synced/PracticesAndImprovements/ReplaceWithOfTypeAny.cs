//
// ReplaceWithOfTypeAny.cs
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
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("Replace with OfType<T>().Any()",
	                  Description = "Replace with call to OfType<T>().Any()",
	                  Category = IssueCategories.PracticesAndImprovements,
	                  Severity = Severity.Suggestion,
	                  ResharperDisableKeyword = "ReplaceWithOfType.Any")]
	public class ReplaceWithOfTypeAnyIssue : GatherVisitorCodeIssueProvider
	{
		static readonly AstNode selectPattern =
			new InvocationExpression(
				new MemberReferenceExpression(
					new InvocationExpression(
						new MemberReferenceExpression(new AnyNode("targetExpr"), "Select"),
						new LambdaExpression {
							Parameters = { new NamedParameterDeclaration ("param1", new AnyType (true, "paramType"), Pattern.AnyString) },
							Body = PatternHelper.OptionalParentheses (new AsExpression(new AnyNode("expr1"), new AnyNode("type")))
						}
					), 
					"Any"
				),
				new LambdaExpression {
					Parameters = { new NamedParameterDeclaration ("param2", new AnyType (true, "paramType"), Pattern.AnyString) },
					Body = PatternHelper.OptionalParentheses (PatternHelper.CommutativeOperator(new AnyNode("expr2"), BinaryOperatorType.InEquality, new NullReferenceExpression()))
				}
			);

		static readonly AstNode selectPatternWithFollowUp =
			new InvocationExpression(
				new MemberReferenceExpression(
					new InvocationExpression(
						new MemberReferenceExpression(new AnyNode("targetExpr"), "Select"),
						new LambdaExpression {
							Parameters = { new NamedParameterDeclaration ("param1", new AnyType (true, "paramType"), Pattern.AnyString) },
							Body = PatternHelper.OptionalParentheses (new AsExpression(new AnyNode("expr1"), new AnyNode("type")))
						}
					),	 
					"Any"
				),
				new NamedNode("lambda", 
					new LambdaExpression {
						Parameters = { new NamedParameterDeclaration ("param2", new AnyType (true, "paramType"), Pattern.AnyString) },
						Body = new BinaryOperatorExpression(
							PatternHelper.OptionalParentheses(PatternHelper.CommutativeOperator(new AnyNode("expr2"), BinaryOperatorType.InEquality, new NullReferenceExpression())),
							BinaryOperatorType.ConditionalAnd,
							new AnyNode("followUpExpr")
						)
					}
				)
			);


		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
		}

		class GatherVisitor : GatherVisitorBase<ReplaceWithOfTypeIssue>
		{
			public GatherVisitor (BaseRefactoringContext ctx) : base (ctx)
			{
			}

			bool CheckParameterMatches(IEnumerable<INode> param, IEnumerable<INode> expr)
			{
				var p = param.Single() as ParameterDeclaration;
				var e = expr.Single();

				if (p == null)
					return false;
				if (e is IdentifierExpression)
					return p.Name == ((IdentifierExpression)e).Identifier;
				return false;
			}

			public override void VisitInvocationExpression (InvocationExpression anyInvoke)
			{
				base.VisitInvocationExpression (anyInvoke);
				var match = selectPattern.Match (anyInvoke);
				if (match.Success) {
					AddIssue (
						anyInvoke,
						ctx.TranslateString("Replace with OfType<T>().Any()"),
						ctx.TranslateString("Replace with call to OfType<T>().Any()"),
						script => {
						var target = match.Get<Expression>("targetExpr").Single().Clone();
							var type = match.Get<AstType>("type").Single().Clone();
							script.Replace(
								anyInvoke,
								new InvocationExpression(
									new MemberReferenceExpression(
										new InvocationExpression(new MemberReferenceExpression(target, "OfType", type)),
										"Any"
									)
								)
							 );
						}
					);
					return;
				}

				match = selectPatternWithFollowUp.Match (anyInvoke);
				if (match.Success) {
					AddIssue (
						anyInvoke,
						ctx.TranslateString("Replace with OfType<T>().Any()"),
						ctx.TranslateString("Replace with call to OfType<T>().Any()"),
						script => {
							var target = match.Get<Expression>("targetExpr").Single().Clone();
							var type = match.Get<AstType>("type").Single().Clone();
							var lambda = match.Get<LambdaExpression>("lambda").Single();
							script.Replace(
								anyInvoke,
								new InvocationExpression(
									new MemberReferenceExpression(
										new InvocationExpression(new MemberReferenceExpression(target, "OfType", type)),
										"Any"
									),
									new LambdaExpression {
										Parameters = { (ParameterDeclaration)lambda.Parameters.First().Clone() },
										Body = match.Get<Expression>("followUpExpr").Single().Clone()
									}
								)
							 );
						}
					);
					return;
				}

			}
		}
	}
}
