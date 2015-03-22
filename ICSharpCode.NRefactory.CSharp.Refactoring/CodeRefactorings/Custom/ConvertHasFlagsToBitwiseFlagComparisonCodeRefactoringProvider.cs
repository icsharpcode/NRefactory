//
// ConvertHasFlagsToBitwiseFlagComparisonAction.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Replace 'Enum.HasFlag' call with bitwise flag comparison")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Replace 'Enum.HasFlag' call with bitwise flag comparison")]
	public class ConvertHasFlagsToBitwiseFlagComparisonCodeRefactoringProvider : CodeRefactoringProvider
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
			var node = root.FindToken(span.Start).Parent;
			if (node.Parent == null || node.Parent.Parent == null || !node.Parent.Parent.IsKind(SyntaxKind.InvocationExpression)) 
				return;
			var symbol = model.GetSymbolInfo(node.Parent).Symbol;

			if (symbol == null || symbol.Kind != SymbolKind.Method || symbol.ContainingType.SpecialType != SpecialType.System_Enum || symbol.Name != "HasFlag")
				return;
			var invocationNode = (InvocationExpressionSyntax)node.Parent.Parent;
			var arg = invocationNode.ArgumentList.Arguments.Select(a => a.Expression).First();
			if (!arg.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax> ().All(bop => bop.IsKind(SyntaxKind.BitwiseOrExpression)))
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(node.Span, DiagnosticSeverity.Info, "To bitwise flag comparison", t2 => Task.FromResult(PerformAction(document, root, invocationNode)))
			);
		}

		static Document PerformAction(Document document, SyntaxNode root, InvocationExpressionSyntax invocationNode)
		{
			var arg = invocationNode.ArgumentList.Arguments.Select(a => a.Expression).First();
			if (!arg.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax> ().All(bop => bop.IsKind(SyntaxKind.BitwiseOrExpression)))
				return document;

			arg = ConvertBitwiseFlagComparisonToHasFlagsCodeRefactoringProvider.MakeFlatExpression(arg, SyntaxKind.BitwiseAndExpression);
			if (arg is BinaryExpressionSyntax)
				arg = SyntaxFactory.ParenthesizedExpression (arg);

			SyntaxNode nodeToReplace = invocationNode;
			while (nodeToReplace.Parent is ParenthesizedExpressionSyntax)
				nodeToReplace = nodeToReplace.Parent;

			bool negateHasFlags = nodeToReplace.Parent != null && nodeToReplace.Parent.IsKind(SyntaxKind.LogicalNotExpression);
			if (negateHasFlags)
				nodeToReplace = nodeToReplace.Parent;

			var expr = SyntaxFactory.BinaryExpression(
				negateHasFlags ? SyntaxKind.EqualsExpression : SyntaxKind.NotEqualsExpression,
				SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression (SyntaxKind.BitwiseAndExpression, ((MemberAccessExpressionSyntax)invocationNode.Expression).Expression, arg))
				.WithAdditionalAnnotations(Formatter.Annotation),
				SyntaxFactory.ParseExpression("0")
			);

			var newRoot = root.ReplaceNode((SyntaxNode)
				nodeToReplace,
				expr.WithAdditionalAnnotations(Formatter.Annotation)
			);
			return document.WithSyntaxRoot(newRoot);
		}
	}
}