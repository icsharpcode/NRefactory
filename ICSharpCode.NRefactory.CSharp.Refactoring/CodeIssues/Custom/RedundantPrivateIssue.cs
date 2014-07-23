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
	[ExportDiagnosticAnalyzer("Remove redundant 'private' modifier", LanguageNames.CSharp)]
	/// <summary>
	/// Finds redundant internal modifiers.
	/// </summary>
	public class RedundantPrivateIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId = "RedundantPrivateIssue";
		const string Description = "Removes 'private' modifiers that are not required";
		const string MessageFormat = "";
		const string Category = IssueCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true);

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
				SyntaxToken privateToken = node.Modifiers.Where(m => m.IsKind(SyntaxKind.PrivateKeyword)).FirstOrDefault();
				if (privateToken != null)
					AddIssue(node, privateToken.GetLocation());
			}

			public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
			{
				SyntaxToken privateToken = node.Modifiers.Where(m => m.IsKind(SyntaxKind.PrivateKeyword)).FirstOrDefault();
				if (privateToken != null)
					AddIssue(node, privateToken.GetLocation());
			}

			public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
			{
				SyntaxToken privateToken = node.Modifiers.Where(m => m.IsKind(SyntaxKind.PrivateKeyword)).FirstOrDefault();
				if (privateToken != null)
					AddIssue(node, privateToken.GetLocation());
			}

			public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
			{
				SyntaxToken privateToken = node.Modifiers.Where(m => m.IsKind(SyntaxKind.PrivateKeyword)).FirstOrDefault();
				if (privateToken != null)
					AddIssue(node, privateToken.GetLocation());
			}

			public override void VisitEventDeclaration(EventDeclarationSyntax node)
			{
				SyntaxToken privateToken = node.Modifiers.Where(m => m.IsKind(SyntaxKind.PrivateKeyword)).FirstOrDefault();
				if (privateToken != null)
					AddIssue(node, privateToken.GetLocation());
			}

			public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
			{
				SyntaxToken privateToken = node.Modifiers.Where(m => m.IsKind(SyntaxKind.PrivateKeyword)).FirstOrDefault();
				if (privateToken != null)
					AddIssue(node, privateToken.GetLocation());
			}

			public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
			{
				SyntaxToken privateToken = node.Modifiers.Where(m => m.IsKind(SyntaxKind.PrivateKeyword)).FirstOrDefault();
				if (privateToken != null)
					AddIssue(node, privateToken.GetLocation());
			}

			public override void VisitClassDeclaration(ClassDeclarationSyntax node)
			{
				if (node.Parent is TypeDeclarationSyntax) {
					SyntaxToken privateToken = node.Modifiers.Where(m => m.IsKind(SyntaxKind.PrivateKeyword)).FirstOrDefault();
					if (privateToken != null)
						AddIssue(node, privateToken.GetLocation());
				}
				base.VisitClassDeclaration(node);
			}

			public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
			{
				if (node.Parent is TypeDeclarationSyntax) {
					SyntaxToken privateToken = node.Modifiers.Where(m => m.IsKind(SyntaxKind.PrivateKeyword)).FirstOrDefault();
					if (privateToken != null)
						AddIssue(node, privateToken.GetLocation());
				}
				base.VisitInterfaceDeclaration(node);
			}

			public override void VisitStructDeclaration(StructDeclarationSyntax node)
			{
				if (node.Parent is StructDeclarationSyntax) {
					SyntaxToken privateToken = node.Modifiers.Where(m => m.IsKind(SyntaxKind.PrivateKeyword)).FirstOrDefault();
					if (privateToken != null)
						AddIssue(node, privateToken.GetLocation());
				}
				base.VisitStructDeclaration(node);
			}
		}
	}

	[ExportCodeFixProvider(RedundantPrivateIssue.DiagnosticId, LanguageNames.CSharp)]
	public class RedundantPrivateFixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return RedundantPrivateIssue.DiagnosticId;
		}

		public async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagonstic in diagnostics) {
				var node = root.FindNode(diagonstic.Location.SourceSpan);
				var newRoot = root.ReplaceNode(node, RemovePrivateFromNode(node).WithAdditionalAnnotations(Formatter.Annotation));
				result.Add(CodeActionFactory.Create(node.Span, diagonstic.Severity, diagonstic.GetMessage(), document.WithSyntaxRoot(newRoot)));
			}
			return result;
		}

		private SyntaxNode RemovePrivateFromNode(SyntaxNode node)
		{
			//there seem to be no base classes to support WithModifiers.
			//dynamic modifiersNode = node;
			//return modifiersNode.WithModifiers(SyntaxFactory.TokenList(modifiersNode.Modifiers.Where(m => !m.IsKind(SyntaxKind.PrivateKeyword))));

			MethodDeclarationSyntax methodNode = node as MethodDeclarationSyntax;
			if (methodNode != null)
				return methodNode.WithModifiers(SyntaxFactory.TokenList(methodNode.Modifiers.Where(m => !m.IsKind(SyntaxKind.PrivateKeyword))));

			FieldDeclarationSyntax fieldNode = node as FieldDeclarationSyntax;
			if (fieldNode != null)
				return fieldNode.WithModifiers(SyntaxFactory.TokenList(fieldNode.Modifiers.Where(m => !m.IsKind(SyntaxKind.PrivateKeyword))));

			PropertyDeclarationSyntax propertyNode = node as PropertyDeclarationSyntax;
			if (propertyNode != null)
				return propertyNode.WithModifiers(SyntaxFactory.TokenList(propertyNode.Modifiers.Where(m => !m.IsKind(SyntaxKind.PrivateKeyword))));

			IndexerDeclarationSyntax indexerNode = node as IndexerDeclarationSyntax;
			if (indexerNode != null)
				return indexerNode.WithModifiers(SyntaxFactory.TokenList(indexerNode.Modifiers.Where(m => !m.IsKind(SyntaxKind.PrivateKeyword))));

			EventDeclarationSyntax eventNode = node as EventDeclarationSyntax;
			if (eventNode != null)
				return eventNode.WithModifiers(SyntaxFactory.TokenList(eventNode.Modifiers.Where(m => !m.IsKind(SyntaxKind.PrivateKeyword))));

			ConstructorDeclarationSyntax ctrNode = node as ConstructorDeclarationSyntax;
			if (ctrNode != null)
				return ctrNode.WithModifiers(SyntaxFactory.TokenList(ctrNode.Modifiers.Where(m => !m.IsKind(SyntaxKind.PrivateKeyword))));

			OperatorDeclarationSyntax opNode = node as OperatorDeclarationSyntax;
			if (opNode != null)
				return opNode.WithModifiers(SyntaxFactory.TokenList(opNode.Modifiers.Where(m => !m.IsKind(SyntaxKind.PrivateKeyword))));

			ClassDeclarationSyntax classNode = node as ClassDeclarationSyntax;
			if (classNode != null)
				return classNode.WithModifiers(SyntaxFactory.TokenList(classNode.Modifiers.Where(m => !m.IsKind(SyntaxKind.PrivateKeyword))));

			InterfaceDeclarationSyntax interfaceNode = node as InterfaceDeclarationSyntax;
			if (interfaceNode != null)
				return interfaceNode.WithModifiers(SyntaxFactory.TokenList(interfaceNode.Modifiers.Where(m => !m.IsKind(SyntaxKind.PrivateKeyword))));

			StructDeclarationSyntax structNode = node as StructDeclarationSyntax;
			if (structNode != null)
				return structNode.WithModifiers(SyntaxFactory.TokenList(structNode.Modifiers.Where(m => !m.IsKind(SyntaxKind.PrivateKeyword))));

			return node;
		}
	}
}