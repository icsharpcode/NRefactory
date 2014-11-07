//
// UseAsAndNullCheckAction.cs
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
	[NRefactoryCodeRefactoringProvider(Description = "Converts a 'is' into an 'as' and null check")]
	[ExportCodeRefactoringProvider("Use 'as' and null check", LanguageNames.CSharp)]
	public class UseAsAndNullCheckAction : CodeRefactoringProvider
	{
		public override async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			var isExpression = root.FindNode(span) as BinaryExpressionSyntax;

			if (!isExpression.IsKind(SyntaxKind.IsExpression) || !isExpression.OperatorToken.Span.Contains(span))
				return Enumerable.Empty<CodeAction>();

			var ifElseStatement = isExpression.Parent.AncestorsAndSelf().FirstOrDefault(e => !(e is ParenthesizedExpressionSyntax) && !(e is BinaryExpressionSyntax)) as IfStatementSyntax;
			var expr = isExpression.Parent as ExpressionSyntax;
			if (expr != null) {
				var p = expr.AncestorsAndSelf().FirstOrDefault(e => !(e is ParenthesizedExpressionSyntax));
				if (p.IsKind(SyntaxKind.LogicalNotExpression)) {
					ifElseStatement = p.Parent as IfStatementSyntax;
				}
			}
			if (ifElseStatement == null)
				return Enumerable.Empty<CodeAction>();

			int foundCasts;
			var action = ScanIfElse(model, document, root, ifElseStatement, isExpression, out foundCasts);
			if (action != null)
				return action;

			return Enumerable.Empty<CodeAction>();
		}

		static bool IsCast(SemanticModel model, SyntaxNode n, ITypeSymbol type)
		{
			var expr = n as ExpressionSyntax;
			if (expr == null)
				return false;
			expr = expr.SkipParens();
			var castExpr = expr as CastExpressionSyntax;
			if (castExpr != null) {
				return model.GetTypeInfo(castExpr.Type).Type == type;
			}
			var binExpr = expr as BinaryExpressionSyntax;
			if (binExpr != null) {
				return binExpr.IsKind(SyntaxKind.AsExpression) && model.GetTypeInfo(binExpr.Right).Type == type;
			}
			return false;
		}

		internal static IEnumerable<CodeAction> ScanIfElse (SemanticModel ctx, Document document, SyntaxNode root, IfStatementSyntax ifElseStatement, BinaryExpressionSyntax isExpression, out int foundCastCount)
		{
			foundCastCount = 0;

			var innerCondition = ifElseStatement.Condition.SkipParens();
			if (innerCondition != null && innerCondition.IsKind(SyntaxKind.LogicalNotExpression)) {
				var c2 = ((PrefixUnaryExpressionSyntax)innerCondition).Operand.SkipParens();
				if (c2.IsKind(SyntaxKind.IsExpression)) {
					return HandleNegatedCase(ctx, document, root, ifElseStatement, ifElseStatement.Condition, isExpression, out foundCastCount);
				}
				return null;
			}

			var castToType       = isExpression.Right;
			var embeddedStatment = ifElseStatement.Statement;

			var rr = ctx.GetTypeInfo(castToType);
			if (rr.Type == null || !rr.Type.IsReferenceType)
				return null;
			var foundCasts = embeddedStatment.DescendantNodesAndSelf(n => !IsCast(ctx, n, rr.Type)).Where(arg => IsCast(ctx, arg, rr.Type)).ToList();
			foundCasts.AddRange(ifElseStatement.Condition.DescendantNodesAndSelf(n => !IsCast(ctx, n, rr.Type)).Where(arg => arg.SpanStart > isExpression.Span.End && IsCast(ctx, arg, rr.Type)));
			foundCastCount = foundCasts.Count;

			return new[] { 
				CodeActionFactory.Create(
					isExpression.Span, 
					DiagnosticSeverity.Info, 
					"Use 'as' and check for null",
					t2 => {
						var varName = CreateBackingStoreAction.GetNameProposal(CreateMethodDeclarationAction.GuessNameFromType(rr.Type), ctx, isExpression);

						var varDec = SyntaxFactory.LocalDeclarationStatement(
							SyntaxFactory.VariableDeclaration(
								SyntaxFactory.ParseTypeName("var"),
								SyntaxFactory.SeparatedList(new [] {
									SyntaxFactory.VariableDeclarator(varName)
										.WithInitializer(SyntaxFactory.EqualsValueClause(
											SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, isExpression.Left, isExpression.Right)
										))
								})
							));
						var outerIs = isExpression.AncestorsAndSelf().FirstOrDefault(e => !(e.Parent is ParenthesizedExpressionSyntax));
						var binaryOperatorExpression = SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, SyntaxFactory.IdentifierName(varName), SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
						SyntaxNode newRoot;
						if (IsEmbeddedStatement(ifElseStatement)) {
							foundCasts = ifElseStatement.DescendantNodesAndSelf(n => !IsCast(ctx, n, rr.Type)).Where(arg => IsCast(ctx, arg, rr.Type)).ToList();
							var newIf = ifElseStatement.TrackNodes(foundCasts.Concat(new [] { outerIs }));

							newIf = newIf.ReplaceNode(newIf.GetCurrentNode(outerIs), binaryOperatorExpression.WithAdditionalAnnotations(Formatter.Annotation));

							foreach (var c in foundCasts) {
								newIf = newIf.ReplaceNode(newIf.GetCurrentNode(c), SyntaxFactory.IdentifierName(varName).WithAdditionalAnnotations(Formatter.Annotation));
							}

							var block = SyntaxFactory.Block(new StatementSyntax[] {
								varDec,
								newIf
							});
							newRoot = root.ReplaceNode(ifElseStatement, block.WithAdditionalAnnotations(Formatter.Annotation));
						} else {
							newRoot = root.TrackNodes(foundCasts.Concat(new SyntaxNode[] { ifElseStatement, outerIs }) );
							newRoot = newRoot.InsertNodesBefore(newRoot.GetCurrentNode(ifElseStatement), new [] { varDec.WithAdditionalAnnotations(Formatter.Annotation) });
							newRoot = newRoot.ReplaceNode(newRoot.GetCurrentNode(outerIs), binaryOperatorExpression.WithAdditionalAnnotations(Formatter.Annotation));
							foreach (var c in foundCasts) {
								newRoot = newRoot.ReplaceNode(newRoot.GetCurrentNode(c), SyntaxFactory.IdentifierName(varName).WithAdditionalAnnotations(Formatter.Annotation));
							}
						}

						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				) 
			};
		}

		internal static bool IsEmbeddedStatement(SyntaxNode stmt)
		{
			return !(stmt.Parent is BlockSyntax);
		}

		internal static IEnumerable<CodeAction> HandleNegatedCase (SemanticModel ctx, Document document, SyntaxNode root, IfStatementSyntax ifElseStatement, ExpressionSyntax c2, BinaryExpressionSyntax isExpression, out int foundCastCount)
		{
			foundCastCount = 0;

			var condition = isExpression;
			var castToType       = isExpression.Right;
			var embeddedStatment = ifElseStatement.Statement;

			var rr = ctx.GetTypeInfo(castToType);
			if (rr.Type == null || !rr.Type.IsReferenceType)
				return null;

			List<SyntaxNode> foundCasts;

			SyntaxNode searchStmt = embeddedStatment;
			if (IsControlFlowChangingStatement(searchStmt)) {
				searchStmt = ifElseStatement.Parent;
				foundCasts = searchStmt.DescendantNodesAndSelf(n => !IsCast(ctx, n, rr.Type)).Where(arg => arg.SpanStart >= ifElseStatement.SpanStart && IsCast(ctx, arg, rr.Type)).ToList();
				foundCasts.AddRange(ifElseStatement.Condition.DescendantNodesAndSelf(n => !IsCast(ctx, n, rr.Type)).Where(arg => arg.SpanStart > isExpression.Span.End && IsCast(ctx, arg, rr.Type)));
			} else {
				foundCasts = new List<SyntaxNode>();
			}

			foundCastCount = foundCasts.Count;

			return new[] { 
				CodeActionFactory.Create(
					isExpression.Span, 
					DiagnosticSeverity.Info, 
					"Use 'as' and check for null",
					t2 => {
						var varName = CreateBackingStoreAction.GetNameProposal(CreateMethodDeclarationAction.GuessNameFromType(rr.Type), ctx, condition);

						var varDec = SyntaxFactory.LocalDeclarationStatement(
							SyntaxFactory.VariableDeclaration(
								SyntaxFactory.ParseTypeName("var"),
								SyntaxFactory.SeparatedList(new [] {
									SyntaxFactory.VariableDeclarator(varName)
										.WithInitializer(SyntaxFactory.EqualsValueClause(
											SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, condition.Left, condition.Right)
										))
									})
							));
						var outerIs = isExpression.AncestorsAndSelf().FirstOrDefault(e => !(e.Parent is ParenthesizedExpressionSyntax));
						var binaryOperatorExpression = SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, SyntaxFactory.IdentifierName(varName), SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
						SyntaxNode newRoot;
						if (IsEmbeddedStatement(ifElseStatement)) {
							var newIf = ifElseStatement.ReplaceNode(c2, binaryOperatorExpression.WithAdditionalAnnotations(Formatter.Annotation));

							foreach (var c in foundCasts) {
								newIf = newIf.ReplaceNode(newIf.GetCurrentNode(c), SyntaxFactory.IdentifierName(varName).WithAdditionalAnnotations(Formatter.Annotation));
							}

							var block = SyntaxFactory.Block(new StatementSyntax[] {
								varDec,
								newIf
							});

							newRoot = root.ReplaceNode(ifElseStatement, block.WithAdditionalAnnotations(Formatter.Annotation));
						} else {
							newRoot = root.TrackNodes(foundCasts.Concat(new SyntaxNode[] { ifElseStatement, c2 }) );
							newRoot = newRoot.InsertNodesBefore(newRoot.GetCurrentNode(ifElseStatement), new [] { varDec.WithAdditionalAnnotations(Formatter.Annotation) });
							newRoot = newRoot.ReplaceNode(newRoot.GetCurrentNode(c2), binaryOperatorExpression.WithAdditionalAnnotations(Formatter.Annotation));
							foreach (var c in foundCasts) {
								newRoot = newRoot.ReplaceNode(newRoot.GetCurrentNode(c), SyntaxFactory.IdentifierName(varName).WithAdditionalAnnotations(Formatter.Annotation));
							}
						}

						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				) 
			};
		}

		static bool IsControlFlowChangingStatement(SyntaxNode searchStmt)
		{
			if (searchStmt is ReturnStatementSyntax || searchStmt is BreakStatementSyntax || searchStmt is ContinueStatementSyntax || searchStmt is GotoStatementSyntax)
				return true;
			var block = searchStmt as BlockSyntax;
			if (block != null) {
				return IsControlFlowChangingStatement(block.Statements.LastOrDefault());
			}
			return false;
		}
	}
}
