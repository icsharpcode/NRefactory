//
// DelegateSubtractionIssue.cs
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
	[ExportDiagnosticAnalyzer("Delegate subtractions", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(Description = "Delegate subtraction has unpredictable result", AnalysisDisableKeyword = "DelegateSubtraction")]
	public class DelegateSubtractionIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "DelegateSubtractionIssue";
		const string Description            = "Delegate subtraction has unpredictable result";
		const string MessageFormat          = "Delegate subtraction has unpredictable result";
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

		class GatherVisitor : GatherVisitorBase<DelegateSubtractionIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
//			{
//				base.VisitBinaryOperatorExpression(binaryOperatorExpression);
//				if (binaryOperatorExpression.Operator != BinaryOperatorType.Subtract)
//					return;
//				var rr = ctx.Resolve(binaryOperatorExpression.Right);
//				if (rr.Type.Kind == ICSharpCode.NRefactory.TypeSystem.TypeKind.Delegate && !IsEvent (binaryOperatorExpression.Left)) {
//					AddIssue(new CodeIssue(binaryOperatorExpression, ctx.TranslateString("")));
//				}
//			}
//
//			bool IsEvent(AstNode node)
//			{
//				var rr = ctx.Resolve(node) as MemberResolveResult;
//				return rr != null && rr.Member.SymbolKind == ICSharpCode.NRefactory.TypeSystem.SymbolKind.Event;
//			}
//
//			public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
//			{
//				base.VisitAssignmentExpression(assignmentExpression);
//				if (assignmentExpression.Operator != AssignmentOperatorType.Subtract)
//					return;
//				var rr = ctx.Resolve(assignmentExpression.Right);
//				if (rr.Type.Kind == ICSharpCode.NRefactory.TypeSystem.TypeKind.Delegate && !IsEvent (assignmentExpression.Left))
//					AddIssue(new CodeIssue(assignmentExpression, ctx.TranslateString("Delegate subtraction has unpredictable result")));
//			}
		}
	}

	[ExportCodeFixProvider(DelegateSubtractionIssue.DiagnosticId, LanguageNames.CSharp)]
	public class DelegateSubtractionFixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return DelegateSubtractionIssue.DiagnosticId;
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