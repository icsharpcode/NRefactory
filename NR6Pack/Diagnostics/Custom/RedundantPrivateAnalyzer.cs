// 
// RedundantPrivateInspector.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	/// <summary>
	/// Finds redundant internal modifiers.
	/// </summary>
	public class RedundantPrivateAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.RedundantPrivateAnalyzerID, 
			GettextCatalog.GetString("Removes 'private' modifiers that are not required"),
			GettextCatalog.GetString("'private' modifier is redundant"), 
			DiagnosticAnalyzerCategories.RedundanciesInCode, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.RedundantPrivateAnalyzerID),
			customTags: DiagnosticCustomTags.Unnecessary
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryGetDiagnostic (nodeContext, out diagnostic)) {
						nodeContext.ReportDiagnostic(diagnostic);
					}
				}, 
				new SyntaxKind[] { 
					SyntaxKind.MethodDeclaration, 
					          SyntaxKind.FieldDeclaration,
					          SyntaxKind.PropertyDeclaration,
					          SyntaxKind.IndexerDeclaration,
					          SyntaxKind.EventDeclaration,
					          SyntaxKind.ConstructorDeclaration,
					          SyntaxKind.OperatorDeclaration,
					          SyntaxKind.ClassDeclaration,
					          SyntaxKind.InterfaceDeclaration,
					          SyntaxKind.StructDeclaration,
					          SyntaxKind.EnumDeclaration,
					          SyntaxKind.DelegateDeclaration
				}
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;

			var methodDeclaration = nodeContext.Node as MethodDeclarationSyntax;
			if (methodDeclaration != null && methodDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))) {
				diagnostic = Diagnostic.Create (descriptor, methodDeclaration.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
				return true;
			}

			var fieldDeclaration = nodeContext.Node as FieldDeclarationSyntax;
			if (fieldDeclaration != null && fieldDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))) {
				diagnostic = Diagnostic.Create (descriptor, fieldDeclaration.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
				return true;
			}

			var propertyDeclaration = nodeContext.Node as PropertyDeclarationSyntax;
			if (propertyDeclaration != null && propertyDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))) {
				diagnostic = Diagnostic.Create (descriptor, propertyDeclaration.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
				return true;
			}

			var indexerDeclaration = nodeContext.Node as IndexerDeclarationSyntax;
			if (indexerDeclaration != null && indexerDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))) {
				diagnostic = Diagnostic.Create (descriptor, indexerDeclaration.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
				return true;
			}


			var eventDeclaration = nodeContext.Node as EventDeclarationSyntax;
			if (eventDeclaration != null && eventDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))) {
				diagnostic = Diagnostic.Create (descriptor, eventDeclaration.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
				return true;
			}

			var constructorDeclaration = nodeContext.Node as ConstructorDeclarationSyntax;
			if (constructorDeclaration != null && constructorDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))) {
				diagnostic = Diagnostic.Create (descriptor, constructorDeclaration.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
				return true;
			}

			var operatorDeclaration = nodeContext.Node as OperatorDeclarationSyntax;
			if (operatorDeclaration != null && operatorDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))) {
				diagnostic = Diagnostic.Create (descriptor, operatorDeclaration.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
				return true;
			}

			var delegateDeclaration = nodeContext.Node as DelegateDeclarationSyntax;
			if (delegateDeclaration != null && delegateDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))) {
				diagnostic = Diagnostic.Create (descriptor, delegateDeclaration.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
				return true;
			}

			var enumDeclaration = nodeContext.Node as EnumDeclarationSyntax;
			if (enumDeclaration != null && enumDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))) {
				diagnostic = Diagnostic.Create (descriptor, enumDeclaration.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
				return true;
			}

			var structDeclaration = nodeContext.Node as StructDeclarationSyntax;
			if (structDeclaration != null && structDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))) {
				diagnostic = Diagnostic.Create (descriptor, structDeclaration.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
				return true;
			}

			var interfaceDeclaration = nodeContext.Node as InterfaceDeclarationSyntax;
			if (interfaceDeclaration != null && interfaceDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))) {
				diagnostic = Diagnostic.Create (descriptor, interfaceDeclaration.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
				return true;
			}

			var classDeclaration = nodeContext.Node as ClassDeclarationSyntax;
			if (classDeclaration != null && classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))) {
				diagnostic = Diagnostic.Create (descriptor, classDeclaration.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
				return true;
			}

			return false;
		}

		public static SyntaxNode RemoveModifierFromNode(SyntaxNode node, SyntaxKind modifier)
		{
			//there seem to be no base classes to support WithModifiers.
			//dynamic modifiersNode = node;
			//return modifiersNode.WithModifiers(SyntaxFactory.TokenList(modifiersNode.Modifiers.Where(m => !m.IsKind(SyntaxKind.PrivateKeyword))));

			MethodDeclarationSyntax methodNode = node as MethodDeclarationSyntax;
			if (methodNode != null)
				return methodNode.WithModifiers(SyntaxFactory.TokenList(methodNode.Modifiers.Where(m => !m.IsKind(modifier))))
					             .WithLeadingTrivia(methodNode.GetLeadingTrivia());

			FieldDeclarationSyntax fieldNode = node as FieldDeclarationSyntax;
			if (fieldNode != null)
				return fieldNode.WithModifiers(SyntaxFactory.TokenList(fieldNode.Modifiers.Where(m => !m.IsKind(modifier))))
					            .WithLeadingTrivia(fieldNode.GetLeadingTrivia());

			PropertyDeclarationSyntax propertyNode = node as PropertyDeclarationSyntax;
			if (propertyNode != null)
				return propertyNode.WithModifiers(SyntaxFactory.TokenList(propertyNode.Modifiers.Where(m => !m.IsKind(modifier))))
					               .WithLeadingTrivia(propertyNode.GetLeadingTrivia());

			IndexerDeclarationSyntax indexerNode = node as IndexerDeclarationSyntax;
			if (indexerNode != null)
				return indexerNode.WithModifiers(SyntaxFactory.TokenList(indexerNode.Modifiers.Where(m => !m.IsKind(modifier))))
					              .WithLeadingTrivia(indexerNode.GetLeadingTrivia());

			EventDeclarationSyntax eventNode = node as EventDeclarationSyntax;
			if (eventNode != null)
				return eventNode.WithModifiers(SyntaxFactory.TokenList(eventNode.Modifiers.Where(m => !m.IsKind(modifier))))
					            .WithLeadingTrivia(eventNode.GetLeadingTrivia());

			ConstructorDeclarationSyntax ctrNode = node as ConstructorDeclarationSyntax;
			if (ctrNode != null)
				return ctrNode.WithModifiers(SyntaxFactory.TokenList(ctrNode.Modifiers.Where(m => !m.IsKind(modifier))))
					          .WithLeadingTrivia(ctrNode.GetLeadingTrivia());

			OperatorDeclarationSyntax opNode = node as OperatorDeclarationSyntax;
			if (opNode != null)
				return opNode.WithModifiers(SyntaxFactory.TokenList(opNode.Modifiers.Where(m => !m.IsKind(modifier))))
					         .WithLeadingTrivia(opNode.GetLeadingTrivia());

			ClassDeclarationSyntax classNode = node as ClassDeclarationSyntax;
			if (classNode != null)
				return classNode.WithModifiers(SyntaxFactory.TokenList(classNode.Modifiers.Where(m => !m.IsKind(modifier))))
					            .WithLeadingTrivia(classNode.GetLeadingTrivia());

			InterfaceDeclarationSyntax interfaceNode = node as InterfaceDeclarationSyntax;
			if (interfaceNode != null)
				return interfaceNode.WithModifiers(SyntaxFactory.TokenList(interfaceNode.Modifiers.Where(m => !m.IsKind(modifier))))
					                .WithLeadingTrivia(interfaceNode.GetLeadingTrivia());

			StructDeclarationSyntax structNode = node as StructDeclarationSyntax;
			if (structNode != null)
				return structNode.WithModifiers(SyntaxFactory.TokenList(structNode.Modifiers.Where(m => !m.IsKind(modifier))))
					             .WithLeadingTrivia(structNode.GetLeadingTrivia());

			var enumNode = node as EnumDeclarationSyntax;
			if (enumNode != null)
				return enumNode.WithModifiers(SyntaxFactory.TokenList(enumNode.Modifiers.Where(m => !m.IsKind(modifier))))
					             .WithLeadingTrivia(enumNode.GetLeadingTrivia());

			var delegateNode = node as DelegateDeclarationSyntax;
			if (delegateNode != null)
				return delegateNode.WithModifiers(SyntaxFactory.TokenList(delegateNode.Modifiers.Where(m => !m.IsKind(modifier))))
					             .WithLeadingTrivia(delegateNode.GetLeadingTrivia());
			return node;
		}
	}
}