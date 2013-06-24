// 
// ConvertIfToNullCoalescingAction.cs
//  
// Author:
//       Luís Reis <luiscubal@gmail.com>
// 
// Copyright (c) 2013 Luís Reis
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
using System.Linq;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction ("Convert 'if' to '??' expression",
	                Category = IssueCategories.Opportunities,
	                Description = "Convert 'if' statement to '??' expression.")]
	public class ConvertIfToNullCoalescingAction : SpecializedCodeAction <IfElseStatement>
	{
		protected override CodeAction GetAction (RefactoringContext context, IfElseStatement node)
		{
			Expression condition = RemoveParenthesis(node.Condition);

			var conditionExpression = condition as BinaryOperatorExpression;
			if (conditionExpression == null) {
				return null;
			}

			bool isEqualityComparison = conditionExpression.Operator == BinaryOperatorType.Equality;
			if (!isEqualityComparison && conditionExpression.Operator != BinaryOperatorType.InEquality) {
				return null;
			}

			Expression leftExpression = RemoveParenthesis(conditionExpression.Left);
			Expression rightExpression = RemoveParenthesis(conditionExpression.Right);

			var nullExpression = new NullReferenceExpression();
			Expression comparedNode;
			if (nullExpression.IsMatch(leftExpression)) {
				comparedNode = rightExpression;
			} else if (nullExpression.IsMatch(rightExpression)) {
				comparedNode = leftExpression;
			} else {
				return null;
			}

			comparedNode = RemoveParenthesis(comparedNode);

			Statement contentStatement;
			if (isEqualityComparison) {
				contentStatement = node.TrueStatement;
				if (!IsEmpty(node.FalseStatement)) {
					return null;
				}
			} else {
				contentStatement = node.FalseStatement;
				if (!IsEmpty(node.TrueStatement)) {
					return null;
				}
			}

			contentStatement = GetSimpleStatement(contentStatement);
			if (contentStatement == null) {
				return null;
			}

			ExpressionStatement expressionStatement = contentStatement as ExpressionStatement;
			if (expressionStatement == null) {
				return null;
			}

			var expression = RemoveParenthesis(expressionStatement.Expression);
			var assignment = expression as AssignmentExpression;
			if (assignment == null) {
				return null;
			}

			if (assignment.Operator != AssignmentOperatorType.Assign) {
				return null;
			}

			if (!comparedNode.IsMatch(RemoveParenthesis(assignment.Left))) {
				return null;
			}

			return new CodeAction(context.TranslateString("Convert if statement to ?? expression"),
			                      script => {

				var previousNode = node.GetPrevSibling(sibling => sibling is Statement);

				var previousDeclaration = previousNode as VariableDeclarationStatement;
				if (previousDeclaration != null && previousDeclaration.Variables.Count() == 1) {
					var variable = previousDeclaration.Variables.First();

					var comparedNodeIdentifierExpression = comparedNode as IdentifierExpression;
					if (comparedNodeIdentifierExpression != null &&
					    comparedNodeIdentifierExpression.Identifier == variable.Name) {

						script.Replace(variable.Initializer, new BinaryOperatorExpression(variable.Initializer.Clone(),
						                                                                  BinaryOperatorType.NullCoalescing,
						                                                                  assignment.Right.Clone()));
						script.Remove(node);

						return;
					}
				}

				var previousExpressionStatement = previousNode as ExpressionStatement;
				if (previousExpressionStatement != null)
				{
					var previousAssignment = previousExpressionStatement.Expression as AssignmentExpression;
					if (previousAssignment != null &&
					    comparedNode.IsMatch(RemoveParenthesis(previousAssignment.Left))) {

						var newExpression = new BinaryOperatorExpression(previousAssignment.Right.Clone(),
						                                                 BinaryOperatorType.NullCoalescing,
						                                                 assignment.Right.Clone());

						script.Replace(previousAssignment.Right, newExpression);
						script.Remove(node);
						return;
					}
				}

				var coalescedExpression = new BinaryOperatorExpression(comparedNode.Clone(),
				                                                       BinaryOperatorType.NullCoalescing,
				                                                       assignment.Right.Clone());

				var newAssignment = new ExpressionStatement(new AssignmentExpression(comparedNode.Clone(), coalescedExpression));
				script.Replace(node, newAssignment);
			}, node);
		}

		Statement GetSimpleStatement (Statement statement)
		{
			BlockStatement blockStatement;
			while ((blockStatement = statement as BlockStatement) != null) {
				var statements = blockStatement.Descendants.OfType<Statement>()
					.Where(descendant => !IsEmpty(descendant)).ToList();

				if (statements.Count() != 1) {
					return null;
				}

				statement = statements.First();
			}
			return statement;
		}

		bool IsEmpty (Statement statement)
		{
			if (statement.IsNull) {
				return true;
			}
			return !statement.DescendantsAndSelf.OfType<Statement>()
				.Any(descendant => !(descendant is EmptyStatement || descendant is BlockStatement));
		}

		Expression RemoveParenthesis (Expression expression)
		{
			ParenthesizedExpression parenthesizedExpression;
			while ((parenthesizedExpression = expression as ParenthesizedExpression) != null) {
				expression = parenthesizedExpression.Expression;
			}

			return expression;
		}
	}
}
