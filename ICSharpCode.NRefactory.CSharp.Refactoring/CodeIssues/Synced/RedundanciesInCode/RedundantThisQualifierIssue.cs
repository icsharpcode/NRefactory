// 
// RedundantThisInspector.cs
// 
// RedundantThisInspector.cs
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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.NRefactory6.CSharp;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	/// <summary>
	/// Finds redundant this usages.
	/// </summary>
	[DiagnosticAnalyzer]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "RedundantThisQualifier")]
	public class RedundantThisQualifierIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "RedundantThisQualifierIssue";
		const string Category               = IssueCategories.RedundanciesInCode;

		public const string InsideConstructors = DiagnosticId +".InsideConstructors";
		public const string EverywhereElse     = DiagnosticId + ".EverywhereElse";

		static readonly DiagnosticDescriptor Rule1 = new DiagnosticDescriptor (InsideConstructors, "Inside constructors", "'this.' is redundant and can be removed safely.", Category, DiagnosticSeverity.Warning, true, "Redundant 'this.' qualifier");
		static readonly DiagnosticDescriptor Rule2 = new DiagnosticDescriptor (EverywhereElse, "Everywhere else", "'this.' is redundant and can be removed safely.", Category, DiagnosticSeverity.Warning, true, "Redundant 'this.' qualifier");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule1, Rule2);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}


		class GatherVisitor : GatherVisitorBase<RedundantThisQualifierIssue>
		{
			bool isInsideConstructor;

			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
			{
				isInsideConstructor = true;
				base.VisitConstructorDeclaration(node);
				isInsideConstructor = false;
			}

			public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
			{
				base.VisitMemberAccessExpression(node);
				if (node.Expression.IsKind(SyntaxKind.ThisExpression)) {
					var replacementNode = node.Name.WithLeadingTrivia(node.GetLeadingTrivia()).WithTrailingTrivia(node.GetTrailingTrivia());
					if (node.CanReplaceWithReducedName(replacementNode, semanticModel, cancellationToken)) {
						AddIssue (Diagnostic.Create(isInsideConstructor ? Rule1 : Rule2, node.Expression.GetLocation(), additionalLocations: new [] { node.OperatorToken.GetLocation() }));
					}
				}
			}
		}
	}

	[ExportCodeFixProvider(RedundantThisQualifierIssue.DiagnosticId, LanguageNames.CSharp)]
	public class RedundantThisQualifierCodeFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return RedundantThisQualifierIssue.InsideConstructors;
			yield return RedundantThisQualifierIssue.EverywhereElse;
		}

		public override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
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
				var token = node.Parent as MemberAccessExpressionSyntax;
				if (token != null) {
					var newRoot = root.ReplaceNode((SyntaxNode)token,
						token.Name
						.WithLeadingTrivia(token.GetLeadingTrivia())
						.WithTrailingTrivia(token.GetTrailingTrivia()));
					context.RegisterFix(CodeActionFactory.Create(token.Span, diagnostic.Severity, "Remove 'this.'", document.WithSyntaxRoot(newRoot)), diagnostic);
				}
			}
		}
	}
}