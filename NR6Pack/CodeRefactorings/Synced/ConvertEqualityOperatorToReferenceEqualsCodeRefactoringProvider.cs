//
// ConvertEqualityOperatorToReferenceEqualsCodeRefactoringProvider.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Convert '==' to 'object.ReferenceEquals()'")]
	public class ConvertEqualityOperatorToReferenceEqualsCodeRefactoringProvider : CodeRefactoringProvider
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
			var node = root.FindNode(span) as BinaryExpressionSyntax;
			if (node == null || !(node.IsKind(SyntaxKind.EqualsExpression) || node.IsKind(SyntaxKind.NotEqualsExpression)))
				return;

			var leftType  = model.GetTypeInfo (node.Left).Type;
			var rightType = model.GetTypeInfo (node.Right).Type;
			if (leftType == null || rightType == null || leftType.IsValueType || rightType.IsValueType)
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Info,
					GettextCatalog.GetString ("To 'ReferenceEquals' call"), 
					t2 => Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode((SyntaxNode)node, CreateEquals(model, node))))
				)
			);
		}

		SyntaxNode CreateEquals(SemanticModel model, BinaryExpressionSyntax node)
		{
			var expr = SyntaxFactory.InvocationExpression(
				GenerateTarget(model, node), 
				SyntaxFactory.ArgumentList(
					new SeparatedSyntaxList<ArgumentSyntax>()
					.Add(SyntaxFactory.Argument(node.Left))
					.Add(SyntaxFactory.Argument(node.Right))
				)
			);
			if (node.IsKind(SyntaxKind.NotEqualsExpression))
				return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, expr).WithAdditionalAnnotations(Formatter.Annotation);
			return expr.WithAdditionalAnnotations(Formatter.Annotation);
		}

		ExpressionSyntax GenerateTarget(SemanticModel model, BinaryExpressionSyntax node)
		{
			var symbols = model.LookupSymbols(node.SpanStart).OfType<IMethodSymbol>();
			if (!symbols.Any() || HasDifferentEqualsMethod(symbols))
				return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ParseExpression("object"), SyntaxFactory.IdentifierName("ReferenceEquals"));
			else
				return SyntaxFactory.IdentifierName("ReferenceEquals");
		}

		static bool HasDifferentEqualsMethod(IEnumerable<IMethodSymbol> symbols)
		{
			foreach (IMethodSymbol method in symbols) {
				if(method.Name == "ReferenceEquals" && method.Parameters.Count() == 2 && method.ToDisplayString() != "object.ReferenceEquals(object, object)")
					return true;
			}
			return false;
		}
	}
}