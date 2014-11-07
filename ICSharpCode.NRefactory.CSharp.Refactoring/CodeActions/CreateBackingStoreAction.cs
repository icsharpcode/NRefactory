// 
// CreateBackingStore.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
	[NRefactoryCodeRefactoringProvider(Description = "Creates a backing field for an auto property")]
	[ExportCodeRefactoringProvider("Create backing store for auto property", LanguageNames.CSharp)]
	public class CreateBackingStoreAction : CodeRefactoringProvider
	{
		public override async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);

			var property = root.FindNode(span) as PropertyDeclarationSyntax;
			if (property == null || !property.Identifier.Span.Contains(span))
				return Enumerable.Empty<CodeAction>();

			if (property.AccessorList.Accessors.Any(b => b.Body != null)) //ignore properties with >=1 accessor body
				return Enumerable.Empty<CodeAction>();
			return new[] {
				CodeActionFactory.Create(
					property.Identifier.Span, 
					DiagnosticSeverity.Info, 
					"Create backing store",
					t2 => {
						string name = GetNameProposal(property.Identifier.ValueText, model, root);

						//create our backing store
						var backingStore = SyntaxFactory.FieldDeclaration(
							SyntaxFactory.VariableDeclaration(
								property.Type,
								SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(name)))
						).WithModifiers(!property.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) ? 
							SyntaxFactory.TokenList() :
							SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
						.WithAdditionalAnnotations(Formatter.Annotation);

						//create our new property
						var fieldExpression = name == "value" ? 
							(ExpressionSyntax)SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(), SyntaxFactory.IdentifierName("value")) : 
							SyntaxFactory.IdentifierName(name);

						var getBody = SyntaxFactory.Block(SyntaxFactory.ReturnStatement(fieldExpression));
						var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, getBody);

						var setBody = SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, fieldExpression,
							             SyntaxFactory.IdentifierName("value"))));
						var setter = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, setBody);

						var newPropAnno = new SyntaxAnnotation();
						var newProperty = property.WithAccessorList(SyntaxFactory.AccessorList(new SyntaxList<AccessorDeclarationSyntax>().Add(getter).Add(setter)))
							.WithAdditionalAnnotations(newPropAnno, Formatter.Annotation);

						var newRoot = root.ReplaceNode(property, newProperty);
						return Task.FromResult(document.WithSyntaxRoot(newRoot.InsertNodesBefore(newRoot.GetAnnotatedNodes(newPropAnno).First(), new List<SyntaxNode>() {
							backingStore
						})));
					})
			};
		}

		public static string GetNameProposal(string name, SemanticModel model, SyntaxNode node)
		{
			string baseName = char.ToLower(name[0]) + name.Substring(1);
			var enclosingClass = node.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();
			if (enclosingClass == null)
				return baseName;

			INamedTypeSymbol typeSymbol = model.GetDeclaredSymbol(enclosingClass);
			IEnumerable<string> members = typeSymbol.MemberNames;

			string proposedName = null;
			int number = 0;
			do {
				proposedName = baseName;
				if (number != 0) {
					proposedName = baseName + number.ToString();
				}
				number++;
			} while (members.Contains(proposedName));
			return proposedName;
		}
	}
}

