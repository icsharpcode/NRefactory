// 
// CS0108UseNewKeywordIfHidingIntendedAnalyzer.cs
// 
// Author:
//      Mark Garnett 
// 
// Copyright (c) 2014 Mark Garnett <mg10g13@soton.ac.uk>
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

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "CSharpWarnings::CS0108", PragmaWarning = 108)]
	public class CS0108UseNewKeywordIfHidingIntendedAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId = "CS0108UseNewKeywordIfHidingIntendedAnalyzer";
		const string Description = "CS0108:member1 hides inherited member member2";
		const string MessageFormat = "member1 hides inherited member member2. Use the new keyword if hiding was intended.";
		const string Category = DiagnosticAnalyzerCategories.CompilerWarnings;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "CS0108:member1 hides inherited member member2. Use the new keyword if hiding was intended.");

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

		class GatherVisitor : GatherVisitorBase<CS0108UseNewKeywordIfHidingIntendedAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
			{
				foreach (var variable in node.Declaration.Variables) {
					//this can only be 1; if it's null, then there was no base field
					var hidden = semanticModel.LookupBaseMembers(variable.SpanStart).Where(v => v.Name.Equals(variable.Identifier.ValueText)).FirstOrDefault();
					if (hidden == null)
						return;
					else {
						AddDiagnosticAnalyzer(Diagnostic.Create(Rule, variable.GetLocation()));
						return;
					}
				}
			}

			public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
			{
				if (node.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.OverrideKeyword)))
					return;

				var hidden = semanticModel.LookupBaseMembers(node.SpanStart).Where(v => v.Name.Equals(node.Identifier.ValueText)).FirstOrDefault();
				if (hidden == null)
					return;
				else //note we just need the span of the identifier, not the entire property body
					AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.Identifier.GetLocation()));
			}

			public override void VisitClassDeclaration(ClassDeclarationSyntax node)
			{
				base.VisitClassDeclaration(node);
				//we need to ignore non-nested classes, else they throw exceptions
				if (node.Parent is CompilationUnitSyntax)
					return;
				var hidden = semanticModel.LookupBaseMembers(node.SpanStart).Where(v => v.Name.Equals(node.Identifier.ValueText)).FirstOrDefault();
				if (hidden == null)
					return;
				else //note we just need the span of the identifier, not the entire property body
					AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.Identifier.GetLocation()));
			}

			public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
			{
				//ignore overriding methods
				if (node.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.OverrideKeyword)))
					return;

				//ignore virtual/abstract base methods, and check the method signature
				var methods = semanticModel.LookupBaseMembers(node.SpanStart).Where(v => (!v.IsVirtual) && (!v.IsAbstract) &&
					v.Name.Equals(node.Identifier.ValueText));
				foreach (var method in methods) {
					//if they have the same name and the same parameters, it's an issue
					if (DoParametersMatch(method, semanticModel.GetDeclaredSymbol(node)))
						AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.Identifier.GetLocation()));
				}
			}

			public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
			{
				if (node.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.OverrideKeyword)))
					return;

				var nodeSymbol = semanticModel.GetDeclaredSymbol(node);
				var containingClass = nodeSymbol.ContainingType;
				var baseType = containingClass.BaseType;
				if (baseType == null)
					return; //somehow we're at System.Object already..
				//hacky, but there seems to be no other way - go through the inheritance hierarchy until we reach System.Object, looking for an indexer with the same parameters
				do {
					//break and quit if we cannot find an indexer in the base classes
					var b = baseType.GetMembers().Where(m => m.Name.Equals("this[]")).FirstOrDefault();
					if (b == null)
						baseType = baseType.BaseType;
					else {
						//if they have the same name and the same parameters, it's an issue
						if (DoParametersMatch(b, semanticModel.GetDeclaredSymbol(node))) {
							AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.ThisKeyword.GetLocation()));
							return; //we found it
						}

					}
				} while (baseType != null);
			}

			public override void VisitStructDeclaration(StructDeclarationSyntax node)
			{
				base.VisitStructDeclaration(node);
				//we need to ignore non-nested
				if (node.Parent is CompilationUnitSyntax)
					return;
				var hidden = semanticModel.LookupBaseMembers(node.SpanStart).Where(v => v.Name.Equals(node.Identifier.ValueText)).FirstOrDefault();
				if (hidden == null)
					return;
				else //note we just need the span of the identifier, not the entire property body
					AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.Identifier.GetLocation()));
			}

			public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
			{
				if (node.Parent is CompilationUnitSyntax)
					return;
				base.VisitInterfaceDeclaration(node);
				var hidden = semanticModel.LookupBaseMembers(node.SpanStart).Where(v => v.Name.Equals(node.Identifier.ValueText)).FirstOrDefault();
				if (hidden == null)
					return;
				else //note we just need the span of the identifier, not the entire property body
					AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.Identifier.GetLocation()));
			}

			public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
			{
				//we need to ignore non-nested
				if (node.Parent is CompilationUnitSyntax)
					return;
				var hidden = semanticModel.LookupBaseMembers(node.SpanStart).Where(v => v.Name.Equals(node.Identifier.ValueText)).FirstOrDefault();
				if (hidden == null)
					return;
				else //note we just need the span of the identifier, not the entire property body
					AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.Identifier.GetLocation()));
			}

			public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
			{
				if (node.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.OverrideKeyword)))
					return;

				foreach (var variable in node.Declaration.Variables) {
					//this can only be 1; if it's null, then there was no base field
					var hidden = semanticModel.LookupBaseMembers(variable.SpanStart).Where(v => v.Name.Equals(variable.Identifier.ValueText)).FirstOrDefault();
					if (hidden == null)
						return;
					else {
						AddDiagnosticAnalyzer(Diagnostic.Create(Rule, variable.GetLocation()));
						return;
					}
				}
			}

			public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
			{
				if (node.Parent is CompilationUnitSyntax)
					return;

				var nodeSymbol = semanticModel.GetDeclaredSymbol(node);
				var containingClass = nodeSymbol.ContainingType;
				var baseType = containingClass.BaseType;
				if (baseType == null)
					return; //somehow we're at System.Object already..
				do {
					//break and quit if we cannot find an indexer in the base classes
					var b = baseType.GetMembers().Where(m => m.Name.Equals(nodeSymbol.Name)).FirstOrDefault();
					if (b == null)
						baseType = baseType.BaseType;
					else {
						//if they have the same name and the same parameters, it's an issue
						if (DoParametersMatch(b, semanticModel.GetDeclaredSymbol(node))) {
							AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.Identifier.GetLocation()));
							return; //we found it
						}

					}
				} while (baseType != null);
			}

			private bool DoParametersMatch(ISymbol a, ISymbol b)
			{
				var parameters = a.GetParameters();
				var derivedParameters = b.GetParameters();
				return parameters.SequenceEqual(derivedParameters, new ParameterTypeComparer());
			}
		}
	}
	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class CS0108UseNewKeywordIfHidingIntendedIssueFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return CS0108UseNewKeywordIfHidingIntendedAnalyzer.DiagnosticId;
		}

		public override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public async override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			var cancellationToken = context.CancellationToken;
			var span = context.Span;
			var diagnostics = context.Diagnostics;


			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();

			var diagnostic = diagnostics.First ();
			var node = root.FindNode(context.Span);
			context.RegisterCodeFix(CodeActionFactory.Create(
				node.Span,
				diagnostic.Severity,
				"Add new modifier to method",
				token =>
				{
					SyntaxNode newRoot;
					if (node.Kind() != SyntaxKind.VariableDeclarator)
						newRoot = root.ReplaceNode(node, AddNewModifier(node));
					else //this one wants to be awkward - you can't add modifiers to a variable declarator
                    {
						SyntaxNode declaringNode = node.Parent.Parent;
						if (declaringNode is FieldDeclarationSyntax)
							newRoot = root.ReplaceNode(node.Parent.Parent, (node.Parent.Parent as FieldDeclarationSyntax).AddModifiers(SyntaxFactory.Token(SyntaxKind.NewKeyword)));
						else //it's an event declaration
							newRoot = root.ReplaceNode(node.Parent.Parent, (node.Parent.Parent as EventFieldDeclarationSyntax).AddModifiers(SyntaxFactory.Token(SyntaxKind.NewKeyword)));
					}
					return Task.FromResult(document.WithSyntaxRoot(newRoot.WithAdditionalAnnotations(Formatter.Annotation)));
				}), diagnostic);
		}

		private SyntaxNode AddNewModifier(SyntaxNode node)
		{
			SyntaxToken newToken = SyntaxFactory.Token(SyntaxKind.NewKeyword);
			switch (node.Kind()) {
				//couldn't find a common base
				case SyntaxKind.IndexerDeclaration:
					var indexer = (IndexerDeclarationSyntax)node;
					return indexer.AddModifiers(newToken);
				case SyntaxKind.ClassDeclaration:
					var classDecl = (ClassDeclarationSyntax)node;
					return classDecl.AddModifiers(newToken);
				case SyntaxKind.PropertyDeclaration:
					var propDecl = (PropertyDeclarationSyntax)node;
					return propDecl.AddModifiers(newToken);
				case SyntaxKind.MethodDeclaration:
					var methDecl = (MethodDeclarationSyntax)node;
					return methDecl.AddModifiers(newToken);
				case SyntaxKind.StructDeclaration:
					var structDecl = (StructDeclarationSyntax)node;
					return structDecl.AddModifiers(newToken);
				case SyntaxKind.EnumDeclaration:
					var enumDecl = (EnumDeclarationSyntax)node;
					return enumDecl.AddModifiers(newToken);
				case SyntaxKind.InterfaceDeclaration:
					var intDecl = (InterfaceDeclarationSyntax)node;
					return intDecl.AddModifiers(newToken);
				case SyntaxKind.DelegateDeclaration:
					var deleDecl = (DelegateDeclarationSyntax)node;
					return deleDecl.AddModifiers(newToken);
				default:
					return node;
			}
		}
	}

	public class ParameterTypeComparer : IEqualityComparer<IParameterSymbol>
	{

		public bool Equals(IParameterSymbol x, IParameterSymbol y)
		{
			return x.Type.Equals(y.Type);
		}

		public int GetHashCode(IParameterSymbol obj)
		{
			return obj.GetHashCode();
		}
	}
}
