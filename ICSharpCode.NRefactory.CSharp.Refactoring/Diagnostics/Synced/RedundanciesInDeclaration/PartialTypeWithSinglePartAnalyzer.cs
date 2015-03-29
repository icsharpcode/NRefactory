// RedundantPartialTypeAnalyzer.cs
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "PartialTypeWithSinglePart")]
	public class PartialTypeWithSinglePartAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "PartialTypeWithSinglePartAnalyzer";
		const string Description            = "Class is declared partial but has only one part";
		const string MessageFormat          = "Partial class with single part";
		const string Category               = DiagnosticAnalyzerCategories.RedundanciesInDeclarations;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Redundant 'partial' modifier in type declaration");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<PartialTypeWithSinglePartAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
//			{
//				if (!typeDeclaration.HasModifier(Modifiers.Partial)) {
//					//We still need to visit the children in search of partial nested types.
//					base.VisitTypeDeclaration(typeDeclaration);
//					return;
//				}
//
//				var resolveResult = ctx.Resolve(typeDeclaration) as TypeResolveResult;
//				if (resolveResult == null)
//					return;
//
//				var typeDefinition = resolveResult.Type.GetDefinition();
//				if (typeDefinition == null)
//					return;
//
//				if (typeDefinition.Parts.Count == 1) {
//					var partialModifierToken = typeDeclaration.ModifierTokens.Single(modifier => modifier.Modifier == Modifiers.Partial);
//					// there may be a disable comment before the partial token somewhere
//					foreach (var child in typeDeclaration.Children.TakeWhile (child => child != partialModifierToken)) {
//						child.AcceptVisitor(this);
//					}
//					AddDiagnosticAnalyzer(new CodeIssue(partialModifierToken,
//					         ctx.TranslateString(""),
//						GetFixAction(typeDeclaration, partialModifierToken)) { IssueMarker = IssueMarker.GrayOut });
//				}
//				base.VisitTypeDeclaration(typeDeclaration);
//			}

			public override void VisitBlock(BlockSyntax node)
			{
				//We never need to visit the children of block statements
			}

//			CodeAction GetFixAction(TypeDeclaration typeDeclaration, CSharpModifierToken partialModifierToken)
//			{
//				return new CodeAction(ctx.TranslateString(""), script => {
//					script.ChangeModifier (typeDeclaration, typeDeclaration.Modifiers & ~Modifiers.Partial);
//				}, partialModifierToken);
//			}
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class PartialTypeWithSinglePartFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return PartialTypeWithSinglePartAnalyzer.DiagnosticId;
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
				// if (!node.IsKind(SyntaxKind.BaseList))
				//	continue;
				var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Remove 'partial'", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}