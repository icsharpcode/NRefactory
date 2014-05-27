//
// BaseMethodCallWithDefaultParameterIssue.cs
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
	[ExportDiagnosticAnalyzer("Call to base member with implicit default parameters", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "BaseMethodCallWithDefaultParameter")]
	public class BaseMethodCallWithDefaultParameterIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "BaseMethodCallWithDefaultParameterIssue";
		const string Description            = "Call to base member with implicit default parameters";
		const string MessageFormat          = "Call to base member with implicit default parameters";
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

		class GatherVisitor : GatherVisitorBase<BaseMethodCallWithDefaultParameterIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}
//
//			public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
//			{
//				// skip
//			}
//
//			public override void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
//			{
//				// skip
//			}
//
//			public override void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
//			{
//				// skip
//			}
//
//			public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
//			{
//				// skip
//			}
//
//			public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
//			{
//				// skip
//			}
//
//			public override void VisitInvocationExpression(InvocationExpression invocationExpression)
//			{
//				base.VisitInvocationExpression(invocationExpression);
//				var mr = invocationExpression.Target as MemberReferenceExpression;
//				if (mr == null || !(mr.Target is BaseReferenceExpression))
//					return;
//
//				var invocationRR = ctx.Resolve(invocationExpression) as InvocationResolveResult;
//				if (invocationRR == null)
//					return;
//
//				var parentEntity = invocationExpression.GetParent<EntityDeclaration>();
//				if (parentEntity == null)
//					return;
//				var rr = ctx.Resolve(parentEntity) as MemberResolveResult;
//				if (rr == null)
//					return;
//
//				if (invocationExpression.Arguments.Count >= invocationRR.Member.Parameters.Count ||
//					invocationRR.Member.Parameters.Count == 0 || 
//				    !invocationRR.Member.Parameters.Last().IsOptional)
//					return;
//
//				if (!InheritanceHelper.GetBaseMembers(rr.Member, false).Any(m => m == invocationRR.Member))
//					return;
//				AddIssue(new CodeIssue(
//					invocationExpression.RParToken,
//					ctx.TranslateString("Call to base member with implicit default parameters")
//				));
//			}
//		
//			public override void VisitIndexerExpression(IndexerExpression indexerExpression)
//			{
//				base.VisitIndexerExpression(indexerExpression);
//				if (!(indexerExpression.Target is BaseReferenceExpression))
//					return;
//				var invocationRR = ctx.Resolve(indexerExpression) as InvocationResolveResult;
//				if (invocationRR == null)
//					return;
//
//				var parentEntity = indexerExpression.GetParent<IndexerDeclaration>();
//				if (parentEntity == null)
//					return;
//				var rr = ctx.Resolve(parentEntity) as MemberResolveResult;
//				if (rr == null)
//					return;
//
//				if (indexerExpression.Arguments.Count >= invocationRR.Member.Parameters.Count ||
//				    invocationRR.Member.Parameters.Count == 0 || 
//				    !invocationRR.Member.Parameters.Last().IsOptional)
//					return;
//
//				if (!InheritanceHelper.GetBaseMembers(rr.Member, false).Any(m => m == invocationRR.Member))
//					return;
//				AddIssue(new CodeIssue(
//					indexerExpression.RBracketToken,
//					ctx.TranslateString("")
//				));
//			}
		}
	}

	[ExportCodeFixProvider(BaseMethodCallWithDefaultParameterIssue.DiagnosticId, LanguageNames.CSharp)]
	public class BaseMethodCallWithDefaultParameterFixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return BaseMethodCallWithDefaultParameterIssue.DiagnosticId;
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