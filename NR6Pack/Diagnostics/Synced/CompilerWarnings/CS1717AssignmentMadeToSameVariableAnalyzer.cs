// 
// CS1717AssignmentMadeToSameVariableAnalyzer.cs
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
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
    [NotPortedYet]
    public class CS1717AssignmentMadeToSameVariableAnalyzer : DiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "CS1717AssignmentMadeToSameVariableAnalyzer";
		const string Description            = "CS1717:Assignment made to same variable";
		const string MessageFormat          = "CS1717:Assignment made to same variable";
		const string Category               = DiagnosticAnalyzerCategories.CompilerWarnings;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "CS1717:Assignment made to same variable");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		public override void Initialize(AnalysisContext context)
		{
			//context.RegisterSyntaxNodeAction(
			//	(nodeContext) => {
			//		Diagnostic diagnostic;
			//		if (TryGetDiagnostic (nodeContext, out diagnostic)) {
			//			nodeContext.ReportDiagnostic(diagnostic);
			//		}
			//	}, 
			//	new SyntaxKind[] { SyntaxKind.None }
			//);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;
			//var node = nodeContext.Node as ;
			//diagnostic = Diagnostic.Create (descriptor, node.GetLocation ());
			//return true;
			return false;
		}

//		class GatherVisitor : GatherVisitorBase<CS1717AssignmentMadeToSameVariableAnalyzer>
//		{
//			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
//				: base (semanticModel, addDiagnostic, cancellationToken)
//			{
//			}

////			public override void VisitAssignmentExpression (AssignmentExpression assignmentExpression)
////			{
////				base.VisitAssignmentExpression (assignmentExpression);
////
////				if (assignmentExpression.Operator != AssignmentOperatorType.Assign)
////					return;
////				if (!(assignmentExpression.Left is IdentifierExpression) && 
////					!(assignmentExpression.Left is MemberReferenceExpression))
////					return;
////
////				var resolveResult = ctx.Resolve (assignmentExpression.Left);
////				var memberResolveResult = resolveResult as MemberResolveResult;
////				if (memberResolveResult != null) {
////					var memberResolveResult2 = ctx.Resolve (assignmentExpression.Right) as MemberResolveResult;
////					if (memberResolveResult2 == null || !AreEquivalent(memberResolveResult, memberResolveResult2))
////						return;
////				} else if (resolveResult is LocalResolveResult) {
////					if (!assignmentExpression.Left.Match (assignmentExpression.Right).Success)
////						return;
////				} else {
////					return;
////				}
////
////				AstNode node;
////				Action<Script> action;
////				if (assignmentExpression.Parent is ExpressionStatement) {
////					node = assignmentExpression.Parent;
////					action = script => script.Remove (assignmentExpression.Parent);
////				} else {
////					node = assignmentExpression;
////					action = script => script.Replace (assignmentExpression, assignmentExpression.Left.Clone ());
////				}
////				AddDiagnosticAnalyzer (new CodeIssue(node, ctx.TranslateString (""),
////					new [] { new CodeAction (ctx.TranslateString (""), action, node) })
////					{ IssueMarker = IssueMarker.GrayOut }
////				);
////			}
////
////			static bool AreEquivalent(ResolveResult first, ResolveResult second)
////			{
////				var firstPath = AccessPath.FromResolveResult(first);
////				var secondPath = AccessPath.FromResolveResult(second);
////				return firstPath != null && firstPath.Equals(secondPath) && !firstPath.MemberPath.Any(m => !(m is IField));
////			}
//		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class CS1717AssignmentMadeToSameVariableFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds {
			get {
				return ImmutableArray.Create (CS1717AssignmentMadeToSameVariableAnalyzer.DiagnosticId);
			}
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
			context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Remove assignment", document.WithSyntaxRoot(newRoot)), diagnostic);
		}
	}
}