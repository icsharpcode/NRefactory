//
// DoNotCallOverridableMethodsInConstructorAnalyzer.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Threading;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "DoNotCallOverridableMethodsInConstructor")]
	public class DoNotCallOverridableMethodsInConstructorAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId = "DoNotCallOverridableMethodsInConstructorAnalyzer";
		const string Description = "Warns about calls to virtual member functions occuring in the constructor.";
		const string MessageFormat = "Virtual member call in constructor";
		const string Category = DiagnosticAnalyzerCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Virtual member call in constructor");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<DoNotCallOverridableMethodsInConstructorAnalyzer>
		{
			readonly VirtualCallFinderVisitor CallFinder;

			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
				CallFinder = new VirtualCallFinderVisitor(this, semanticModel, addDiagnostic, cancellationToken);
			}

			INamedTypeSymbol currentType;

			public override void VisitClassDeclaration(ClassDeclarationSyntax node)
			{
				var olddeclaredSymbol = currentType;
				currentType = semanticModel.GetDeclaredSymbol(node); 
				base.VisitClassDeclaration(node);
				currentType = olddeclaredSymbol;
			}

			public override void VisitStructDeclaration(StructDeclarationSyntax node)
			{
				var olddeclaredSymbol = currentType;
				currentType = semanticModel.GetDeclaredSymbol(node); 
				base.VisitStructDeclaration(node);
				currentType = olddeclaredSymbol;
			}

			public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
			{
				if (currentType.IsSealed || currentType.IsStatic)
					return;
				var body = node.Body;
				if (body != null)
					CallFinder.Visit(body);
			}

			public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
			{
				// nothing
			}

			public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
			{
				// nothing
			}

			public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
			{
				// nothing
			}

			public override void VisitEventDeclaration(EventDeclarationSyntax node)
			{
				// nothing
			}

			public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
			{
				// nothing
			}

			public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
			{
				// nothing
			}

			public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
			{
				// nothing
			}

			public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
			{
				// nothing
			}

			public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
			{
				// nothing
			}

			public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
			{
				// nothing
			}

			class VirtualCallFinderVisitor: GatherVisitorBase<DoNotCallOverridableMethodsInConstructorAnalyzer>
			{
				GatherVisitor gv;

				public VirtualCallFinderVisitor(GatherVisitor gv, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
					: base(semanticModel, addDiagnostic, cancellationToken)
				{
					this.gv = gv;
				}

				void Check(SyntaxNode n)
				{
					var symbol = semanticModel.GetSymbolInfo(n);
					if (symbol.Symbol == null || symbol.Symbol.ContainingType != gv.currentType)
						return;
					if (symbol.Symbol.IsVirtual ||
						symbol.Symbol.IsAbstract ||
						symbol.Symbol.IsOverride) {
						AddDiagnosticAnalyzer(Diagnostic.Create(Rule, n.GetLocation()));
					}
				}

				public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
				{
					base.VisitMemberAccessExpression(node);
					if (node.Parent is MemberAccessExpressionSyntax || node.Parent is InvocationExpressionSyntax)
						return;
					if (node.Expression.IsKind(SyntaxKind.ThisExpression))
						Check(node);
				}

				static bool IsSimpleThisCall(ExpressionSyntax expression)
				{
					var ma = expression as MemberAccessExpressionSyntax;
					if (ma == null)
						return false;
					return ma.Expression.IsKind(SyntaxKind.ThisExpression);
				}

				public override void VisitInvocationExpression(InvocationExpressionSyntax node)
				{
					base.VisitInvocationExpression(node);
					if (node.Parent is MemberAccessExpressionSyntax || node.Parent is InvocationExpressionSyntax)
						return;
					if (node.Expression.IsKind(SyntaxKind.IdentifierName) || IsSimpleThisCall(node.Expression))
						Check(node);
				}

				public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
				{
					// ignore lambdas
				}

				public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
				{
					// ignore lambdas
				}

				public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
				{
					// ignore anonymous methods
				}
			}
		}
	}

	//	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	//	public class DoNotCallOverridableMethodsInConstructorFixProvider : NRefactoryCodeFixProvider
	//	{
	//		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
	//		{
	//			yield return DoNotCallOverridableMethodsInConstructorAnalyzer.DiagnosticId;
	//		}
	//
	//		public override async Task ComputeFixesAsync(CodeFixContext  context)
	//		{
	//			var root = await document.GetSyntaxRootAsync(cancellationToken);
	//			var result = new List<CodeAction>();
	//			var diagnostic = diagnostics.First ();
	//				var node = root.FindNode(context.Span);
	//				//if (!node.IsKind(SyntaxKind.BaseList))
	//				//	continue;
	//				var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
	//				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Make class '{0}' sealed", document.WithSyntaxRoot(newRoot)), diagnostic);
	//			return result;
	//		}
	//	}
}