//
// ExtensionMethodInvocationToStaticMethodInvocationAction.cs
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
	[NRefactoryCodeRefactoringProvider(Description = "Converts the call into static method call syntax")]
	[ExportCodeRefactoringProvider("Invoke using static method syntax", LanguageNames.CSharp)]
	public class ExtensionMethodInvocationToStaticMethodInvocationAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);

			var node = root.FindNode(span) as IdentifierNameSyntax;
			if (node == null || !node.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
				return;
			var memberAccess = node.Parent as MemberAccessExpressionSyntax;
			var invocation = node.Parent.Parent as InvocationExpressionSyntax;
			if (invocation == null)
				return;

			var invocationRR = model.GetSymbolInfo(invocation);
			if (invocationRR.Symbol == null)
				return;

			var method = invocationRR.Symbol as IMethodSymbol;
			if (method == null || !method.IsExtensionMethod)
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Info, 
					"Convert to static method call", 
					t2 => {
						var newRoot = root.ReplaceNode((SyntaxNode)invocation, ToStaticMethodInvocation(model, invocation, memberAccess, invocationRR).WithAdditionalAnnotations(Formatter.Annotation));
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				) 
			);
		}

		static SyntaxNode ToStaticMethodInvocation(SemanticModel model, InvocationExpressionSyntax invocation, MemberAccessExpressionSyntax memberAccess, SymbolInfo invocationRR)
		{
			var newArgumentList = invocation.ArgumentList.Arguments.ToList();
			newArgumentList.Insert(0, SyntaxFactory.Argument(memberAccess.Expression));

			var newTarget = memberAccess.WithExpression(SyntaxFactory.ParseTypeName(invocationRR.Symbol.ContainingType.ToMinimalDisplayString(model, memberAccess.SpanStart)));
			return SyntaxFactory.InvocationExpression(newTarget, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(newArgumentList)));
		}

	}
}

