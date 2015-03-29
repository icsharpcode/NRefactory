//
// ConvertStatementBodyToExpressionBodyCodeRefactoringProvider.cs
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

using System.Threading;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[NRefactoryCodeRefactoringProvider(Description = "Convert statement body member to expression body")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Convert statement body member to expression body")]
	public class ConvertStatementBodyToExpressionBodyCodeRefactoringProvider : CodeRefactoringProvider
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
			var model = await document.GetSemanticModelAsync (cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync (cancellationToken);
			var parseOptions = root.SyntaxTree.Options as CSharpParseOptions;
			if (parseOptions != null && parseOptions.LanguageVersion < LanguageVersion.CSharp6)
				return;

			var token = root.FindToken (span.Start);
			var method = GetDeclaration<MethodDeclarationSyntax>(token);
			if (method != null)
				HandleMethodCase (context, root, token, method);

			var property = GetDeclaration<PropertyDeclarationSyntax>(token);
			if (property != null)
				HandlePropertyCase (context, root, token, property);
		}

		static bool IsSimpleReturn (BlockSyntax body, out ExpressionSyntax returnedExpression)
		{
			if (body == null || 
				body.Statements.Count != 1 ||
				!body.Statements [0].IsKind (SyntaxKind.ReturnStatement)) {
				returnedExpression = null;
				return false;
			}
			returnedExpression = ((ReturnStatementSyntax)body.Statements [0]).Expression;
			return true;
		}

		static T GetDeclaration<T> (SyntaxToken token) where T : MemberDeclarationSyntax
		{
			if (token.IsKind (SyntaxKind.IdentifierToken))
				return token.Parent as T;
			if (token.IsKind (SyntaxKind.ReturnKeyword))
				return token.Parent.GetAncestors ().OfType<T> ().FirstOrDefault ();
			return null;
		}

		static void HandleMethodCase (CodeRefactoringContext context, SyntaxNode root, SyntaxToken token, MethodDeclarationSyntax method)
		{
			ExpressionSyntax expr;
			if (!IsSimpleReturn (method.Body, out expr))
				return;
			context.RegisterRefactoring(
				CodeActionFactory.Create(
					token.Span, 
					DiagnosticSeverity.Info, 
					GettextCatalog.GetString ("To expression body"), 
					t2 => {
						var newRoot = root.ReplaceNode((SyntaxNode)
							method, 
							method
							.WithBody (null)
							.WithExpressionBody (SyntaxFactory.ArrowExpressionClause (expr))
							.WithSemicolonToken (SyntaxFactory.Token (SyntaxKind.SemicolonToken))
							.WithAdditionalAnnotations(Formatter.Annotation)						
						);
						return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
					}
				) 
			);

		}

		static void HandlePropertyCase (CodeRefactoringContext context, SyntaxNode root,SyntaxToken token, PropertyDeclarationSyntax property)
		{
			var getter = property.AccessorList.Accessors.FirstOrDefault (acc => acc.IsKind(SyntaxKind.GetAccessorDeclaration));
			ExpressionSyntax expr;
			if (getter == null || property.AccessorList.Accessors.Count != 1 || !IsSimpleReturn (getter.Body, out expr))
				return;
			context.RegisterRefactoring(
				CodeActionFactory.Create(
					token.Span, 
					DiagnosticSeverity.Info, 
					GettextCatalog.GetString ("To expression body"), 
					t2 => {
						var newRoot = root.ReplaceNode((SyntaxNode)
							property, 
							property
							.WithAccessorList (null)
							.WithExpressionBody (SyntaxFactory.ArrowExpressionClause (expr))
							.WithSemicolon (SyntaxFactory.Token (SyntaxKind.SemicolonToken))
							.WithAdditionalAnnotations(Formatter.Annotation)
						);
						return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
					}
				) 
			);
		}
	}
}
