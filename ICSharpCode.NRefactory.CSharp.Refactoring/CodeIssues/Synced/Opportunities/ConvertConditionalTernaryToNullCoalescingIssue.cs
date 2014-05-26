// 
// ConditionalToNullCoalescingInspector.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
	/// <summary>
	/// Checks for "a != null ? a : other"<expr>
	/// Converts to: "a ?? other"<expr>
	/// </summary>
	[DiagnosticAnalyzer]
	[ExportDiagnosticAnalyzer("'?:' expression can be converted to '??' expression", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(Description = "'?:' expression can be converted to '??' expression.", AnalysisDisableKeyword = "ConvertConditionalTernaryToNullCoalescing")]
	public class ConvertConditionalTernaryToNullCoalescingIssue : GatherVisitorCodeIssueProvider
	{
//		static readonly Pattern unequalPattern = new Choice {
//			// a != null ? a : other
//			new ConditionalExpression(
//				PatternHelper.CommutativeOperatorWithOptionalParentheses(new AnyNode("a"), BinaryOperatorType.InEquality, new NullReferenceExpression()),
//				new Backreference("a"),
//				new AnyNode("other")
//			),
//
//			// obj != null ? (Type)obj : other
//			new ConditionalExpression(
//				PatternHelper.CommutativeOperatorWithOptionalParentheses(new AnyNode("obj"), BinaryOperatorType.InEquality, new NullReferenceExpression()),
//				new NamedNode("a", new CastExpression(new AnyNode(), new Backreference("obj"))),
//				new AnyNode("other")
//			)
//
//		};
//
//		static readonly Pattern equalPattern = new Choice {
//			// a == null ? other : a
//			new ConditionalExpression(
//				PatternHelper.CommutativeOperatorWithOptionalParentheses(new AnyNode("a"), BinaryOperatorType.Equality, new NullReferenceExpression()),
//				new AnyNode("other"),
//				new Backreference("a")
//			)
//		};

		
		internal const string DiagnosticId  = "ConvertConditionalTernaryToNullCoalescingIssue";
		const string Description            = "'?:' expression can be re-written as '??' expression";
		const string MessageFormat          = "Replace '?:'  operator with '??";
		const string Category               = IssueCategories.Opportunities;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<ConvertConditionalTernaryToNullCoalescingIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}
//
//			public override void VisitConditionalExpression(ConditionalExpression conditionalExpression)
//			{
//				Match m = unequalPattern.Match(conditionalExpression);
//				bool isEqual = false;
//				if (!m.Success) {
//					isEqual = true;
//					m = equalPattern.Match(conditionalExpression);
//				}
//				if (m.Success) {
//					var a = m.Get<Expression>("a").Single();
//					var other = m.Get<Expression>("other").Single();
//
//					if (isEqual) {
//						var castExpression = other as CastExpression;
//						if (castExpression != null) {
//							a = new CastExpression(castExpression.Type.Clone(), a.Clone());
//							other = castExpression.Expression;
//						}
//					}
//
//					AddIssue(new CodeIssue(conditionalExpression, ctx.TranslateString(), new CodeAction (
//						ctx.TranslateString(), script => {
//							var expr = new BinaryOperatorExpression (a.Clone (), BinaryOperatorType.NullCoalescing, other.Clone ());
//							script.Replace (conditionalExpression, expr);
//						}, conditionalExpression)));
//				}
//				base.VisitConditionalExpression (conditionalExpression);
//			}
		}
	}

	[ExportCodeFixProvider(ConvertConditionalTernaryToNullCoalescingIssue.DiagnosticId, LanguageNames.CSharp)]
	public class ConvertConditionalTernaryToNullCoalescingFixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return ConvertConditionalTernaryToNullCoalescingIssue.DiagnosticId;
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