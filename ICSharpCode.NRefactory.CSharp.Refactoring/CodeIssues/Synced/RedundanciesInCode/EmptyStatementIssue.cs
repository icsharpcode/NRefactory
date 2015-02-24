//
// EmptyStatementIssue.cs
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
using System.ComponentModel;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "EmptyStatement")]
	[Description("Empty statement is redundant")]
	public class EmptyStatementIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "EmptyStatementIssue";
		const string Category               = IssueCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, "Empty statement is redundant", "Empty statement is redundant", Category, DiagnosticSeverity.Warning, true, "Empty statement is redundant");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<EmptyStatementIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitEmptyStatement(Microsoft.CodeAnalysis.CSharp.Syntax.EmptyStatementSyntax node)
			{
				if (IsEmbeddedStatement(node))
					return;
				VisitLeadingTrivia(node); 
				AddIssue (Diagnostic.Create(Rule, node.GetLocation()));
			}
		}

		internal static bool IsEmbeddedStatement(SyntaxNode stmt)
		{
			return !stmt.Parent.IsKind(SyntaxKind.Block);
		}
	}

	[ExportCodeFixProvider(EmptyStatementIssue.DiagnosticId, LanguageNames.CSharp)]
	public class EmptyStatementCodeFixProvider : NRefactoryCodeFixProvider
	{
		#region ICodeFixProvider implementation

		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return EmptyStatementIssue.DiagnosticId;
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
				if (node.IsKind(SyntaxKind.EmptyStatement)) {
					var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
					context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Remove ';'", document.WithSyntaxRoot(newRoot)), diagnostic);
				}
			}
		}
		#endregion
	}
}
