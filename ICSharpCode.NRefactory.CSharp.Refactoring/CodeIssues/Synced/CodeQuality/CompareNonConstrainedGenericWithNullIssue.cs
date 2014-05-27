//
// CompareNonConstrainedGenericWithNullIssue.cs
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
	[ExportDiagnosticAnalyzer("Possible compare of value type with 'null'", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "CompareNonConstrainedGenericWithNull")]
	public class CompareNonConstrainedGenericWithNullIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "CompareNonConstrainedGenericWithNullIssue";
		const string Description            = "Possible compare of value type with 'null'";
		const string MessageFormat          = "Possible compare of value type with 'null'";
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

		class GatherVisitor : GatherVisitorBase<CompareNonConstrainedGenericWithNullIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			static readonly AstNode pattern = new Choice {
//				PatternHelper.CommutativeOperatorWithOptionalParentheses(new AnyNode("node"), BinaryOperatorType.Equality, new NullReferenceExpression ()),
//				PatternHelper.CommutativeOperatorWithOptionalParentheses(new AnyNode("node"), BinaryOperatorType.InEquality, new NullReferenceExpression ())
//			};
//
//			void CheckCase(IType type, BinaryOperatorExpression binaryOperatorExpression, Expression expr)
//			{
//				if (type.Kind != TypeKind.TypeParameter || type.IsReferenceType == true)
//					return;
//				AddIssue(new CodeIssue(
//					binaryOperatorExpression,
//					ctx.TranslateString(""),
//					ctx.TranslateString(""),
//					s => {
//						var builder = ctx.CreateTypeSystemAstBuilder(binaryOperatorExpression);
//						s.Replace(binaryOperatorExpression, 
//							new BinaryOperatorExpression(expr.Clone(), 
//								binaryOperatorExpression.Operator,
//								new DefaultValueExpression(builder.ConvertType(type))
//							)
//						); 
//					}
//				));
//			}
//
//			public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
//			{
//				base.VisitBinaryOperatorExpression(binaryOperatorExpression);
//				var match = pattern.Match(binaryOperatorExpression);
//				if (!match.Success)
//					return;
//				var expr = match.Get<Expression>("node").SingleOrDefault();
//				if (expr == null)
//					return;
//				var rr = ctx.Resolve(expr);
//
//				var lr = rr as LocalResolveResult;
//				if (lr != null)
//					CheckCase (lr.Variable.Type, binaryOperatorExpression, expr);
//				var mr = rr as MemberResolveResult;
//				if (mr != null)
//					CheckCase (mr.Member.ReturnType, binaryOperatorExpression, expr);
//			}
		}
	}

	[ExportCodeFixProvider(CompareNonConstrainedGenericWithNullIssue.DiagnosticId, LanguageNames.CSharp)]
	public class CompareNonConstrainedGenericWithNullFixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return CompareNonConstrainedGenericWithNullIssue.DiagnosticId;
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
				result.Add(CodeActionFactory.Create(node.Span, diagonstic.Severity, "Replace with 'default'", document.WithSyntaxRoot(newRoot)));
			}
			return result;
		}
	}
}