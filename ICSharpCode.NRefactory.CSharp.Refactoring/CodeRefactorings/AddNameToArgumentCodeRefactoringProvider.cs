// 
// AddArgumentNameAction.cs
//  
// Author:
//       Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013 Ji Kun <jikun.nus@gmail.com>
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
	/// <summary>
	///  Add name for argument
	/// </summary>
	[NRefactoryCodeRefactoringProvider(Description = "Add name for argument including method, indexer invocation and attibute usage")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Add name for argument")]
	public class AddNameToArgumentCodeRefactoringProvider : SpecializedCodeAction<ExpressionSyntax>
	{
		static CodeAction CreateAttributeCodeAction(Document document, SyntaxNode root, ExpressionSyntax node, IMethodSymbol constructor, AttributeSyntax attribute)
		{
			var arguments = attribute.ArgumentList.Arguments;
			var idx = arguments.IndexOf(node.Parent as AttributeArgumentSyntax);

			var name = constructor.Parameters[idx].Name;
			return CodeActionFactory.Create(
				node.Span,
				DiagnosticSeverity.Info,
				string.Format("Add argument name '{0}'", name),
				t2 => {
					var newArguments = SyntaxFactory.SeparatedList<AttributeArgumentSyntax>(
						attribute.ArgumentList.Arguments.Take(idx).Concat(
							attribute.ArgumentList.Arguments.Skip(idx).Select((arg, i) => {
								if (arg.NameEquals != null)
									return arg;
								return SyntaxFactory.AttributeArgument(null, SyntaxFactory.NameColon(constructor.Parameters[i + idx].Name), arg.Expression);
							})
						)
					);
					var newAttribute = attribute.WithArgumentList(attribute.ArgumentList.WithArguments(newArguments));
					var newRoot = root.ReplaceNode((SyntaxNode)attribute, newAttribute).WithAdditionalAnnotations(Formatter.Annotation);
					return Task.FromResult(document.WithSyntaxRoot(newRoot));
				}
			);
		}

		static CodeAction CreateIndexerCodeAction(Document document, SyntaxNode root, ExpressionSyntax node, IPropertySymbol indexer, ElementAccessExpressionSyntax elementAccess)
		{
			var arguments = elementAccess.ArgumentList.Arguments;
			var idx = arguments.IndexOf(node.Parent as ArgumentSyntax);

			var name = indexer.Parameters[idx].Name;
			return CodeActionFactory.Create(
				node.Span,
				DiagnosticSeverity.Info,
				string.Format("Add argument name '{0}'", name),
				t2 => {
					var newArguments = SyntaxFactory.SeparatedList<ArgumentSyntax>(
						elementAccess.ArgumentList.Arguments.Take(idx).Concat(
							elementAccess.ArgumentList.Arguments.Skip(idx).Select((arg, i) => {
								if (arg.NameColon != null)
									return arg;
								return arg.WithNameColon(SyntaxFactory.NameColon(indexer.Parameters[i + idx].Name));
							})
						)
					);
					var newAttribute = elementAccess.WithArgumentList(elementAccess.ArgumentList.WithArguments(newArguments));
					var newRoot = root.ReplaceNode((SyntaxNode)elementAccess, newAttribute).WithAdditionalAnnotations(Formatter.Annotation);
					return Task.FromResult(document.WithSyntaxRoot(newRoot));
				}
			);
		}

		static CodeAction CreateInvocationCodeAction(Document document, SyntaxNode root, ExpressionSyntax node, IMethodSymbol method, InvocationExpressionSyntax invocation)
		{
			var arguments = invocation.ArgumentList.Arguments;
			var idx = arguments.IndexOf(node.Parent as ArgumentSyntax);
			if (idx >= method.Parameters.Length)
				return null;
			var parameters = method.Parameters[idx];
			var name = parameters.Name;
			return CodeActionFactory.Create(
				node.Span,
				DiagnosticSeverity.Info,
				string.Format("Add argument name '{0}'", name),
				t2 => {
					var newArguments = SyntaxFactory.SeparatedList<ArgumentSyntax>(
						invocation.ArgumentList.Arguments.Take(idx).Concat(
							invocation.ArgumentList.Arguments.Skip(idx).Select((arg, i) => {
								if (arg.NameColon != null)
									return arg;
								return arg.WithNameColon(SyntaxFactory.NameColon(method.Parameters[i].Name));
							})
						)
					);
					var newAttribute = invocation.WithArgumentList(invocation.ArgumentList.WithArguments(newArguments));
					var newRoot = root.ReplaceNode((SyntaxNode)invocation, newAttribute).WithAdditionalAnnotations(Formatter.Annotation);
					return Task.FromResult(document.WithSyntaxRoot(newRoot));
				}
			);
		}

		protected override IEnumerable<CodeAction> GetActions(Document document, SemanticModel semanticModel, SyntaxNode root, TextSpan span, ExpressionSyntax node, CancellationToken cancellationToken)
		{
			if (!node.Parent.IsKind(SyntaxKind.Argument) && !node.Parent.IsKind(SyntaxKind.AttributeArgument))
				yield break;
			if (span.Start != node.SpanStart)
				yield break;
			var parent = node.Parent.Parent.Parent;
			var attribute = parent as AttributeSyntax;
			if (attribute != null) {
				var resolvedResult = semanticModel.GetSymbolInfo(attribute);
				var constructor = resolvedResult.Symbol as IMethodSymbol;
				if (constructor == null)
					yield break;
				yield return CreateAttributeCodeAction(document, root, node, constructor, attribute);
			}

			var indexerExpression = parent as ElementAccessExpressionSyntax;
			if (indexerExpression != null) {
				var resolvedResult = semanticModel.GetSymbolInfo(indexerExpression);
				var indexer = resolvedResult.Symbol as IPropertySymbol;
				if (indexer == null)
					yield break;
				yield return CreateIndexerCodeAction(document, root, node, indexer, indexerExpression);
			}

			var invocationExpression = parent as InvocationExpressionSyntax;
			if (invocationExpression != null) {
				var resolvedResult = semanticModel.GetSymbolInfo(invocationExpression);
				var method = resolvedResult.Symbol as IMethodSymbol;
				if (method == null)
					yield break;
				var codeAction = CreateInvocationCodeAction(document, root, node, method, invocationExpression);
				if (codeAction != null)
					yield return codeAction;
			}
		}
	}
}
