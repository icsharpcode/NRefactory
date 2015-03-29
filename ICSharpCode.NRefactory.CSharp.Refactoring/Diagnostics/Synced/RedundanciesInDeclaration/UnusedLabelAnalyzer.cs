//
// UnusedLabelAnalyzer.cs
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "UnusedLabel", PragmaWarning = 164)]
	public class UnusedLabelAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "UnusedLabelAnalyzer";
		const string Description            = "Label is never referenced";
		const string MessageFormat          = "Label is unused";
		const string Category               = DiagnosticAnalyzerCategories.RedundanciesInDeclarations;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Unused label");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<UnusedLabelAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			class LabelDescriptor 
//			{
//				public List<LabelStatement> LabelStatement = new List<ICSharpCode.NRefactory6.CSharp.LabelStatement>();
//				public bool IsUsed;
//
//				public LabelDescriptor(LabelStatement labelStatement)
//				{
//					this.LabelStatement.Add(labelStatement);
//				}
//			}
//
//			readonly Dictionary<string, LabelDescriptor> labels = new Dictionary<string, LabelDescriptor> ();
//
//			void GatherLabels(BlockStatement body)
//			{
//				foreach (var node in body.Descendants) {
//					var labelStatement = node as LabelStatement;
//					if (labelStatement == null)
//						continue;
//					// note: duplicate labels are checked by the parser.
//					LabelDescriptor desc;
//					if (!labels.TryGetValue(labelStatement.Label, out desc)) {
//						labels[labelStatement.Label] = new LabelDescriptor(labelStatement);
//					} else {
//						desc.LabelStatement.Add(labelStatement);
//					}
//				}
//			}
//
//			void CheckLables()
//			{
//				foreach (var label in labels.Values) {
//					if (label.IsUsed)
//						continue;
//					foreach (var stmt in label.LabelStatement) {
//						AddDiagnosticAnalyzer(new CodeIssue(
//							stmt.LabelToken.StartLocation,
//							stmt.ColonToken.EndLocation,
//							ctx.TranslateString(""),
//							ctx.TranslateString(""),
//							s => { s.Remove(stmt); s.FormatText(stmt.Parent); }
//						) { IssueMarker = IssueMarker.GrayOut });
//					}
//				}
//
//				labels.Clear();
//			}
//
//			public override void VisitGotoStatement(GotoStatement gotoStatement)
//			{
//				LabelDescriptor desc;
//				if (labels.TryGetValue(gotoStatement.Label, out desc)) {
//					desc.IsUsed = true;
//				}
//			}
//
//			public override void VisitLabelStatement(LabelStatement labelStatement)
//			{
//				if (IsSuppressed(labelStatement.StartLocation)) {
//					LabelDescriptor desc;
//					if (labels.TryGetValue(labelStatement.Label, out desc)) 
//						desc.LabelStatement.Remove(labelStatement);
//				}
//			}
//
//			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
//			{
//				GatherLabels(methodDeclaration.Body);
//				base.VisitMethodDeclaration(methodDeclaration);
//				CheckLables();
//			}
//
//			public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
//			{
//				GatherLabels(constructorDeclaration.Body);
//				base.VisitConstructorDeclaration(constructorDeclaration);
//				CheckLables();
//			}
//
//			public override void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
//			{
//				GatherLabels(destructorDeclaration.Body);
//				base.VisitDestructorDeclaration(destructorDeclaration);
//				CheckLables();
//			}
//
//			public override void VisitAccessor(Accessor accessor)
//			{
//				GatherLabels(accessor.Body);
//				base.VisitAccessor(accessor);
//				CheckLables();
//			}
//
//			public override void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
//			{
//				GatherLabels(operatorDeclaration.Body);
//				base.VisitOperatorDeclaration(operatorDeclaration);
//				CheckLables();
//			}
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class UnusedLabelFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return UnusedLabelAnalyzer.DiagnosticId;
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
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Remove unused label", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}

