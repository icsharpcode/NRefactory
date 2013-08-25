//
// ReverseDirectionForForLoopAction.cs
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
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Reverse the direction of a for loop", Description = "Reverse the direction of a for loop")]
	public class ReverseDirectionForForLoopAction : SpecializedCodeAction<ForStatement>
	{
		bool? IsForward(ExpressionStatement statement, string name)
		{
			if (statement == null)
				return null;

			var forwardPattern = new Choice {
				PatternHelper.OptionalParentheses(new UnaryOperatorExpression (UnaryOperatorType.Increment, new IdentifierExpression(name))),
				PatternHelper.OptionalParentheses(new UnaryOperatorExpression (UnaryOperatorType.PostIncrement, new IdentifierExpression(name))),
				PatternHelper.OptionalParentheses(new BinaryOperatorExpression (new IdentifierExpression(name), BinaryOperatorType.Add, new PrimitiveExpression(1)))
			};
			if (forwardPattern.IsMatch(statement.Expression))
				return true;

			var backwardPattern = new Choice {
				PatternHelper.OptionalParentheses(new UnaryOperatorExpression (UnaryOperatorType.Decrement, new IdentifierExpression(name))),
				PatternHelper.OptionalParentheses(new UnaryOperatorExpression (UnaryOperatorType.PostDecrement, new IdentifierExpression(name))),
				PatternHelper.OptionalParentheses(new BinaryOperatorExpression (new IdentifierExpression(name), BinaryOperatorType.Subtract, new PrimitiveExpression(1)))
			};
			if (backwardPattern.IsMatch(statement.Expression))
				return false;
			return null;
		}

		Expression GetBound(Expression condition, string name, bool? direction)
		{
			var bOp = condition as BinaryOperatorExpression;
			if (bOp == null || direction == null)
				return null;
			if (direction == true) {
				var upwardPattern = new Choice {
					PatternHelper.OptionalParentheses(new BinaryOperatorExpression(PatternHelper.OptionalParentheses(new IdentifierExpression(name)), BinaryOperatorType.LessThan, PatternHelper.OptionalParentheses(new AnyNode("bound")))),
					PatternHelper.OptionalParentheses(new BinaryOperatorExpression(PatternHelper.OptionalParentheses(new AnyNode("bound")), BinaryOperatorType.GreaterThan, PatternHelper.OptionalParentheses(new IdentifierExpression(name))))
				};
				var upMatch = upwardPattern.Match(condition);
				if (!upMatch.Success)
					return null;
				return upMatch.Get<Expression>("bound").FirstOrDefault();
			}

			var downPattern = new Choice {
				PatternHelper.OptionalParentheses(new BinaryOperatorExpression(PatternHelper.OptionalParentheses(new IdentifierExpression(name)), BinaryOperatorType.GreaterThanOrEqual, PatternHelper.OptionalParentheses(new AnyNode("bound")))),
				PatternHelper.OptionalParentheses(new BinaryOperatorExpression(PatternHelper.OptionalParentheses(new AnyNode("bound")), BinaryOperatorType.LessThanOrEqual, PatternHelper.OptionalParentheses(new IdentifierExpression(name))))
			};
			var downMatch = downPattern.Match(condition);
			if (!downMatch.Success)
				return null;
			return downMatch.Get<Expression>("bound").FirstOrDefault();
		}

		Expression SubtractOne(Expression astNode)
		{
			var pe = astNode as PrimitiveExpression;
			if (pe != null) {
				return new PrimitiveExpression((int)pe.Value - 1);
			}
			return new BinaryOperatorExpression(pe, BinaryOperatorType.Subtract, new PrimitiveExpression(1));
		}

		Expression AddOne(Expression astNode)
		{
			var pe = astNode as PrimitiveExpression;
			if (pe != null) {
				return new PrimitiveExpression((int)pe.Value + 1);
			}
			return new BinaryOperatorExpression(pe, BinaryOperatorType.Add, new PrimitiveExpression(1));
		}

		Expression GetNewBound(string name, bool? direction, Expression initializer)
		{
			return new BinaryOperatorExpression (
				new IdentifierExpression (name),
				direction == true ? BinaryOperatorType.LessThan : BinaryOperatorType.GreaterThanOrEqual,
				direction == true ? AddOne(initializer) : initializer
			);
		}

		Expression CreateIterator(string name, bool? direction)
		{
			return new UnaryOperatorExpression(direction == true ? UnaryOperatorType.PostIncrement : UnaryOperatorType.PostDecrement, new IdentifierExpression(name));
		}

		protected override CodeAction GetAction(RefactoringContext context, ForStatement node)
		{
			if (!node.ForToken.Contains(context.Location))
				return null;

			var varDelc = node.Initializers.SingleOrDefault() as VariableDeclarationStatement;
			if (varDelc == null)
				return null;

			var initalizer = varDelc.Variables.SingleOrDefault();
			if (initalizer == null)
				return null;

			if (!context.Resolve(initalizer.Initializer).Type.IsKnownType(KnownTypeCode.Int32))
				return null;

			var iterator = node.Iterators.SingleOrDefault();
			var direction = IsForward(iterator as ExpressionStatement, initalizer.Name);

			var bound = GetBound(node.Condition, initalizer.Name, direction);
			if (bound == null)
				return null;

			return new CodeAction (
				context.TranslateString("Reverse 'for' loop"),
				s => {
					var newFor = new ForStatement() {
						Initializers = { new VariableDeclarationStatement(varDelc.Type.Clone(), initalizer.Name, direction == true ? SubtractOne(bound.Clone()) : bound.Clone() ) },
						Condition = GetNewBound(initalizer.Name, !direction, initalizer.Initializer.Clone()),
						Iterators = { CreateIterator(initalizer.Name, !direction) },
						EmbeddedStatement = node.EmbeddedStatement.Clone()
					};
					s.Replace(node, newFor);
				},
				node.ForToken
			);
		}
	}
}

