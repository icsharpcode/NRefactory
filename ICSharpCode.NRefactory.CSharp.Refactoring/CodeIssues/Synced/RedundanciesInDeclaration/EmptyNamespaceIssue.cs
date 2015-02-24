// EmptyNamespaceIssue.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "EmptyNamespace")]
	public class EmptyNamespaceIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "EmptyNamespaceIssue";
		const string Description            = "Empty namespace declaration is redundant";
		const string MessageFormat          = "Empty namespace declaration is redundant";
		const string Category               = IssueCategories.RedundanciesInDeclarations;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Empty namespace declaration");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<EmptyNamespaceIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
			{
				bool hasContents = false;
				foreach (var member in node.Members) {
					if (!member.IsKind(SyntaxKind.NamespaceDeclaration)) {
						hasContents = true;
					} else {
						hasContents = true;
						base.VisitNamespaceDeclaration(node);
					}
				}

				if (!hasContents) {
					base.VisitLeadingTrivia(node);
					AddIssue(Diagnostic.Create(Rule, node.GetLocation()));
				}
			}

			public override void VisitBlock(BlockSyntax node)
			{
				// skip
			}
		}
	}

	[ExportCodeFixProvider(EmptyNamespaceIssue.DiagnosticId, LanguageNames.CSharp)]
	public class EmptyNamespaceFixProvider : NRefactoryCodeFixProvider
	{
		#region ICodeFixProvider implementation

		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return EmptyNamespaceIssue.DiagnosticId;
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
				if (node.IsKind(SyntaxKind.CompilationUnit)) {
					var cu = (CompilationUnitSyntax)node;
					node = cu.Members.FirstOrDefault();
				}

				if (!node.IsKind(SyntaxKind.NamespaceDeclaration))
					continue;
				var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Remove redundant namespace", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
		#endregion
	}
}