//
// RedundantComparisonWithNullAnalyzer.cs
//
// Author:
//	   Ji Kun <jikun.nus@gmail.com>
//
// Copyright (c) 2013 Ji Kun
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
// THE SOFTWARE.using System;

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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "RedundantComparisonWithNull")]
	public class RedundantComparisonWithNullAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "RedundantComparisonWithNullAnalyzer";
		const string Description            = "When 'is' keyword is used, which implicitly check null.";
		const string MessageFormat          = "Redundant comparison with 'null'";
		const string Category               = DiagnosticAnalyzerCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Redundant comparison with 'null'");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<RedundantComparisonWithNullAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
//			{
//				base.VisitBinaryOperatorExpression(binaryOperatorExpression);
//				Match m1 = pattern1.Match(binaryOperatorExpression);
//				if (m1.Success) {
//					AddDiagnosticAnalyzer(new CodeIssue(binaryOperatorExpression,
//					         ctx.TranslateString(""),
//					         ctx.TranslateString(""), 
//					         script => {
//					         	var isExpr = m1.Get<AstType>("t").Single().Parent;
//					         	script.Replace(binaryOperatorExpression, isExpr);
//					         }
//					) { IssueMarker = IssueMarker.GrayOut });
//					return;
//				}
//			}
		}

//		private static readonly Pattern pattern1
//		= new Choice {
//			//  a is Record && a != null
//			new BinaryOperatorExpression(
//				PatternHelper.OptionalParentheses(
//					new IsExpression {
//						Expression = new AnyNode("a"),
//						Type = new AnyNode("t")
//					}),
//				BinaryOperatorType.ConditionalAnd,
//				PatternHelper.CommutativeOperatorWithOptionalParentheses(new Backreference("a"),
//					BinaryOperatorType.InEquality,
//					new NullReferenceExpression())
//			),
//			//  a != null && a is Record
//			new BinaryOperatorExpression (
//				PatternHelper.CommutativeOperatorWithOptionalParentheses(new AnyNode("a"),
//					BinaryOperatorType.InEquality,
//					new NullReferenceExpression()),
//				BinaryOperatorType.ConditionalAnd,
//				PatternHelper.OptionalParentheses(
//					new IsExpression {
//						Expression = new Backreference("a"),
//						Type = new AnyNode("t")
//					})
//			)
//		};

	}

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class RedundantComparisonWithNullFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return RedundantComparisonWithNullAnalyzer.DiagnosticId;
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
			var diagnostic = diagnostics.First ();
			var node = root.FindNode(context.Span);
			//if (!node.IsKind(SyntaxKind.BaseList))
			//	continue;
			var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
			context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Remove expression", document.WithSyntaxRoot(newRoot)), diagnostic);
		}
	}
}