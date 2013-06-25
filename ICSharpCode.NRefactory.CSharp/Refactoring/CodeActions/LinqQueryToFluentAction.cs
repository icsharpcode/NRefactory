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
			return new CodeAction(context.TranslateString("Convert LINQ query to fluent syntax"),
			                      script => ConvertQueryToFluent(script, node),
			                      node);
		}

		static void ConvertQueryToFluent (Script script, QueryExpression query)
		{
			var conversionContext = new ConversionContext ();

			QueryFromClause fromClause = (QueryFromClause) query.Clauses.FirstOrNullObject();
			conversionContext.Expression = fromClause.Expression.Clone();
			conversionContext.Variables.Add(fromClause.Identifier);

			if (!fromClause.Type.IsNull) {
				CastToType(conversionContext, fromClause.Type);
			}

			foreach (QueryClause clause in query.Clauses.Skip(1))
			{
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

				throw new NotImplementedException("Unknown clause");
			}

			script.Replace(query, conversionContext.Expression);
		}

		static void HandleLetClause(ConversionContext conversionContext, QueryLetClause letClause)
		{
			var lambda = new LambdaExpression();
			CreateLambdaParameters(conversionContext, lambda);

			var arguments = conversionContext.Variables
				.Select (name => (Expression) new IdentifierExpression(name)).ToList ();

			var newVariableValue = ConvertBodyToNewParameters(conversionContext, letClause.Expression);
			var newIdentifierValue = newVariableValue as IdentifierExpression;
			if (newIdentifierValue != null && newIdentifierValue.Identifier == letClause.Identifier) {
				arguments.Add(new IdentifierExpression(letClause.Identifier));
			} else {
				arguments.Add(new NamedExpression(letClause.Identifier, (Expression)newVariableValue));
			}

			var returnedData = new AnonymousTypeCreateExpression(arguments);

			lambda.Body = returnedData;

			var selectMember = new MemberReferenceExpression(conversionContext.Expression,
			                                                 "Select");

			var invocation = new InvocationExpression(selectMember, new List<Expression> { lambda });

			conversionContext.Expression = invocation;
			conversionContext.Variables.Add(letClause.Identifier);
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
			conversionContext.Variables.Clear();
		}

		static AstNode ConvertBodyToNewParameters(ConversionContext conversionContext, Expression expression)
		{
			var newExpression = expression.Clone();

			if (conversionContext.Variables.Count > 1) {
				var identifiers = newExpression.DescendantsAndSelf
					.OfType<IdentifierExpression> ();

				foreach (var identifier in identifiers) {
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

		static void CastToType(ConversionContext conversionContext, AstType type)
		{
			var castMember = new MemberReferenceExpression (conversionContext.Expression,
			                                                "Cast",
			                                                new List<AstType> { type.Clone() });
			var invocation = new InvocationExpression (castMember, Enumerable.Empty<Expression>());

			conversionContext.Expression = invocation;
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

