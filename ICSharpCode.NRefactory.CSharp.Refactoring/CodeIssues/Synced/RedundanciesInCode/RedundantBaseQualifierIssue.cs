//
// RedundantBaseQualifierIssue.cs
//
// Author:
//       Ji Kun <jikun.nus@gmail.com>
//
// Copyright (c) 2013  Ji Kun <jikun.nus@gmail.com>
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
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	/// <summary>
	/// Finds redundant base qualifier 
	/// </summary>
	[DiagnosticAnalyzer]
	[ExportDiagnosticAnalyzer("Redundant 'base.' qualifier", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(Description = "'base.' is redundant and can safely be removed.", AnalysisDisableKeyword = "RedundantBaseQualifier")]
	public class RedundantBaseQualifierIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "RedundantBaseQualifierIssue";
		const string Description            = "'base.' is redundant and can be removed safely.";
		internal const string MessageFormat = "Remove 'base.'";
		const string Category               = IssueCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<RedundantBaseQualifierIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
			{
				if (node.Expression.IsKind(SyntaxKind.BaseExpression)) {
					var replacementNode = node.Name.WithLeadingTrivia(node.GetLeadingTrivia()).WithTrailingTrivia(node.GetTrailingTrivia());
					if (node.CanReplaceWithReducedName(replacementNode, semanticModel, cancellationToken)) {
						base.VisitMemberAccessExpression(node);

						AddIssue (Diagnostic.Create(Rule, node.Expression.GetLocation(), additionalLocations: new [] { node.OperatorToken.GetLocation() }));
					}
				} else {
					base.VisitMemberAccessExpression(node);
				}
			}
		}
	}

	[ExportCodeFixProvider(RedundantBaseQualifierIssue.DiagnosticId, LanguageNames.CSharp)]
	public class RedundantBaseQualifierCodeFixProvider : ICodeFixProvider
	{
		#region ICodeFixProvider implementation

		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return RedundantBaseQualifierIssue.DiagnosticId;
		}

		public async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagonstic in diagnostics) {
				var node = root.FindNode(diagonstic.Location.SourceSpan);
				var token = node.Parent as MemberAccessExpressionSyntax;
				if (token != null) {
					var newRoot = root.ReplaceNode((SyntaxNode)token,
						token.Name
						.WithLeadingTrivia(token.GetLeadingTrivia())
						.WithTrailingTrivia(token.GetTrailingTrivia()));
					result.Add(CodeActionFactory.Create(token.Span, diagonstic.Severity, diagonstic.GetMessage(), document.WithSyntaxRoot(newRoot)));
				}
			}
			return result;
		}
		#endregion
	}
}