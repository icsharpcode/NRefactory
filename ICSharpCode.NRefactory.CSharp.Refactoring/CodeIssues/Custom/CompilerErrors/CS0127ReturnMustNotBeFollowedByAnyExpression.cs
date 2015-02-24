//
// CS0127ReturnMustNotBeFollowedByAnyExpression.cs
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
	public class CS0127ReturnMustNotBeFollowedByAnyExpression : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "CS0127ReturnMustNotBeFollowedByAnyExpression";
		const string Description            = "Since 'function' returns void, a return keyword must not be followed by an object expression";
		const string MessageFormat          = "";
		const string Category               = IssueCategories.CompilerErrors;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Error, true, "CS0127: A method with a void return type cannot return a value.");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<CS0127ReturnMustNotBeFollowedByAnyExpression>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitReturnStatement(ReturnStatement returnStatement)
//			{
//				base.VisitReturnStatement(returnStatement);
//
//				if (ctx.GetExpectedType(returnStatement.Expression).Kind == TypeKind.Void) {
//					var actions = new List<CodeAction>();
//					actions.Add(new CodeAction(ctx.TranslateString("Remove returned expression"), script => {
//						script.Replace(returnStatement, new ReturnStatement());
//					}, returnStatement));
//
//					var method = returnStatement.GetParent<MethodDeclaration>();
//					if (method != null) {
//						var rr = ctx.Resolve(returnStatement.Expression);
//						if (rr != null && !rr.IsError) {
//							actions.Add(new CodeAction(ctx.TranslateString("Change return type of method."), script => {
//								script.Replace(method.ReturnType, ctx.CreateTypeSystemAstBuilder(method).ConvertType(rr.Type));
//							}, returnStatement));
//						}
//					}
//
//					AddIssue(new CodeIssue(
//						returnStatement, 
//						ctx.TranslateString("Return type is 'void'"),
//						actions
//					));
//				}
//			}
		}
	}

	[ExportCodeFixProvider(CS0127ReturnMustNotBeFollowedByAnyExpression.DiagnosticId, LanguageNames.CSharp)]
	public class CS0127ReturnMustNotBeFollowedByAnyExpressionFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return CS0127ReturnMustNotBeFollowedByAnyExpression.DiagnosticId;
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