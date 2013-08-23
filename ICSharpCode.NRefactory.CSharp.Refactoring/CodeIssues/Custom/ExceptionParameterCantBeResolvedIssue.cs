//
// ExceptionParameterCantBeResolvedIssue.cs
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

using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using System;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("Exception constructor parameter can't be resolved.",
	                  Description = "Warns about when a constructor parameter can't be resolved.",
	                  Category = IssueCategories.CodeQualityIssues,
	                  Severity = Severity.Warning)]
	public class ExceptionParameterCantBeResolvedIssue : GatherVisitorCodeIssueProvider
	{
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
		}

		class GatherVisitor : GatherVisitorBase<ExceptionParameterCantBeResolvedIssue>
		{
			readonly BaseRefactoringContext context;

			static GatherVisitor()
			{
			}

			public GatherVisitor(BaseRefactoringContext context) : base (context)
			{
				this.context = context;
			}

			static string GetArgumentParameterName(Expression expression)
			{
				var pExpr = expression as PrimitiveExpression;
				if (pExpr != null)
					return pExpr.Value as string;
				return null;
			}


			bool CheckExceptionType(ObjectCreateExpression objectCreateExpression, out Expression paramNode, out Expression altParam)
			{
				paramNode = null;
				altParam = null;

				var rr = context.Resolve(objectCreateExpression.Type) as TypeResolveResult;
				if (rr == null)
					return false;

				var type = rr.Type;
				if (type.Name == typeof(ArgumentException).Name && type.Namespace == typeof(ArgumentException).Namespace) {
					if (objectCreateExpression.Arguments.Count >= 2) {
						altParam = objectCreateExpression.Arguments.ElementAt(0);
						paramNode = objectCreateExpression.Arguments.ElementAt(1);
					}
					return paramNode != null;
				}
				if (type.Name == typeof(ArgumentNullException).Name && type.Namespace == typeof(ArgumentNullException).Namespace ||
				    type.Name == typeof(ArgumentOutOfRangeException).Name && type.Namespace == typeof(ArgumentOutOfRangeException).Namespace ||
				    type.Name == typeof(DuplicateWaitObjectException).Name && type.Namespace == typeof(DuplicateWaitObjectException).Namespace) {
					if (objectCreateExpression.Arguments.Count >= 1) {
						paramNode = objectCreateExpression.Arguments.FirstOrDefault();
						if (objectCreateExpression.Arguments.Count == 2) {
							altParam = objectCreateExpression.Arguments.ElementAt(1);
							if (!context.Resolve(altParam).Type.IsKnownType(KnownTypeCode.String))
								paramNode = null;
						}
						if (objectCreateExpression.Arguments.Count == 3)
							altParam = objectCreateExpression.Arguments.ElementAt(2);
					}
					return paramNode != null;
				}
				return false;
			}

			static List<string> GetValidParameterNames(ObjectCreateExpression objectCreateExpression)
			{
				var names = new List<string>();
				var node = objectCreateExpression.Parent;
				while (node != null && !(node is TypeDeclaration) && !(node is AnonymousTypeCreateExpression)) {
					var lambda = node as LambdaExpression;
					if (lambda != null)
						names.AddRange(lambda.Parameters.Select(p => p.Name));
					var anonymousMethod = node as AnonymousMethodExpression;
					if (anonymousMethod != null)
						names.AddRange(anonymousMethod.Parameters.Select(p => p.Name));

					var indexer = node as IndexerDeclaration;
					if (indexer != null) {
						names.AddRange(indexer.Parameters.Select(p => p.Name));
						break;
					}

					var methodDeclaration = node as MethodDeclaration;
					if (methodDeclaration != null) {
						names.AddRange(methodDeclaration.Parameters.Select(p => p.Name));
						break;
					}
					node = node.Parent;
				}
				return names;
			}

			string GetParameterName(Expression expr)
			{
				foreach (var node in expr.DescendantsAndSelf) {
					if (!(node is Expression))
						continue;
					var rr = context.Resolve(node) as LocalResolveResult;
					if (rr != null && rr.Variable.SymbolKind == SymbolKind.Parameter)
						return rr.Variable.Name;
				}
				return null;
			}

			string GuessParameterName(ObjectCreateExpression objectCreateExpression, List<string> validNames)
			{
				if (validNames.Count == 1)
					return validNames[0];
				var parent = objectCreateExpression.GetParent<IfElseStatement>();
				if (parent == null)
					return null;
				return GetParameterName(parent.Condition);
			}

			public override void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
			{
				base.VisitObjectCreateExpression(objectCreateExpression);

				Expression paramNode;
				Expression altParamNode;
				if (!CheckExceptionType(objectCreateExpression, out paramNode, out altParamNode))
					return;

				var paramName = GetArgumentParameterName(paramNode);
				if (paramName == null)
					return;
				var validNames = GetValidParameterNames(objectCreateExpression);

				if (!validNames.Contains(paramName)) {
					// Case 1: Parameter name is swapped
					var altParamName = GetArgumentParameterName(altParamNode);
					if (altParamName != null && validNames.Contains(altParamName)) {
						AddIssue(
							paramNode,
							string.Format(context.TranslateString("The parameter '{0}' can't be resolved"), paramName),
							context.TranslateString("Swap parameter."),
							script => {
								var newAltNode = paramNode.Clone();
								script.Replace(paramNode, altParamNode.Clone());
								script.Replace(altParamNode, newAltNode);
							}
						);
						AddIssue(
							altParamNode,
							context.TranslateString("The parameter name is on the wrong argument."),
							context.TranslateString("Swap parameter."),
							script => {
								var newAltNode = paramNode.Clone();
								script.Replace(paramNode, altParamNode.Clone());
								script.Replace(altParamNode, newAltNode);
							}
						);
						return;
					}
					var guessName = GuessParameterName(objectCreateExpression, validNames);
					if (guessName != null) {
						AddIssue(
							paramNode,
							string.Format(context.TranslateString("The parameter '{0}' can't be resolved"), paramName),
							string.Format(context.TranslateString("Replace with '\"{0}\"'."), guessName),
							script => {
							script.Replace(paramNode, new PrimitiveExpression(guessName));
						}
						);
						return;
					}

					// General case: mark only
					AddIssue(
						paramNode,
						string.Format(context.TranslateString("The parameter '{0}' can't be resolved"), paramName)
						);
				}
			}
		}
	}
}
