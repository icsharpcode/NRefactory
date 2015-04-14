//
// FlipEqualsQualifierAndArgumentAction.cs
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

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Swap 'Equals' target and argument")]
	public class FlipEqualsTargetAndArgumentCodeRefactoringProvider : CodeRefactoringProvider
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

			var node = root.FindNode(span) as IdentifierNameSyntax;
			if (node == null || !node.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
				return;
			var memberAccess = node.Parent as MemberAccessExpressionSyntax;
			var invocation = node.Parent.Parent as InvocationExpressionSyntax;
			if (invocation == null || invocation.ArgumentList.Arguments.Count != 1 || invocation.ArgumentList.Arguments[0].Expression.IsKind(SyntaxKind.NullLiteralExpression))
				return;

			var invocationRR = model.GetSymbolInfo(invocation);
			if (invocationRR.Symbol == null)
				return;

			var method = invocationRR.Symbol as IMethodSymbol;
			if (method == null)
				return;

			if (method.Name != "Equals" || method.IsStatic || method.ReturnType.SpecialType != SpecialType.System_Boolean)
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Info, 
					GettextCatalog.GetString ("Flip 'Equals' target and argument"), 
					t2 => {
						var newRoot = root.ReplaceNode((SyntaxNode)invocation, 
							invocation
							.WithExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, AddParensIfRequired (invocation.ArgumentList.Arguments[0].Expression), memberAccess.Name))
							.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(new [] { SyntaxFactory.Argument(memberAccess.Expression.SkipParens()) })))
							.WithAdditionalAnnotations(Formatter.Annotation)
						);
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				) 
			);
		}

		internal static ExpressionSyntax AddParensIfRequired(ExpressionSyntax expression)
		{
			if ((expression is BinaryExpressionSyntax) ||
				(expression is PostfixUnaryExpressionSyntax) ||
				(expression is PrefixUnaryExpressionSyntax) ||
				(expression is AssignmentExpressionSyntax) ||
				(expression is CastExpressionSyntax) ||
				(expression is ParenthesizedLambdaExpressionSyntax) ||
				(expression is SimpleLambdaExpressionSyntax) ||
				(expression is ConditionalExpressionSyntax)) {
				return SyntaxFactory.ParenthesizedExpression(expression);
			}

			return expression;
		}
	}
}