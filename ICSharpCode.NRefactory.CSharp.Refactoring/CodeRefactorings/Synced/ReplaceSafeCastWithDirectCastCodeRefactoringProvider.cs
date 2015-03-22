// 
// ConvertAsToCastAction.cs
//  
// Author:
//       Mansheng Yang <lightyang0@gmail.com>
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	/// <summary>
	/// Converts an 'as' expression to a cast expression
	/// </summary>
	[NRefactoryCodeRefactoringProvider(Description = "Convert 'as' to cast.")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Convert 'as' to cast.")]
	public class ReplaceSafeCastWithDirectCastCodeRefactoringProvider : CodeRefactoringProvider
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

			if (!token.IsKind(SyntaxKind.AsKeyword))
				return;
			var node = token.Parent as BinaryExpressionSyntax;

			context.RegisterRefactoring(
				CodeActionFactory.Create(token.Span, DiagnosticSeverity.Info, "Convert 'as' to cast", t2 => Task.FromResult(PerformAction (document, root, node)))
			);
		}

		static Document PerformAction(Document document, SyntaxNode root, BinaryExpressionSyntax bop)
		{
			var nodeToReplace = bop.IsParentKind(SyntaxKind.ParenthesizedExpression) ? bop.Parent : bop;
			var castExpr = (ExpressionSyntax)SyntaxFactory.CastExpression(bop.Right as TypeSyntax, FlipEqualsTargetAndArgumentCodeRefactoringProvider.AddParensIfRequired(bop.Left.WithoutLeadingTrivia().WithoutTrailingTrivia())).WithLeadingTrivia(bop.GetLeadingTrivia()).WithTrailingTrivia(bop.GetTrailingTrivia());

			var newRoot = root.ReplaceNode((SyntaxNode)nodeToReplace, castExpr);
			return document.WithSyntaxRoot(newRoot);
		}
	}
}
