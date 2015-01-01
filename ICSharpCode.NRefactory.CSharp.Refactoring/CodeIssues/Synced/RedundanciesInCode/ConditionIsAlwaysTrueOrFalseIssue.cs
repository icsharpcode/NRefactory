//
// ConditionIsAlwaysTrueOrFalseIssue.cs
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
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.ComponentModel;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ConditionIsAlwaysTrueOrFalse")]
	[Description("Condition is always true or false")]
	public class ConditionIsAlwaysTrueOrFalseIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticIdTrue  = "ConditionIsAlwaysTrueOrFalseIssue.True";
		internal const string DiagnosticIdFalse = "ConditionIsAlwaysTrueOrFalseIssue.False";
		const string Category               = IssueCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule1 = new DiagnosticDescriptor (DiagnosticIdTrue, "Value of the expression is always 'true'", "Expression is always 'true'", Category, DiagnosticSeverity.Warning, true, "Expression is always 'true' or always 'false'");
		static readonly DiagnosticDescriptor Rule2 = new DiagnosticDescriptor (DiagnosticIdFalse, "Value of the expression is always 'false'", "Expression is always 'false'", Category, DiagnosticSeverity.Warning, true, "Expression is always 'true' or always 'false'");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule1, Rule2);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<ConditionIsAlwaysTrueOrFalseIssue>
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
					AddIssue(Diagnostic.Create(!binaryOperatorExpression.IsKind(SyntaxKind.EqualsExpression) ? Rule1 : Rule2, binaryOperatorExpression.GetLocation()));
					return true;
				}
				return false;
			}

			bool CheckConstant(SyntaxNode expr)
			{
				var rr = semanticModel.GetConstantValue(expr);
				if (rr.HasValue && rr.Value is bool) {
					var result = (bool)rr.Value;
					AddIssue(Diagnostic.Create(result ? Rule1 : Rule2, expr.GetLocation()));
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

	[ExportCodeFixProvider("Value of the expression can be determined at compile time", LanguageNames.CSharp)]
	public class ConditionIsAlwaysTrueOrFalseFixProvider : NRefactoryCodeFixProvider
	{
		#region ICodeFixProvider implementation

		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ConditionIsAlwaysTrueOrFalseIssue.DiagnosticIdTrue;
			yield return ConditionIsAlwaysTrueOrFalseIssue.DiagnosticIdFalse;
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
				var node = root.FindNode(diagnostic.Location.SourceSpan);
				var newRoot = root.ReplaceNode(node,
					SyntaxFactory.LiteralExpression(diagnostic.Id == ConditionIsAlwaysTrueOrFalseIssue.DiagnosticIdTrue ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression) 
					.WithLeadingTrivia(node.GetLeadingTrivia())
					.WithTrailingTrivia(node.GetTrailingTrivia()));
				context.RegisterFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, diagnostic.Id == ConditionIsAlwaysTrueOrFalseIssue.DiagnosticIdTrue ? "Replace with 'true'" : "Replace with 'false'", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
		#endregion
	}
}