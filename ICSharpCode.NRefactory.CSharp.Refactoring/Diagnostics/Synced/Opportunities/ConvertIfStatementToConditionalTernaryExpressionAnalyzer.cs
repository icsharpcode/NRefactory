//
// ConvertIfStatementToConditionalTernaryExpressionAnalyzer.cs
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
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ICSharpCode.NRefactory6.CSharp.CodeRefactorings;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ConvertIfStatementToConditionalTernaryExpressionAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ConvertIfStatementToConditionalTernaryExpressionAnalyzerID, 
			GettextCatalog.GetString("Convert 'if' to '?:'"),
			GettextCatalog.GetString("Convert to '?:' expression"), 
			DiagnosticAnalyzerCategories.Opportunities, 
			DiagnosticSeverity.Info, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.ConvertIfStatementToConditionalTernaryExpressionAnalyzerID)
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
				new SyntaxKind[] {  SyntaxKind.IfStatement }
			);
		}

		bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			var node = nodeContext.Node as IfStatementSyntax;
			var semanticModel = nodeContext.SemanticModel;
			var cancellationToken = nodeContext.CancellationToken;

			diagnostic = default(Diagnostic);
			ExpressionSyntax condition, target;
			AssignmentExpressionSyntax trueAssignment, falseAssignment;
			if (!ConvertIfStatementToConditionalTernaryExpressionCodeRefactoringProvider.ParseIfStatement(node, out condition, out target, out trueAssignment, out falseAssignment))
				return false;
			if (IsComplexCondition(condition) || IsComplexExpression(trueAssignment.Right) || IsComplexExpression(falseAssignment.Right))
				return false;

			diagnostic = Diagnostic.Create (
				descriptor,
				node.IfKeyword.GetLocation ()
			);
			return true;
		}

		public static bool IsComplexExpression(ExpressionSyntax expr)
		{
			var loc = expr.GetLocation().GetLineSpan();
			return loc.StartLinePosition.Line != loc.EndLinePosition.Line ||
				expr is ConditionalExpressionSyntax ||
				expr is BinaryExpressionSyntax;
		}

		public static bool IsComplexCondition(ExpressionSyntax expr)
		{
			var loc = expr.GetLocation().GetLineSpan();
			if (loc.StartLinePosition.Line != loc.EndLinePosition.Line)
				return true;

			if (expr is LiteralExpressionSyntax || expr is IdentifierNameSyntax || expr is MemberAccessExpressionSyntax || expr is InvocationExpressionSyntax)
				return false;

			var pexpr = expr as ParenthesizedExpressionSyntax;
			if (pexpr != null)
				return IsComplexCondition(pexpr.Expression);

			var uOp = expr as PrefixUnaryExpressionSyntax;
			if (uOp != null)
				return IsComplexCondition(uOp.Operand);

			var bop = expr as BinaryExpressionSyntax;
			if (bop == null)
				return true;
			return !(bop.IsKind(SyntaxKind.GreaterThanExpression) ||
				bop.IsKind(SyntaxKind.GreaterThanOrEqualExpression) ||
				bop.IsKind(SyntaxKind.EqualsExpression) ||
				bop.IsKind(SyntaxKind.NotEqualsExpression) ||
				bop.IsKind(SyntaxKind.LessThanExpression) ||
				bop.IsKind(SyntaxKind.LessThanOrEqualExpression));
		}
	}
}