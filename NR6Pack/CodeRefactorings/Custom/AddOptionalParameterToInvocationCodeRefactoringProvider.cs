//
// AddOptionalParameterToInvocationAction.cs
//
// Author:
//      Luís Reis <luiscubal@gmail.com>
//
// Copyright (c) 2013 Luís Reis
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
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Add one or more optional parameters to an invocation, using their default values")]
	public class AddOptionalParameterToInvocationCodeRefactoringProvider : CodeRefactoringProvider
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
			if (model.IsFromGeneratedCode(cancellationToken))
				return;
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

			var node = root.FindNode(span);

			InvocationExpressionSyntax invocationExpression;
			if (node.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression)) {
				invocationExpression = node.Parent.Parent as InvocationExpressionSyntax;
			} else {
				invocationExpression = node.Parent as InvocationExpressionSyntax;
			}
			if (invocationExpression == null)
				return;

			var symbolInfo = model.GetSymbolInfo(invocationExpression);
			var method = symbolInfo.Symbol as IMethodSymbol;
			if (method == null)
				return;


			bool foundOptionalParameter = false;
			foreach (var parameter in method.Parameters) {
				if (parameter.IsParams) {
					return;
				}
				if (parameter.IsOptional) {
					foundOptionalParameter = true;
					break;
				}
			}
			if (!foundOptionalParameter)
				return;

			//Basic sanity checks done, now see if there are any missing optional arguments
			var missingParameters = new List<IParameterSymbol>(method.Parameters);
			if (method.Parameters.Length != invocationExpression.ArgumentList.Arguments.Count) {
				//Extension method
				if (missingParameters[0].IsThis)
					missingParameters.RemoveAt (0);
			}

			foreach (var argument in invocationExpression.ArgumentList.Arguments) {
				if (argument.NameColon  != null) {
					missingParameters.RemoveAll(parameter => parameter.Name == argument.NameColon.Name.ToString());
				} else {
					missingParameters.RemoveAt(0);
				}
			}

			foreach (var parameterToAdd in missingParameters) {
				//Add specific parameter
				context.RegisterRefactoring(CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Info, 
					string.Format(GettextCatalog.GetString ("Add optional parameter \"{0}\""), parameterToAdd.Name), 
					t2 => {
						var newInvocation = AddArgument(invocationExpression, parameterToAdd, parameterToAdd == missingParameters.First()).
							WithAdditionalAnnotations(Formatter.Annotation);
						return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode((SyntaxNode)invocationExpression, newInvocation)));
					}
				));
			}

			if (missingParameters.Count > 1) {
				//Add all parameters at once
				context.RegisterRefactoring(CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Info, 
					GettextCatalog.GetString ("Add all optional parameters"),
					t2 => {
						var newInvocation = invocationExpression;
						foreach (var parameterToAdd in missingParameters) {
							newInvocation = AddArgument(newInvocation, parameterToAdd, true);
						}
						var newRoot = root.ReplaceNode((SyntaxNode)invocationExpression, newInvocation)
							.WithAdditionalAnnotations(Formatter.Annotation);
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				));
			}
		}

		static InvocationExpressionSyntax AddArgument(InvocationExpressionSyntax invocationExpression, IParameterSymbol parameterToAdd, bool isNextInSequence)
		{
			ExpressionSyntax defaultValue;
			if (parameterToAdd.HasExplicitDefaultValue) {
				defaultValue = ComputeConstantValueCodeRefactoringProvider.GetLiteralExpression(parameterToAdd.ExplicitDefaultValue);
			} else {
				return invocationExpression;
			}
			ArgumentSyntax newArgument = SyntaxFactory.Argument(defaultValue);
			if (invocationExpression.ArgumentList.Arguments.Any(argument => argument.NameColon != null) || !isNextInSequence) {
				newArgument = newArgument.WithNameColon(SyntaxFactory.NameColon(parameterToAdd.Name));
			}

			var newArguments = invocationExpression.ArgumentList.AddArguments(newArgument);
			return invocationExpression.WithArgumentList(newArguments);
		}
	}
}