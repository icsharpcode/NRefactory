// 
// RedundantThisInspector.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Linq;
using ICSharpCode.NRefactory.Refactoring;
using System.Diagnostics;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Finds redundant namespace usages.
	/// </summary>
	[IssueDescription("Redundant 'this.' qualifier",
	       Description= "'this.' is redundant and can safely be removed.",
	       Category = IssueCategories.RedundanciesInCode,
	       Severity = Severity.Warning,
	       IssueMarker = IssueMarker.GrayOut,
	       ResharperDisableKeyword = "RedundantThisQualifier")]
	[SubIssueAttribute(RedundantThisQualifierIssue.InsideConstructors, Severity = Severity.None)]
	[SubIssueAttribute(RedundantThisQualifierIssue.EverywhereElse)]
	public class RedundantThisQualifierIssue : GatherVisitorCodeIssueProvider
	{
		public const string InsideConstructors = "Inside constructors";
		public const string EverywhereElse = "Everywhere else";

		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context, this);
		}

		class GatherVisitor : GatherVisitorBase<RedundantThisQualifierIssue>
		{
			bool InsideConstructors {
				get {
					return SubIssue == RedundantThisQualifierIssue.InsideConstructors;
				}
			}

			public GatherVisitor (BaseRefactoringContext ctx, RedundantThisQualifierIssue qualifierDirectiveEvidentIssueProvider) : base (ctx, qualifierDirectiveEvidentIssueProvider)
			{
			}

			static IMember GetMember (ResolveResult result)
			{
				if (result is MemberResolveResult) {
					return ((MemberResolveResult)result).Member;
				} else if (result is MethodGroupResolveResult) {
					return ((MethodGroupResolveResult)result).Methods.FirstOrDefault ();
				}

				return null;
			}
			
			public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
			{
				if (InsideConstructors)
					base.VisitConstructorDeclaration(constructorDeclaration);
			}

			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
			{
				if (!InsideConstructors)
					base.VisitMethodDeclaration(methodDeclaration);
			}

			public override void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
			{
				if (!InsideConstructors)
					base.VisitIndexerDeclaration(indexerDeclaration);
			}

			public override void VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration)
			{
				if (!InsideConstructors)
					base.VisitCustomEventDeclaration(eventDeclaration);
			}

			public override void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
			{
				if (!InsideConstructors)
					base.VisitDestructorDeclaration(destructorDeclaration);
			}

			public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
			{
				if (!InsideConstructors)
					base.VisitFieldDeclaration(fieldDeclaration);
			}

			public override void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
			{
				if (!InsideConstructors)
					base.VisitOperatorDeclaration(operatorDeclaration);
			}

			public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
			{
				if (!InsideConstructors)
					base.VisitPropertyDeclaration(propertyDeclaration);
			}
			
			// We keep this stack so that we can check for cases where a field is used in the initializer
			// of a variable declaration. Currently the resolver does not resolve the variable name
			// to the variable until after the end of the statement, which makes this workaround necessary.
			Stack<VariableInitializer> currentDeclaringVariabes = new Stack<VariableInitializer> ();
			public override void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
			{
				foreach (var vi in variableDeclarationStatement.Variables) {
					currentDeclaringVariabes.Push(vi);
				}
				
				base.VisitVariableDeclarationStatement(variableDeclarationStatement);
				
				foreach (var vi in variableDeclarationStatement.Variables.Reverse()) {
					var popped = currentDeclaringVariabes.Pop();
					Debug.Assert(popped == vi);
				}
			}

			public override void VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression)
			{
				base.VisitThisReferenceExpression(thisReferenceExpression);
				var memberReference = thisReferenceExpression.Parent as MemberReferenceExpression;
				if (memberReference == null) {
					return;
				}
				
				if (currentDeclaringVariabes.Any(vi => vi.Name == memberReference.MemberName))
					return;

				var state = ctx.GetResolverStateAfter(thisReferenceExpression);
				var wholeResult = ctx.Resolve(memberReference);
			
				IMember member = GetMember(wholeResult);
				if (member == null) { 
					return;
				}
				
				var localDeclarationSpace = thisReferenceExpression.GetLocalVariableDeclarationSpace();
				if (ContainsConflictingDeclarationNamed(member.Name, localDeclarationSpace))
					return;

				var result = state.LookupSimpleNameOrTypeName(memberReference.MemberName, EmptyList<IType>.Instance, NameLookupMode.Expression);
				var parentResult = ctx.Resolve(memberReference.Parent) as CSharpInvocationResolveResult;
				
				bool isRedundant;
				if (result is MemberResolveResult) {
					isRedundant = ((MemberResolveResult)result).Member.Region.Equals(member.Region);
				} else if (parentResult != null && parentResult.IsExtensionMethodInvocation) {
					// 'this.' is required for extension method invocation
					isRedundant = false;
				} else if (result is MethodGroupResolveResult) {
					isRedundant = ((MethodGroupResolveResult)result).Methods.Any(m => m.Region.Equals(member.Region));
				} else {
					return;
				}

				if (isRedundant) {
					AddIssue(
						thisReferenceExpression.StartLocation, 
						memberReference.MemberNameToken.StartLocation, 
						ctx.TranslateString("Qualifier 'this.' is redundant"), 
						new CodeAction(
							ctx.TranslateString("Remove redundant 'this.'"),
							script => {
								script.Replace(memberReference, RefactoringAstHelper.RemoveTarget(memberReference));
							},
							thisReferenceExpression.StartLocation,
							memberReference.MemberNameToken.StartLocation
						) 
					);
				}
			}
			
			static bool ContainsConflictingDeclarationNamed(string name, AstNode rootNode)
			{
				var declarationFinder = new DeclarationFinder(name);
				rootNode.AcceptVisitor(declarationFinder);
				return declarationFinder.Declarations.Any();
			}
			
			class DeclarationFinder : DepthFirstAstVisitor
			{
				string name;
				
				public DeclarationFinder (string name)
				{
					this.name = name;
					Declarations = new List<AstNode>();
				}
				
				public IList<AstNode> Declarations {
					get;
					private set;
				}
				
				public override void VisitVariableInitializer(VariableInitializer variableInitializer)
				{
					if (variableInitializer.Name == name) {
						Declarations.Add(variableInitializer.NameToken);
					}
					base.VisitVariableInitializer(variableInitializer);
				}
				
				public override void VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
				{
					if (parameterDeclaration.Name == name) {
						Declarations.Add(parameterDeclaration);
					}
					
					base.VisitParameterDeclaration(parameterDeclaration);
				}
				
				public override void VisitForStatement(ForStatement forStatement)
				{
					base.VisitForStatement(forStatement);
				}
				
				public override void VisitForeachStatement(ForeachStatement foreachStatement)
				{
					if (foreachStatement.VariableName == name) {
						Declarations.Add(foreachStatement.VariableNameToken);
					}
				
					base.VisitForeachStatement(foreachStatement);
				}
			}
		}
	}
	
}