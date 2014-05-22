// 
// DoubleNegationOperatorIssue.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang <lightyang0@gmail.com>
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[ExportDiagnosticAnalyzer("Double negation operator", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(Description = "Double negation is meaningless", AnalysisDisableKeyword = "DoubleNegationOperator")]
	public class DoubleNegationOperatorIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "DoubleNegationOperatorIssue";
		const string Description            = "Double negation operator";
		internal const string MessageFormat1 = "Remove '!!'";
		internal const string MessageFormat2 = "Remove '~~'";
		const string Category               = IssueCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule1 = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat1, Category, DiagnosticSeverity.Warning);
		static readonly DiagnosticDescriptor Rule2 = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat2, Category, DiagnosticSeverity.Warning);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule1);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<DoubleNegationOperatorIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
			{
				base.VisitPrefixUnaryExpression(node);

				if (node.IsKind(SyntaxKind.LogicalNotExpression)) {
					var innerUnaryOperatorExpr = ConvertBitwiseFlagComparisonToHasFlagsAction.StripParenthesizedExpression(node.Operand) as PrefixUnaryExpressionSyntax;

					if (innerUnaryOperatorExpr == null || !innerUnaryOperatorExpr.IsKind(SyntaxKind.LogicalNotExpression))
						return;
					AddIssue(Diagnostic.Create(Rule1, node.GetLocation()));

				}

				if (node.IsKind(SyntaxKind.BitwiseNotExpression)) {
					var innerUnaryOperatorExpr = ConvertBitwiseFlagComparisonToHasFlagsAction.StripParenthesizedExpression(node.Operand) as PrefixUnaryExpressionSyntax;

					if (innerUnaryOperatorExpr == null || !innerUnaryOperatorExpr.IsKind(SyntaxKind.BitwiseNotExpression))
						return;
					AddIssue(Diagnostic.Create(Rule2, node.GetLocation()));
				}
			}
		}
	}

	[ExportCodeFixProvider(DoubleNegationOperatorIssue.DiagnosticId, LanguageNames.CSharp)]
	public class DoubleNegationOperatorFixProvider : ICodeFixProvider
	{
		#region ICodeFixProvider implementation

		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return DoubleNegationOperatorIssue.DiagnosticId;
		}

		public async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagonstic in diagnostics) {
				var n = root.FindNode(diagonstic.Location.SourceSpan, true, true);
				var node = n as PrefixUnaryExpressionSyntax;
				if (node == null)
					continue;
				var innerUnaryOperatorExpr = ConvertBitwiseFlagComparisonToHasFlagsAction.StripParenthesizedExpression(node.Operand) as PrefixUnaryExpressionSyntax;
				if (innerUnaryOperatorExpr == null)
					continue;
				var newRoot = root.ReplaceNode(node, ConvertBitwiseFlagComparisonToHasFlagsAction.StripParenthesizedExpression(innerUnaryOperatorExpr.Operand));
				result.Add(CodeActionFactory.Create(node.Span, diagonstic.Severity, diagonstic.GetMessage(), document.WithSyntaxRoot(newRoot)));
			}
			return result;
		}
		#endregion
	}

}
