//
// CombineQueryExpressions.cs
//
// Modified by Luís Reis <luiscubal@gmail.com> (Copyright (C) 2013)
//
// Copyright header of the original version follows:
//
// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Combines query expressions and removes transparent identifiers.
	/// </summary>
	public class CombineQueryExpressions
	{
		static readonly InvocationExpression castPattern = new InvocationExpression {
			Target = new MemberReferenceExpression {
				Target = new AnyNode("inExpr"),
				MemberName = "Cast",
				TypeArguments = { new AnyNode("targetType") }
			}};

		public void CombineQuery(AstNode node, AstNode rootQuery = null)
		{
			if (rootQuery == null) {
				rootQuery = node;
			}

			QueryExpression query = node as QueryExpression;
			if (query != null) {
				foreach (var clause in query.Clauses) {
					var continuation = clause as QueryContinuationClause;
					if (continuation != null) {
						CombineQuery(continuation.PrecedingQuery);
					}

					var from = clause as QueryFromClause;
					if (from != null) {
						CombineQuery(from.Expression, rootQuery);
					}
				}

				QueryFromClause fromClause = (QueryFromClause)query.Clauses.First();
				QueryExpression innerQuery = fromClause.Expression as QueryExpression;
				if (innerQuery != null) {
					string transparentIdentifier;
					if (TryRemoveTransparentIdentifier(query, fromClause, innerQuery, out transparentIdentifier)) {
						RemoveTransparentIdentifierReferences(rootQuery, transparentIdentifier);
					} else if (fromClause.Type.IsNull) {
						QueryContinuationClause continuation = new QueryContinuationClause();
						continuation.PrecedingQuery = innerQuery.Detach();
						continuation.Identifier = fromClause.Identifier;
						fromClause.ReplaceWith(continuation);
					}
				} else {
					Match m = castPattern.Match(fromClause.Expression);
					if (m.Success) {
						fromClause.Type = m.Get<AstType>("targetType").Single().Detach();
						fromClause.Expression = m.Get<Expression>("inExpr").Single().Detach();
					}
				}
			}
		}

		static readonly QuerySelectClause selectTransparentIdentifierPattern = new QuerySelectClause {
			Expression = new AnonymousTypeCreateExpression {
					Initializers = {
						new AnyNode("nae1"),
						new AnyNode("nae2")
					}
				}
			};

		bool TryRemoveTransparentIdentifier(QueryExpression query, QueryFromClause fromClause, QueryExpression innerQuery, out string transparentIdentifier)
		{
			transparentIdentifier = fromClause.Identifier;

			Match match = selectTransparentIdentifierPattern.Match(innerQuery.Clauses.Last());
			if (!match.Success)
				return false;
			QuerySelectClause selectClause = (QuerySelectClause)innerQuery.Clauses.Last();
			Expression nae1 = match.Get<Expression>("nae1").SingleOrDefault();
			string nae1Name = ExtractExpressionName(ref nae1);
			if (nae1Name == null)
				return false;

			Expression nae2 = match.Get<Expression>("nae2").SingleOrDefault();
			string nae2Name = ExtractExpressionName(ref nae2);
			if (nae1Name == null)
				return false;

			// from * in (from x in ... select new { x = x, y = expr }) ...
			// =>
			// from x in ... let y = expr ...
			fromClause.Remove();
			selectClause.Remove();
			// Move clauses from innerQuery to query
			QueryClause insertionPos = null;
			foreach (var clause in innerQuery.Clauses) {
				query.Clauses.InsertAfter(insertionPos, insertionPos = clause.Detach());
			}
			query.Clauses.InsertAfter(insertionPos, new QueryLetClause { Identifier = nae2Name, Expression = nae2.Detach() });
			return true;
		}

		/// <summary>
		/// Removes all occurrences of transparent identifiers
		/// </summary>
		void RemoveTransparentIdentifierReferences(AstNode node, string transparentIdentifier)
		{
			foreach (AstNode child in node.Children) {
				RemoveTransparentIdentifierReferences(child, transparentIdentifier);
			}
			MemberReferenceExpression mre = node as MemberReferenceExpression;
			if (mre != null) {
				IdentifierExpression ident = mre.Target as IdentifierExpression;
				if (ident != null && ident.Identifier == transparentIdentifier) {
					IdentifierExpression newIdent = new IdentifierExpression(mre.MemberName);
					mre.TypeArguments.MoveTo(newIdent.TypeArguments);
					newIdent.CopyAnnotationsFrom(mre);
					newIdent.RemoveAnnotations<PropertyDeclaration>(); // remove the reference to the property of the anonymous type
					mre.ReplaceWith(newIdent);
					return;
				} else if (mre.MemberName == transparentIdentifier) {
					var newVar = mre.Target.Detach();
					newVar.CopyAnnotationsFrom(mre);
					newVar.RemoveAnnotations<PropertyDeclaration>(); // remove the reference to the property of the anonymous type
					mre.ReplaceWith(newVar);
					return;
				}
			}
		}

		string ExtractExpressionName(ref Expression expr)
		{
			NamedExpression namedExpr = expr as NamedExpression;
			if (namedExpr != null) {
				expr = namedExpr.Expression;
				return namedExpr.Name;
			}

			IdentifierExpression identifier = expr as IdentifierExpression;
			if (identifier != null) {
				return identifier.Identifier;
			}

			MemberReferenceExpression memberRef = expr as MemberReferenceExpression;
			if (memberRef != null) {
				return memberRef.MemberName;
			}

			return null;
		}
	}
}