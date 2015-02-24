//
// MethodOverloadWithOptionalParameterIssue.cs
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
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "MethodOverloadWithOptionalParameter")]
	public class MethodOverloadWithOptionalParameterIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "MethodOverloadWithOptionalParameterIssue";
		const string Description            = "Method with optional parameter is hidden by overload";
		const string MessageFormat          = "";
		const string Category               = IssueCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule1 = new DiagnosticDescriptor (DiagnosticId, Description, "Method with optional parameter is hidden by overload", Category, DiagnosticSeverity.Warning, true, "Method with optional parameter is hidden by overload");
		static readonly DiagnosticDescriptor Rule2 = new DiagnosticDescriptor (DiagnosticId, Description, "Indexer with optional parameter is hidden by overload", Category, DiagnosticSeverity.Warning, true, "Method with optional parameter is hidden by overload");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule1, Rule2);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<MethodOverloadWithOptionalParameterIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}
//
//			void CheckParameters(IParameterizedMember member,  List<IParameterizedMember> overloads, List<ParameterDeclaration> parameterDeclarations)
//			{
//				for (int i = 0; i < member.Parameters.Count; i++) {
//					if (!member.Parameters[i].IsOptional)
//						continue;
//
//					foreach (var overload in overloads) {
//						if (overload.Parameters.Count != i)
//							continue;
//						bool equal = true;
//						for (int j = 0; j < i; j++)  {
//							if (overload.Parameters[j].Type != member.Parameters[j].Type) {
//								equal = false;
//								break;
//							}
//						}
//						if (equal) {
//							AddIssue(new CodeIssue(
//								parameterDeclarations[i],
//								member.SymbolKind == SymbolKind.Method ?
			//								ctx.TranslateString("Method with optional parameter is hidden by overload") :
			//								ctx.TranslateString("Indexer with optional parameter is hidden by overload")));
//						}
//					}
//				}
//			}
//
//			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
//			{
//				var rr = ctx.Resolve(methodDeclaration) as MemberResolveResult;
//				if (rr == null || rr.IsError)
//					return;
//				var method = rr.Member as IMethod;
//				if (method == null)
//					return;
//				CheckParameters (method, 
//					method.DeclaringType.GetMethods(m =>
//						m.Name == method.Name && m.TypeParameters.Count == method.TypeParameters.Count).Cast<IParameterizedMember>().ToList(),
//					methodDeclaration.Parameters.ToList()
//				);
//
//			}
//
//			public override void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
//			{
//				var rr = ctx.Resolve(indexerDeclaration) as MemberResolveResult;
//				if (rr == null || rr.IsError)
//					return;
//				var method = rr.Member as IProperty;
//				if (method == null)
//					return;
//				CheckParameters (method, 
//					method.DeclaringType.GetProperties(m =>
//						m.IsIndexer &&
//						m != method.UnresolvedMember).Cast<IParameterizedMember>().ToList(),
//					indexerDeclaration.Parameters.ToList()
//				);
//			}
//
//
//			public override void VisitBlockStatement(BlockStatement blockStatement)
//			{
//				// SKIP
//			}
		}
	}

	[ExportCodeFixProvider(MethodOverloadWithOptionalParameterIssue.DiagnosticId, LanguageNames.CSharp)]
	public class MethodOverloadWithOptionalParameterFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return MethodOverloadWithOptionalParameterIssue.DiagnosticId;
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