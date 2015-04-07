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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	/// <summary>
	/// Checks for "a != null ? a : other"<expr>
	/// Converts to: "a ?? other"<expr>
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ConvertConditionalTernaryToNullCoalescingAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ConvertConditionalTernaryToNullCoalescingAnalyzerID, 
			GettextCatalog.GetString("'?:' expression can be converted to '??' expression"),
			GettextCatalog.GetString("'?:' expression can be converted to '??' expression"), 
			DiagnosticAnalyzerCategories.Opportunities, 
			DiagnosticSeverity.Info, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.ConvertConditionalTernaryToNullCoalescingAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryGetDiagnostic (nodeContext, out diagnostic)) {
						nodeContext.ReportDiagnostic(diagnostic);
					}
				}, 
				new SyntaxKind[] {  SyntaxKind.ConditionalExpression }
			);
		}

		bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			var node = nodeContext.Node as ConditionalExpressionSyntax;
			var semanticModel = nodeContext.SemanticModel;
			var cancellationToken = nodeContext.CancellationToken;

			diagnostic = default(Diagnostic);
			var obj = AnalyzeBinaryExpression(node.Condition);
			if (obj == null)
				return false;
			if (node.Condition.SkipParens().IsKind(SyntaxKind.NotEqualsExpression)) {
				var whenTrue = ConvertConditionalTernaryToNullCoalescingCodeFixProvider.UnpackNullableValueAccess(semanticModel, node.WhenTrue, cancellationToken);
				if (!CanBeNull(semanticModel, whenTrue, cancellationToken))
					return false;
				if (obj.SkipParens().IsEquivalentTo(whenTrue.SkipParens(), true)) {
					diagnostic = Diagnostic.Create (
						descriptor,
						node.GetLocation ()
					);
					return true;
				}
				var cast = whenTrue as CastExpressionSyntax;
				if (cast != null && cast.Expression != null && obj.SkipParens().IsEquivalentTo(cast.Expression.SkipParens(), true)) {
					diagnostic = Diagnostic.Create (
						descriptor,
						node.GetLocation ()
					);
					return true;
				}
			} else {
				var whenFalse = ConvertConditionalTernaryToNullCoalescingCodeFixProvider.UnpackNullableValueAccess(semanticModel, node.WhenFalse, cancellationToken);
				if (!CanBeNull(semanticModel, whenFalse, cancellationToken))
					return false;
				if (obj.SkipParens().IsEquivalentTo(whenFalse.SkipParens(), true)) {
					diagnostic = Diagnostic.Create (
						descriptor,
						node.GetLocation ()
					);
					return true;
				}
			}
			return false;
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

		static bool CanBeNull(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
		{
			var info = semanticModel.GetTypeInfo(expression, cancellationToken);
			if (info.ConvertedType.IsReferenceType || info.ConvertedType.IsNullableType())
				return true;
			return false;
		}
	}
}