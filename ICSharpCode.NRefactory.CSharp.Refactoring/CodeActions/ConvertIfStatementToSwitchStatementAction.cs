// 
// ConvertIfToSwitchAction.cs
//  
// Author:
//       Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang <lightyang0@gmail.com>
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
using System.Threading;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Convert 'if' statement to 'switch' statement")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Convert 'if' to 'switch'")]
	public class ConvertIfStatementToSwitchStatementAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var node = root.FindNode(span) as IfStatementSyntax;

			if (node == null)
				return;

			var switchExpr = GetSwitchExpression(model, node.Condition);
			if (switchExpr == null)
				return;

			var switchSections = new List<SwitchSectionSyntax>();
			if (!CollectSwitchSections(switchSections, model, node, switchExpr))
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					span,
					DiagnosticSeverity.Info,
					"Convert to 'switch'",
					ct => {
						var switchStatement = SyntaxFactory.SwitchStatement(switchExpr, new SyntaxList<SwitchSectionSyntax>().AddRange(switchSections));
						return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(
							(SyntaxNode)node, switchStatement
							.WithLeadingTrivia(node.GetLeadingTrivia())
							.WithAdditionalAnnotations(Formatter.Annotation))));
					})
			);
		}

		internal static ExpressionSyntax GetSwitchExpression(SemanticModel context, ExpressionSyntax expr)
		{
			var binaryOp = expr as BinaryExpressionSyntax;
			if (binaryOp == null)
				return null;

			if (binaryOp.OperatorToken.IsKind(SyntaxKind.LogicalOrExpression))
				return GetSwitchExpression(context, binaryOp.Left);

			if (binaryOp.OperatorToken.IsKind(SyntaxKind.EqualsEqualsToken)) {
				ExpressionSyntax switchExpr = null;
				if (IsConstantExpression(context, binaryOp.Right))
					switchExpr = binaryOp.Left;
				if (IsConstantExpression(context, binaryOp.Left))
					switchExpr = binaryOp.Right;
				if (switchExpr != null && IsValidSwitchType(context.GetTypeInfo(switchExpr).Type))
					return switchExpr;
			}

			return null;
		}

		static bool IsConstantExpression(SemanticModel context, ExpressionSyntax expr)
		{
			if (expr is LiteralExpressionSyntax)
				return true;
			if (expr is DefaultExpressionSyntax)
				return true;
			return context.GetConstantValue(expr).HasValue;
		}

		static readonly SpecialType[] validTypes = {
			SpecialType.System_String, SpecialType.System_Boolean, SpecialType.System_Char,
			SpecialType.System_Byte, SpecialType.System_SByte,
			SpecialType.System_Int16, SpecialType.System_Int32, SpecialType.System_Int64,
			SpecialType.System_UInt16, SpecialType.System_UInt32, SpecialType.System_UInt64
		};

		static bool IsValidSwitchType(ITypeSymbol type)
		{
			if (type == null || type is IErrorTypeSymbol)
				return false;
			if (type.TypeKind == TypeKind.Enum)
				return true;

			if (type.IsNullableType()) {
				type = type.GetNullableUnderlyingType();
				if (type == null || type is IErrorTypeSymbol)
					return false;
			}
			return Array.IndexOf(validTypes, type.SpecialType) != -1;
		}

		internal static bool CollectSwitchSections(List<SwitchSectionSyntax> result, SemanticModel context,
										   IfStatementSyntax ifStatement, ExpressionSyntax switchExpr)
		{
			// if
			var labels = new List<SwitchLabelSyntax>();
			if (!CollectCaseLabels(labels, context, ifStatement.Condition, switchExpr))
				return false;
			var statements = new List<StatementSyntax>();
			CollectSwitchSectionStatements(statements, context, ifStatement.Statement);
			result.Add(SyntaxFactory.SwitchSection(new SyntaxList<SwitchLabelSyntax>().AddRange(labels), new SyntaxList<StatementSyntax>().AddRange(statements)));

			if (ifStatement.Statement.DescendantNodes().Any(n => n is BreakStatementSyntax))
				return false;

			if (ifStatement.Else == null)
				return true;

			// else if
			var falseStatement = ifStatement.Else.Statement as IfStatementSyntax;
			if (falseStatement != null)
				return CollectSwitchSections(result, context, falseStatement, switchExpr);

			if (ifStatement.Else.Statement.DescendantNodes().Any(n => n is BreakStatementSyntax))
				return false;
			// else (default label)
			labels = new List<SwitchLabelSyntax>();
			labels.Add(SyntaxFactory.DefaultSwitchLabel());
			statements = new List<StatementSyntax>();
			CollectSwitchSectionStatements(statements, context, ifStatement.Else.Statement);
			result.Add(SyntaxFactory.SwitchSection(new SyntaxList<SwitchLabelSyntax>().AddRange(labels), new SyntaxList<StatementSyntax>().AddRange(statements)));

			return true;
		}

		static bool CollectCaseLabels(List<SwitchLabelSyntax> result, SemanticModel context,
									   ExpressionSyntax condition, ExpressionSyntax switchExpr)
		{
			if (condition is ParenthesizedExpressionSyntax)
				return CollectCaseLabels(result, context, ((ParenthesizedExpressionSyntax)condition).Expression, switchExpr);

			var binaryOp = condition as BinaryExpressionSyntax;
			if (binaryOp == null)
				return false;

			if (binaryOp.IsKind(SyntaxKind.LogicalOrExpression))
				return CollectCaseLabels(result, context, binaryOp.Left, switchExpr) &&
					   CollectCaseLabels(result, context, binaryOp.Right, switchExpr);

			if (binaryOp.IsKind(SyntaxKind.EqualsExpression)) {
				if (switchExpr.IsEquivalentTo(binaryOp.Left, true)) {
					if (IsConstantExpression(context, binaryOp.Right)) {
						result.Add(SyntaxFactory.CaseSwitchLabel(binaryOp.Right));
						return true;
					}
				} else if (switchExpr.IsEquivalentTo(binaryOp.Right, true)) {
					if (IsConstantExpression(context, binaryOp.Left)) {
						result.Add(SyntaxFactory.CaseSwitchLabel(binaryOp.Left));
						return true;
					}
				}
			}

			return false;
		}

		static void CollectSwitchSectionStatements(List<StatementSyntax> result, SemanticModel context,
													StatementSyntax statement)
		{
			var blockStatement = statement as BlockSyntax;
			if (blockStatement != null)
				result.AddRange(blockStatement.Statements);
			else
				result.Add(statement);

			// add 'break;' at end if necessary
			var reachabilityAnalysis = context.AnalyzeControlFlow(statement);
			if (reachabilityAnalysis.EndPointIsReachable)
				result.Add(SyntaxFactory.BreakStatement());
		}
	}
}
