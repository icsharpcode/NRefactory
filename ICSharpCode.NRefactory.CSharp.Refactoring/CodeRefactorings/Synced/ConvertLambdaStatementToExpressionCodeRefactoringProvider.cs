// 
// ConvertLambdaBodyStatementToExpressionAction.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
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
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Converts statement of lambda body to expression")]
	public class ConvertLambdaStatementToExpressionCodeRefactoringProvider : CodeRefactoringProvider
	{
		internal static bool TryGetConvertableExpression(SyntaxNode body, out BlockSyntax blockStatement, out ExpressionSyntax expr)
		{
			expr = null;
			blockStatement = body as BlockSyntax;
			if (blockStatement == null || blockStatement.Statements.Count > 1)
				return false;
			var returnStatement = blockStatement.Statements.FirstOrDefault() as ReturnStatementSyntax;
			if (returnStatement != null) {
				expr = returnStatement.Expression;
			} else {
				var exprStatement = blockStatement.Statements.FirstOrDefault() as ExpressionStatementSyntax;
				if (exprStatement == null)
					return false;
				expr = exprStatement.Expression;
			}
			return true;
		}

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
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);

			var token = root.FindToken(span.Start);
			if (!token.IsKind(SyntaxKind.EqualsGreaterThanToken))
				return;
			var node = token.Parent;
			if (!node.IsKind(SyntaxKind.ParenthesizedLambdaExpression) && !node.IsKind(SyntaxKind.SimpleLambdaExpression))
				return;

			CSharpSyntaxNode body;
			if (node.IsKind(SyntaxKind.ParenthesizedLambdaExpression)) {
				body = ((ParenthesizedLambdaExpressionSyntax)node).Body;
			} else {
				body = ((SimpleLambdaExpressionSyntax)node).Body;
			}		
			if (body == null)
				return;

			BlockSyntax blockStatement;
			ExpressionSyntax expr;
			if (!TryGetConvertableExpression(body, out blockStatement, out expr))
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					token.Span,
					DiagnosticSeverity.Info,
					GettextCatalog.GetString ("To lambda expression"),
					t2 => {
						SyntaxNode lambdaExpression;
						if (node.IsKind(SyntaxKind.ParenthesizedLambdaExpression)) {
							lambdaExpression = ((ParenthesizedLambdaExpressionSyntax)node).WithBody(expr);
						} else {
							lambdaExpression = ((SimpleLambdaExpressionSyntax)node).WithBody(expr);
						}

						var newRoot = root.ReplaceNode((SyntaxNode)node, lambdaExpression.WithAdditionalAnnotations(Formatter.Annotation));
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
			);
		}
	}
}
