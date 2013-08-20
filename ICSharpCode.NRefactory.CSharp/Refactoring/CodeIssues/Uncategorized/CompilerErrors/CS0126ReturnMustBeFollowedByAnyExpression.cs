//
// CS0126ReturnMustBeFollowedByAnyExpression.cs
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
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("CS0126: A method with return type cannot return without value.",
	                  Description = "Since 'function' doesn't return void, a return keyword must be followed by an object expression",
	                  Category = IssueCategories.CompilerErrors,
	                  Severity = Severity.Error)]
	public class CS0126ReturnMustBeFollowedByAnyExpression : GatherVisitorCodeIssueProvider
	{
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
		}

		class GatherVisitor : GatherVisitorBase<CS0127ReturnMustNotBeFollowedByAnyExpression>
		{
			string currentMethodName;

			public GatherVisitor (BaseRefactoringContext ctx) : base (ctx)
			{
			}

			bool skip;

			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
			{
				var primitiveType = methodDeclaration.ReturnType as PrimitiveType;
				skip = (primitiveType == null || primitiveType.Keyword == "void");
				currentMethodName = methodDeclaration.Name;
				base.VisitMethodDeclaration(methodDeclaration);
			}

			public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
			{
				currentMethodName = constructorDeclaration.Name;
				skip = true;
				base.VisitConstructorDeclaration(constructorDeclaration);
			}

			public override void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
			{
				currentMethodName = "~" + destructorDeclaration.Name;
				skip = true;
				base.VisitDestructorDeclaration(destructorDeclaration);
			}

			public override void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
			{
				bool old = skip;
				skip = !CreateFieldAction.GetValidTypes(ctx.Resolver, anonymousMethodExpression).Any(t => !t.IsKnownType(KnownTypeCode.Void));
				base.VisitAnonymousMethodExpression(anonymousMethodExpression);
				skip = old;
			}
			public override void VisitLambdaExpression(LambdaExpression lambdaExpression)
			{
				bool old = skip;
				skip = !CreateFieldAction.GetValidTypes(ctx.Resolver, lambdaExpression).Any(t => !t.IsKnownType(KnownTypeCode.Void));
				base.VisitLambdaExpression(lambdaExpression);
				skip = old;
			}

			public override void VisitReturnStatement(ReturnStatement returnStatement)
			{
				if (skip)
					return;

				if (returnStatement.Expression.IsNull) {
					var actions = new List<CodeAction>();
					actions.Add(new CodeAction(ctx.TranslateString("Return default value"), script => {
						script.Replace(returnStatement, new ReturnStatement (new PrimitiveExpression(0)));
					}, returnStatement));

					var method = returnStatement.GetParent<MethodDeclaration>();
					if (method != null) {
						actions.Add(new CodeAction(ctx.TranslateString("Change method return type to 'void'"), script => {
							script.Replace(method.ReturnType, new PrimitiveType("void"));
						}, returnStatement));
					}

					AddIssue(
						returnStatement, 
						string.Format(ctx.TranslateString("`{0}': A return keyword must be followed by any expression when method returns a value"), currentMethodName),
						actions
					);
				}
			}
		}
	}
}

