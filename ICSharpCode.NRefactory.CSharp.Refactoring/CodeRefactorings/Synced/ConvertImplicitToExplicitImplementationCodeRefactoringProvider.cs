// 
// ConvertImplicitToExplicitImplementationAction.cs
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
using System.Threading;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[NRefactoryCodeRefactoringProvider(Description = "Convert implict implementation of an interface method to explicit implementation")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Convert implict to explicit implementation")]
	public class ConvertImplicitToExplicitImplementationCodeRefactoringProvider : CodeRefactoringProvider
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
			var node = root.FindNode(span);
			while (node != null && !(node is MemberDeclarationSyntax))
				node = node.Parent;
			if (node == null)
				return;

			if (!node.IsKind(SyntaxKind.MethodDeclaration) &&
				!node.IsKind(SyntaxKind.PropertyDeclaration) &&
				!node.IsKind(SyntaxKind.IndexerDeclaration) &&
				!node.IsKind(SyntaxKind.EventDeclaration))
				return;
//			if (!node.NameToken.Contains (context.Location))
//				return null;

			var memberDeclaration = node as MemberDeclarationSyntax;
			var explicitSyntax = memberDeclaration.GetExplicitInterfaceSpecifierSyntax();
			if (explicitSyntax != null)
				return;

			var enclosingSymbol = model.GetDeclaredSymbol(memberDeclaration, cancellationToken);
			if (enclosingSymbol == null)
				return;

			var containingType = enclosingSymbol.ContainingType;
			if (containingType.TypeKind == TypeKind.Interface)
				return;

			var implementingInterface = GetImplementingInterface(enclosingSymbol, containingType);
			if (implementingInterface == null)
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Info, 
					GettextCatalog.GetString ("To explicit implementation"), 
					t2 => {
						var newNode = memberDeclaration;
						var nameSpecifier = SyntaxFactory.ExplicitInterfaceSpecifier(SyntaxFactory.ParseName(implementingInterface.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)))
							.WithAdditionalAnnotations(Simplifier.Annotation);
						switch (newNode.Kind()) {
							case SyntaxKind.MethodDeclaration:
								var method = (MethodDeclarationSyntax)memberDeclaration;
								newNode = method
									.WithModifiers(SyntaxFactory.TokenList())
									.WithExplicitInterfaceSpecifier(nameSpecifier);
								break;
							case SyntaxKind.PropertyDeclaration:
								var property = (PropertyDeclarationSyntax)memberDeclaration;
								newNode = property
									.WithModifiers(SyntaxFactory.TokenList())
									.WithExplicitInterfaceSpecifier(nameSpecifier);
								break;
							case SyntaxKind.IndexerDeclaration:
								var indexer = (IndexerDeclarationSyntax)memberDeclaration;
								newNode = indexer
									.WithModifiers(SyntaxFactory.TokenList())
									.WithExplicitInterfaceSpecifier(nameSpecifier);
								break;
							case SyntaxKind.EventDeclaration:
								var evt = (EventDeclarationSyntax)memberDeclaration;
								newNode = evt
									.WithModifiers(SyntaxFactory.TokenList())
									.WithExplicitInterfaceSpecifier(nameSpecifier);
								break;
						}
						var newRoot = root.ReplaceNode((SyntaxNode)
							memberDeclaration,
							newNode.WithLeadingTrivia(memberDeclaration.GetLeadingTrivia()).WithAdditionalAnnotations(Formatter.Annotation)
						);
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				) 
			);
		}

		static INamedTypeSymbol GetImplementingInterface(ISymbol enclosingSymbol, INamedTypeSymbol containingType)
		{
			INamedTypeSymbol result = null;
			foreach (var iface in containingType.AllInterfaces) {
				foreach (var member in iface.GetMembers()) {
					var implementation = containingType.FindImplementationForInterfaceMember(member);
					if (implementation == enclosingSymbol) {
						if (result != null)
							return null;
						result = iface;
					}
				}
			}
			return result;
		}
	}
}