//
// SimplifyConditionalTernaryExpressionAnalyzer.cs
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "SimplifyConditionalTernaryExpression")]
	public class SimplifyConditionalTernaryExpressionAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "SimplifyConditionalTernaryExpressionAnalyzer.;
		const string Description            = "Conditional expression can be simplified";
		const string MessageFormat          = "Simplify conditional expression";
		const string Category               = DiagnosticAnalyzerCategories.PracticesAndImprovements;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "Simplify conditional expression");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<SimplifyConditionalTernaryExpressionAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
			{
				base.VisitConditionalExpression(node);

				bool? trueBranch = SimplifyConditionalTernaryExpressionFixProvider.GetBool(node.WhenTrue.SkipParens());
				bool? falseBranch = SimplifyConditionalTernaryExpressionFixProvider.GetBool(node.WhenFalse.SkipParens());

				if (trueBranch == falseBranch ||
					trueBranch == true && falseBranch == false)	// Handled by RedundantTernaryExpressionIssue
					return;

				AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.GetLocation()));
			}
		}
	}

	[ExportCodeFixProvider(SimplifyConditionalTernaryExpressionAnalyzer.DiagnosticId, LanguageNames.CSharp)]
	public class SimplifyConditionalTernaryExpressionFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return SimplifyConditionalTernaryExpressionAnalyzer.DiagnosticId;
		}

		public override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		internal static bool? GetBool(ExpressionSyntax trueExpression)
		{
			var pExpr = trueExpression as LiteralExpressionSyntax;
			if (pExpr == null || !(pExpr.Token.Value is bool))
				return null;
			return (bool)pExpr.Token.Value;
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
				var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie:true) as ConditionalExpressionSyntax;
				var newRoot = root;

				bool? trueBranch = GetBool(node.WhenTrue.SkipParens());
				bool? falseBranch = GetBool(node.WhenFalse.SkipParens());

				if (trueBranch == false && falseBranch == true) {
					newRoot = newRoot.ReplaceNode(node, CSharpUtil.InvertCondition(node.Condition).WithAdditionalAnnotations(Formatter.Annotation));
				} else if (trueBranch == true) {
					newRoot = newRoot.ReplaceNode(
						(SyntaxNode)node,
						SyntaxFactory.BinaryExpression(
							SyntaxKind.LogicalOrExpression,
							node.Condition,
							SyntaxFactory.ParseToken(" || "),
							node.WhenFalse
						).WithAdditionalAnnotations(Formatter.Annotation)
					);
				} else if (trueBranch == false) {
					newRoot = newRoot.ReplaceNode(
						(SyntaxNode)node,
						SyntaxFactory.BinaryExpression(
							SyntaxKind.LogicalAndExpression,
							CSharpUtil.InvertCondition(node.Condition),
							SyntaxFactory.ParseToken(" && "),
							node.WhenFalse
						).WithAdditionalAnnotations(Formatter.Annotation)
					);
				} else if (falseBranch == true) {
					newRoot = newRoot.ReplaceNode(
						(SyntaxNode)node,
						SyntaxFactory.BinaryExpression(
							SyntaxKind.LogicalOrExpression,
							CSharpUtil.InvertCondition(node.Condition),
							SyntaxFactory.ParseToken(" || "),
							node.WhenTrue
						).WithAdditionalAnnotations(Formatter.Annotation)
					);
				} else if (falseBranch == false) {
					newRoot = newRoot.ReplaceNode(
						(SyntaxNode)node,
						SyntaxFactory.BinaryExpression(
							SyntaxKind.LogicalAndExpression,
							node.Condition,
							SyntaxFactory.ParseToken(" && "),
							node.WhenTrue
						).WithAdditionalAnnotations(Formatter.Annotation)
					);
				}

				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Simplify conditional expression", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}