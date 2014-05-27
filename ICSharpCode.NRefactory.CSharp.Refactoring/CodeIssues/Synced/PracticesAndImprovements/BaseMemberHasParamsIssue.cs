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
	[DiagnosticAnalyzer]
	[ExportDiagnosticAnalyzer("Base parameter has 'params' modifier, but missing in overrider", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(Description = "Base parameter has 'params' modifier, but missing in overrider", AnalysisDisableKeyword = "BaseMemberHasParams")]
	public class BaseMemberHasParamsIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "";
		const string Description            = "Base method has a 'params' modifier";
		const string MessageFormat          = "Add 'params' modifier";
		const string Category               = IssueCategories.PracticesAndImprovements;

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

		class GatherVisitor : GatherVisitorBase<BaseMemberHasParamsIssue>
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
//				if (lastParam == null || lastParam.ParameterModifier == ParameterModifier.Params)
//					return;
//				var type = lastParam.Type as ComposedType;
//				if (type == null || !type.ArraySpecifiers.Any())
//					return;
//				var rr = ctx.Resolve(methodDeclaration) as MemberResolveResult;
//				if (rr == null)
//					return;
//				var baseMember = InheritanceHelper.GetBaseMember(rr.Member) as IMethod;
//				if (baseMember == null || baseMember.Parameters.Count == 0 || !baseMember.Parameters.Last().IsParams)
//					return;
//				AddIssue(new CodeIssue(
//					lastParam.NameToken,
//					string.Format(ctx.TranslateString("Base method '{0}' has a 'params' modifier"), baseMember.FullName),
//					ctx.TranslateString("Add 'params' modifier"),
//					script => {
//						script.ChangeModifier(lastParam, ParameterModifier.Params);
//					}
//				));
//			}

//			public override void VisitBlockStatement(BlockStatement blockStatement)
//			{
//				// SKIP
//			}
		}
	}

	[ExportCodeFixProvider(BaseMemberHasParamsIssue.DiagnosticId, LanguageNames.CSharp)]
	public class BaseMemberHasParamsFixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return BaseMemberHasParamsIssue.DiagnosticId;
		}

		public async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagonstic in diagnostics) {
				var node = root.FindNode(diagonstic.Location.SourceSpan);
				//if (!node.IsKind(SyntaxKind.BaseList))
				//	continue;
				var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
				result.Add(CodeActionFactory.Create(node.Span, diagonstic.Severity, diagonstic.GetMessage(), document.WithSyntaxRoot(newRoot)));
			}
			return result;
		}
	}
}