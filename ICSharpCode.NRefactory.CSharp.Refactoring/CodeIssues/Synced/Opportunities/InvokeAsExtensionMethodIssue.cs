//
// InvokeAsExtensionMethodIssue.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Simon Lindgren
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "InvokeAsExtensionMethod")]
	public class InvokeAsExtensionMethodIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId = "InvokeAsExtensionMethodIssue";
		const string Description = "If an extension method is called as static method convert it to method syntax";
		const string MessageFormat = "Convert static method call to extension method call";
		const string Category = IssueCategories.Opportunities;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "Convert static method call to extension method call");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
		{
			get
			{
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<InvokeAsExtensionMethodIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitInvocationExpression(InvocationExpressionSyntax node)
			{
				base.VisitInvocationExpression(node);
				var memberReference = node.Expression as MemberAccessExpressionSyntax;
				if (memberReference == null)
					return;
				var firstArgument = node.ArgumentList.Arguments.FirstOrDefault();
				if (firstArgument == null || firstArgument.Expression.IsKind(SyntaxKind.NullLiteralExpression))
					return;
				var expressionSymbol = semanticModel.GetSymbolInfo(node.Expression).Symbol as IMethodSymbol;
				//ignore non-extensions and reduced extensions (so a.Ext, as opposed to B.Ext(a))
				if (expressionSymbol == null || !expressionSymbol.IsExtensionMethod || expressionSymbol.MethodKind == MethodKind.ReducedExtension)
					return;
				AddIssue(Diagnostic.Create(Rule, memberReference.Name.GetLocation()));
			}
		}
	}

	[ExportCodeFixProvider(InvokeAsExtensionMethodIssue.DiagnosticId, LanguageNames.CSharp)]
	public class InvokeAsExtensionMethodFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return InvokeAsExtensionMethodIssue.DiagnosticId;
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
				var node = root.FindNode(diagnostic.Location.SourceSpan).Parent.Parent as InvocationExpressionSyntax;
				if (node == null)
					continue;
				var newRoot = root.ReplaceNode((SyntaxNode)node, node.WithArgumentList(node.ArgumentList.WithArguments(node.ArgumentList.Arguments.RemoveAt(0)))
					.WithExpression(((MemberAccessExpressionSyntax)node.Expression).WithExpression(node.ArgumentList.Arguments.First().Expression))
					.WithLeadingTrivia(node.GetLeadingTrivia()));
				context.RegisterFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Convert to extension method call", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}