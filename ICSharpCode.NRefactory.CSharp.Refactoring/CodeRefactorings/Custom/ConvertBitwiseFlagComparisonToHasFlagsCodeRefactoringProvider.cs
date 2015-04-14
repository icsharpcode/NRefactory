//
// ConvertBitwiseFlagComparisonToHasFlagsAction.cs
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
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Replace bitwise flag comparison with call to 'Enum.HasFlag'")]
	public class ConvertBitwiseFlagComparisonToHasFlagsCodeRefactoringProvider : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
				return;
			var span = context.Span;
			if (!span.IsEmpty)
				return;
			var cancellationToken = context.CancellationToken;
			if (cancellationToken.IsCancellationRequested)
				return;
			var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			if (model.IsFromGeneratedCode())
				return;
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
			var token = root.FindToken(span.Start);
			var boP = token.Parent as BinaryExpressionSyntax;
			if (boP == null || !boP.OperatorToken.Span.Contains(span))
				return;

			ExpressionSyntax flagsExpression, targetExpression;
			bool testFlagset;
			if (!AnalyzeComparisonWithNull (boP, out flagsExpression, out targetExpression, out testFlagset) && !AnalyzeComparisonWithFlags (boP, out flagsExpression, out targetExpression, out testFlagset))
				return;

			if (!testFlagset && !flagsExpression.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax> ().All(bop => bop.IsKind(SyntaxKind.BitwiseOrExpression)))
				return;
			if (testFlagset && !flagsExpression.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax> ().All(bop => bop.IsKind(SyntaxKind.BitwiseAndExpression)))
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(token.Span, DiagnosticSeverity.Info, GettextCatalog.GetString ("To 'Enum.HasFlag'"), t2 => Task.FromResult(PerformAction (document, root, boP, flagsExpression, targetExpression, testFlagset)))
			);
		}

		Document PerformAction(Document document, SyntaxNode root, BinaryExpressionSyntax boP, ExpressionSyntax flagsExpression, ExpressionSyntax targetExpression, bool testFlagset)
		{
			var nodeToReplace = boP.SkipParens();

			var castExpr = BuildHasFlagExpression (targetExpression, flagsExpression);

			if (testFlagset) {
				castExpr = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, castExpr);
			}

			var newRoot = root.ReplaceNode((SyntaxNode)nodeToReplace, castExpr.WithAdditionalAnnotations(Formatter.Annotation));
			return document.WithSyntaxRoot(newRoot);
		}

		static ExpressionSyntax BuildHasFlagExpression (ExpressionSyntax target, ExpressionSyntax expr)
		{
			var bOp = expr as BinaryExpressionSyntax;
			if (bOp != null){
				if (bOp.IsKind(SyntaxKind.BitwiseOrExpression)) {
					return SyntaxFactory.BinaryExpression(
						SyntaxKind.BitwiseOrExpression,
						BuildHasFlagExpression(target, bOp.Left),
						BuildHasFlagExpression(target, bOp.Right)
					);
				}
			}

			var arguments = new SeparatedSyntaxList<ArgumentSyntax>();
			arguments = arguments.Add(SyntaxFactory.Argument(MakeFlatExpression (expr, SyntaxKind.BitwiseOrExpression))); 

			return SyntaxFactory.InvocationExpression(
				SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, target, SyntaxFactory.IdentifierName("HasFlag")),
				SyntaxFactory.ArgumentList(arguments)
			);
		}

		static void DecomposeBinaryOperator(BinaryExpressionSyntax binOp, out ExpressionSyntax flagsExpression, out ExpressionSyntax targetExpression)
		{
			targetExpression = binOp.Left.SkipParens();
			flagsExpression = binOp.Right.SkipParens();
		}

		static bool AnalyzeComparisonWithFlags (BinaryExpressionSyntax boP, out ExpressionSyntax flagsExpression, out ExpressionSyntax targetExpression, out bool testFlagset)
		{
			var left = boP.Left.SkipParens();
			var right = boP.Right.SkipParens();

			testFlagset = boP.Kind() != SyntaxKind.EqualsExpression;
			flagsExpression = targetExpression = null;

			BinaryExpressionSyntax primExp;

			if (left.IsKind(SyntaxKind.BitwiseAndExpression)) {
				primExp = left as BinaryExpressionSyntax;
				flagsExpression = right;
			} else {
				primExp = right as BinaryExpressionSyntax;
				flagsExpression = left;
			}

			if (primExp == null)
				return false;

			if (primExp.Left.IsEquivalentTo(flagsExpression)) {
				DecomposeBinaryOperator(primExp, out flagsExpression, out targetExpression);
				return true;
			}

			if (primExp.Right.IsEquivalentTo(flagsExpression)) {
				DecomposeBinaryOperator(primExp, out flagsExpression, out targetExpression);
				return true;
			}

			return false;
		}

		static bool AnalyzeComparisonWithNull (BinaryExpressionSyntax boP, out ExpressionSyntax flagsExpression, out ExpressionSyntax targetExpression, out bool testFlagset)
		{
			var left = boP.Left.SkipParens();
			var right = boP.Right.SkipParens();
			testFlagset = boP.Kind() == SyntaxKind.EqualsExpression;
			flagsExpression = targetExpression = null;

			ExpressionSyntax primExp;
			BinaryExpressionSyntax binOp;
			if (left.IsKind(SyntaxKind.NumericLiteralExpression)) {
				primExp = left;
				binOp = right as BinaryExpressionSyntax;
			} else {
				primExp = right;
				binOp = left as BinaryExpressionSyntax;
			}

			if (binOp == null) {
				return false;
			}

			DecomposeBinaryOperator(binOp, out flagsExpression, out targetExpression);
			return primExp != null && primExp.ToString() == "0";
		}

		internal static ExpressionSyntax MakeFlatExpression (ExpressionSyntax expr, SyntaxKind opType)
		{
			var bOp = expr as BinaryExpressionSyntax;
			if (bOp == null)
				return expr;
			return SyntaxFactory.BinaryExpression(opType,
				MakeFlatExpression(bOp.Left, opType),
				MakeFlatExpression(bOp.Right, opType)
			);
		}
	}
}

