//
// ReplaceWithLastOrDefaultAnalyzer.cs
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ReplaceWithLastOrDefault")]
	public class ReplaceWithLastOrDefaultAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "ReplaceWithLastOrDefaultAnalyzer";
		const string Description            = "Replace with call to LastOrDefault<T>()";
		const string MessageFormat          = "Expression can be simlified to 'LastOrDefault<T>()'";
		const string Category               = DiagnosticAnalyzerCategories.PracticesAndImprovements;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "Replace with LastOrDefault<T>()");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<ReplaceWithLastOrDefaultAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
			{
				base.VisitConditionalExpression(node);
				//pattern is Any(param) ? Last(param) : null/default
				var anyInvocation = node.Condition as InvocationExpressionSyntax;
				var lastInvocation = node.WhenTrue as InvocationExpressionSyntax;
				var nullDefaultWhenFalse = node.WhenFalse;

				if (anyInvocation == null || lastInvocation == null || nullDefaultWhenFalse == null)
					return;
				var anyExpression = anyInvocation.Expression as MemberAccessExpressionSyntax;
				if (anyExpression == null || anyExpression.Name.Identifier.ValueText != "Any")
					return;
				var anyParam = anyInvocation.ArgumentList;

				var lastExpression = lastInvocation.Expression as MemberAccessExpressionSyntax;
				if (lastExpression == null || lastExpression.Name.Identifier.ValueText != "Last" || !lastInvocation.ArgumentList.IsEquivalentTo(anyParam))
					return;

				if (!nullDefaultWhenFalse.IsKind(SyntaxKind.NullLiteralExpression) && !nullDefaultWhenFalse.IsKind(SyntaxKind.DefaultExpression))
					return;

				AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.GetLocation()));
			}
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class ReplaceWithLastOrDefaultFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ReplaceWithLastOrDefaultAnalyzer.DiagnosticId;
		}

		public async override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			var cancellationToken = context.CancellationToken;
			var span = context.Span;
			var diagnostics = context.Diagnostics;
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var diagnostic = diagnostics.First ();
			var node = root.FindNode(context.Span) as ConditionalExpressionSyntax;
			//replace a conditional Any(x) ? Last(x) : null/default with LastOrDefault(x)
			var parameterExpr = ((InvocationExpressionSyntax)node.Condition).ArgumentList;
			var baseExpression = ((MemberAccessExpressionSyntax)((InvocationExpressionSyntax)node.Condition).Expression).Expression;
			var newNode = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, baseExpression,
				SyntaxFactory.IdentifierName("LastOrDefault")), parameterExpr);
			var newRoot = root.ReplaceNode((ExpressionSyntax)node, newNode.WithAdditionalAnnotations(Formatter.Annotation));
			context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Replace with 'LastOrDefault<T>()'", document.WithSyntaxRoot(newRoot)), diagnostic);
		}
	}
}