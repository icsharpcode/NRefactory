//
// FieldCanBeMadeReadOnlyIssue.cs
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
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Threading;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using System;
using System.Diagnostics;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("Convert field to readonly",
	                  Description = "Convert field to readonly",
	                  Category = IssueCategories.PracticesAndImprovements,
	                  Severity = Severity.Suggestion,
	                  ResharperDisableKeyword = "FieldCanBeMadeReadOnly.Local")]
	public class FieldCanBeMadeReadOnlyIssue : GatherVisitorCodeIssueProvider
	{
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
		}

		class GatherVisitor : GatherVisitorBase<FieldCanBeMadeReadOnlyIssue>
		{
			Stack<List<VariableInitializer>> potentialReadonlyFields = new Stack<List<VariableInitializer>> ();

			public GatherVisitor(BaseRefactoringContext context) : base (context)
			{
			}

			void Collect()
			{
				foreach (var varDecl in potentialReadonlyFields.Pop()) {
					AddIssue(
						varDecl.NameToken,
						ctx.TranslateString("Convert to readonly"),
						ctx.TranslateString("To readonly"),
						script => {
							var field = (FieldDeclaration)varDecl.Parent;
							script.ChangeModifier(field, field.Modifiers | Modifiers.Readonly);
						}
					);
				}
			}

			public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
			{
				var fieldVisitor = new ConvertToConstantIssue.FieldCollectVisitor<FieldCanBeMadeReadOnlyIssue>(ctx);
				typeDeclaration.AcceptVisitor(fieldVisitor);
				potentialReadonlyFields.Push(new List<VariableInitializer> ()); 
				foreach (var fieldDeclaration in fieldVisitor.CollectedFields) {
					if (fieldDeclaration.HasModifier(Modifiers.Const) || fieldDeclaration.HasModifier(Modifiers.Readonly))
						continue;
					if (fieldDeclaration.HasModifier(Modifiers.Public) || fieldDeclaration.HasModifier(Modifiers.Protected) || fieldDeclaration.HasModifier(Modifiers.Internal))
						continue;
					if (fieldDeclaration.Variables.Count() > 1)
						continue;
					var variable = fieldDeclaration.Variables.First();
					var rr = ctx.Resolve(fieldDeclaration.ReturnType);
					if ((rr.Type.IsReferenceType.HasValue && !rr.Type.IsReferenceType.Value) && (ctx.Resolve (variable.Initializer) is ConstantResolveResult))
						continue;

					potentialReadonlyFields.Peek().Add(variable); 
				}
				base.VisitTypeDeclaration(typeDeclaration);
				Collect();
				potentialReadonlyFields.Pop();
			}

			public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
			{
				// SKIP
			}

			public override void VisitBlockStatement(BlockStatement blockStatement)
			{
				base.VisitBlockStatement(blockStatement);
				if (blockStatement.Parent is EntityDeclaration || blockStatement.Parent is Accessor) {
					var assignmentAnalysis = new ConvertToConstantIssue.VariableAssignmentAnalysis (blockStatement, ctx.Resolver, ctx.CancellationToken);
					List<VariableInitializer> newVars = new List<VariableInitializer>();
					var oldVars = potentialReadonlyFields.Pop();
					foreach (var variable in oldVars) {
						var rr = ctx.Resolve(variable) as MemberResolveResult; 
						if (rr == null)
							continue;
						assignmentAnalysis.Analyze(rr.Member as IField, DefiniteAssignmentStatus.PotentiallyAssigned, ctx.CancellationToken);
						var definiteAssignmentStatus = assignmentAnalysis.GetEndState();
						if (definiteAssignmentStatus == DefiniteAssignmentStatus.DefinitelyAssigned)
							continue;
						newVars.Add(variable);
					}
					potentialReadonlyFields.Push(newVars);
				}
			}
		}
	}
}

