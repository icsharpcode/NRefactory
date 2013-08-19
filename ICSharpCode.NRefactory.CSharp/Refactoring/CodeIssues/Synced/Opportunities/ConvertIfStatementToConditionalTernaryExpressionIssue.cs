//
// ConvertIfStatementToConditionalTernaryExpressionIssue.cs
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

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("'if' statement can be re-written as '?:' expression",
	                  Description="Convert 'if' to '?:'",
	                  Category = IssueCategories.Opportunities,
	                  Severity = Severity.Hint,
	                  IssueMarker = IssueMarker.DottedLine,
	                  ResharperDisableKeyword = "ConvertIfStatementToConditionalTernaryExpression")]
	public class ConvertIfStatementToConditionalTernaryExpressionIssue : GatherVisitorCodeIssueProvider
	{
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
		}

		class GatherVisitor : GatherVisitorBase<ConvertIfStatementToConditionalTernaryExpressionIssue>
		{
			public GatherVisitor (BaseRefactoringContext ctx) : base (ctx)
			{
			}

			static readonly AstNode ifElsePattern = 
				new IfElseStatement(
					new AnyNode("condition"),
					PatternHelper.EmbeddedStatement (new ExpressionStatement(new AssignmentExpression(new AnyNode("target"), new AnyNode("expr1")))),
					PatternHelper.EmbeddedStatement (new ExpressionStatement(new AssignmentExpression(new Backreference("target"), new AnyNode("expr2"))))
				);


			void AddTo(IfElseStatement ifElseStatement, Expression target, Expression condition, Expression trueExpr, Expression falseExpr)
			{
				AddIssue(
					ifElseStatement.IfToken,
					ctx.TranslateString("Convert to '?:' expression"),
					ctx.TranslateString("Replace with '?:' expression"),
					script => {
						script.Replace(ifElseStatement, new ExpressionStatement(
							new AssignmentExpression(target.Clone(), new ConditionalExpression(condition.Clone(), trueExpr.Clone(), falseExpr.Clone()))
						)
					); 
				}
				);
			}

			public override void VisitIfElseStatement(IfElseStatement ifElseStatement)
			{
				base.VisitIfElseStatement(ifElseStatement);

				var match = ifElsePattern.Match(ifElseStatement);
				if (match.Success) {
					AddTo(ifElseStatement,
					      match.Get<Expression>("target").Single(),
					      match.Get<Expression>("condition").Single(),
					      match.Get<Expression>("expr1").Single(),
					      match.Get<Expression>("expr2").Single());
					return;
				}
			}
		}
	}
}

