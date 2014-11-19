//
// RedundantParamsIssue.cs
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "RedundantParams")]
	public class RedundantParamsIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "RedundantParamsIssue";
		const string Description            = "'params' is ignored on overrides";
		const string MessageFormat          = "'params' is always ignored in overrides";
		const string Category               = IssueCategories.RedundanciesInDeclarations;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "'params' is ignored on overrides");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<RedundantParamsIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
//			{
//				if (!methodDeclaration.HasModifier(Modifiers.Override))
//					return;
//				var lastParam = methodDeclaration.Parameters.LastOrDefault();
//				if (lastParam == null || lastParam.ParameterModifier != ParameterModifier.Params)
//					return;
//				var type = lastParam.Type as ComposedType;
//				if (type == null || !type.ArraySpecifiers.Any())
//					return;
//				var rr = ctx.Resolve(methodDeclaration) as MemberResolveResult;
//				if (rr == null)
//					return;
//				var baseMember = InheritanceHelper.GetBaseMember(rr.Member) as IMethod;
//				if (baseMember == null || baseMember.Parameters.Count == 0 || baseMember.Parameters.Last().IsParams)
//					return;
//				AddIssue(new CodeIssue(
//					lastParam.GetChildByRole(ParameterDeclaration.ParamsModifierRole),
//					ctx.TranslateString(""),
//					ctx.TranslateString(""),
//					script => {
//						var p = (ParameterDeclaration)lastParam.Clone();
//						p.ParameterModifier = ParameterModifier.None;
//						script.Replace(lastParam, p);
//					}
//				) { IssueMarker = IssueMarker.GrayOut });
//			}
//
//			public override void VisitBlockStatement(BlockStatement blockStatement)
//			{
//				// SKIP
//			}
		}
	}

	[ExportCodeFixProvider(RedundantParamsIssue.DiagnosticId, LanguageNames.CSharp)]
	public class RedundantParamsFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return RedundantParamsIssue.DiagnosticId;
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
				//if (!node.IsKind(SyntaxKind.BaseList))
				//	continue;
				var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
				context.RegisterFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Remove 'params' modifier", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}

