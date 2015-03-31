//
// ConditionIsAlwaysTrueOrFalseAnalyzer.cs
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
using System.Linq;
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
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ConditionIsAlwaysTrueOrFalse")]
	public class ConditionIsAlwaysTrueOrFalseAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticIdTrue  = "ConditionIsAlwaysTrueOrFalseAnalyzer.True";
		internal const string DiagnosticIdFalse = "ConditionIsAlwaysTrueOrFalseAnalyzer.False";
		const string Category               = DiagnosticAnalyzerCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule1 = new DiagnosticDescriptor (DiagnosticIdTrue, "Expression is always 'true'", "Value of the expression is always 'true'", Category, DiagnosticSeverity.Warning, true, "Expression is always 'true' or always 'false'");
		static readonly DiagnosticDescriptor Rule2 = new DiagnosticDescriptor (DiagnosticIdFalse, "Expression is always 'false'", "Value of the expression is always 'false'", Category, DiagnosticSeverity.Warning, true, "Expression is always 'true' or always 'false'");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule1, Rule2);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<ConditionIsAlwaysTrueOrFalseAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitBinaryExpression(BinaryExpressionSyntax node)
			{
				base.VisitBinaryExpression(node);
				if (CheckConstant(node))
					return;

				if (node.Left.SkipParens().IsKind(SyntaxKind.NullLiteralExpression)) {
					if (CheckNullComparison(node, node.Right, node.Left))
						return;
				} else if (node.Right.SkipParens().IsKind(SyntaxKind.NullLiteralExpression)) {
					if (CheckNullComparison(node, node.Left, node.Right))
						return;
				}
			}

			bool CheckNullComparison(BinaryExpressionSyntax binaryOperatorExpression, ExpressionSyntax right, ExpressionSyntax nullNode)
			{
				if (!binaryOperatorExpression.IsKind(SyntaxKind.EqualsExpression) && !binaryOperatorExpression.IsKind(SyntaxKind.NotEqualsExpression))
					return false;
				// note null == null is checked by similiar expression comparison.
				var expr = right.SkipParens();

				var rr = semanticModel.GetTypeInfo(expr);
				if (rr.Type == null)
					return false;
				var returnType = rr.Type;
				if (returnType != null && returnType.IsValueType) {
					// nullable check
					if (returnType.IsNullableType())
						return false;

					var conversion = semanticModel.GetConversion(nullNode);
					if (conversion.IsUserDefined)
						return false;
					// check for user operators
					foreach (IMethodSymbol op in returnType.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.UserDefinedOperator && m.Parameters.Length == 2)) {
						if (op.Parameters[0].Type.IsReferenceType == false && op.Parameters[1].Type.IsReferenceType == false)
							continue;
						if (binaryOperatorExpression.IsKind(SyntaxKind.EqualsExpression) && op.Name == "op_Equality")
							return false;
						if (binaryOperatorExpression.IsKind(SyntaxKind.NotEqualsExpression) && op.Name == "op_Inequality")
							return false;
					}
					AddDiagnosticAnalyzer(Diagnostic.Create(!binaryOperatorExpression.IsKind(SyntaxKind.EqualsExpression) ? Rule1 : Rule2, binaryOperatorExpression.GetLocation()));
					return true;
				}
				return false;
			}

			bool CheckConstant(SyntaxNode expr)
			{
				var rr = semanticModel.GetConstantValue(expr);
				if (rr.HasValue && rr.Value is bool) {
					var result = (bool)rr.Value;
					AddDiagnosticAnalyzer(Diagnostic.Create(result ? Rule1 : Rule2, expr.GetLocation()));
					return true;
				}
				return false;
			}

			public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
			{
				base.VisitPrefixUnaryExpression(node);
				CheckConstant(node);
			}
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class ConditionIsAlwaysTrueOrFalseFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ConditionIsAlwaysTrueOrFalseAnalyzer.DiagnosticIdTrue;
			yield return ConditionIsAlwaysTrueOrFalseAnalyzer.DiagnosticIdFalse;
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
			var diagnostic = diagnostics.First ();
			var node = root.FindNode(context.Span);
			var newRoot = root.ReplaceNode(node,
				SyntaxFactory.LiteralExpression(diagnostic.Id == ConditionIsAlwaysTrueOrFalseAnalyzer.DiagnosticIdTrue ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression) 
				.WithLeadingTrivia(node.GetLeadingTrivia())
				.WithTrailingTrivia(node.GetTrailingTrivia()));
			context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, diagnostic.Id == ConditionIsAlwaysTrueOrFalseAnalyzer.DiagnosticIdTrue ? "Replace with 'true'" : "Replace with 'false'", document.WithSyntaxRoot(newRoot)), diagnostic);
		}
	}
}