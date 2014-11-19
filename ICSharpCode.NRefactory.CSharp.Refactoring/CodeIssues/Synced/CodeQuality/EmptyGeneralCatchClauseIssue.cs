//
// EmptyGeneralCatchClauseIssue.cs
//
// Author:
//       Ji Kun <jikun.nus@gmail.com>
//
// Copyright (c) 2013 Ji Kun <jikun.nus@gmail.com>
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
	/// <summary>
	/// A catch clause that catches System.Exception and has an empty body
	/// </summary>
	[DiagnosticAnalyzer]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "EmptyGeneralCatchClause")]
	public class EmptyGeneralCatchClauseIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId = "EmptyGeneralCatchClauseIssue";
		const string Description = "A catch clause that catches System.Exception and has an empty body";
		const string MessageFormat = "Empty general catch clause suppresses any error";
		const string Category = IssueCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Empty general catch clause");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
		{
			get
			{
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<EmptyGeneralCatchClauseIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitCatchClause(CatchClauseSyntax node)
			{
				base.VisitCatchClause(node);

				if (node.Declaration == null)
					AddIssue(Diagnostic.Create(Rule, node.CatchKeyword.GetLocation()));
				else {
					var type = node.Declaration.Type;
					if (type != null) {
						ITypeSymbol typeSymbol = semanticModel.GetTypeInfo(type).Type;
						if (typeSymbol == null || typeSymbol.TypeKind == TypeKind.Error || !typeSymbol.GetFullName().Equals("System.Exception"))
							return;

						BlockSyntax body = node.Block;
						if (body.Statements.Any())
							return;

						AddIssue(Diagnostic.Create(Rule, node.CatchKeyword.GetLocation()));
					}
				}
			}
		}
	}

	[ExportCodeFixProvider(EmptyGeneralCatchClauseIssue.DiagnosticId, LanguageNames.CSharp)]
	public class EmptyGeneralCatchClauseFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return EmptyGeneralCatchClauseIssue.DiagnosticId;
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
				//original has no fix - leave it without any fixes?
				//var node = root.FindNode(diagnostic.Location.SourceSpan);
				//if (!node.IsKind(SyntaxKind.BaseList))
				//	continue;
				//var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
				//context.RegisterFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, diagnostic.GetMessage(), document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}