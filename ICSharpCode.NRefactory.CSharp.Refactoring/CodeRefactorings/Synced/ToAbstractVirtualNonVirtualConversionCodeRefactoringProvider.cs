//
// AbstractAndVirtualConversionAction.cs
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
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Make abstract member virtual")]
	public class ToAbstractVirtualNonVirtualConversionCodeRefactoringProvider : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);

			var token = root.FindToken(span.Start);

			if (!token.IsKind(SyntaxKind.IdentifierToken) &&
			    !token.IsKind(SyntaxKind.AbstractKeyword) &&
			    !token.IsKind(SyntaxKind.VirtualKeyword) &&
			    !token.IsKind(SyntaxKind.ThisKeyword))
				return;
			var declaration = token.Parent as MemberDeclarationSyntax;
			if (token.IsKind(SyntaxKind.IdentifierToken)) {
				if (token.Parent.Parent.IsKind(SyntaxKind.VariableDeclaration) && 
					token.Parent.Parent.Parent.IsKind(SyntaxKind.EventFieldDeclaration)) {
					declaration = token.Parent.Parent.Parent as MemberDeclarationSyntax;
				}
			}
			if (declaration == null
				|| declaration is BaseTypeDeclarationSyntax
				|| declaration is ConstructorDeclarationSyntax
				|| declaration is DestructorDeclarationSyntax)
				return;
			var modifiers = declaration.GetModifiers();
			if (modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword)))
				return;

			var declarationParent = declaration.Parent as TypeDeclarationSyntax;

			var explicitInterface = declaration.GetExplicitInterfaceSpecifierSyntax();
			if (explicitInterface != null) {
				return;
			}

//			if (selectedNode != node.NameToken) {
//				if ((node is EventDeclaration && node is CustomEventDeclaration || selectedNode.Role != Roles.Identifier) && 
//					selectedNode.Role != IndexerDeclaration.ThisKeywordRole) {
//					var modToken = selectedNode as CSharpModifierToken;
//					if (modToken == null || (modToken.Modifier & (Modifiers.Abstract | Modifiers.Virtual)) == 0)
//						yield break;
//				} else {
//					if (!(node is EventDeclaration || node is CustomEventDeclaration) && selectedNode.Parent != node)
//						yield break;
//				}
//			}
//			if (!node.GetChildByRole(EntityDeclaration.PrivateImplementationTypeRole).IsNull)
//				yield break;
//
			if (declarationParent.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword))) {
				if (modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword))) {
					context.RegisterRefactoring(CodeActionFactory.Create(
						token.Span,
						DiagnosticSeverity.Info,
						GettextCatalog.GetString ("To non-abstract"),
						t2 => {
							var newRoot = root.ReplaceNode((SyntaxNode)declaration, ImplementAbstractDeclaration (declaration).WithAdditionalAnnotations(Formatter.Annotation));
							return Task.FromResult(document.WithSyntaxRoot(newRoot));
						}
					)
					);
				} else {
					if (CheckBody(declaration)) {
						context.RegisterRefactoring(CodeActionFactory.Create(
							token.Span,
							DiagnosticSeverity.Info,
							GettextCatalog.GetString ("To abstract"),
							t2 => {
								var newRoot = root.ReplaceNode((SyntaxNode)declaration, MakeAbstractDeclaration(declaration).WithAdditionalAnnotations(Formatter.Annotation));
								return Task.FromResult(document.WithSyntaxRoot(newRoot));
							}
						)
						);
					}
				}
			}

			if (modifiers.Any(m => m.IsKind(SyntaxKind.VirtualKeyword))) {
				context.RegisterRefactoring(CodeActionFactory.Create(
					token.Span,
					DiagnosticSeverity.Info,
					GettextCatalog.GetString ("To non-virtual"),
					t2 => {
						var newRoot = root.ReplaceNode((SyntaxNode)declaration, RemoveVirtualModifier(declaration));
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
				);
			} else {
				if (modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword))) {
					context.RegisterRefactoring(CodeActionFactory.Create(
						token.Span,
						DiagnosticSeverity.Info,
						GettextCatalog.GetString ("To virtual"),
						t2 => {
							var newRoot = root.ReplaceNode((SyntaxNode)declaration, ImplementAbstractDeclaration(declaration, true).WithAdditionalAnnotations(Formatter.Annotation));
							return Task.FromResult(document.WithSyntaxRoot(newRoot));
						}
					)
					);
				} else {
					context.RegisterRefactoring(CodeActionFactory.Create(
						token.Span,
						DiagnosticSeverity.Info,
						GettextCatalog.GetString ("To virtual"),
						t2 => {
							var newRoot = root.ReplaceNode((SyntaxNode)declaration, AddModifier(declaration, SyntaxKind.VirtualKeyword).WithAdditionalAnnotations(Formatter.Annotation));
							return Task.FromResult(document.WithSyntaxRoot(newRoot));
						}
					)
					);
				}
			}
		}

		internal static BlockSyntax CreateNotImplementedBody()
		{
			var throwStatement = SyntaxFactory.ThrowStatement(
				SyntaxFactory.ParseExpression("new System.NotImplementedException()").WithAdditionalAnnotations(Simplifier.Annotation)
			);
			return SyntaxFactory.Block(throwStatement);
		}

		static SyntaxNode ImplementAbstractDeclaration (MemberDeclarationSyntax abstractDeclaration, bool implementAsVirtual = false)
		{
			var method = abstractDeclaration as MethodDeclarationSyntax;

			var modifier = abstractDeclaration.GetModifiers();
			var newMods = modifier.Where(m => !m.IsKind(SyntaxKind.AbstractKeyword) && !m.IsKind(SyntaxKind.StaticKeyword));
			if (implementAsVirtual){
				newMods = newMods.Concat(
					new [] { SyntaxFactory.Token(SyntaxKind.VirtualKeyword) }
				);
			}

			var newModifier = SyntaxFactory.TokenList(newMods); 

			if (method != null) {
				return SyntaxFactory.MethodDeclaration (
					method.AttributeLists,
					newModifier,
					method.ReturnType,
					method.ExplicitInterfaceSpecifier,
					method.Identifier,
					method.TypeParameterList,
					method.ParameterList,
					method.ConstraintClauses,
					CreateNotImplementedBody(),
					method.ExpressionBody);
			}

			var property = abstractDeclaration as PropertyDeclarationSyntax;
			if (property != null) {
				var accessors = new List<AccessorDeclarationSyntax> ();
				foreach (var accessor in property.AccessorList.Accessors) {
					accessors.Add(SyntaxFactory.AccessorDeclaration(accessor.Kind(), accessor.AttributeLists, accessor.Modifiers, CreateNotImplementedBody()));
				}
				return SyntaxFactory.PropertyDeclaration(
					property.AttributeLists,
					newModifier,
					property.Type,
					property.ExplicitInterfaceSpecifier,
					property.Identifier,
					SyntaxFactory.AccessorList(SyntaxFactory.List<AccessorDeclarationSyntax>(accessors)),
					property.ExpressionBody,
					property.Initializer);
			}

			var indexer = abstractDeclaration as IndexerDeclarationSyntax;
			if (indexer != null) {
				var accessors = new List<AccessorDeclarationSyntax> ();
				foreach (var accessor in indexer.AccessorList.Accessors) {
					accessors.Add(SyntaxFactory.AccessorDeclaration(accessor.Kind(), accessor.AttributeLists, accessor.Modifiers, CreateNotImplementedBody()));
				}
				return SyntaxFactory.IndexerDeclaration(
					indexer.AttributeLists,
					newModifier,
					indexer.Type,
					indexer.ExplicitInterfaceSpecifier,
					indexer.ParameterList,
					SyntaxFactory.AccessorList(SyntaxFactory.List<AccessorDeclarationSyntax>(accessors)),
					indexer.ExpressionBody);
			}

			var evt = abstractDeclaration as EventDeclarationSyntax;
			if (evt != null) {
				var accessors = new List<AccessorDeclarationSyntax> ();
				foreach (var accessor in evt.AccessorList.Accessors) {
					accessors.Add(SyntaxFactory.AccessorDeclaration(accessor.Kind(), CreateNotImplementedBody()));
				}
				return SyntaxFactory.EventDeclaration(
					evt.AttributeLists,
					newModifier,
					evt.Type,
					evt.ExplicitInterfaceSpecifier,
					evt.Identifier,
					SyntaxFactory.AccessorList(SyntaxFactory.List<AccessorDeclarationSyntax>(accessors))
				);
			}

			var evtField = abstractDeclaration as EventFieldDeclarationSyntax;
			if (evtField != null) {
				return SyntaxFactory.EventFieldDeclaration (
					evtField.AttributeLists,
					newModifier,
					evtField.Declaration
				);
			}

			return null;
		}

		static bool CheckBody(MemberDeclarationSyntax node)
		{
			var property = node as BasePropertyDeclarationSyntax;
			if (property != null && property.AccessorList.Accessors.Any(acc => !IsValidBody(acc.Body)))
				return false;

			var m = node as MethodDeclarationSyntax;
			if (m != null && !IsValidBody(m.Body))
				return false;
			return true;
		}

		static bool IsValidBody(BlockSyntax body)
		{
			if (body == null)
				return true;
			var first = body.Statements.FirstOrDefault();
			if (first == null)
				return true;
//			if (first.GetNextSibling(s => s.Role == BlockStatement.StatementRole) != null)
//				return false;
			return first is EmptyStatementSyntax || first is ThrowStatementSyntax;
		}

		static SyntaxNode MakeAbstractDeclaration(MemberDeclarationSyntax abstractDeclaration)
		{
			var method = abstractDeclaration as MethodDeclarationSyntax;

			var modifier = abstractDeclaration.GetModifiers();
			var newModifier = SyntaxFactory.TokenList(modifier.Where(m => !m.IsKind(SyntaxKind.VirtualKeyword) && !m.IsKind(SyntaxKind.StaticKeyword) && !m.IsKind(SyntaxKind.SealedKeyword)).Concat(
				new [] { SyntaxFactory.Token(SyntaxKind.AbstractKeyword) }
			)); 

			if (method != null) {
				return SyntaxFactory.MethodDeclaration (
					method.AttributeLists,
					newModifier,
					method.ReturnType,
					method.ExplicitInterfaceSpecifier,
					method.Identifier,
					method.TypeParameterList,
					method.ParameterList,
					method.ConstraintClauses,
					null,
					method.ExpressionBody,
					SyntaxFactory.Token(SyntaxKind.SemicolonToken) );
			}

			var property = abstractDeclaration as PropertyDeclarationSyntax;
			if (property != null) {
				var accessors = new List<AccessorDeclarationSyntax> ();
				foreach (var accessor in property.AccessorList.Accessors) {
					accessors.Add(SyntaxFactory.AccessorDeclaration(accessor.Kind(), accessor.AttributeLists, accessor.Modifiers, accessor.Keyword, null, SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
				}
				return SyntaxFactory.PropertyDeclaration(
					property.AttributeLists,
					newModifier,
					property.Type,
					property.ExplicitInterfaceSpecifier,
					property.Identifier,
					SyntaxFactory.AccessorList(SyntaxFactory.List<AccessorDeclarationSyntax>(accessors)),
					property.ExpressionBody,
					property.Initializer);
			}

			var indexer = abstractDeclaration as IndexerDeclarationSyntax;
			if (indexer != null) {
				var accessors = new List<AccessorDeclarationSyntax> ();
				foreach (var accessor in indexer.AccessorList.Accessors) {
					accessors.Add(SyntaxFactory.AccessorDeclaration(accessor.Kind(), accessor.AttributeLists, accessor.Modifiers, accessor.Keyword, null, SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
				}
				return SyntaxFactory.IndexerDeclaration(
					indexer.AttributeLists,
					newModifier,
					indexer.Type,
					indexer.ExplicitInterfaceSpecifier,
					indexer.ParameterList,
					SyntaxFactory.AccessorList(SyntaxFactory.List<AccessorDeclarationSyntax>(accessors)),
					indexer.ExpressionBody);
			}
			var evt = abstractDeclaration as EventDeclarationSyntax;
			if (evt != null) {
				return SyntaxFactory.EventFieldDeclaration(
					evt.AttributeLists,
					newModifier,
					SyntaxFactory.VariableDeclaration(
						evt.Type,
						SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>(
							new [] {
								SyntaxFactory.VariableDeclarator(evt.Identifier)
							}
						)
					)
				);
			}
			var evt2 = abstractDeclaration as EventFieldDeclarationSyntax;
			if (evt2 != null) {
				return evt2.WithModifiers(newModifier);
			}
			return null;
		}

		static SyntaxNode AddModifier(MemberDeclarationSyntax abstractDeclaration, SyntaxKind token)
		{
			var method = abstractDeclaration as MethodDeclarationSyntax;

			var modifier = abstractDeclaration.GetModifiers();
			var newMods = modifier.Where(m => !m.IsKind(SyntaxKind.AbstractKeyword) && !m.IsKind(SyntaxKind.StaticKeyword));

			newMods = newMods.Concat(
				new [] { SyntaxFactory.Token(token) }
			);

			var newModifier = SyntaxFactory.TokenList(newMods); 

			if (method != null) {
				return method.WithModifiers(newModifier);
			}

			var property = abstractDeclaration as PropertyDeclarationSyntax;
			if (property != null) {
				return property.WithModifiers(newModifier);
			}

			var indexer = abstractDeclaration as IndexerDeclarationSyntax;
			if (indexer != null) {
				return indexer.WithModifiers(newModifier);
			}

			var evt = abstractDeclaration as EventDeclarationSyntax;
			if (evt != null) {
				return evt.WithModifiers(newModifier);
			}

			var evt2 = abstractDeclaration as EventFieldDeclarationSyntax;
			if (evt2 != null) {
				return evt2.WithModifiers(newModifier);
			}
			return null;
		}

		static SyntaxNode RemoveVirtualModifier(MemberDeclarationSyntax abstractDeclaration)
		{
			var method = abstractDeclaration as MethodDeclarationSyntax;

			if (method != null) {
				return method.WithModifiers(SyntaxFactory.TokenList(method.Modifiers.Where(m => !m.IsKind(SyntaxKind.VirtualKeyword))));
			}

			var property = abstractDeclaration as PropertyDeclarationSyntax;
			if (property != null) {
				return property.WithModifiers(SyntaxFactory.TokenList(property.Modifiers.Where(m => !m.IsKind(SyntaxKind.VirtualKeyword))));
			}

			var indexer = abstractDeclaration as IndexerDeclarationSyntax;
			if (indexer != null) {
				return indexer.WithModifiers(SyntaxFactory.TokenList(indexer.Modifiers.Where(m => !m.IsKind(SyntaxKind.VirtualKeyword))));
			}

			var evt = abstractDeclaration as EventDeclarationSyntax;
			if (evt != null) {
				return evt.WithModifiers(SyntaxFactory.TokenList(evt.Modifiers.Where(m => !m.IsKind(SyntaxKind.VirtualKeyword))));
			}

			var evt2 = abstractDeclaration as EventFieldDeclarationSyntax;
			if (evt2 != null) {
				return evt2.WithModifiers(SyntaxFactory.TokenList(evt2.Modifiers.Where(m => !m.IsKind(SyntaxKind.VirtualKeyword))));
			}
			return abstractDeclaration;
		}
	}
}