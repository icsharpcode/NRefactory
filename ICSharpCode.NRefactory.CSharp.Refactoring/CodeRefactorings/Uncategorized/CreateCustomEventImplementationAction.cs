// 
// CreateCustomEventImplementationAction.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
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
	[NRefactoryCodeRefactoringProvider(Description = "Create custom event implementation")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Create custom event implementation")]
	public class CreateCustomEventImplementationAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			var variableDeclarator = root.FindNode(span) as VariableDeclaratorSyntax;
			if (variableDeclarator == null || 
				variableDeclarator.Parent == null || 
				variableDeclarator.Parent.Parent == null || 
				!variableDeclarator.Parent.Parent.IsKind(SyntaxKind.EventFieldDeclaration) || 
				!variableDeclarator.Identifier.Span.Contains(span))
				return;
			var eventDecl = (EventFieldDeclarationSyntax)variableDeclarator.Parent.Parent;

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Info,
					GettextCatalog.GetString ("Create custom event implementation"),
					t2 => {
//					var accessor = new Accessor
//					{
//						Body = new BlockStatement
//						{
//							new ThrowStatement(
//								new ObjectCreateExpression(context.CreateShortType("System", "NotImplementedException")))	
//						}
//					};
//					var e = new CustomEventDeclaration
//					{
//						Name = node.Name,
//						Modifiers = eventDecl.Modifiers,
//						ReturnType = eventDecl.ReturnType.Clone (),
//						AddAccessor = accessor,
//						RemoveAccessor = (Accessor)accessor.Clone(),
//					};
//					if (eventDecl.Variables.Count > 1) {
//						var newEventDecl = (EventDeclaration)eventDecl.Clone ();
//						newEventDecl.Variables.Remove (
//							newEventDecl.Variables.FirstOrNullObject (v => v.Name == node.Name));
//						script.InsertBefore (eventDecl, newEventDecl);
//					}
//					script.Replace (eventDecl, e);

						var e = SyntaxFactory.EventDeclaration(
							eventDecl.AttributeLists,
							eventDecl.Modifiers,
							eventDecl.Declaration.Type,
							null,
							variableDeclarator.Identifier,
							SyntaxFactory.AccessorList(SyntaxFactory.List<AccessorDeclarationSyntax>(new [] {
								SyntaxFactory.AccessorDeclaration(SyntaxKind.AddAccessorDeclaration, ToAbstractVirtualNonVirtualConversionCodeRefactoringProvider.CreateNotImplementedBody()),
								SyntaxFactory.AccessorDeclaration(SyntaxKind.RemoveAccessorDeclaration, ToAbstractVirtualNonVirtualConversionCodeRefactoringProvider.CreateNotImplementedBody())
							}))
						);

						SyntaxNode newRoot;

						if (eventDecl.Declaration.Variables.Count > 1) {
							newRoot = root.ReplaceNode((SyntaxNode)
								eventDecl, 
								new SyntaxNode[] {
									eventDecl.WithDeclaration(
											eventDecl.Declaration.WithVariables(
												SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>(
													eventDecl.Declaration.Variables.Where(decl => decl != variableDeclarator)
												)
											)
									).WithAdditionalAnnotations(Formatter.Annotation),
									e.WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation)
								}
							);
						} else {
							newRoot = root.ReplaceNode((SyntaxNode)eventDecl, e.WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation));
						}

						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					})
			);
		}
	}
}
