// 
// RedundantNamespaceUsageInspector.cs
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
	/// Finds redundant namespace usages.
	/// </summary>
	[DiagnosticAnalyzer]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "RedundantNameQualifier")]
	public class RedundantNameQualifierIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "RedundantNameQualifierIssue";

		const string Description = "Removes namespace usages that are obsolete.";
		const string MessageFormat = "Qualifier is redundant";
		const string Category               = IssueCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Redundant name qualifier");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<RedundantNameQualifierIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitQualifiedName(QualifiedNameSyntax node)
			{
				var replacementNode = node.Right.WithLeadingTrivia(node.GetLeadingTrivia()).WithTrailingTrivia(node.GetTrailingTrivia());
				if (node.CanReplaceWithReducedName(replacementNode, semanticModel, cancellationToken)) {
					base.VisitQualifiedName(node);
					AddIssue (Diagnostic.Create(Rule, node.Left.GetLocation(), additionalLocations: new [] { node.DotToken.GetLocation() }));
				} else {
					base.VisitQualifiedName(node);
				}

			}

			public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
			{
				if (node.Expression.IsKind(SyntaxKind.BaseExpression) || node.Expression.IsKind(SyntaxKind.ThisExpression))
					return;
 				
				var replacementNode = node.Name.WithLeadingTrivia(node.GetLeadingTrivia()).WithTrailingTrivia(node.GetTrailingTrivia());
				if (node.CanReplaceWithReducedName(replacementNode, semanticModel, cancellationToken)) {
					base.VisitMemberAccessExpression(node);
					AddIssue (Diagnostic.Create(Rule, node.Expression.GetLocation(), additionalLocations: new [] { node.OperatorToken.GetLocation() }));
				} else {
					base.VisitMemberAccessExpression(node);
				}
			}
		}
	}

	[ExportCodeFixProvider(RedundantNameQualifierIssue.DiagnosticId, LanguageNames.CSharp)]
	public class RedundantNameQualifierCodeFixProvider : NRefactoryCodeFixProvider
	{
		#region ICodeFixProvider implementation

		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return RedundantNameQualifierIssue.DiagnosticId;
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
				var memberAccess = node.Parent as MemberAccessExpressionSyntax;
				if (memberAccess != null) {
					var newRoot = root.ReplaceNode((SyntaxNode)memberAccess,
						memberAccess.Name
						.WithLeadingTrivia(memberAccess.GetLeadingTrivia())
						.WithTrailingTrivia(memberAccess.GetTrailingTrivia()));
					context.RegisterFix(CodeActionFactory.Create(memberAccess.Span, DiagnosticSeverity.Info, "Remove redundant qualifier", document.WithSyntaxRoot(newRoot)), diagnostic);
					continue;
				}

				var qualifiedName = node.Parent as QualifiedNameSyntax;
				if (qualifiedName != null) {
					var newRoot = root.ReplaceNode((SyntaxNode)qualifiedName,
						qualifiedName.Right
						.WithLeadingTrivia(qualifiedName.GetLeadingTrivia())
						.WithTrailingTrivia(qualifiedName.GetTrailingTrivia()));
					context.RegisterFix(CodeActionFactory.Create(qualifiedName.Span, diagnostic.Severity, "Remove redundant qualifier", document.WithSyntaxRoot(newRoot)), diagnostic);
				}
			}
		}
		#endregion
	}
}
