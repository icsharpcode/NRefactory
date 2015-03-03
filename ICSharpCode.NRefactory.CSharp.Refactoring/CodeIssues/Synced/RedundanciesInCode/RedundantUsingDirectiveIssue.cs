// 
// RedundantUsingInspector.cs
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
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "RedundantUsingDirective")]
	public class RedundantUsingDirectiveIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "RedundantUsingDirectiveIssue";
		const string Description            = "Using directive is not required and can safely be removed.";
		const string MessageFormat          = "Using directive is not used by code and can be removed safely.";
		const string Category               = IssueCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "Redundant using directive");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}


		readonly List<string> namespacesToKeep = new List<string>();

		/// <summary>
		/// The list of namespaces that should be kept even if they are not being used.
		/// Used in SharpDevelop to always keep the "System" namespace around.
		/// </summary>
		public IList<string> NamespacesToKeep {
			get { return namespacesToKeep; }
		}


//		public override IEnumerable<CodeIssue> GetIssues(BaseSemanticModel context, string subIssue)
//		{
//			var visitor = new GatherVisitor(context, this);
//			context.RootNode.AcceptVisitor (visitor);
//			visitor.Collect (0);
//			return visitor.FoundIssues;
//		}

		class GatherVisitor : GatherVisitorBase<RedundantUsingDirectiveIssue>
		{
//			class UsingDeclarationSpecifier {
//				public UsingDeclaration UsingDeclaration { get; set; }
//				public bool IsUsed { get; set; }
//
//				public UsingDeclarationSpecifier(UsingDeclaration usingDeclaration)
//				{
//					this.UsingDeclaration = usingDeclaration;
//				}
//			}
//
//			List<UsingDeclarationSpecifier> declarations = new List<UsingDeclarationSpecifier>();
//			HashSet<string> usedNamespaces = new HashSet<string>();

			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}
//
//			public void Collect(int startIndex)
//			{
//				var unused = new List<UsingDeclaration>();
//				foreach (var u in declarations.Skip (startIndex)) {
//					if (u.IsUsed || 
//					    issueProvider.namespacesToKeep.Contains(u.UsingDeclaration.Namespace))
//						continue;
//					unused.Add(u.UsingDeclaration);
//				}
//
//				foreach (var decl in unused) {
//					AddIssue(new CodeIssue(
//						decl,
//						ctx.TranslateString(""), ctx.TranslateString(""),
//						script => {
//						foreach (var u2 in unused) {
//							script.Remove (u2);
//						}
//					}
//					) { IssueMarker = IssueMarker.GrayOut });
//				}
//			}
//
//			public override void VisitUsingDeclaration(UsingDeclaration usingDeclaration)
//			{
//				if (IsSuppressed(usingDeclaration.StartLocation))
//					return;
//				declarations.Add(new UsingDeclarationSpecifier (usingDeclaration));
//			}
//
//			public override void VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
//			{
//				int idx = declarations.Count;
//				usedNamespaces.Clear();
//				base.VisitNamespaceDeclaration(namespaceDeclaration);
//				Collect(idx);
//				declarations.RemoveRange(idx, declarations.Count - idx);
//			}
//			
//			void UseNamespace(string ns)
//			{
//				if (usedNamespaces.Contains(ns))
//					return;
//				usedNamespaces.Add(ns);
//				for (int i = declarations.Count - 1; i >= 0; i--) {
//					var decl = declarations [i];
//					if (decl.UsingDeclaration.Namespace == ns) {
//						decl.IsUsed = true;
//						break;
//					}
//				}
//			}
//
//			public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
//			{
//				base.VisitIdentifierExpression(identifierExpression);
//				var trr = ctx.Resolve(identifierExpression) as TypeResolveResult;
//				if (trr != null) {
//					UseNamespace(trr.Type.Namespace);
//				}
//			}
//
//			public override void VisitSimpleType(SimpleType simpleType)
//			{
//				base.VisitSimpleType(simpleType);
//				UseNamespace(ctx.Resolve(simpleType).Type.Namespace);
//			}
//
//			public override void VisitInvocationExpression (InvocationExpression invocationExpression)
//			{
//				base.VisitInvocationExpression (invocationExpression);
//				UseExtensionMethod(ctx.Resolve(invocationExpression));
//			}
//			
//			void UseExtensionMethod(ResolveResult rr)
//			{
//				var mg = rr as CSharpInvocationResolveResult;
//				if (mg != null && mg.IsExtensionMethodInvocation) {
//					UseNamespace (mg.Member.DeclaringType.Namespace);
//				}
//			}
//			
//			public override void VisitQueryExpression(QueryExpression queryExpression)
//			{
//				base.VisitQueryExpression(queryExpression);
//				foreach (var clause in queryExpression.Clauses) {
//					UseExtensionMethod(ctx.Resolve(clause));
//				}
//			}
		}
	}

	[ExportCodeFixProvider(RedundantUsingDirectiveIssue.DiagnosticId, LanguageNames.CSharp)]
	public class RedundantUsingDirectiveFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return RedundantUsingDirectiveIssue.DiagnosticId;
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
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Remove redundant using directives", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}