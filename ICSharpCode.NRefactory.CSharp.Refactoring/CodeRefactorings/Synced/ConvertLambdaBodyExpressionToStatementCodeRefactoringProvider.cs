// 
// ConvertLambdaBodyExpressionToStatementAction.cs
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
using System.Security.Principal;

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Converts expression of lambda body to statement")]
	public class ConvertLambdaBodyExpressionToStatementCodeRefactoringProvider : CodeRefactoringProvider
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
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);

			var token = root.FindToken(span.Start);
			if (!token.IsKind(SyntaxKind.EqualsGreaterThanToken))
				return;
			var node = token.Parent;
			if (!node.IsKind(SyntaxKind.ParenthesizedLambdaExpression) && !node.IsKind(SyntaxKind.SimpleLambdaExpression))
				return;

			ExpressionSyntax bodyExpr = null;
			if (node.IsKind(SyntaxKind.ParenthesizedLambdaExpression)) {
				bodyExpr = ((ParenthesizedLambdaExpressionSyntax)node).Body as ExpressionSyntax;
			} else {
				bodyExpr = ((SimpleLambdaExpressionSyntax)node).Body as ExpressionSyntax;
			}		
			if (bodyExpr == null)
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					token.Span,
					DiagnosticSeverity.Info,
					GettextCatalog.GetString ("To lambda statement"),
					t2 => {
						SyntaxNode lambdaExpression;
						if (node.IsKind(SyntaxKind.ParenthesizedLambdaExpression)) {
							lambdaExpression = ((ParenthesizedLambdaExpressionSyntax)node).WithBody(SyntaxFactory.Block(
								RequireReturnStatement (model, node) ? (StatementSyntax)SyntaxFactory.ReturnStatement (bodyExpr) : SyntaxFactory.ExpressionStatement(bodyExpr)
							));
						} else {
							lambdaExpression = ((SimpleLambdaExpressionSyntax)node).WithBody(SyntaxFactory.Block(
								RequireReturnStatement (model, node) ? (StatementSyntax)SyntaxFactory.ReturnStatement (bodyExpr) : SyntaxFactory.ExpressionStatement(bodyExpr)
							));
						}

						var newRoot = root.ReplaceNode((SyntaxNode)node, lambdaExpression.WithAdditionalAnnotations(Formatter.Annotation));
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
			);
		}

		internal static bool RequireReturnStatement (SemanticModel model, SyntaxNode lambda)
		{
			var typeInfo = model.GetTypeInfo(lambda);
			var type = typeInfo.ConvertedType ?? typeInfo.Type;
			if (type == null || !type.IsDelegateType())
				return false;
			var returnType = type.GetDelegateInvokeMethod().GetReturnType();
			return returnType != null && returnType.SpecialType != SpecialType.System_Void;
		}
	}
}
