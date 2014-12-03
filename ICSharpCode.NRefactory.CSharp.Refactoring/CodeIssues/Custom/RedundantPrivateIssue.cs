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

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using System.Threading;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	/// <summary>
	/// Finds redundant internal modifiers.
	/// </summary>
	public class RedundantPrivateIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId = "RedundantPrivateIssue";
		const string Description = "Removes 'private' modifiers that are not required";
		const string MessageFormat = "";
		const string Category = IssueCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Remove redundant 'private' modifier");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
		{
			get
			{
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
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

			return node;
		}

		class GatherVisitor : GatherVisitorBase<RedundantPrivateIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}
			private void AddIssue(SyntaxNode node, Location location)
			{
				AddIssue(Diagnostic.Create(Rule, location));
			}

			public override void VisitDestructorDeclaration(DestructorDeclarationSyntax node)
			{
				// SKIP
			}

			public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
			{
				if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
					AddIssue(node, node.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
			}

			public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
			{
				if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
					AddIssue(node, node.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
			}

			public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
			{
				if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
					AddIssue(node, node.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
			}

			public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
			{
				if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
					AddIssue(node, node.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
			}

			public override void VisitEventDeclaration(EventDeclarationSyntax node)
			{
				if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
					AddIssue(node, node.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
			}

			public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
			{
				if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
					AddIssue(node, node.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
			}

			public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
			{
				if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
					AddIssue(node, node.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
			}

			public override void VisitClassDeclaration(ClassDeclarationSyntax node)
			{
				if (node.Parent is TypeDeclarationSyntax) {
					if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
						AddIssue(node, node.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
				}
				base.VisitClassDeclaration(node);
			}

			public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
			{
				if (node.Parent is TypeDeclarationSyntax) {
					if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
						AddIssue(node, node.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
				}
				base.VisitInterfaceDeclaration(node);
			}

			public override void VisitStructDeclaration(StructDeclarationSyntax node)
			{
				if (node.Parent is StructDeclarationSyntax) {
					if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
						AddIssue(node, node.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PrivateKeyword)).GetLocation());
				}
				base.VisitStructDeclaration(node);
			}
		}
	}

	[ExportCodeFixProvider(RedundantPrivateIssue.DiagnosticId, LanguageNames.CSharp)]
	public class RedundantPrivateFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return RedundantPrivateIssue.DiagnosticId;
		}

		public override async Task ComputeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			var cancellationToken = context.CancellationToken;
			var span = context.Span;
			var diagnostics = context.Diagnostics;
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagnostic in diagnostics) {
				var node = root.FindNode(diagnostic.Location.SourceSpan);
				var newRoot = root.ReplaceNode((SyntaxNode)node, RedundantPrivateIssue.RemoveModifierFromNode(node, SyntaxKind.PrivateKeyword).WithAdditionalAnnotations(Formatter.Annotation));
				context.RegisterFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Remove redundant 'private' modifier", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}