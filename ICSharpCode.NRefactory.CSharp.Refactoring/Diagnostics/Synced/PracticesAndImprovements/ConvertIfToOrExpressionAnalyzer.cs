//
// ConvertIfToOrExpressionAnalyzer.cs
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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ConvertIfToOrExpressionAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ConvertIfToOrExpressionAnalyzerID, 
			GettextCatalog.GetString("Convert 'if' to '||' expression"),
			GettextCatalog.GetString("{0}"), 
			DiagnosticAnalyzerCategories.PracticesAndImprovements, 
			DiagnosticSeverity.Info, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.ConvertIfToOrExpressionAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryGetDiagnostic(nodeContext, out diagnostic))
						nodeContext.ReportDiagnostic(diagnostic);
				},
				SyntaxKind.IfStatement
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			var node = nodeContext.Node as IfStatementSyntax;

			ExpressionSyntax target;
			SyntaxTriviaList assignmentTrailingTriviaList;
			if (MatchIfElseStatement(node, SyntaxKind.TrueLiteralExpression, out target, out assignmentTrailingTriviaList))
			{
				var varDeclaration = FindPreviousVarDeclaration(node);
				if (varDeclaration != null)
				{
					var targetIdentifier = target as IdentifierNameSyntax;
					if (targetIdentifier == null)
						return false;
					var declaredVarName = varDeclaration.Declaration.Variables.First().Identifier.Value;
					var assignedVarName = targetIdentifier.Identifier.Value;
					if (declaredVarName != assignedVarName)
						return false;
					if (!CheckTarget(targetIdentifier, node.Condition))
						return false;
					diagnostic = Diagnostic.Create (
						descriptor,
						node.IfKeyword.GetLocation (),
						"Convert to '||' expression"
					);
					return true;
				}
				else
				{
					if (!CheckTarget(target, node.Condition))
						return false;
					diagnostic = Diagnostic.Create (
						descriptor,
						node.IfKeyword.GetLocation (),
						"Replace with '|='"
					);
					return true;
				}
			}

			return false;
		}

		internal static bool MatchIfElseStatement(IfStatementSyntax ifStatement, SyntaxKind assignmentLiteralExpressionType, out ExpressionSyntax assignmentTarget, out SyntaxTriviaList assignmentTrailingTriviaList)
		{
			assignmentTarget = null;
			assignmentTrailingTriviaList = SyntaxFactory.TriviaList(SyntaxFactory.SyntaxTrivia(SyntaxKind.DisabledTextTrivia, ""));

			if (ifStatement.Else != null)
				return false;

			var trueExpression = ifStatement.Statement as ExpressionStatementSyntax;
			if (trueExpression != null)
			{
				return CheckForAssignmentOfLiteral(trueExpression, assignmentLiteralExpressionType, out assignmentTarget, out assignmentTrailingTriviaList);
			}

			var blockExpression = ifStatement.Statement as BlockSyntax;
			if (blockExpression != null)
			{
				if (blockExpression.Statements.Count != 1)
					return false;
				return CheckForAssignmentOfLiteral(blockExpression.Statements[0], assignmentLiteralExpressionType, out assignmentTarget, out assignmentTrailingTriviaList);
			}

			return false;
		}

		internal static bool CheckForAssignmentOfLiteral(StatementSyntax statement, SyntaxKind literalExpressionType, out ExpressionSyntax assignmentTarget, out SyntaxTriviaList assignmentTrailingTriviaList)
		{
			assignmentTarget = null;
			assignmentTrailingTriviaList = SyntaxFactory.TriviaList(SyntaxFactory.SyntaxTrivia(SyntaxKind.DisabledTextTrivia, ""));
			var expressionStatement = statement as ExpressionStatementSyntax;
			if (expressionStatement == null)
				return false;
			var assignmentExpression = expressionStatement.Expression as AssignmentExpressionSyntax;
			if (assignmentExpression == null)
				return false;
			assignmentTarget = assignmentExpression.Left as IdentifierNameSyntax;
			assignmentTrailingTriviaList = assignmentExpression.OperatorToken.TrailingTrivia;
			if (assignmentTarget == null)
				assignmentTarget = assignmentExpression.Left as MemberAccessExpressionSyntax;
			var rightAssignment = assignmentExpression.Right as LiteralExpressionSyntax;
			return (assignmentTarget != null) && (rightAssignment != null) && (rightAssignment.IsKind(literalExpressionType));
		}

		internal static LocalDeclarationStatementSyntax FindPreviousVarDeclaration(StatementSyntax statement)
		{
			var siblingStatements = statement.Parent.ChildNodes().OfType<StatementSyntax>();
			StatementSyntax lastSibling = null;
			foreach (var sibling in siblingStatements)
			{
				if (sibling == statement)
				{
					return lastSibling as LocalDeclarationStatementSyntax;
				}
				lastSibling = sibling;
			}

			return null;
		}

		internal static bool CheckTarget(ExpressionSyntax target, ExpressionSyntax expr)
		{
			if (target.IsKind(SyntaxKind.IdentifierName))
				return !expr.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(n => ((IdentifierNameSyntax)target).Identifier.ValueText == n.Identifier.ValueText);
			if (target.IsKind(SyntaxKind.SimpleMemberAccessExpression))
				return !expr.DescendantNodesAndSelf().Any(
						n =>
						{
							if (n.IsKind(SyntaxKind.IdentifierName))
								return ((MemberAccessExpressionSyntax)target).Expression.ToString() == ((IdentifierNameSyntax)n).Identifier.ValueText;
							if (n.IsKind(SyntaxKind.SimpleMemberAccessExpression))
								return ((MemberAccessExpressionSyntax)target).Expression.ToString() == ((MemberAccessExpressionSyntax)n).Expression.ToString();
							return false;
						}
					);
			return false;
		}

		internal static ExpressionSyntax AddParensToComplexExpression(ExpressionSyntax condition)
		{
			var binaryExpression = condition as BinaryExpressionSyntax;
			if (binaryExpression == null)
				return condition;

			if (binaryExpression.IsKind(SyntaxKind.LogicalOrExpression)
				|| binaryExpression.IsKind(SyntaxKind.LogicalAndExpression))
				return SyntaxFactory.ParenthesizedExpression(binaryExpression.SkipParens());

			return condition;
		}
	}
}