//
// UnassignedReadonlyFieldIssue.cs
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
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.CSharp.Analysis;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("Unassigned readonly field",
	                  Description = "Unassigned readonly field",
	                  Category = IssueCategories.CompilerWarnings,
	                  Severity = Severity.Warning,
	                  PragmaWarning = 649,
	                  ResharperDisableKeyword = "UnassignedReadonlyField.Compiler")]
	public class UnassignedReadonlyFieldIssue : GatherVisitorCodeIssueProvider
	{
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
		}

		class GatherVisitor : GatherVisitorBase<UnassignedReadonlyFieldIssue>
		{
			List<VariableInitializer> potentialReadonlyFields = new List<VariableInitializer>();

			public GatherVisitor(BaseRefactoringContext context) : base (context)
			{
			}
			
			void Collect()
			{
				foreach (var varDecl in potentialReadonlyFields) {
					var resolveResult = ctx.Resolve(varDecl) as MemberResolveResult;
					if (resolveResult == null || resolveResult.IsError)
						continue;
					AddIssue(
						varDecl.NameToken,
						string.Format(ctx.TranslateString("Readonly field '{0}' is never assigned"), varDecl.Name),
						ctx.TranslateString("Initialize field from constructor parameter"),
						script => {
							script.InsertWithCursor(
								ctx.TranslateString("Create constructor"),
								resolveResult.Member.DeclaringTypeDefinition,
								(s, c) => {
									return new ConstructorDeclaration {
										Name = resolveResult.Member.DeclaringTypeDefinition.Name,
										Modifiers = Modifiers.Public,
										Body = new BlockStatement {
											new ExpressionStatement(
												new AssignmentExpression(
													new MemberReferenceExpression(new ThisReferenceExpression(), varDecl.Name),
													new IdentifierExpression(varDecl.Name)
												)
											)
										},
										Parameters = {
											new ParameterDeclaration(c.CreateShortType(resolveResult.Type), varDecl.Name)
										}
									};
								}
						);
					}
					);
				}
			}

			public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
			{
				foreach (var fieldDeclaration in typeDeclaration.Members.OfType<FieldDeclaration>()) {
					if (IsSuppressed(fieldDeclaration.StartLocation))
						continue;
					if (!fieldDeclaration.HasModifier(Modifiers.Readonly))
						continue;
					if (fieldDeclaration.Variables.Count() > 1)
						continue;
					if (!fieldDeclaration.Variables.First().Initializer.IsNull)
						continue;
					potentialReadonlyFields.AddRange(fieldDeclaration.Variables); 
				}
				base.VisitTypeDeclaration(typeDeclaration);
				Collect();
				potentialReadonlyFields.Clear();
			}


			public override void VisitBlockStatement(BlockStatement blockStatement)
			{
				base.VisitBlockStatement(blockStatement);
				if (blockStatement.Parent is EntityDeclaration || blockStatement.Parent is Accessor) {
					var assignmentAnalysis = new ConvertToConstantIssue.VariableAssignmentAnalysis (blockStatement, ctx.Resolver, ctx.CancellationToken);
					List<VariableInitializer> newVars = new List<VariableInitializer>();
					foreach (var variable in potentialReadonlyFields) {
						var rr = ctx.Resolve(variable) as MemberResolveResult; 
						if (rr == null)
							continue;
						assignmentAnalysis.Analyze(rr.Member as IField, DefiniteAssignmentStatus.PotentiallyAssigned, ctx.CancellationToken);
						if (assignmentAnalysis.GetEndState() == DefiniteAssignmentStatus.DefinitelyAssigned)
							continue;
						newVars.Add(variable);
					}
					potentialReadonlyFields = newVars;
				}
			}

		}
	}
}

