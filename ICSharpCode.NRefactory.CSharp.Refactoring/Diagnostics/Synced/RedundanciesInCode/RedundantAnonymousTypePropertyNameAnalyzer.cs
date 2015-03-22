//
// RedundantAnonymousTypePropertyNameAnalyzer.cs
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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.ComponentModel;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzerAttribute(AnalysisDisableKeyword = "RedundantAnonymousTypePropertyName")]
	[Description("Redundant explicit property name")]
	public class RedundantAnonymousTypePropertyNameAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "RedundantAnonymousTypePropertyNameAnalyzer";
		const string Category               = DiagnosticAnalyzerCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, "The name can be inferred from the initializer expression", "Redundant explicit property name", Category, DiagnosticSeverity.Warning, true, "Redundant anonymous type property namen");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<RedundantAnonymousTypePropertyNameAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			static string GetAnonymousTypePropertyName(SyntaxNode expr)
			{
				var mAccess = expr as MemberAccessExpressionSyntax;
				return mAccess != null ? mAccess.Name.ToString() : expr.ToString();
			}

			public override void VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node)
			{
				base.VisitAnonymousObjectCreationExpression(node);

				foreach (var expr in node.Initializers) {
					if (expr.NameEquals == null || expr.NameEquals.Name == null)
						continue;

					if (expr.NameEquals.Name.ToString() == GetAnonymousTypePropertyName(expr.Expression)) {
						AddDiagnosticAnalyzer (Diagnostic.Create(Rule, expr.NameEquals.GetLocation()));
					}
				}
			}
		}
	}

	[ExportCodeFixProvider(RedundantAnonymousTypePropertyNameAnalyzer.DiagnosticId, LanguageNames.CSharp)]
	public class RedundantAnonymousTypePropertyNameFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return RedundantAnonymousTypePropertyNameAnalyzer.DiagnosticId;
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
			var result = new List<CodeAction>();
			foreach (var diagnostic in diagnostics) {
				var node = root.FindNode(diagnostic.Location.SourceSpan);
				if (node.IsKind(SyntaxKind.NameEquals)) {
					var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepLeadingTrivia);
					context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Remove redundant name", document.WithSyntaxRoot(newRoot)), diagnostic);
				}
			}
		}
	}
}