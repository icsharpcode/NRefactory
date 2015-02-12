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
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using System.Threading;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "FieldCanBeMadeReadOnly.Local")]
	public class FieldCanBeMadeReadOnlyIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "FieldCanBeMadeReadOnlyIssue";
		const string Description            = "Convert field to readonly";
		const string MessageFormat          = "Convert field to readonly";
		const string Category               = IssueCategories.PracticesAndImprovements;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "Convert field to readonly");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<FieldCanBeMadeReadOnlyIssue>
		{
			//			readonly Stack<List<Tuple<VariableInitializer, IVariable, VariableState>>> fieldStack = new Stack<List<Tuple<VariableInitializer, IVariable, VariableState>>>();

			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}
//
//			void Collect()
//			{
//				foreach (var varDecl in fieldStack.Peek()) {
//					if (varDecl.Item3 == VariableState.None)
//						continue;
//					AddIssue(new CodeIssue(
//						varDecl.Item1.NameToken,
//						ctx.TranslateString(""),
//						ctx.TranslateString(""),
//						script => {
//						var field = (FieldDeclaration)varDecl.Item1.Parent;
//						script.ChangeModifier(field, field.Modifiers | Modifiers.Readonly);
//					}
//					));
//				}
//			}
//
//			public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
//			{	
//				var list = new List<Tuple<VariableInitializer, IVariable, VariableState>>();
//				fieldStack.Push(list);
//
//				foreach (var fieldDeclaration in ConvertToConstantIssue.CollectFields (this, typeDeclaration)) {
//					if (fieldDeclaration.HasModifier(Modifiers.Const) || fieldDeclaration.HasModifier(Modifiers.Readonly))
//						continue;
//					if (fieldDeclaration.HasModifier(Modifiers.Public) || fieldDeclaration.HasModifier(Modifiers.Protected) || fieldDeclaration.HasModifier(Modifiers.Internal))
//						continue;
//					if (fieldDeclaration.Variables.Count() > 1)
//						continue;
//					var variable = fieldDeclaration.Variables.First();
//					var rr = ctx.Resolve(fieldDeclaration.ReturnType);
//					if (rr.Type.IsReferenceType == false) {
//						// Value type:
//						var def = rr.Type.GetDefinition();
//						if (def != null && def.KnownTypeCode == KnownTypeCode.None) {
//							// user-defined value type -- might be mutable
//							continue;
//						} else if (ctx.Resolve (variable.Initializer).IsCompileTimeConstant) {
//							// handled by ConvertToConstantIssue
//							continue;
//						}
//					}
//
//					var mr = ctx.Resolve(variable) as MemberResolveResult;
//					if (mr == null || !(mr.Member is IVariable))
//						continue;
//					list.Add(Tuple.Create(variable, (IVariable)mr.Member, VariableState.None)); 
//				}
//				base.VisitTypeDeclaration(typeDeclaration);
//				Collect();
//				fieldStack.Pop();
//			}
//
//			public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
//			{
//
//				foreach (var node in constructorDeclaration.Descendants) {
//					if (node is AnonymousMethodExpression || node is LambdaExpression) {
//						node.AcceptVisitor(this);
//					} else {
//						var assignmentAnalysis = new ConvertToConstantIssue.VariableUsageAnalyzation (ctx);
//						var newVars = new List<Tuple<VariableInitializer, IVariable, VariableState>>();
//						node.AcceptVisitor(assignmentAnalysis); 
//						foreach (var variable in fieldStack.Pop()) {
//							var state = assignmentAnalysis.GetStatus(variable.Item2);
//							if (variable.Item3 > state)
//								state = variable.Item3;
//							newVars.Add(new Tuple<VariableInitializer, IVariable, VariableState> (variable.Item1, variable.Item2, state));
//						}
//						fieldStack.Push(newVars);
//
//					}
//				}
//			}
//
//			public override void VisitBlockStatement(BlockStatement blockStatement)
//			{
//				var assignmentAnalysis = new ConvertToConstantIssue.VariableUsageAnalyzation (ctx);
//				var newVars = new List<Tuple<VariableInitializer, IVariable, VariableState>>();
//				blockStatement.AcceptVisitor(assignmentAnalysis); 
//					foreach (var variable in fieldStack.Pop()) {
//						var state = assignmentAnalysis.GetStatus(variable.Item2);
//						if (state == VariableState.Changed)
//							continue;
//						newVars.Add(new Tuple<VariableInitializer, IVariable, VariableState> (variable.Item1, variable.Item2, state));
//					}
//					fieldStack.Push(newVars);
//			}
		}
	}

	[ExportCodeFixProvider(FieldCanBeMadeReadOnlyIssue.DiagnosticId, LanguageNames.CSharp)]
	public class FieldCanBeMadeReadOnlyFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return FieldCanBeMadeReadOnlyIssue.DiagnosticId;
		}

		public override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override async Task ComputeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			var cancellationToken = context.CancellationToken;
			var span = context.Span;
			var diagnostics = context.Diagnostics;
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagnostic in diagnostics) {
				var node = root.FindNode(diagnostic.Location.SourceSpan);
				//if (!node.IsKind(SyntaxKind.BaseList))
				//	continue;
				var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
				context.RegisterFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "To readonly", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}