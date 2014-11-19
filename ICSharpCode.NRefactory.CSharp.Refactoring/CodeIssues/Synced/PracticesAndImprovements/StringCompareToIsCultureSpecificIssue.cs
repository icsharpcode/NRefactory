//
// StringCompareToIsCultureSpecificIssue.cs
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "StringCompareToIsCultureSpecific")]
	public class StringCompareToIsCultureSpecificIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "StringCompareToIsCultureSpecificIssue";
		const string Description            = "Warns when a culture-aware 'string.CompareTo' call is used by default.";
		const string MessageFormat          = "'string.CompareTo' is culture-aware";
		const string Category               = IssueCategories.PracticesAndImprovements;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "'string.CompareTo' is culture-aware");
		// "Use ordinal comparison" / "Use culture-aware comparison"

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<StringCompareToIsCultureSpecificIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitInvocationExpression(InvocationExpression invocationExpression)
//			{
//				base.VisitInvocationExpression(invocationExpression);
//
//				var rr = ctx.Resolve(invocationExpression) as CSharpInvocationResolveResult;
//				if (rr == null || rr.IsError)
//					return;
//
//				if (rr.Member.Name != "CompareTo" || 
//				    !rr.Member.DeclaringType.IsKnownType (KnownTypeCode.String) ||
//				    rr.Member.Parameters.Count != 1 ||
//				    !rr.Member.Parameters[0].Type.IsKnownType(KnownTypeCode.String)) {
//					return;
//				}
//				AddIssue(new CodeIssue(
//					invocationExpression,
//					ctx.TranslateString(""), 
//					new CodeAction(ctx.TranslateString(), script => AddArgument(script, invocationExpression, "Ordinal"), invocationExpression),
//					new CodeAction(ctx.TranslateString(), script => AddArgument(script, invocationExpression, "CurrentCulture"), invocationExpression)
//				));
//
//			}
//
//			void AddArgument(Script script, InvocationExpression invocationExpression, string ordinal)
//			{
//				var mr = invocationExpression.Target as MemberReferenceExpression;
//				if (mr == null)
//					return;
//
//				var astBuilder = ctx.CreateTypeSystemAstBuilder(invocationExpression);
//				var newArgument = astBuilder.ConvertType(new TopLevelTypeName("System", "StringComparison")).Member(ordinal);
//
//				var newInvocation = new PrimitiveType("string").Invoke(
//					"Compare",
//					mr.Target.Clone(),
//					invocationExpression.Arguments.First().Clone(),
//					newArgument
//				);
//				script.Replace(invocationExpression, newInvocation);
//			}
		}
	}

	[ExportCodeFixProvider(StringCompareToIsCultureSpecificIssue.DiagnosticId, LanguageNames.CSharp)]
	public class StringCompareToIsCultureSpecificFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return StringCompareToIsCultureSpecificIssue.DiagnosticId;
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
				context.RegisterFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, diagnostic.GetMessage(), document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}