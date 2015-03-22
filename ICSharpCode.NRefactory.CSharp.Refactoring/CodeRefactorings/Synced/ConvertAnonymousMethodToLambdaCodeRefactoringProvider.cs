//
// ConvertAnonymousDelegateToExpression.cs
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
	[NRefactoryCodeRefactoringProvider(Description = "Converts an anonymous method expression into a lambda expression")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Convert anonymous method to lambda expression")]
	public class ConvertAnonymousMethodToLambdaCodeRefactoringProvider : SpecializedCodeRefactoringProvider<AnonymousMethodExpressionSyntax>
	{
		protected override IEnumerable<CodeAction> GetActions(Document document, SemanticModel semanticModel, SyntaxNode root, TextSpan span, AnonymousMethodExpressionSyntax node, CancellationToken cancellationToken)
		{
			if (!node.DelegateKeyword.Span.Contains(span))
				return Enumerable.Empty<CodeAction>();

			ExpressionSyntax convertExpression = null;
			if (node.Block.Statements.Count == 1) {
				var stmt = node.Block.Statements.FirstOrDefault() as ExpressionStatementSyntax;
				if (stmt != null)
					convertExpression = stmt.Expression;
			}

			ITypeSymbol guessedType = null;
			if (node.ParameterList == null) {
				var info = semanticModel.GetTypeInfo(node);
				guessedType = info.ConvertedType ?? info.Type;
				if (guessedType == null)
					return Enumerable.Empty<CodeAction>();
			}
			return new []  { 
				CodeActionFactory.Create(
					node.DelegateKeyword.Span,
					DiagnosticSeverity.Info,
					"To lambda expression",
					t2 => {
						var parent = node.Parent.Parent.Parent;
						bool explicitLambda = parent is VariableDeclarationSyntax && ((VariableDeclarationSyntax)parent).Type.IsVar;
						ParameterListSyntax parameterList;
						if (node.ParameterList != null) {
							if (explicitLambda) {
								parameterList = node.ParameterList;
							} else {
								parameterList = SyntaxFactory.ParameterList(
									SyntaxFactory.SeparatedList(node.ParameterList.Parameters.Select(p => SyntaxFactory.Parameter(p.AttributeLists, p.Modifiers, null, p.Identifier, p.Default)))
								);
							}
						} else {
							var invokeMethod = guessedType.GetDelegateInvokeMethod();
							parameterList = SyntaxFactory.ParameterList(
								SyntaxFactory.SeparatedList(
									invokeMethod.Parameters.Select(p => 
										SyntaxFactory.Parameter(SyntaxFactory.Identifier(p.Name))
									)
								)
							);
						}
						var lambdaExpression = explicitLambda || parameterList.Parameters.Count != 1 ? 
							(SyntaxNode)SyntaxFactory.ParenthesizedLambdaExpression(parameterList, (CSharpSyntaxNode)convertExpression ?? node.Block) :
							SyntaxFactory.SimpleLambdaExpression(parameterList.Parameters[0], (CSharpSyntaxNode)convertExpression ?? node.Block);
						var newRoot = root.ReplaceNode((SyntaxNode)node, lambdaExpression.WithAdditionalAnnotations(Formatter.Annotation));
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
			};
		}
	}
}