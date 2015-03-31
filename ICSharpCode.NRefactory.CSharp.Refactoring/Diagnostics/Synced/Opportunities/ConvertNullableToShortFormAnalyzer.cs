//
// ConvertNullableToShortFormAnalyzer.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ConvertNullableToShortForm")]
	public class ConvertNullableToShortFormAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "ConvertNullableToShortFormAnalyzer";
		const string Description            = "Convert 'Nullable<T>' to the short form 'T?'";
		const string MessageFormat          = "Nullable type can be simplified.";
		const string Category               = DiagnosticAnalyzerCategories.Opportunities;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "Convert 'Nullable<T>' to 'T?'");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		internal static TypeSyntax GetTypeArgument (SyntaxNode node)
		{
			var gns = node as GenericNameSyntax;
			if (gns == null) {
				var qns = node as QualifiedNameSyntax;
				if (qns != null)
					gns = qns.Right as GenericNameSyntax;
			} else {
				var parent = gns.Parent as QualifiedNameSyntax;
				if (parent != null && parent.Right == node)
					return null;
			}

			if (gns != null) {
				if (gns.TypeArgumentList.Arguments.Count == 1) {
					var typeArgument = gns.TypeArgumentList.Arguments[0];
					if (!typeArgument.IsKind(SyntaxKind.OmittedTypeArgument))
						return typeArgument;
				}
			}

			return null;
		}

		class GatherVisitor : GatherVisitorBase<ConvertNullableToShortFormAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			void CheckType(SyntaxNode simpleType)
			{
				if (GetTypeArgument(simpleType) == null)
					return;
				var rr = semanticModel.GetSymbolInfo(simpleType);
				var type = rr.Symbol as ITypeSymbol;
				if (type == null || type.Name != "Nullable" || type.ContainingNamespace.ToDisplayString() != "System")
					return;

				AddDiagnosticAnalyzer (Diagnostic.Create(Rule, Location.Create(semanticModel.SyntaxTree, simpleType.Span)));
			}

			public override void VisitQualifiedName(QualifiedNameSyntax node)
			{
				base.VisitQualifiedName(node);
				CheckType(node);
			}

			public override void VisitGenericName(GenericNameSyntax node)
			{
				base.VisitGenericName(node);
				CheckType(node);
			}
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class ConvertNullableToShortFormFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ConvertNullableToShortFormAnalyzer.DiagnosticId;
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
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var diagnostic = diagnostics.First ();
			var node = root.FindNode(context.Span);

			//if (!node.IsKind(SyntaxKind.GenericName))
			//	continue;

			var arg = ConvertNullableToShortFormAnalyzer.GetTypeArgument(node);


			var newRoot = root.ReplaceNode((SyntaxNode)node, SyntaxFactory.NullableType(arg)
				.WithAdditionalAnnotations(Formatter.Annotation)
				.WithLeadingTrivia(node.GetLeadingTrivia()));
			context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Rewrite to '{0}?'", document.WithSyntaxRoot(newRoot)), diagnostic);
		}
	}
}