//
// PartialMethodParameterNameMismatchAnalyzer.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "PartialMethodParameterNameMismatch")]
	public class PartialMethodParameterNameMismatchAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "PartialMethodParameterNameMismatchAnalyzer";
		const string Description            = "Parameter name differs in partial method declaration";
		const string MessageFormat          = "";
		const string Category               = DiagnosticAnalyzerCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Parameter name differs in partial method declaration");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<PartialMethodParameterNameMismatchAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
//			{
//				// skip
//			}
//
//			public override void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
//			{
//				// skip
//			}
//
//			public override void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
//			{
//				// skip
//			}
//
//			public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
//			{
//				// skip
//			}
//
//			public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
//			{
//				// skip
//			}
//		
//			public override void VisitBlockStatement(BlockStatement blockStatement)
//			{
//				// SKIP
//			}
//
//			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
//			{
//				if (!methodDeclaration.HasModifier(Modifiers.Partial))
//					return;
//				var rr = ctx.Resolve(methodDeclaration) as MemberResolveResult;
//				if (rr == null || rr.IsError)
//					return;
//				var method = rr.Member as IMethod;
//				if (method == null)
//					return;
//
//				int arg = 0;
//				foreach (var param in methodDeclaration.Parameters) {
//					var pr = ctx.Resolve(param) as LocalResolveResult;
//					if (pr == null)
//						continue;
//					foreach (var part in method.Parts) {
//						if (param.Name != part.Parameters[arg].Name) {
//							List<CodeAction> fixes = new List<CodeAction>();
//							foreach (var p2 in method.Parts) {
//								if (param.Name != p2.Parameters[arg].Name) {
//									int _arg = arg;
//									fixes.Add(new CodeAction (
//										string.Format(ctx.TranslateString("Rename to '{0}'"), p2.Parameters[_arg].Name),
//										s => {
//											s.Rename(pr.Variable, p2.Parameters[_arg].Name);
//										},
//										param
//									)); 
//								}
//							}
//							// TODO: Atm I think it makes only sense to offer a fix if the issue disappears
//							// which might not be the case here.
//							if (fixes.Count > 1) {
//								fixes.Clear();
//							}
//							AddDiagnosticAnalyzer(new CodeIssue(
//								param.NameToken,
//								ctx.TranslateString("Parameter name differs in partial method declaration"),
//								fixes
//							));
//							break;
//						}
//					}
//					arg++;
//				}
//			}
		}
	}

	[ExportCodeFixProvider(PartialMethodParameterNameMismatchAnalyzer.DiagnosticId, LanguageNames.CSharp)]
	public class PartialMethodParameterNameMismatchFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return PartialMethodParameterNameMismatchAnalyzer.DiagnosticId;
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
			foreach (var diagnostic in diagnostics) {
				var node = root.FindNode(diagnostic.Location.SourceSpan);
				//if (!node.IsKind(SyntaxKind.BaseList))
				//	continue;
				var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, diagnostic.GetMessage(), document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}