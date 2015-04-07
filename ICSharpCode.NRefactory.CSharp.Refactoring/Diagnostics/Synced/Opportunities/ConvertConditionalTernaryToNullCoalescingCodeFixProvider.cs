//
// ConvertConditionalTernaryToNullCoalescingCodeFixProvider.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class ConvertConditionalTernaryToNullCoalescingCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds {
			get {
				return ImmutableArray.Create (NRefactoryDiagnosticIDs.ConvertConditionalTernaryToNullCoalescingAnalyzerID);
			}
		}

		public override FixAllProvider GetFixAllProvider ()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public async override Task RegisterCodeFixesAsync (CodeFixContext context)
		{
			var document = context.Document;
			var cancellationToken = context.CancellationToken;
			var span = context.Span;
			var diagnostics = context.Diagnostics;
			var root = await document.GetSyntaxRootAsync (cancellationToken);
			var model = await document.GetSemanticModelAsync (cancellationToken);
			var diagnostic = diagnostics.First ();
			var node = root.FindNode (context.Span) as ConditionalExpressionSyntax;
			if (node == null)
				return;
			context.RegisterCodeFix (CodeActionFactory.Create (node.Span, diagnostic.Severity, "Replace '?:'  operator with '??", token => {
				ExpressionSyntax a, other;
				if (node.Condition.SkipParens ().IsKind (SyntaxKind.EqualsExpression)) {
					a = node.WhenFalse;
					other = node.WhenTrue;
				} else {
					other = node.WhenFalse;
					a = node.WhenTrue;
				}

				if (node.Condition.SkipParens ().IsKind (SyntaxKind.EqualsExpression)) {
					var castExpression = other as CastExpressionSyntax;
					if (castExpression != null) {
						a = SyntaxFactory.CastExpression (castExpression.Type, a);
						other = castExpression.Expression;
					}
				}

				a = UnpackNullableValueAccess (model, a, token);

				ExpressionSyntax newNode = SyntaxFactory.BinaryExpression (SyntaxKind.CoalesceExpression, a, other);

				var newRoot = root.ReplaceNode ((SyntaxNode)node, newNode.WithLeadingTrivia (node.GetLeadingTrivia ()).WithAdditionalAnnotations (Formatter.Annotation));
				return Task.FromResult (document.WithSyntaxRoot (newRoot));
			}), diagnostic);
		}

		internal static ExpressionSyntax UnpackNullableValueAccess (SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
		{
			var expr = expression.SkipParens ();
			if (!expr.IsKind (SyntaxKind.SimpleMemberAccessExpression))
				return expression;
			var info = semanticModel.GetTypeInfo (((MemberAccessExpressionSyntax)expr).Expression, cancellationToken);
			if (!info.ConvertedType.IsNullableType ())
				return expression;
			return ((MemberAccessExpressionSyntax)expr).Expression;
		}
	}
}