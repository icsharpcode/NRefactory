//
// ConvertEqualsToEqualityOperatorAction.cs
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
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Converts 'Equals' call to '==' or '!='")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Convert 'Equals' call to '==' or '!='")]
	public class ConvertEqualsToEqualityOperatorCodeRefactoringProvider : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);

			var node = root.FindNode(span) as IdentifierNameSyntax;
			if (node == null)
				return;
			var invocation = node.Parent as InvocationExpressionSyntax ?? node.Parent.Parent as InvocationExpressionSyntax;
			if (invocation == null)
				return;

			var symbol = model.GetSymbolInfo(node).Symbol;
			if (symbol == null || symbol.Name != "Equals" || symbol.ContainingType.SpecialType != SpecialType.System_Object)
				return;

			ExpressionSyntax expr = invocation;
			bool useEquality = true;

			if (invocation.ArgumentList.Arguments.Count != 2 && invocation.ArgumentList.Arguments.Count != 1)
				return;
			//node is identifier, parent is invocation, parent.parent (might) be unary negation
			var uOp = invocation.Parent as PrefixUnaryExpressionSyntax;
			if (uOp != null && uOp.IsKind(SyntaxKind.LogicalNotExpression)) {
				expr = uOp;
				useEquality = false;
			}

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Info, 
					useEquality ? "To '=='" : "To '!='", 
					t2 => {
						var newRoot = root.ReplaceNode((SyntaxNode)
							expr, 
							SyntaxFactory.BinaryExpression(
								useEquality ? SyntaxKind.EqualsExpression : SyntaxKind.NotEqualsExpression,
								invocation.ArgumentList.Arguments.Count == 1 ? ((MemberAccessExpressionSyntax)invocation.Expression).Expression : invocation.ArgumentList.Arguments.First().Expression,
								invocation.ArgumentList.Arguments.Last().Expression
							).WithAdditionalAnnotations(Formatter.Annotation)
						);

						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				) 
			);
		}
	}
}

