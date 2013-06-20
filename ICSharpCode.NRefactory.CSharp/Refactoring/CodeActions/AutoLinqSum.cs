// 
// AutoLinqSum.cs
//
// Author:
//       Luís Reis <luiscubal@gmail.com>
// 
// Copyright (c) 2013 Luís Reis <luiscubal@gmail.com>
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
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Convers a loop to a Linq expression.
	/// </summary>
	[ContextAction("Convert loop to Linq expression", Description = "Converts a loop to a Linq expression")]
	public class AutoLinqSum : ICodeActionProvider
	{
		// Disabled for nullables, since int? x = 3; x += null; has result x = null,
		// but LINQ Sum behaves differently : nulls are treated as zero
		static readonly IEnumerable<string> LinqSummableTypes = new string[] {
			"System.UInt16",
			"System.Int16",
			"System.UInt32",
			"System.Int32",
			"System.UInt64",
			"System.Int64",
			"System.Single",
			"System.Double",
			"System.Decimal"
		};

		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var loop = GetForeachStatement (context);
			if (loop == null) {
				yield break;
			}

			var outputStatement = GetTransformedAssignmentExpression (context, loop);
			if (outputStatement == null) {
				yield break;
			}

			yield return new CodeAction(context.TranslateString("Convert foreach loop to LINQ expression"), script => {

				script.Replace(loop, new ExpressionStatement(outputStatement));

			}, loop);
		}

		AssignmentExpression GetTransformedAssignmentExpression (RefactoringContext context, ForeachStatement foreachStatement)
		{
			var enumerableToIterate = foreachStatement.InExpression.Clone();

			Statement statement = foreachStatement.EmbeddedStatement;

			Expression leftExpression, rightExpression;
			if (!ExtractExpression(statement, out leftExpression, out rightExpression)) {
				return null;
			}
			if (leftExpression == null || rightExpression == null) {
				return null;
			}

			var type = context.Resolve(leftExpression).Type;
			if (!IsLinqSummableType(type)) {
				return null;
			}

			if (rightExpression.DescendantsAndSelf.OfType<AssignmentExpression>().Any()) {
				// Reject loops such as
				// int k = 0;
				// foreach (var x in y) { k += (z = 2); }

				return null;
			}

			var arguments = new List<Expression>();
			if (!IsIdentifier(rightExpression, foreachStatement.VariableName)) {
				var lambda = new LambdaExpression();
				lambda.Parameters.Add(new ParameterDeclaration() { Name = foreachStatement.VariableName });
				lambda.Body = rightExpression.Clone();
				arguments.Add(lambda);
			}

			var rightSide = new InvocationExpression(new MemberReferenceExpression(enumerableToIterate, "Sum"), arguments);

			return new AssignmentExpression(leftExpression.Clone(), AssignmentOperatorType.Add, rightSide);
		}

		bool IsIdentifier(Expression expr, string variableName)
		{
			var identifierExpr = expr as IdentifierExpression;
			if (identifierExpr != null) {
				return identifierExpr.Identifier == variableName;
			}

			var parenthesizedExpr = expr as ParenthesizedExpression;
			if (parenthesizedExpr != null) {
				return IsIdentifier(parenthesizedExpr.Expression, variableName);
			}

			return false;
		}

		bool IsLinqSummableType(IType type) {
			return LinqSummableTypes.Contains(type.FullName);
		}

		bool ExtractExpression (Statement statement, out Expression leftSide, out Expression rightSide) {
			ExpressionStatement expression = statement as ExpressionStatement;
			if (expression != null) {
				AssignmentExpression assignment = expression.Expression as AssignmentExpression;
				if (assignment != null) {
					if (assignment.Operator == AssignmentOperatorType.Add) {
						leftSide = assignment.Left;
						rightSide = assignment.Right;
						return true;
					}
					if (assignment.Operator == AssignmentOperatorType.Subtract) {
						leftSide = assignment.Left;
						rightSide = new UnaryOperatorExpression(UnaryOperatorType.Minus, assignment.Right.Clone());
						return true;
					}

					leftSide = null;
					rightSide = null;
					return false;
				}

				leftSide = null;
				rightSide = null;
				return false;
			}

			BlockStatement block = statement as BlockStatement;
			if (block != null) {
				leftSide = null;
				rightSide = null;

				foreach (Statement child in block.Statements) {
					Expression newLeft, newRight;
					if (!ExtractExpression(child, out newLeft, out newRight)) {
						leftSide = null;
						rightSide = null;
						return false;
					}

					if (newLeft == null) {
						continue;
					}

					if (leftSide == null) {
						leftSide = newLeft;
						rightSide = newRight;
					} else {
						//TODO
						return false;
					}
				}

				return true;
			}

			leftSide = null;
			rightSide = null;
			return false;
		}

		ForeachStatement GetForeachStatement (RefactoringContext context)
		{
			var foreachStatement = context.GetNode();
			if (foreachStatement == null) {
				return null;
			}

			return foreachStatement.GetParent<ForeachStatement> ();
		}
	}
}

