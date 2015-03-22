//
// ConvertLambdaToDelegateAction.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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
	[NRefactoryCodeRefactoringProvider(Description = "Converts a lambda to an anonymous method")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Convert lambda to anonymous method")]
	public class ConvertLambdaToAnonymousMethodCodeRefactoringProvider : CodeRefactoringProvider
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

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					token.Span,
					DiagnosticSeverity.Info,
					"To anonymous method",
					t2 => {
						var parameters = new List<ParameterSyntax> ();

						CSharpSyntaxNode bodyExpr;
						if (node.IsKind(SyntaxKind.ParenthesizedLambdaExpression)) {
							var ple = (ParenthesizedLambdaExpressionSyntax)node;
							parameters.AddRange(ConvertParameters(model, node, ple.ParameterList.Parameters));
							bodyExpr = ple.Body;
						} else {
							var sle = ((SimpleLambdaExpressionSyntax)node);
							parameters.AddRange(ConvertParameters(model, node, new []  { sle.Parameter }));
							bodyExpr = sle.Body;
						}

						if (ConvertLambdaBodyExpressionToStatementCodeRefactoringProvider.RequireReturnStatement(model, node)) {
							bodyExpr = SyntaxFactory.Block(SyntaxFactory.ReturnStatement(bodyExpr as ExpressionSyntax));
						}
						var ame = SyntaxFactory.AnonymousMethodExpression(
							parameters.Count == 0 ? null : SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)),
							bodyExpr as BlockSyntax ?? SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(bodyExpr as ExpressionSyntax))
						);

						var newRoot = root.ReplaceNode((SyntaxNode)node, ame.WithAdditionalAnnotations(Formatter.Annotation));
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
			);
		}

		static IEnumerable<ParameterSyntax> ConvertParameters(SemanticModel model, SyntaxNode lambda, IEnumerable<ParameterSyntax> list)
		{
			ITypeSymbol type = null;
			int i = 0;
			foreach (var param in list) {
				if (param.Type != null) {
					yield return param;
				} else {
					if (type == null) {
						var typeInfo = model.GetTypeInfo(lambda);
						type = typeInfo.ConvertedType ?? typeInfo.Type;
						if (type == null || !type.IsDelegateType())
							yield break;
					}

					yield return SyntaxFactory.Parameter(
						param.AttributeLists,
						param.Modifiers,
						SyntaxFactory.ParseTypeName(type.GetDelegateInvokeMethod().Parameters[i].Type.ToMinimalDisplayString(model, lambda.SpanStart)),
						param.Identifier,
						null
					);
				}
				i++;
			}
		}
	}
}

