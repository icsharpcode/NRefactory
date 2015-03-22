//
// RewriteIfReturnToReturnAnalyzer.cs
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
	public class RewriteIfReturnToReturnAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "RewriteIfReturnToReturnAnalyzer";
		const string Description            = "Convert 'if...return' to 'return'";
		const string MessageFormat          = "Convert to 'return' statement";
		const string Category               = DiagnosticAnalyzerCategories.Opportunities;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "'if...return' statement can be re-written as 'return' statement");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<RewriteIfReturnToReturnAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}
//
//			public override void VisitIfElseStatement(IfElseStatement ifElseStatement)
//			{
//				base.VisitIfElseStatement(ifElseStatement);
//
//				if (ifElseStatement.Parent is IfElseStatement)
//					return;
//				Expression c, e1, e2;
//				AstNode rs;
//				if (!ConvertIfStatementToReturnStatementAction.GetMatch(ifElseStatement, out c, out e1, out e2, out rs))
//					return;
//				if (ConvertIfStatementToConditionalTernaryExpressionAnalyzer.IsComplexCondition(c) || 
//				    ConvertIfStatementToConditionalTernaryExpressionAnalyzer.IsComplexExpression(e1) || 
//				    ConvertIfStatementToConditionalTernaryExpressionAnalyzer.IsComplexExpression(e2))
//					return;
//				AddDiagnosticAnalyzer(new CodeIssue(
//					ifElseStatement.IfToken,
//					ctx.TranslateString("")
//				) { IssueMarker = IssueMarker.DottedLine, ActionProvider = { typeof(ConvertIfStatementToReturnStatementAction) } });
//			}
		}
	}

	[ExportCodeFixProvider(RewriteIfReturnToReturnAnalyzer.DiagnosticId, LanguageNames.CSharp)]
	public class RewriteIfReturnToReturnFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return RewriteIfReturnToReturnAnalyzer.DiagnosticId;
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
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Convert to 'return' statement", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}