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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class DoNotCallOverridableMethodsInConstructorAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.DoNotCallOverridableMethodsInConstructorAnalyzerID, 
			GettextCatalog.GetString("Warns about calls to virtual member functions occuring in the constructor"),
			GettextCatalog.GetString("Virtual member call in constructor"), 
			DiagnosticAnalyzerCategories.CodeQualityIssues, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.DoNotCallOverridableMethodsInConstructorAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					IEnumerable<Diagnostic> diagnostics;
					if (TryGetDiagnostic(nodeContext, out diagnostics))
						foreach (var diagnostic in diagnostics)
							nodeContext.ReportDiagnostic(diagnostic);
				},
				SyntaxKind.ConstructorDeclaration
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out IEnumerable<Diagnostic> diagnostic)
		{
			diagnostic = default(IEnumerable<Diagnostic>);

			var node = nodeContext.Node as ConstructorDeclarationSyntax;
			var type = node.Parent as TypeDeclarationSyntax;
			if (type == null || type.Modifiers.Any (m => m.IsKind (SyntaxKind.SealedKeyword) || m.IsKind (SyntaxKind.StaticKeyword))) {
				return false;
			}
			var visitor = new VirtualCallFinderVisitor (nodeContext, node, type);
			visitor.Visit (node);
			diagnostic = visitor.Diagnostics;
			return true;
		}


		class VirtualCallFinderVisitor : CSharpSyntaxWalker
		{
			readonly SyntaxNodeAnalysisContext nodeContext;
			internal List<Diagnostic> Diagnostics = new List<Diagnostic> ();
			ConstructorDeclarationSyntax node;
			TypeDeclarationSyntax type;

			public VirtualCallFinderVisitor (SyntaxNodeAnalysisContext nodeContext, ConstructorDeclarationSyntax node, TypeDeclarationSyntax type)
			{
				this.nodeContext = nodeContext;
				this.node = node;
				this.type = type;
			}

			void Check(SyntaxNode n)
			{
				var info = nodeContext.SemanticModel.GetSymbolInfo(n);
				var symbol = info.Symbol;
				if (symbol == null || symbol.ContainingType.Locations.Where (loc => loc.IsInSource && loc.SourceTree.FilePath == type.SyntaxTree.FilePath).All (loc => !type.Span.Contains (loc.SourceSpan)))
					return;
				if (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride) {
					Diagnostics.Add(Diagnostic.Create(descriptor, n.GetLocation()));
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