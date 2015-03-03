//
// SetterDoesNotUseValueParameterTests.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ValueParameterNotUsed")]
	public class ValueParameterNotUsedIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId = "ValueParameterNotUsedIssue";
		const string Description = "Warns about property or indexer setters and event adders or removers that do not use the value parameter.";
		const string MessageFormat = "";
		const string Category = IssueCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "'value' parameter not used");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<ValueParameterNotUsedIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}
//
//			public override void VisitAccessor(Accessor accessor)
//			{
//				if (accessor.Role == PropertyDeclaration.SetterRole) {
//					FindIssuesInAccessor(accessor, ctx.TranslateString("The setter does not use the 'value' parameter"));
//				} else if (accessor.Role == CustomEventDeclaration.AddAccessorRole) {
//					FindIssuesInAccessor(accessor, ctx.TranslateString("The add accessor does not use the 'value' parameter"));
//				} else if (accessor.Role == CustomEventDeclaration.RemoveAccessorRole) {
//					FindIssuesInAccessor(accessor, ctx.TranslateString("The remove accessor does not use the 'value' parameter"));
//				}
//			}
//
//			public override void VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration)
//			{
//				if (eventDeclaration.AddAccessor.Body.Statements.Count == 0 && eventDeclaration.RemoveAccessor.Body.Statements.Count == 0)
//					return;
//		        
//				base.VisitCustomEventDeclaration(eventDeclaration);
//			}
//
//			void FindIssuesInAccessor(Accessor accessor, string accessorName)
//			{
//				var body = accessor.Body;
//				if (!IsEligible(body))
//					return;
//
//				var localResolveResult = ctx.GetResolverStateBefore(body)
//					.LookupSimpleNameOrTypeName("value", new List<IType>(), NameLookupMode.Expression) as LocalResolveResult; 
//				if (localResolveResult == null)
//					return;
//
//				bool referenceFound = false;
//				foreach (var result in ctx.FindReferences (body, localResolveResult.Variable)) {
//					var node = result.Node;
//					if (node.StartLocation >= body.StartLocation && node.EndLocation <= body.EndLocation) {
//						referenceFound = true;
//						break;
//					}
//				}
//
//				if (!referenceFound)
//					AddIssue(new CodeIssue(accessor.Keyword, accessorName));
//			}
//
//			static bool IsEligible(BlockStatement body)
//			{
//				if (body == null || body.IsNull)
//					return false;
//				if (body.Statements.FirstOrNullObject() is ThrowStatement)
//					return false;
//				return true;
//			}
		}
	}

	[ExportCodeFixProvider(ValueParameterNotUsedIssue.DiagnosticId, LanguageNames.CSharp)]
	public class ValueParameterNotUsedFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ValueParameterNotUsedIssue.DiagnosticId;
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
			var result = new List<CodeAction>();
			foreach (var diagnostic in diagnostics) {
				var node = root.FindNode(diagnostic.Location.SourceSpan);
				//if (!node.IsKind(SyntaxKind.BaseList))
				//	continue;
				var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, diagnostic.GetMessage(), document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}