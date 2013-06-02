// LockThisIssue.cs 
// 
// Author:
//      Luís Reis <luiscubal@gmail.com>
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
using ICSharpCode.NRefactory.CSharp.Analysis;
using ICSharpCode.NRefactory.Refactoring;
using System.Threading.Tasks;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("Use of lock (this) is discouraged",
	                  Description = "Warns about using lock (this).",
	                  Category = IssueCategories.CodeQualityIssues,
	                  Severity = Severity.Warning)]
	public class LockThisIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues (BaseRefactoringContext context)
		{
			return new GatherVisitor(context).GetIssues();
		}

		class GatherVisitor : GatherVisitorBase<LockThisIssue>
		{
			public GatherVisitor (BaseRefactoringContext context) : base (context)
			{
			}

			public override void VisitLockStatement(LockStatement lockStatement)
			{
				base.VisitLockStatement(lockStatement);

				var expression = lockStatement.Expression;

				if (IsThisReference(expression)) {
					var fixAction = new CodeAction(ctx.TranslateString("Create private locker field"), script => {
						var containerEntity = lockStatement.GetParent<EntityDeclaration>();
						var containerType = containerEntity.GetParent<TypeDeclaration>();

						var objectType = new PrimitiveType("object");

						var lockerDefinition = new FieldDeclaration() { ReturnType = objectType.Clone() };
						var lockerVariable = new VariableInitializer("locker",
						                                             new ObjectCreateExpression(objectType.Clone()));
						lockerDefinition.Variables.Add(lockerVariable);

						script.InsertBefore(containerEntity, lockerDefinition);

						FixLocks(script, containerType, lockerVariable);
					}, lockStatement);

					AddIssue(lockStatement, ctx.TranslateString("Found lock (this)"), fixAction);
				}
			}

			static Task FixLocks(Script script, TypeDeclaration containerType, VariableInitializer lockerVariable)
			{
				List<AstNode> linkNodes = new List<AstNode>();
				linkNodes.Add(lockerVariable.NameToken);

				foreach (var lockToModify in LocksInType(containerType)) {
					if (IsThisReference(lockToModify.Expression)) {
						var identifier = new IdentifierExpression("locker");
						script.Replace(lockToModify.Expression, identifier);

						linkNodes.Add(identifier);
					}
				}

				return script.Link(linkNodes.ToArray());
			}

			static IEnumerable<LockStatement> LocksInType(TypeDeclaration containerType)
			{
				return containerType.Descendants.OfType<LockStatement>().Where(lockStatement => {
					var childContainerType = lockStatement.GetParent<TypeDeclaration>();

					return childContainerType == containerType;
				});
			}

			static bool IsThisReference (Expression expression)
			{
				if (expression is ThisReferenceExpression) {
					return true;
				}

				var parenthesizedExpression = expression as ParenthesizedExpression;
				if (parenthesizedExpression != null) {
					return IsThisReference(parenthesizedExpression.Expression);
				}

				return false;
			}
		}
	}
}

