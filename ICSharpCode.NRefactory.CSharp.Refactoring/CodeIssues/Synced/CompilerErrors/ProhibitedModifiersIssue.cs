//
// ProhibitedModifiersIssue.cs
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
using ICSharpCode.NRefactory.Refactoring;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription(
		"Checks for prohibited modifiers",
		Description = "Checks for prohibited modifiers",
		Category = IssueCategories.CompilerErrors,
		Severity = Severity.Error)]
	public class ProhibitedModifiersIssue : GatherVisitorCodeIssueProvider
	{
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
		}

		class GatherVisitor : GatherVisitorBase<RedundantArgumentDefaultValueIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx)
				: base (ctx)
			{
			}

			readonly Stack<TypeDeclaration> curType = new Stack<TypeDeclaration> ();
			public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
			{
				curType.Push(typeDeclaration); 
				base.VisitTypeDeclaration(typeDeclaration);
				curType.Pop();
			}

			void AddStaticRequiredError (EntityDeclaration entity, AstNode node)
			{
				AddIssue(
					node,
					ctx.TranslateString("'static' modifier is required inside a static class"),
					ctx.TranslateString("Add 'static' modifier"), 
					s => {
						s.ChangeModifier(entity, entity.Modifiers | Modifiers.Static);
					}
				);
			}

			void CheckStaticRequired(EntityDeclaration entity)
			{
				if (!curType.Peek().HasModifier(Modifiers.Static) || entity.HasModifier(Modifiers.Static))
					return;
				var fd = entity as FieldDeclaration;
				if (fd != null) {
					foreach (var init in fd.Variables)
						AddStaticRequiredError(entity, init.NameToken);
					return;
				}

				var ed = entity as EventDeclaration;
				if (ed != null) {
					foreach (var init in ed.Variables)
						AddStaticRequiredError(entity, init.NameToken);
					return;
				}

				AddStaticRequiredError(entity, entity.NameToken);
			}

			void CheckForbiddenVirtual(EntityDeclaration entity)
			{
				if (!curType.Peek().HasModifier(Modifiers.Sealed) || !entity.HasModifier(Modifiers.Virtual))
					return;
				AddIssue(
					entity.ModifierTokens.First(t => t.Modifier == Modifiers.Virtual),
					ctx.TranslateString("'virtual' modifier is not usable in a sealed class"),
					ctx.TranslateString("Remove 'virtual' modifier"), 
					s => {
						s.ChangeModifier(entity, entity.Modifiers & ~Modifiers.Virtual);
					}
				);
			}

			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
			{
				CheckStaticRequired(methodDeclaration);
				CheckForbiddenVirtual(methodDeclaration);
				base.VisitMethodDeclaration(methodDeclaration);
			}

			public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
			{
				CheckStaticRequired(fieldDeclaration);
				base.VisitFieldDeclaration(fieldDeclaration);
			}

			public override void VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration)
			{
				CheckStaticRequired(fixedFieldDeclaration);
				base.VisitFixedFieldDeclaration(fixedFieldDeclaration);
			}

			public override void VisitEventDeclaration(EventDeclaration eventDeclaration)
			{
				CheckStaticRequired(eventDeclaration);
				base.VisitEventDeclaration(eventDeclaration);
			}

			public override void VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration)
			{
				CheckStaticRequired(eventDeclaration);
				CheckForbiddenVirtual(eventDeclaration);
				base.VisitCustomEventDeclaration(eventDeclaration);
			}

			public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
			{
				CheckStaticRequired(constructorDeclaration);

				if (constructorDeclaration.HasModifier(Modifiers.Static)) {
					if ((constructorDeclaration.Modifiers & Modifiers.Static) != 0) {
						foreach (var mod in constructorDeclaration.ModifierTokens) {
							if (mod.Modifier == Modifiers.Static)
								continue;
							AddIssue(
								mod,
								ctx.TranslateString("Static constructors can't have any other modifier"),
								ctx.TranslateString("Remove prohibited modifier"), 
								s => {
									s.ChangeModifier(constructorDeclaration, Modifiers.Static);
								}
							);
						}
					}
				}
				base.VisitConstructorDeclaration(constructorDeclaration);
			}

			public override void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
			{
				base.VisitDestructorDeclaration(destructorDeclaration);
			}

			public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
			{
				CheckStaticRequired(propertyDeclaration);
				CheckForbiddenVirtual(propertyDeclaration);
				base.VisitPropertyDeclaration(propertyDeclaration);
			}

			public override void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
			{
				CheckForbiddenVirtual(indexerDeclaration);
				base.VisitIndexerDeclaration(indexerDeclaration);
			}

			public override void VisitBlockStatement(BlockStatement blockStatement)
			{
				// SKIP
			}
		}
	}
}