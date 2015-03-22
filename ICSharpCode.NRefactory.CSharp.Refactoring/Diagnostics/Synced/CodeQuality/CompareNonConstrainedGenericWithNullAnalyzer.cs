//
// CompareNonConstrainedGenericWithNullAnalyzer.cs
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "CompareNonConstrainedGenericWithNull")]
	public class CompareNonConstrainedGenericWithNullAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "CompareNonConstrainedGenericWithNullAnalyzer";
		const string Description            = "Possible compare of value type with 'null'";
		const string MessageFormat          = "Possible compare of value type with 'null'";
		const string Category               = DiagnosticAnalyzerCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Possible compare of value type with 'null'");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<CompareNonConstrainedGenericWithNullAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			void CheckCase(ITypeSymbol type, ExpressionSyntax expr)
			{
				if (type.TypeKind != TypeKind.TypeParameter || type.IsReferenceType)
					return;
				AddDiagnosticAnalyzer (Diagnostic.Create(Rule, expr.GetLocation()));
			}

			public override void VisitBinaryExpression(BinaryExpressionSyntax node)
			{
				base.VisitBinaryExpression(node);
				var left = node.Left.SkipParens();
				var right = node.Right.SkipParens();
				ExpressionSyntax expr = null, highlightExpr = null;
				if (left.IsKind(SyntaxKind.NullLiteralExpression)) {
					expr = right;
					highlightExpr = node.Left;
				}
				if (right.IsKind(SyntaxKind.NullLiteralExpression)) {
					expr = left;
					highlightExpr = node.Right;
				}
				if (expr == null)
					return;
				var rr = semanticModel.GetTypeInfo(expr);
				CheckCase (rr.Type, highlightExpr);
			}
		}
	}

	[ExportCodeFixProvider(CompareNonConstrainedGenericWithNullAnalyzer.DiagnosticId, LanguageNames.CSharp)]
	public class CompareNonConstrainedGenericWithNullFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return CompareNonConstrainedGenericWithNullAnalyzer.DiagnosticId;
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
			var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
			var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagnostic in diagnostics) {
				var node = root.FindNode(diagnostic.Location.SourceSpan);
				if (!node.IsKind(SyntaxKind.NullLiteralExpression))
					continue;
				context.RegisterCodeFix(CodeActionFactory.Create(
					node.Span,
					diagnostic.Severity,
					"Replace with 'default'",
					token => {
						var bOp = (BinaryExpressionSyntax)node.Parent;
						var n = node == bOp.Left.SkipParens() ? bOp.Right : bOp.Left;
						var info = semanticModel.GetTypeInfo(n);

						var newRoot = root.ReplaceNode(
							node,
							SyntaxFactory.DefaultExpression(
								SyntaxFactory.ParseTypeName(info.Type.ToMinimalDisplayString(semanticModel, node.SpanStart)))
								.WithLeadingTrivia(node.GetLeadingTrivia()
							)
						);

						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}), diagnostic
				);
			}
		}
	}
}