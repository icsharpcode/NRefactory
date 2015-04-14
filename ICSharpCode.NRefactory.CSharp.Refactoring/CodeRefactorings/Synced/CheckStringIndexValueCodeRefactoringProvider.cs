//
// CheckStringIndexValueCodeRefactoringProvider.cs
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
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Check string index value")]
	public class CheckStringIndexValueCodeRefactoringProvider : CodeRefactoringProvider
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
			if (model.IsFromGeneratedCode())
				return;
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

			var token = root.FindToken(span.Start);
			if (token.Parent == null)
				return;

			var bracketedList = token.Parent.AncestorsAndSelf ().OfType<BracketedArgumentListSyntax> ().FirstOrDefault ();
			if (bracketedList == null)
				return;
			var elementAccess = bracketedList.AncestorsAndSelf ().OfType<ElementAccessExpressionSyntax> ().FirstOrDefault ();
			if (elementAccess == null)
				return;
			var elementType = model.GetTypeInfo (elementAccess.Expression);
			if (elementType.Type == null || elementType.Type.SpecialType != SpecialType.System_String)
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Info, 
					string.Format (GettextCatalog.GetString ("Check 'if ({0}.Length > {1})'"), elementAccess.Expression, elementAccess.ArgumentList.Arguments.First ()), 
					t2 => {
						var parentStatement = elementAccess.Parent.AncestorsAndSelf ().OfType<StatementSyntax> ().FirstOrDefault ();

						var newParent = SyntaxFactory.IfStatement (
							SyntaxFactory.BinaryExpression (
								SyntaxKind.GreaterThanExpression,
								SyntaxFactory.MemberAccessExpression (SyntaxKind.SimpleMemberAccessExpression, elementAccess.Expression, SyntaxFactory.IdentifierName ("Length")),
								elementAccess.ArgumentList.Arguments.First ().Expression
							),
							parentStatement
						);

						return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode((SyntaxNode)parentStatement, newParent.WithAdditionalAnnotations(Formatter.Annotation))));
					}
				)
			);
		}
	}
}