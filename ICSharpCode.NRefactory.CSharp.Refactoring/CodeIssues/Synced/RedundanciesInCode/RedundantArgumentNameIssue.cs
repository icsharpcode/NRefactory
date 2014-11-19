// 
// RedundantArgumentNameIssue.cs
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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[NRefactoryCodeDiagnosticAnalyzerAttribute(AnalysisDisableKeyword = "RedundantArgumentName")]
	public class RedundantArgumentNameIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "RedundantArgumentNameIssue";
		const string Category               = IssueCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, "Redundant explicit argument name specification", "Redundant argument name specification", Category, DiagnosticSeverity.Warning, true, "Redundant explicit argument name specification");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<RedundantArgumentNameIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			void CheckParameters(ISymbol ir, IEnumerable<ArgumentSyntax> arguments)
			{
				if (ir == null)
					return;
				var parameters = ir.GetParameters();
				int i = 0;

				foreach (var arg in arguments) {
					var na = arg.NameColon;
					if (na != null) {
						if (i >= parameters.Length || na.Name.ToString() != parameters[i].Name)
							break;
						AddIssue (Diagnostic.Create(Rule, na.GetLocation()));
					}
					i++;
				}
			}

			void CheckParameters(ISymbol ir, IEnumerable<AttributeArgumentSyntax> arguments)
			{
				if (ir == null)
					return;
				var parameters = ir.GetParameters();
				int i = 0;

				foreach (var arg in arguments) {
					var na = arg.NameColon;
					if (na != null) {
						if (i >= parameters.Length || na.Name.ToString() != parameters[i].Name)
							break;
						AddIssue (Diagnostic.Create(Rule, na.GetLocation()));
					}
					i++;
				}
			}

			public override void VisitInvocationExpression(InvocationExpressionSyntax node)
			{
				base.VisitInvocationExpression(node);
				CheckParameters(semanticModel.GetSymbolInfo(node).Symbol, node.ArgumentList.Arguments);
			}

			public override void VisitElementAccessExpression(ElementAccessExpressionSyntax node)
			{
				base.VisitElementAccessExpression(node);
				CheckParameters(semanticModel.GetSymbolInfo(node).Symbol, node.ArgumentList.Arguments);
			}

			public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
			{
				base.VisitObjectCreationExpression(node);
				CheckParameters(semanticModel.GetSymbolInfo(node).Symbol, node.ArgumentList.Arguments);
			}

			public override void VisitAttribute(AttributeSyntax node)
			{
				base.VisitAttribute(node);
				CheckParameters(semanticModel.GetSymbolInfo(node).Symbol, node.ArgumentList.Arguments);
			}
		}
	}

	[ExportCodeFixProvider(RedundantArgumentNameIssue.DiagnosticId, LanguageNames.CSharp)]
	public class RedundantArgumentNameFixProvider : NRefactoryCodeFixProvider
	{
		const string CodeActionMessage = "Remove argument name specification";

		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return RedundantArgumentNameIssue.DiagnosticId;
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
				var argListSyntax = node.Parent.Parent as BaseArgumentListSyntax;
				if (node.IsKind(SyntaxKind.NameColon) && argListSyntax != null) {
					bool replace = true;
					var newRoot = root;
					var args = new List<ArgumentSyntax> ();

					foreach (var arg in argListSyntax.Arguments) {
						if (replace) {
							args.Add(arg);
						}
						replace &= arg != node.Parent;

					}
					newRoot = newRoot.ReplaceNodes(args, (arg, arg2) => SyntaxFactory.Argument(arg.Expression).WithAdditionalAnnotations(Formatter.Annotation));

					context.RegisterFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, CodeActionMessage, document.WithSyntaxRoot(newRoot)), diagnostic);
					continue;
				}
				var attrListSyntax = node.Parent.Parent as AttributeArgumentListSyntax;
				if (node.IsKind(SyntaxKind.NameColon) && attrListSyntax != null) {
					bool replace = true;
					var newRoot = root;
					var args = new List<AttributeArgumentSyntax> ();

					foreach (var arg in attrListSyntax.Arguments) {
						if (replace) {
							args.Add(arg);
						}
						replace &= arg != node.Parent;

					}
					newRoot = newRoot.ReplaceNodes(args, (arg, arg2) => SyntaxFactory.AttributeArgument(arg.Expression).WithAdditionalAnnotations(Formatter.Annotation));

					context.RegisterFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, CodeActionMessage, document.WithSyntaxRoot(newRoot)), diagnostic);
					continue;
				}
			}
		}
	}

}