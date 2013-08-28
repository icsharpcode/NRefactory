// 
// ComputeConstantValueAction.cs
//  
// Author:
//       Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013 Ji Kun <jikun.nus@gmail.com>
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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.Refactoring;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.CSharp.Resolver;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Compute Constant Value
	/// </summary>
	using System;
	using System.Collections.Generic;
	
	[ContextAction("Add name for argument", Description = "Computes value of the current expression and replace it.")]
	public class ComputeConstantValueAction : SpecializedCodeAction<Expression>
	{
		private KnownTypeCode CurrentType = KnownTypeCode.None;

		private Expression UnpackExpression(ParenthesizedExpression paranthesizeExpression)
		{
			Expression temp = paranthesizeExpression;
			while (temp is ParenthesizedExpression) {
				temp = (temp as ParenthesizedExpression).Expression;
			}
			return temp;
		}

		private bool IsPrimitive(RefactoringContext context, Expression expression)
		{
			Expression temp = expression;
			if (temp is ParenthesizedExpression) {
				temp = UnpackExpression(expression as ParenthesizedExpression);
			}

			if (temp is PrimitiveExpression) {
				KnownTypeCode tempCurrentType =context.Resolve(temp).Type.GetDefinition().KnownTypeCode;;
//				if ((tempCurrentType == KnownTypeCode.Double && CurrentType == KnownTypeCode.Int32) || 
//				    (tempCurrentType == KnownTypeCode.Int32 && CurrentType == KnownTypeCode.Double))
//				{
//					CurrentType = KnownTypeCode.Double;
//					return true;
//				}
				if (CurrentType != KnownTypeCode.None) {
					if (tempCurrentType != CurrentType)
						return false;
				}
				CurrentType = tempCurrentType;
				return true;
			} else if (temp is IdentifierExpression)
				return false;
			else if (temp is UnaryOperatorExpression) {
				Expression temp2 = (temp as UnaryOperatorExpression).Expression;
				if (temp2 is BinaryOperatorExpression) {
					return BinaryCanBeComputed(context, temp2 as BinaryOperatorExpression);
				} else 
					return IsPrimitive(context, temp2);
			} else if (temp is BinaryOperatorExpression) {
				return BinaryCanBeComputed(context, temp as BinaryOperatorExpression);
			} else {
				return false;
			}
		}

		private bool BinaryCanBeComputed(RefactoringContext context, BinaryOperatorExpression binaryOperatorExpression)
		{
			bool leftCanBeComputed = false;
			bool rightCanBeComputed = false;

			if (binaryOperatorExpression.Left is BinaryOperatorExpression) {
				leftCanBeComputed = BinaryCanBeComputed(context, binaryOperatorExpression.Left as BinaryOperatorExpression);
			} else {
				leftCanBeComputed = IsPrimitive(context, binaryOperatorExpression.Left);
			}

			if (binaryOperatorExpression.Right is BinaryOperatorExpression) {
				rightCanBeComputed = BinaryCanBeComputed(context, binaryOperatorExpression.Right as BinaryOperatorExpression);
			} else {
				rightCanBeComputed = IsPrimitive(context, binaryOperatorExpression.Right);
			}

			return (leftCanBeComputed & rightCanBeComputed);
		}

		private PrimitiveExpression ComputeUnaryOperatorExpression(RefactoringContext context, UnaryOperatorExpression unaryOperatorExpression)
		{
			Expression expression = unaryOperatorExpression.Expression;

			if (expression is ParenthesizedExpression)
				expression = UnpackExpression(expression as ParenthesizedExpression);

			if (expression is UnaryOperatorExpression) {
				expression = ComputeUnaryOperatorExpression(context, expression as UnaryOperatorExpression);
			} else if (expression is BinaryOperatorExpression) {
				expression = ComputeBinaryOperatorExpression(context, expression as BinaryOperatorExpression);
			}

			if (expression is PrimitiveExpression) {
				PrimitiveExpression temp = expression as PrimitiveExpression;

				if (CurrentType == KnownTypeCode.Int16 || CurrentType == KnownTypeCode.Int32 || CurrentType == KnownTypeCode.Int64) {
					switch (unaryOperatorExpression.Operator) {
						case UnaryOperatorType.BitNot:
							return new PrimitiveExpression(~((int)temp.Value));
						case UnaryOperatorType.Minus:
							return new PrimitiveExpression(-((int)temp.Value));
						case UnaryOperatorType.Plus:
							return new PrimitiveExpression(+((int)temp.Value));
						default:
							return new PrimitiveExpression(temp.Value);
					}
				}
				else if (CurrentType == KnownTypeCode.Boolean) {
					if (unaryOperatorExpression.Operator == UnaryOperatorType.Not) {
						return new PrimitiveExpression(!((bool)temp.Value));
					} else 
						return new PrimitiveExpression(((bool)temp.Value));
				} else 
					return new PrimitiveExpression(temp.Value);
			}
			return null;
		}


		private PrimitiveExpression ComputeBinaryOperatorExpression(RefactoringContext context, BinaryOperatorExpression binaryOperatorExpression)
		{
			PrimitiveExpression leftExpression = new PrimitiveExpression("");
			PrimitiveExpression rightExpression = new PrimitiveExpression("");

			Expression tempLeftExpression = binaryOperatorExpression.Left;
			Expression tempRightExpression = binaryOperatorExpression.Right;
			//KnowTypeCode leftTypeCode = context.Resolve(tempLeftExpression).Type.GetDefinition().KnownTypeCode;
			//KnowTypeCode rightTypeCode = context.Resolve(tempRightExpression).Type.GetDefinition().KnownTypeCode;


			if (tempLeftExpression is ParenthesizedExpression) {
				tempLeftExpression = UnpackExpression(tempLeftExpression as ParenthesizedExpression);
			}
			if (tempLeftExpression is BinaryOperatorExpression) {
				leftExpression = ComputeBinaryOperatorExpression(context, tempLeftExpression as BinaryOperatorExpression);
			} else if (tempLeftExpression is PrimitiveExpression) {
				leftExpression = tempLeftExpression as PrimitiveExpression;
			} else if (tempLeftExpression is UnaryOperatorExpression) {
				leftExpression = ComputeUnaryOperatorExpression(context, tempLeftExpression as UnaryOperatorExpression);
			} else {
				//
			}

			if (tempRightExpression is ParenthesizedExpression) {
				tempRightExpression = UnpackExpression(tempRightExpression as ParenthesizedExpression);
			}
			if (tempRightExpression is BinaryOperatorExpression) {
				rightExpression = ComputeBinaryOperatorExpression(context, tempRightExpression as BinaryOperatorExpression);
			} else if (tempRightExpression is PrimitiveExpression) {
				rightExpression = tempRightExpression as PrimitiveExpression;
			} else if (tempRightExpression is UnaryOperatorExpression) {
				rightExpression = ComputeUnaryOperatorExpression(context, tempRightExpression as UnaryOperatorExpression);
			} else {
				//
			}

			if (CurrentType == KnownTypeCode.Boolean) {
				bool left = (bool)leftExpression.Value;
				bool right = (bool)rightExpression.Value;
				switch (binaryOperatorExpression.Operator) {
					case BinaryOperatorType.BitwiseAnd:
						return new PrimitiveExpression(left & right);
					case BinaryOperatorType.BitwiseOr:
						return new PrimitiveExpression(left | right);
					case BinaryOperatorType.Equality:
						return new PrimitiveExpression(left == right);
					case BinaryOperatorType.ExclusiveOr:
						return new PrimitiveExpression(left ^ right);
					case BinaryOperatorType.InEquality:
						return new PrimitiveExpression(left != right);
					case BinaryOperatorType.ConditionalAnd:
						return new PrimitiveExpression(left && right);
					case BinaryOperatorType.ConditionalOr:
						return new PrimitiveExpression(left || right);
					default:
						return null;
				}
			} else if (CurrentType == KnownTypeCode.Int16 || CurrentType == KnownTypeCode.Int32 || CurrentType == KnownTypeCode.Int64) {
				int left = (int)leftExpression.Value;
				int right = (int)rightExpression.Value;
				switch (binaryOperatorExpression.Operator) {
					case BinaryOperatorType.Add:
						return new PrimitiveExpression(left + right);
					case BinaryOperatorType.BitwiseAnd:
						return new PrimitiveExpression(left & right);
					case BinaryOperatorType.BitwiseOr:
						return new PrimitiveExpression(left | right);
					case BinaryOperatorType.Divide:
						return new PrimitiveExpression(left / right);
					case BinaryOperatorType.Equality:
						CurrentType = KnownTypeCode.Boolean;
						return new PrimitiveExpression(left == right);
					case BinaryOperatorType.ExclusiveOr:
						return new PrimitiveExpression(left ^ right);
					case BinaryOperatorType.GreaterThan:
						CurrentType = KnownTypeCode.Boolean;
						return new PrimitiveExpression(left > right);
					case BinaryOperatorType.GreaterThanOrEqual:
						CurrentType = KnownTypeCode.Boolean;
						return new PrimitiveExpression(left >= right);
					case BinaryOperatorType.InEquality:
						CurrentType = KnownTypeCode.Boolean;
						return new PrimitiveExpression(left != right);
					case BinaryOperatorType.ShiftLeft:
						return new PrimitiveExpression(left << right);
					case BinaryOperatorType.LessThan:
						CurrentType = KnownTypeCode.Boolean;
						return new PrimitiveExpression(left < right);
					case BinaryOperatorType.LessThanOrEqual:
						CurrentType = KnownTypeCode.Boolean;
						return new PrimitiveExpression(left <= right);
					case BinaryOperatorType.Modulus:
						return new PrimitiveExpression(left % right);
					case BinaryOperatorType.Multiply:
						return new PrimitiveExpression(left * right);
					case BinaryOperatorType.Subtract:
						return new PrimitiveExpression(left - right);
					case BinaryOperatorType.ShiftRight:
						return new PrimitiveExpression(left >> right);
					default:
						return null;
				}
			} else if (CurrentType == KnownTypeCode.Double) {
				double left = (double)leftExpression.Value;
				double right = (double)rightExpression.Value;
				switch (binaryOperatorExpression.Operator) {
					case BinaryOperatorType.Add:
						return new PrimitiveExpression(left + right);
					case BinaryOperatorType.Divide:
						return new PrimitiveExpression(left / right);
					case BinaryOperatorType.Equality:
						CurrentType = KnownTypeCode.Boolean;
						return new PrimitiveExpression(left == right);
					case BinaryOperatorType.GreaterThan:
						CurrentType = KnownTypeCode.Boolean;
						CurrentType = KnownTypeCode.Boolean;
						return new PrimitiveExpression(left > right);
					case BinaryOperatorType.GreaterThanOrEqual:
						CurrentType = KnownTypeCode.Boolean;
						return new PrimitiveExpression(left >= right);
					case BinaryOperatorType.InEquality:
						CurrentType = KnownTypeCode.Boolean;
						return new PrimitiveExpression(left != right);
					case BinaryOperatorType.LessThanOrEqual:
						CurrentType = KnownTypeCode.Boolean;
						return new PrimitiveExpression(left <= right);
					case BinaryOperatorType.Modulus:
						return new PrimitiveExpression(left % right);
					case BinaryOperatorType.Multiply:
						return new PrimitiveExpression(left * right);
					case BinaryOperatorType.Subtract:
						return new PrimitiveExpression(left - right);
					default:
						return null;
				}
			} else if (CurrentType == KnownTypeCode.String) {
				string left = (string)leftExpression.Value;
				string right = (string)rightExpression.Value;
				switch (binaryOperatorExpression.Operator) {
					case BinaryOperatorType.Add:
						return new PrimitiveExpression(left + right);
					case BinaryOperatorType.Equality:
						CurrentType = KnownTypeCode.Boolean;
						return new PrimitiveExpression(leftExpression.Value == rightExpression.Value);
					case BinaryOperatorType.InEquality:
						CurrentType = KnownTypeCode.Boolean;
						return new PrimitiveExpression(leftExpression.Value != rightExpression.Value);
					default:
						return null;
				}
			} else 
				return new PrimitiveExpression(leftExpression.Value);
		}

		protected override CodeAction GetAction(RefactoringContext context, Expression expression)
		{	
			if (expression == null)
				return null;
 			
			AstNode operatorToken;
			PrimitiveExpression resultExpression;

			if (expression is BinaryOperatorExpression) {
				if (BinaryCanBeComputed(context, expression as BinaryOperatorExpression)) {
					operatorToken = (expression as BinaryOperatorExpression).OperatorToken;
					resultExpression = ComputeBinaryOperatorExpression(context, expression as BinaryOperatorExpression);
				} else
					return null;
			} else if (expression is UnaryOperatorExpression) {
				if (IsPrimitive(context, expression as UnaryOperatorExpression)) {
					operatorToken = (expression as UnaryOperatorExpression).OperatorToken;
					resultExpression = ComputeUnaryOperatorExpression(context, expression as UnaryOperatorExpression);
				} else 
					return null;
			} else {
				return null;
			}

			if (resultExpression != null)
				return new CodeAction(context.TranslateString("Compute constant Value"),
				                      script => script.Replace(expression, resultExpression), operatorToken);
			else 
				return null;				
		}
	}
}

