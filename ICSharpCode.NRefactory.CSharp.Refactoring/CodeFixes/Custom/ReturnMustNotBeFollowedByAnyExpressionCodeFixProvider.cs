//
// CS0127ReturnMustNotBeFollowedByAnyExpression.cs
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
using ICSharpCode.NRefactory6.CSharp;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Simplification;

namespace ICSharpCode.NRefactory6.CSharp.CodeFixes
{
	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class ReturnMustNotBeFollowedByAnyExpressionCodeFixProvider : CodeFixProvider
	{
		const string CS0127 = "CS0127"; // Error CS0127: Since 'function' returns void, a return keyword must not be followed by an object expression
		const string CS8030 = "CS8030"; // Error CS8030: Anonymous function or lambda expression converted to a void returning delegate cannot return a value

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(CS0127, CS8030); }
		}

		public override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public async override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
				return;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			if (cancellationToken.IsCancellationRequested)
				return;
			var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			if (model.IsFromGeneratedCode(cancellationToken))
				return;
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
			var diagnostics = context.Diagnostics;
			var diagnostic = diagnostics.First ();
			var node = root.FindNode(context.Span) as ReturnStatementSyntax;
			if (node == null)
				return;

			context.RegisterCodeFix(CodeActionFactory.Create(
				node.Span,
				diagnostic.Severity,
				GettextCatalog.GetString ("Remove returned expression"),
				token => {
					var newRoot = root.ReplaceNode(node, SyntaxFactory.ReturnStatement ().WithLeadingTrivia (node.GetLeadingTrivia ()).WithTrailingTrivia (node.GetTrailingTrivia ()));
					return Task.FromResult(document.WithSyntaxRoot(newRoot));
				}), diagnostic);

			if (diagnostic.Id == CS0127) {
				var method = node.Parent.AncestorsAndSelf ().OfType<MethodDeclarationSyntax> ().First ();
				if (method != null) {
					context.RegisterCodeFix(CodeActionFactory.Create(
						node.Span,
						diagnostic.Severity,
						GettextCatalog.GetString ("Change return type of method"),
						token => {
							var type = model.GetTypeInfo (node.Expression).Type;
							if (type == null)
								return Task.FromResult(document);
							
							var newRoot = root.ReplaceNode(method, method.WithReturnType (type.GenerateTypeSyntax ().WithAdditionalAnnotations (Simplifier.Annotation)));
							return Task.FromResult(document.WithSyntaxRoot(newRoot));
						}
					), diagnostic);
				}
            }
			//context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, diagnostic.GetMessage(), document.WithSyntaxRoot(newRoot)), diagnostic);
		}

//			public override void VisitReturnStatement(ReturnStatement returnStatement)
//			{
//				base.VisitReturnStatement(returnStatement);
//
//				if (ctx.GetExpectedType(returnStatement.Expression).Kind == TypeKind.Void) {
//					var method = returnStatement.GetParent<MethodDeclaration>();
//					if (method != null) {
//						var rr = ctx.Resolve(returnStatement.Expression);
//						if (rr != null && !rr.IsError) {
//							actions.Add(new CodeAction(ctx.TranslateString("."), script => {
//								script.Replace(method.ReturnType, ctx.CreateTypeSystemAstBuilder(method).ConvertType(rr.Type));
//							}, returnStatement));
//						}
//					}
//
//					AddDiagnosticAnalyzer(new CodeIssue(
//						returnStatement, 
//						ctx.TranslateString("Return type is 'void'"),
//						actions
//					));
//				}
//			}

	}
}