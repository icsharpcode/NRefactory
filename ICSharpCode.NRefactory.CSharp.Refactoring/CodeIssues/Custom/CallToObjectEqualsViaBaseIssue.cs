//
// CallToObjectEqualsViaBaseIssue.cs
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
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[ExportDiagnosticAnalyzer("Call to base.Equals resolves to Object.Equals, which is reference equality", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "BaseObjectEqualsIsObjectEquals")]
	public class CallToObjectEqualsViaBaseIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "CallToObjectEqualsViaBaseIssue";
		const string Description            = "Finds potentially erroneous calls to Object.Equals.";
		const string MessageFormat          = "Call to base.Equals resolves to Object.Equals, which is reference equality";
		const string Category               = IssueCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<CallToObjectEqualsViaBaseIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitInvocationExpression(InvocationExpressionSyntax node)
			{
				base.VisitInvocationExpression(node);
				if (node.ArgumentList.Arguments.Count != 1)
					return;
				var memberExpression = node.Expression as MemberAccessExpressionSyntax;
				if (memberExpression == null || memberExpression.Name.Identifier.ToString() != "Equals" || !(memberExpression.Expression.IsKind(SyntaxKind.BaseExpression)))
					return;

				var resolveResult = semanticModel.GetSymbolInfo(node);
				if (resolveResult.Symbol == null || resolveResult.Symbol.ContainingType.SpecialType != SpecialType.System_Object)
					return;
				AddIssue (Diagnostic.Create(Rule, Location.Create(semanticModel.SyntaxTree, node.Span)));
			}
		}
	}

	[ExportCodeFixProvider(CallToObjectEqualsViaBaseIssue.DiagnosticId, LanguageNames.CSharp)]
	public class FixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return CallToObjectEqualsViaBaseIssue.DiagnosticId;
		}

		public async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagonstic in diagnostics) {
				var node = root.FindNode(diagonstic.Location.SourceSpan) as InvocationExpressionSyntax;
				if (node == null)
					continue;

				result.Add(CodeActionFactory.Create(node.Span, diagonstic.Severity, "Change invocation to call 'object.ReferenceEquals'", arg => {
					var arguments = new SeparatedSyntaxList<ArgumentSyntax>();
					arguments = arguments.Add(SyntaxFactory.Argument(SyntaxFactory.ThisExpression())); 
					arguments = arguments.Add(node.ArgumentList.Arguments[0]); 

					return Task.FromResult(document.WithSyntaxRoot(
						root.ReplaceNode(
							node, 
							SyntaxFactory.InvocationExpression(
								SyntaxFactory.ParseExpression("object.ReferenceEquals"),
								SyntaxFactory.ArgumentList(arguments)
							)
								.WithLeadingTrivia(node.GetLeadingTrivia())
								.WithAdditionalAnnotations(Formatter.Annotation))
						)
					);
				}));

				result.Add(CodeActionFactory.Create(node.Span, diagonstic.Severity, "Remove 'base.'", arg => {
					return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(node, node.WithExpression(SyntaxFactory.IdentifierName("Equals")))));
				}));
			}
			return result;
		}
	}
}