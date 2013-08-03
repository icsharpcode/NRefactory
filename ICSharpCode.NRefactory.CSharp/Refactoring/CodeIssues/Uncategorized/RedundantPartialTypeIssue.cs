// RedundantPartialTypeIssue.cs
// 
// Author:
//      Luís Reis <luiscubal@gmail.com>
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
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.PatternMatching;
using Mono.CSharp;
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Redundant partial modifier in type declaration",
	                   Description = "Redundant partial modifier in type declaration",
	                   Category = IssueCategories.Redundancies,
	                   Severity = Severity.Warning,
	                   IssueMarker = IssueMarker.GrayOut)]
	public class RedundantPartialTypeIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context).GetIssues();
		}

		class GatherVisitor : GatherVisitorBase<CS0759RedundantPartialMethodIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx)
				: base(ctx)
			{
			}

			public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
			{
				if (!typeDeclaration.HasModifier(Modifiers.Partial))
					return;

				var resolveResult = ctx.Resolve(typeDeclaration) as TypeResolveResult;
				if (resolveResult == null)
					return;

				var typeDefinition = resolveResult.Type.GetDefinition();
				if (typeDefinition == null)
					return;

				if (typeDefinition.Parts.Count == 1) {
					var partialModifierToken = typeDeclaration.ModifierTokens.Single(modifier => modifier.Modifier == Modifiers.Partial);
					AddIssue(partialModifierToken,
					         ctx.TranslateString("Type declaration has a partial modifier, but there are no other partial declarations for the same type"),
					         GetFixAction(typeDeclaration));
				}
			}

			public override void VisitBlockStatement(BlockStatement blockStatement)
			{
				//We never need to visit the children of block statements
			}

			CodeAction GetFixAction(TypeDeclaration typeDeclaration)
			{
				return new CodeAction(ctx.TranslateString("Make type non-partial"), script => {
					var newDeclaration = (TypeDeclaration)typeDeclaration.Clone();
					newDeclaration.Modifiers &= ~(Modifiers.Partial);
					script.Replace(typeDeclaration, newDeclaration);
				}, typeDeclaration);
			}
		}
	}
}

