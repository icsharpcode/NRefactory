//
// CS0169FieldIsNeverUsedAnalyzer.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(PragmaWarning = 169)]
	public class CS0169FieldIsNeverUsedAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "CS0169FieldIsNeverUsedAnalyzer";
		const string Description            = "CS0169: Field is never used";
		const string MessageFormat          = "";
		const string Category               = DiagnosticAnalyzerCategories.CompilerWarnings;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "CS0169: Field is never used");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<CS0169FieldIsNeverUsedAnalyzer>
		{
			//readonly Stack<List<Tuple<VariableInitializer, IVariable>>> fieldStack = new Stack<List<Tuple<VariableInitializer, IVariable>>>();

			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			void Collect()
//			{
//				foreach (var varDecl in fieldStack.Peek()) {
//					AddDiagnosticAnalyzer(new CodeIssue(
//						varDecl.Item1.NameToken,
//						string.Format(ctx.TranslateString("The private field '{0}' is never assigned"), varDecl.Item2.Name)
//					));
//				}
//			}
//
//			public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
//			{	
//				var list = new List<Tuple<VariableInitializer, IVariable>>();
//				fieldStack.Push(list);
//
//				foreach (var fieldDeclaration in ConvertToConstantAnalyzer.CollectFields (this, typeDeclaration)) {
//					if (fieldDeclaration.HasModifier(Modifiers.Const))
//						continue;
//					if (fieldDeclaration.HasModifier(Modifiers.Public) || fieldDeclaration.HasModifier(Modifiers.Protected) || fieldDeclaration.HasModifier(Modifiers.Internal))
//						continue;
//					if (fieldDeclaration.Variables.Count() > 1)
//						continue;
//					var variable = fieldDeclaration.Variables.First();
//					if (!variable.Initializer.IsNull)
//						continue;
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
//					list.Add(Tuple.Create(variable, (IVariable)mr.Member)); 
//				}
//				base.VisitTypeDeclaration(typeDeclaration);
//				Collect();
//				fieldStack.Pop();
//			}
//
//			public override void VisitBlockStatement(BlockStatement blockStatement)
//			{
//				var assignmentAnalysis = new ConvertToConstantAnalyzer.VariableUsageAnalyzation (ctx);
//				var newVars = new List<Tuple<VariableInitializer, IVariable>>();
//				blockStatement.AcceptVisitor(assignmentAnalysis); 
//				foreach (var variable in fieldStack.Pop()) {
//					if (assignmentAnalysis.GetStatus(variable.Item2) == VariableState.Changed)
//						continue;
//					newVars.Add(variable);
//				}
//				fieldStack.Push(newVars);
//			}
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class CS0169FieldIsNeverUsedFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return CS0169FieldIsNeverUsedAnalyzer.DiagnosticId;
		}

		public override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public async override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			var cancellationToken = context.CancellationToken;
			var span = context.Span;
			var diagnostics = context.Diagnostics;
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();
			var diagnostic = diagnostics.First ();
			var node = root.FindNode(context.Span);
			//if (!node.IsKind(SyntaxKind.BaseList))
			//	continue;
			var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
			context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, diagnostic.GetMessage(), document.WithSyntaxRoot(newRoot)), diagnostic);
		}
	}
}