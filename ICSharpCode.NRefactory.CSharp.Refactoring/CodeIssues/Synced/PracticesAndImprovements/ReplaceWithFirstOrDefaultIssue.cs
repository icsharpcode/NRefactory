//
// ReplaceWithFirstOrDefaultIssue.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ReplaceWithFirstOrDefault")]
	public class ReplaceWithFirstOrDefaultIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "ReplaceWithFirstOrDefaultIssue";
		const string Description            = "Replace with call to FirstOrDefault<T>()";
		const string MessageFormat          = "Expression can be simlified to 'FirstOrDefault<T>()'";
		const string Category               = IssueCategories.PracticesAndImprovements;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "Replace with FirstOrDefault<T>()");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<ReplaceWithFirstOrDefaultIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
			{
				base.VisitConditionalExpression(node);
				//pattern is Any(param) ? First(param) : null/default
				var anyInvocation = node.Condition as InvocationExpressionSyntax;
				var firstInvocation = node.WhenTrue as InvocationExpressionSyntax;
				var nullDefaultWhenFalse = node.WhenFalse;

				if (anyInvocation == null || firstInvocation == null || nullDefaultWhenFalse == null)
					return;
				var anyExpression = anyInvocation.Expression as MemberAccessExpressionSyntax;
				if (anyExpression == null || anyExpression.Name.Identifier.ValueText != "Any")
					return;
				var anyParam = anyInvocation.ArgumentList;

				var firstExpression = firstInvocation.Expression as MemberAccessExpressionSyntax;
				if (firstExpression == null || firstExpression.Name.Identifier.ValueText != "First" || !firstInvocation.ArgumentList.IsEquivalentTo(anyParam))
					return;

				if (!nullDefaultWhenFalse.IsKind(SyntaxKind.NullLiteralExpression) && !nullDefaultWhenFalse.IsKind(SyntaxKind.DefaultExpression))
					return;

				AddIssue(Diagnostic.Create(Rule, node.GetLocation()));
			}
		}
	}

	[ExportCodeFixProvider(ReplaceWithFirstOrDefaultIssue.DiagnosticId, LanguageNames.CSharp)]
	public class ReplaceWithFirstOrDefaultFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ReplaceWithFirstOrDefaultIssue.DiagnosticId;
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
				var node = root.FindNode(diagnostic.Location.SourceSpan) as ConditionalExpressionSyntax;
				//replace a conditional Any(x) ? First(x) : null/default with FirstOrDefault(x)
				var parameterExpr = ((InvocationExpressionSyntax)node.Condition).ArgumentList;
				var baseExpression = ((MemberAccessExpressionSyntax)((InvocationExpressionSyntax)node.Condition).Expression).Expression;
				var newNode = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, baseExpression,
					SyntaxFactory.IdentifierName("FirstOrDefault")), parameterExpr);
				var newRoot = root.ReplaceNode((ExpressionSyntax)node, newNode.WithAdditionalAnnotations(Formatter.Annotation));
				context.RegisterFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Replace with 'FirstOrDefault<T>()'", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}