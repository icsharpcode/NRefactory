// 
// LinqQueryToFluentAction.cs
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
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Convert LINQ query to fluent syntax",
	               Description = "Converts a LINQ query to the equivalent fluent syntax.")]
	public class LinqQueryToFluentAction : SpecializedCodeAction<QueryExpression>
	{
		protected override CodeAction GetAction(RefactoringContext context, QueryExpression node)
		{
			AstNode currentNode = node;
			for (;;) {
				QueryContinuationClause continuationParent = currentNode.Parent as QueryContinuationClause;
				if (continuationParent != null) {
					currentNode = continuationParent;
					continue;
				}
				QueryExpression exprParent = currentNode.Parent as QueryExpression;
				if (exprParent != null) {
					currentNode = exprParent;
					continue;
				}

				break;
			}

			node = (QueryExpression)currentNode;

			return new CodeAction(context.TranslateString("Convert LINQ query to fluent syntax"),
			                      script => ConvertQueryToFluent(script, node),
			                      node);
		}

		static void ConvertQueryToFluent(Script script, QueryExpression query) {
			script.Replace(query, GetFluentFromQuery(query));
		}

		static Expression GetFluentFromQuery (QueryExpression query)
		{
			var conversionContext = new ConversionContext ();

			QueryClause firstClause = query.Clauses.FirstOrNullObject();
			QueryFromClause firstFromClause = firstClause as QueryFromClause;
			if (firstFromClause != null) {
				conversionContext.Expression = ParenthesizeIfNeeded(firstFromClause.Expression.Clone());
				conversionContext.Variables.Add(firstFromClause.Identifier);

				if (!firstFromClause.Type.IsNull) {
					conversionContext.Expression = CastToType(conversionContext.Expression, firstFromClause.Type);
				}
			} else {
				QueryContinuationClause intoClause = firstClause as QueryContinuationClause;
				var precedingExpression = GetFluentFromQuery(intoClause.PrecedingQuery);

				precedingExpression = ParenthesizeIfNeeded(precedingExpression);

				conversionContext.Expression = precedingExpression;
				conversionContext.Variables.Add(intoClause.Identifier);
			}

			bool skipNext = false;
			foreach (QueryClause clause in query.Clauses.Skip(1))
			{
				if (skipNext) {
					skipNext = false;
					continue;
				}

				QueryFromClause fromClause = clause as QueryFromClause;
				if (fromClause != null) {
					HandleFromClause (conversionContext, fromClause, out skipNext);
					continue;
				}

				QuerySelectClause selectClause = clause as QuerySelectClause;
				if (selectClause != null) {
					HandleSelectClause (conversionContext, selectClause);
					continue;
				}

				QueryLetClause letClause = clause as QueryLetClause;
				if (letClause != null) {
					HandleLetClause (conversionContext, letClause);
					continue;
				}

				QueryWhereClause whereClause = clause as QueryWhereClause;
				if (whereClause != null) {
					HandleWhereClause (conversionContext, whereClause);
					continue;
				}

				QueryOrderClause orderClause = clause as QueryOrderClause;
				if (orderClause != null) {
					HandleOrderClause (conversionContext, orderClause);
					continue;
				}

				QueryGroupClause groupClause = clause as QueryGroupClause;
				if (groupClause != null) {
					HandleGroupClause (conversionContext, groupClause);
					continue;
				}

				QueryJoinClause joinClause = clause as QueryJoinClause;
				if (joinClause != null) {
					HandleJoinClause (conversionContext, joinClause, out skipNext);
					continue;
				}

				throw new NotImplementedException("Unknown clause");
			}

			return conversionContext.Expression;
		}

		static bool NeedsToBeParenthesized(Expression expr)
		{
			UnaryOperatorExpression unary = expr as UnaryOperatorExpression;
			if (unary != null) {
				if (unary.Operator == UnaryOperatorType.PostIncrement || unary.Operator == UnaryOperatorType.PostDecrement) {
					return false;
				}
				return true;
			}

			if (expr is BinaryOperatorExpression || expr is ConditionalExpression || expr is AssignmentExpression) {
				return true;
			}

			return false;
		}

		static Expression ParenthesizeIfNeeded(Expression expr)
		{
			return NeedsToBeParenthesized(expr) ? new ParenthesizedExpression(expr) : expr;
		}

		static void HandleJoinClause(ConversionContext conversionContext, QueryJoinClause joinClause, out bool skipNext)
		{
			Expression inExpression = joinClause.InExpression;
			if (!joinClause.Type.IsNull) {
				inExpression = CastToType(inExpression, joinClause.Type);
			}

			Expression outerExpression = joinClause.OnExpression;
			Expression innerExpression = joinClause.EqualsExpression;

			LambdaExpression outerLambda = new LambdaExpression();
			CreateLambdaParameters(conversionContext, outerLambda);
			outerLambda.Body = ConvertBodyToNewParameters(conversionContext, outerExpression);

			LambdaExpression innerLambda = new LambdaExpression();
			innerLambda.Parameters.Add(new ParameterDeclaration { Name = joinClause.JoinIdentifier });
			innerLambda.Body = innerExpression.Clone();

			bool isGroupJoin = !joinClause.IntoIdentifierToken.IsNull;

			string resultIdentifier = isGroupJoin ? joinClause.IntoIdentifier : joinClause.JoinIdentifier;

			LambdaExpression resultLambda = new LambdaExpression();
			CreateLambdaParameters(conversionContext, resultLambda);
			resultLambda.Parameters.Add(new ParameterDeclaration { Name = resultIdentifier });

			var nextSelect = joinClause.GetNextSibling(node => node is QueryClause) as QuerySelectClause;
			if (nextSelect != null) {
				skipNext = true;
				resultLambda.Body = ConvertBodyToNewParameters(conversionContext, nextSelect.Expression);
			} else {
				skipNext = false;
				resultLambda.Body = CreateAnonymousDataReturn(conversionContext, resultIdentifier, new IdentifierExpression(resultIdentifier));
			}

			var joinMember = new MemberReferenceExpression(conversionContext.Expression,
			                                               isGroupJoin ? "GroupJoin" : "Join");

			var arguments = new List<Expression>();
			arguments.Add(inExpression.Clone());
			arguments.Add(outerLambda);
			arguments.Add(innerLambda);
			arguments.Add(resultLambda);
			var invocation = new InvocationExpression(joinMember, arguments);

			conversionContext.Expression = invocation;
			conversionContext.Variables.Add(resultIdentifier);
		}

		static void HandleFromClause(ConversionContext conversionContext, QueryFromClause fromClause, out bool skipNext)
		{
			LambdaExpression collectionLambda = new LambdaExpression();
			CreateLambdaParameters(conversionContext, collectionLambda);
			var fromExpression = fromClause.Expression.Clone();
			if (!fromClause.Type.IsNull) {
				fromExpression = CastToType(fromExpression, fromClause.Type);
			}
			collectionLambda.Body = ConvertBodyToNewParameters(conversionContext, fromExpression);

			LambdaExpression resultLambda = new LambdaExpression();
			CreateLambdaParameters(conversionContext, resultLambda);
			resultLambda.Parameters.Add(new ParameterDeclaration { Name = fromClause.Identifier });

			var nextSelect = fromClause.GetNextSibling(node => node is QueryClause) as QuerySelectClause;
			if (nextSelect != null) {
				skipNext = true;
				resultLambda.Body = ConvertBodyToNewParameters(conversionContext, nextSelect.Expression);
			} else {
				skipNext = false;
				resultLambda.Body = CreateAnonymousDataReturn(conversionContext, fromClause.Identifier, new IdentifierExpression(fromClause.Identifier));
			}

			var selectManyMember = new MemberReferenceExpression(conversionContext.Expression,
			                                                     "SelectMany");

			var arguments = new List<Expression>();
			arguments.Add(collectionLambda);
			arguments.Add(resultLambda);
			var invocation = new InvocationExpression(selectManyMember, arguments);

			conversionContext.Expression = invocation;
			conversionContext.Variables.Add(fromClause.Identifier);
		}

		static void HandleGroupClause(ConversionContext conversionContext, QueryGroupClause groupClause)
		{
			var projection = groupClause.Projection;
			var key = groupClause.Key;

			LambdaExpression keyLambda = new LambdaExpression();
			CreateLambdaParameters(conversionContext, keyLambda);
			keyLambda.Body = ConvertBodyToNewParameters(conversionContext, key);

			LambdaExpression projectionLambda = null;
			IdentifierExpression projectionIdentifierExpression = projection as IdentifierExpression;
			if (conversionContext.Variables.Count != 1 ||
			    projectionIdentifierExpression == null ||
			    projectionIdentifierExpression.Identifier != conversionContext.Variables[0]) {

				projectionLambda = new LambdaExpression();
				CreateLambdaParameters(conversionContext, projectionLambda);
				projectionLambda.Body = ConvertBodyToNewParameters(conversionContext, projection);
			}

			var groupMember = new MemberReferenceExpression(conversionContext.Expression,
			                                                "GroupBy");

			var arguments = new List<Expression>();
			arguments.Add(keyLambda);
			if (projectionLambda != null) {
				arguments.Add(projectionLambda);
			}
			var invocation = new InvocationExpression(groupMember, arguments);

			conversionContext.Expression = invocation;
		}

		static void HandleOrderClause(ConversionContext conversionContext, QueryOrderClause orderClause)
		{
			bool isFirstOrdering = true;

			foreach (var ordering in orderClause.Orderings) {
				var lambda = new LambdaExpression();
				CreateLambdaParameters(conversionContext, lambda);

				lambda.Body = ConvertBodyToNewParameters(conversionContext, ordering.Expression);

				string methodName = (isFirstOrdering ?
				                     ordering.Direction == QueryOrderingDirection.Descending ? "OrderByDescending" : "OrderBy" :
				                     ordering.Direction == QueryOrderingDirection.Descending ? "ThenByDescending" : "ThenBy");

				var orderMember = new MemberReferenceExpression(conversionContext.Expression,
				                                                 methodName);

				var invocation = new InvocationExpression(orderMember, new List<Expression> { lambda });

				conversionContext.Expression = invocation;

				isFirstOrdering = false;
			}
		}

		static void HandleWhereClause(ConversionContext conversionContext, QueryWhereClause whereClause)
		{
			var lambda = new LambdaExpression();
			CreateLambdaParameters(conversionContext, lambda);

			lambda.Body = ConvertBodyToNewParameters(conversionContext, whereClause.Condition);

			var whereMember = new MemberReferenceExpression(conversionContext.Expression,
			                                                "Where");

			var invocation = new InvocationExpression(whereMember, new List<Expression> { lambda });
			
			conversionContext.Expression = invocation;
		}

		static void HandleLetClause(ConversionContext conversionContext, QueryLetClause letClause)
		{
			var lambda = new LambdaExpression();
			CreateLambdaParameters(conversionContext, lambda);

			var returnedData = CreateAnonymousDataReturn(conversionContext, letClause.Identifier, letClause.Expression);

			lambda.Body = returnedData;

			var letMember = new MemberReferenceExpression(conversionContext.Expression,
			                                              "Select");

			var invocation = new InvocationExpression(letMember, new List<Expression> { lambda });

			conversionContext.Expression = invocation;
			conversionContext.Variables.Add(letClause.Identifier);
		}

		static AnonymousTypeCreateExpression CreateAnonymousDataReturn(ConversionContext conversionContext, string newName, Expression newExpression)
		{
			var arguments = conversionContext.Variables.Select(name => CreateVariableInAnonymousType(conversionContext, name, new IdentifierExpression(name))).ToList();
			arguments.Add(CreateVariableInAnonymousType(conversionContext, newName, newExpression));
			var returnedData = new AnonymousTypeCreateExpression(arguments);
			return returnedData;
		}

		static Expression CreateVariableInAnonymousType(ConversionContext conversionContext, string newName, Expression newExpression)
		{
			var newVariableValue = ConvertBodyToNewParameters(conversionContext, newExpression);
			var newIdentifierValue = newVariableValue as IdentifierExpression;
			if (newIdentifierValue != null && newIdentifierValue.Identifier == newName) {
				return new IdentifierExpression(newName);
			}
			else {
				return new NamedExpression(newName, (Expression)newVariableValue);
			}
		}

		static void HandleSelectClause(ConversionContext conversionContext, QuerySelectClause selectClause)
		{
			var lambda = new LambdaExpression();
			CreateLambdaParameters(conversionContext, lambda);
			lambda.Body = ConvertBodyToNewParameters(conversionContext, selectClause.Expression);

			var selectMember = new MemberReferenceExpression(conversionContext.Expression,
			                                                 "Select");

			var invocation = new InvocationExpression(selectMember, new List<Expression> { lambda });

			conversionContext.Expression = invocation;
		}

		static AstNode ConvertBodyToNewParameters(ConversionContext conversionContext, Expression expression)
		{
			var newExpression = expression.Clone();

			if (conversionContext.Variables.Count > 1) {
				var identifiers = newExpression.DescendantsAndSelf
					.OfType<IdentifierExpression> ();

				foreach (var identifier in identifiers) {
					if (!conversionContext.Variables.Contains(identifier.Identifier)) {
						continue;
					}

					var replacement = new MemberReferenceExpression(
						new IdentifierExpression("_"), identifier.Identifier);

					if (identifier.Parent == null) {
						//Is Root Node
						return replacement;
					}

					identifier.ReplaceWith (replacement);
				}
			}

			return newExpression;
		}

		static void CreateLambdaParameters(ConversionContext conversionContext, LambdaExpression lambda)
		{
			if (conversionContext.Variables.Count == 1) {
				lambda.Parameters.Add(new ParameterDeclaration { Name = conversionContext.Variables[0] });
			} else {
				lambda.Parameters.Add(new ParameterDeclaration { Name = "_" });
			}
		}

		static Expression CastToType(Expression expression, AstType type)
		{
			var castMember = new MemberReferenceExpression (expression.Clone(),
			                                                "Cast",
			                                                new List<AstType> { type.Clone() });
			var invocation = new InvocationExpression (castMember, Enumerable.Empty<Expression>());

			return invocation;
		}

		class ConversionContext
		{
			/// <summary>
			/// The obtained expression so far
			/// Must not be in a tree (that is, clone if necessary *before* setting it)
			/// </summary>
			internal Expression Expression;
			internal List<string> Variables = new List<string>();
		}
	}
}

