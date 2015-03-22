// 
// ConditionalToNullCoalescingInspector.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	/// <summary>
	/// Checks for "a != null ? a : other"<expr>
	/// Converts to: "a ?? other"<expr>
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ConvertConditionalTernaryToNullCoalescing")]
	public class ConvertConditionalTernaryToNullCoalescingAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "ConvertConditionalTernaryToNullCoalescingAnalyzer.;
		const string Description            = "'?:' expression can be converted to '??' expression.";
		const string MessageFormat          = "'?:' expression can be re-written as '??' expression";
		const string Category               = DiagnosticAnalyzerCategories.Opportunities;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "'?:' expression can be converted to '??' expression");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<ConvertConditionalTernaryToNullCoalescingAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			static ExpressionSyntax AnalyzeBinaryExpression (ExpressionSyntax node)
			{
				var bOp = node.SkipParens() as BinaryExpressionSyntax;
				if (bOp == null)
					return null;
				if (bOp.IsKind(SyntaxKind.NotEqualsExpression) || bOp.IsKind(SyntaxKind.EqualsExpression)) {
					if (bOp.Left != null && bOp.Left.SkipParens().IsKind(SyntaxKind.NullLiteralExpression))
						return bOp.Right;
					if (bOp.Right != null && bOp.Right.SkipParens().IsKind(SyntaxKind.NullLiteralExpression))
						return bOp.Left;
				}
				return null;
			}

			public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
			{
				base.VisitConditionalExpression(node);
				var obj = AnalyzeBinaryExpression(node.Condition);
				if (obj == null)
					return;
				if (node.Condition.SkipParens().IsKind(SyntaxKind.NotEqualsExpression)) {
					var whenTrue = ConvertConditionalTernaryToNullCoalescingFixProvider.UnpackNullableValueAccess(semanticModel, node.WhenTrue, cancellationToken);
					if (!CanBeNull(whenTrue))
						return;
					if (obj.SkipParens().IsEquivalentTo(whenTrue.SkipParens(), true)) {
						AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.GetLocation()));
						return;
					}
					var cast = whenTrue as CastExpressionSyntax;
					if (cast != null && cast.Expression != null && obj.SkipParens().IsEquivalentTo(cast.Expression.SkipParens(), true)) {
						AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.GetLocation()));
						return;
					}
				} else {
					var whenFalse = ConvertConditionalTernaryToNullCoalescingFixProvider.UnpackNullableValueAccess(semanticModel, node.WhenFalse, cancellationToken);
					if (!CanBeNull(whenFalse))
						return;
					if (obj.SkipParens().IsEquivalentTo(whenFalse.SkipParens(), true)) {
						AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.GetLocation()));
						return;
					}
				}
			}

			bool CanBeNull(ExpressionSyntax expression)
			{
				var info = semanticModel.GetTypeInfo(expression, cancellationToken);
				if (info.ConvertedType.IsReferenceType || info.ConvertedType.IsNullableType())
					return true;
				return false;
			}
		}
	}

	[ExportCodeFixProvider(ConvertConditionalTernaryToNullCoalescingAnalyzer.DiagnosticId, LanguageNames.CSharp)]
	public class ConvertConditionalTernaryToNullCoalescingFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ConvertConditionalTernaryToNullCoalescingAnalyzer.DiagnosticId;
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
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagnostic in diagnostics) {
				var node = root.FindNode(diagnostic.Location.SourceSpan) as ConditionalExpressionSyntax;
				if (node == null)
					continue;
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Replace '?:'  operator with '??", token => {
					ExpressionSyntax a, other;
					if (node.Condition.SkipParens().IsKind(SyntaxKind.EqualsExpression)) {
						a = node.WhenFalse;
						other = node.WhenTrue;
					} else {
						other = node.WhenFalse;
						a = node.WhenTrue;
					}

					if (node.Condition.SkipParens().IsKind(SyntaxKind.EqualsExpression)) {
						var castExpression = other as CastExpressionSyntax;
						if (castExpression != null) {
							a = SyntaxFactory.CastExpression(castExpression.Type, a);
							other = castExpression.Expression;
						}
					}

					a = UnpackNullableValueAccess(model, a, token);

					ExpressionSyntax newNode = SyntaxFactory.BinaryExpression(SyntaxKind.CoalesceExpression, a, other);

					var newRoot = root.ReplaceNode((SyntaxNode)node, newNode.WithLeadingTrivia(node.GetLeadingTrivia()).WithAdditionalAnnotations(Formatter.Annotation));
					return Task.FromResult(document.WithSyntaxRoot(newRoot));
 				}), diagnostic);
			}
		}

		internal static ExpressionSyntax UnpackNullableValueAccess(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
		{
			var expr = expression.SkipParens();
			if (!expr.IsKind(SyntaxKind.SimpleMemberAccessExpression))
				return expression;
			var info = semanticModel.GetTypeInfo(((MemberAccessExpressionSyntax)expr).Expression, cancellationToken);
			if (!info.ConvertedType.IsNullableType())
				return expression;
			return ((MemberAccessExpressionSyntax)expr).Expression;
		}
	}
}