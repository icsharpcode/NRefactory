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
		static readonly IEnumerable<string> LinqSummableTypes = new string[] {
			"System.Int16",
			"System.Int32",
			"System.Int64"
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

			bool isValid;
			AssignmentExpression expression = GetSingleAssignmentExpression(statement, out isValid);
			if (expression == null || !isValid) {
				return null;
			}

			var type = context.Resolve(expression.Left).Type;
			if (!IsLinqSummableType(type)) {
				return null;
			}

			if (expression.Right.DescendantsAndSelf.OfType<AssignmentExpression>().Any()) {
				// Reject loops such as
				// int k = 0;
				// foreach (var x in y) { k += (z = 2); }

				return null;
			}

			var rightSide = new InvocationExpression(new MemberReferenceExpression(enumerableToIterate, "Sum"));

			return new AssignmentExpression(expression.Left.Clone(), AssignmentOperatorType.Add, rightSide);
		}

		bool IsLinqSummableType(IType type) {
			return LinqSummableTypes.Contains(type.FullName);
		}

		AssignmentExpression GetSingleAssignmentExpression (Statement statement, out bool isValid) {
			ExpressionStatement expression = statement as ExpressionStatement;
			if (expression != null) {
				AssignmentExpression assignment = expression.Expression as AssignmentExpression;
				if (assignment != null) {
					if (assignment.Operator != AssignmentOperatorType.Add) {
						isValid = false;
						return null;
					}

					isValid = true;
					return assignment;
				}

				isValid = false;
				return null;
			}

			BlockStatement block = statement as BlockStatement;
			if (block != null) {
				AssignmentExpression assignment = null;
				foreach (Statement child in block.Statements) {
					var newAssignment = GetSingleAssignmentExpression(child, out isValid);
					if (!isValid) {
						return null;
					}

					if (assignment == null) {
						assignment = newAssignment;
					} else {
						//TODO
						isValid = false;
						return null;
					}
				}

				isValid = true;
				return assignment;
			}

			isValid = false;
			return null;
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

