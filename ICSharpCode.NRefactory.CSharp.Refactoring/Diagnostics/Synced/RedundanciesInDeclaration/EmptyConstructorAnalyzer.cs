// 
// EmptyConstructorAnalyzer.cs
// 
// Author:
//      Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013 Ji Kun
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
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SaHALL THE
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

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "EmptyConstructor")]
	public class EmptyConstructorAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "EmptyConstructorAnalyzer";
		const string Description            = "An empty public constructor without paramaters is redundant.";
		const string MessageFormat          = "Empty constructor is redundant.";
		const string Category               = DiagnosticAnalyzerCategories.RedundanciesInDeclarations;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Empty constructor");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<EmptyConstructorAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}
			bool hasEmptyConstructor;
			bool hasUnemptyConstructor;
			ConstructorDeclarationSyntax emptyContructorNode;

			public override void VisitClassDeclaration(ClassDeclarationSyntax node)
			{
				hasEmptyConstructor = false;
				hasUnemptyConstructor = false;
				emptyContructorNode = null;

				foreach (var child in node.Members.OfType<ConstructorDeclarationSyntax>()) {
					if (child.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword))) 
						continue;
					if (child.ParameterList.Parameters.Count > 0 || !EmptyDestructorAnalyzer.IsEmpty(child.Body)) {
						hasUnemptyConstructor = true;
					} else if (child.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword))) {
						if (child.Initializer != null && child.Initializer.ArgumentList.Arguments.Count > 0)
							continue;
						hasEmptyConstructor = true;
						emptyContructorNode = child;
					}
				}
				if (!hasUnemptyConstructor && hasEmptyConstructor)
					base.VisitClassDeclaration(node);
			}

			public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
			{
				if (!hasUnemptyConstructor && hasEmptyConstructor && emptyContructorNode == node) {
					AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.GetLocation()));
				}
			}

			public override void VisitBlock(BlockSyntax node)
			{
				// skip
			}
		}
	}

	[ExportCodeFixProvider(EmptyConstructorAnalyzer.DiagnosticId, LanguageNames.CSharp)]
	public class EmptyConstructorFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return EmptyConstructorAnalyzer.DiagnosticId;
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
				if (!node.IsKind(SyntaxKind.ConstructorDeclaration))
					continue;
				var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Remove redundant constructor", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}