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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ConvertIfToOrExpression")]
	public class ConvertIfToOrExpressionAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId = "ConvertIfToOrExpressionAnalyzer.;
		const string Description = "Convert 'if' to '||' expression";
		const string MessageFormat = "{0}";
		const string Category = DiagnosticAnalyzerCategories.PracticesAndImprovements;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "'if' statement can be re-written as '||' expression");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
		{
			get
			{
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
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

		class GatherVisitor : GatherVisitorBase<ConvertIfToOrExpressionAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitIfStatement(IfStatementSyntax node)
			{
				base.VisitIfStatement(node);

				ExpressionSyntax target;
				SyntaxTriviaList assignmentTrailingTriviaList;
				if (MatchIfElseStatement(node, SyntaxKind.TrueLiteralExpression, out target, out assignmentTrailingTriviaList))
				{
					var varDeclaration = FindPreviousVarDeclaration(node);
					if (varDeclaration != null)
					{
						var targetIdentifier = target as IdentifierNameSyntax;
						if (targetIdentifier == null)
							return;
						var declaredVarName = varDeclaration.Declaration.Variables.First().Identifier.Value;
						var assignedVarName = targetIdentifier.Identifier.Value;
						if (declaredVarName != assignedVarName)
							return;
						if (!CheckTarget(targetIdentifier, node.Condition))
							return;
						AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.IfKeyword.GetLocation(), "Convert to '||' expression"));
					}
					else
					{
						if (!CheckTarget(target, node.Condition))
							return;
						AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.IfKeyword.GetLocation(), "Replace with '|='"));
					}
				}
			}
		}
	}

	[ExportCodeFixProvider(ConvertIfToOrExpressionAnalyzer.DiagnosticId, LanguageNames.CSharp)]
	public class ConvertIfToOrExpressionFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ConvertIfToOrExpressionAnalyzer.DiagnosticId;
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
			foreach (var diagnostic in diagnostics)
			{
				var node = root.FindNode(diagnostic.Location.SourceSpan) as IfStatementSyntax;
				ExpressionSyntax target;
				SyntaxTriviaList assignmentTrailingTriviaList;
				ConvertIfToOrExpressionAnalyzer.MatchIfElseStatement(node, SyntaxKind.TrueLiteralExpression, out target, out assignmentTrailingTriviaList);
				SyntaxNode newRoot = null;
				var varDeclaration = ConvertIfToOrExpressionAnalyzer.FindPreviousVarDeclaration(node);
				if (varDeclaration != null)
				{
					var varDeclarator = varDeclaration.Declaration.Variables[0];
					newRoot = root.ReplaceNodes(new SyntaxNode[] { varDeclaration, node }, (arg, arg2) =>
					{
						if (arg is LocalDeclarationStatementSyntax)
							return SyntaxFactory.LocalDeclarationStatement(
									SyntaxFactory.VariableDeclaration(varDeclaration.Declaration.Type,
										SyntaxFactory.SeparatedList(
											new[] {
												SyntaxFactory.VariableDeclarator(varDeclarator.Identifier.ValueText)
													.WithInitializer(
														SyntaxFactory.EqualsValueClause(
															SyntaxFactory.BinaryExpression(SyntaxKind.LogicalOrExpression, ConvertIfToOrExpressionAnalyzer.AddParensToComplexExpression(varDeclarator.Initializer.Value), ConvertIfToOrExpressionAnalyzer.AddParensToComplexExpression(node.Condition)))
																.WithAdditionalAnnotations(Formatter.Annotation)
													)
											}
										))
								).WithLeadingTrivia(varDeclaration.GetLeadingTrivia()).WithTrailingTrivia(node.GetTrailingTrivia());
						return null;
					});
				}
				else
				{
					newRoot = root.ReplaceNode((SyntaxNode)node,
						SyntaxFactory.ExpressionStatement(
							SyntaxFactory.AssignmentExpression(
								SyntaxKind.OrAssignmentExpression,
								ConvertIfToOrExpressionAnalyzer.AddParensToComplexExpression(target),
								ConvertIfToOrExpressionAnalyzer.AddParensToComplexExpression(node.Condition).WithAdditionalAnnotations(Formatter.Annotation)
							)
						).WithLeadingTrivia(node.GetLeadingTrivia()).WithTrailingTrivia(node.GetTrailingTrivia()));
				}

				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, diagnostic.GetMessage(), document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}