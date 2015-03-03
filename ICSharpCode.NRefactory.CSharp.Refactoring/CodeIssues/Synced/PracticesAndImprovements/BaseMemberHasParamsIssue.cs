//
// BaseMemberHasParamsIssue.cs
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
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "BaseMemberHasParams")]
	public class BaseMemberHasParamsIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "BaseMemberHasParamsIssue";
		const string Description            = "Base parameter has 'params' modifier, but missing in overrider";
		const string MessageFormat          = "Base method '{0}' has a 'params' modifier";
		const string Category               = IssueCategories.PracticesAndImprovements;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Base parameter has 'params' modifier, but missing in overrider");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<BaseMemberHasParamsIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
			{
				if (!node.Modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword)))
					return;
				var lastParam = node.ParameterList.Parameters.LastOrDefault();
				if (lastParam == null || lastParam.Modifiers.Any(m => m.IsKind(SyntaxKind.ParamsKeyword)))
					return;
				if (lastParam.Type == null || !lastParam.Type.IsKind(SyntaxKind.ArrayType))
					return;
				var rr = semanticModel.GetDeclaredSymbol(node);
				if (rr == null || !rr.IsOverride)
					return;
				var baseMember = rr.OverriddenMethod;
				if (baseMember == null || baseMember.Parameters.Length == 0 || !baseMember.Parameters.Last().IsParams)
					return;
				VisitLeadingTrivia(node);
				AddIssue (Diagnostic.Create(Rule, Location.Create(semanticModel.SyntaxTree, lastParam.Span), baseMember.Name));
			}


			public override void VisitBlock(BlockSyntax node)
			{
				// SKIP
			}
		}
	}

	[ExportCodeFixProvider(BaseMemberHasParamsIssue.DiagnosticId, LanguageNames.CSharp)]
	public class BaseMemberHasParamsFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return BaseMemberHasParamsIssue.DiagnosticId;
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
				if (!node.IsKind(SyntaxKind.Parameter))
					continue;
				var param = (ParameterSyntax)node;
				var newRoot = root.ReplaceNode(node, param.AddModifiers(SyntaxFactory.Token(SyntaxKind.ParamsKeyword)).WithAdditionalAnnotations(Formatter.Annotation));
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Add 'params' modifier", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}