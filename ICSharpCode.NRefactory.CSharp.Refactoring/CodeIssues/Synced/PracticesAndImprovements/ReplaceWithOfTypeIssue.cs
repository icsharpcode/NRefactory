//
// ReplaceWithOfTypeIssue.cs
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ReplaceWithOfType")]
	public class ReplaceWithOfTypeIssue : GatherVisitorCodeIssueProvider
	{
//		internal static readonly AstNode selectNotNullPattern =
//			new InvocationExpression(
//				new MemberReferenceExpression(new AnyNode("target"), "SelectNotNull"),
//				new LambdaExpression {
//					Parameters = { PatternHelper.NamedParameter ("param1", PatternHelper.AnyType ("paramType", true), Pattern.AnyString) },
//					Body = PatternHelper.OptionalParentheses (new AsExpression(new AnyNode("expr1"), new AnyNode("type")))
//				}
//			);
//
//		internal static readonly AstNode wherePatternCase1 =
//			new InvocationExpression(
//				new MemberReferenceExpression(
//					new InvocationExpression(
//						new MemberReferenceExpression(new AnyNode("target"), "Where"),
//						new LambdaExpression {
//							Parameters = { PatternHelper.NamedParameter ("param1", PatternHelper.AnyType ("paramType", true), Pattern.AnyString) },
//							Body = PatternHelper.OptionalParentheses (new IsExpression(new AnyNode("expr1"), new AnyNode("type")))
//						}
//					), "Select"),
//				new LambdaExpression {
//					Parameters = { PatternHelper.NamedParameter ("param2", PatternHelper.AnyType ("paramType", true), Pattern.AnyString) },
//					Body = PatternHelper.OptionalParentheses (new AsExpression(PatternHelper.OptionalParentheses (new AnyNode("expr2")), new Backreference("type")))
//				}
//		);
//
//		internal static readonly AstNode wherePatternCase2 =
//			new InvocationExpression(
//				new MemberReferenceExpression(
//					new InvocationExpression(
//						new MemberReferenceExpression(new AnyNode("target"), "Where"),
//						new LambdaExpression {
//							Parameters = { PatternHelper.NamedParameter ("param1", PatternHelper.AnyType ("paramType", true), Pattern.AnyString) },
//							Body = PatternHelper.OptionalParentheses (new IsExpression(PatternHelper.OptionalParentheses (new AnyNode("expr1")), new AnyNode("type")))
//						}
//					), "Select"),
//				new LambdaExpression {
//					Parameters = { PatternHelper.NamedParameter ("param2", PatternHelper.AnyType ("paramType", true), Pattern.AnyString) },
//					Body = PatternHelper.OptionalParentheses (new CastExpression(new Backreference("type"), PatternHelper.OptionalParentheses (new AnyNode("expr2"))))
//				}
//		);

		internal const string DiagnosticId  = "ReplaceWithOfTypeIssue";
		const string Description            = "Replace with call to OfType<T>";
		const string MessageFormat          = "Replace with OfType<T>()";
		const string Category               = IssueCategories.PracticesAndImprovements;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "Replace with OfType<T>");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

//		internal static bool CheckParameterMatches(IEnumerable<INode> paramMatch, IEnumerable<INode> expressionMatch)
//		{
//			var p = paramMatch.Single() as ParameterDeclaration;
//			var e = expressionMatch.Single();
//
//			if (p == null)
//				return false;
//			if (e is IdentifierExpression)
//				return p.Name == ((IdentifierExpression)e).Identifier;
//			return false;
//		}
//

		class GatherVisitor : GatherVisitorBase<ReplaceWithOfTypeIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitInvocationExpression (InvocationExpression anyInvoke)
//			{
//				var match = selectNotNullPattern.Match (anyInvoke);
//				if (!match.Success) {
//					match = wherePatternCase1.Match (anyInvoke);
//					if (!match.Success) {
//						match = wherePatternCase2.Match (anyInvoke); 
//						if (!match.Success) {
//							base.VisitInvocationExpression(anyInvoke);
//							return;
//						}
//					}
//					if (!CheckParameterMatches(match.Get("param1"), match.Get("expr1")) ||
//					    !CheckParameterMatches(match.Get("param2"), match.Get("expr2"))) {
//						base.VisitInvocationExpression (anyInvoke);
//						return;
//					}
//				} else {
//					if (!CheckParameterMatches(match.Get("param1"), match.Get("expr1"))) {
//						base.VisitInvocationExpression (anyInvoke);
//						return;
//					}
//				}
//				AddIssue(new CodeIssue(
//					anyInvoke,
//					ctx.TranslateString("Replace with OfType<T>"),
//					ctx.TranslateString("Replace with call to OfType<T>"),
//					script => {
//						var target = match.Get<Expression>("target").Single().Clone ();
//						var type = match.Get<AstType>("type").Single().Clone();
//						script.Replace(anyInvoke, new InvocationExpression(new MemberReferenceExpression(target, "OfType", type)));
//					}
//				));
//			}
		}
	}

	[ExportCodeFixProvider(ReplaceWithOfTypeIssue.DiagnosticId, LanguageNames.CSharp)]
	public class ReplaceWithOfTypeFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ReplaceWithOfTypeIssue.DiagnosticId;
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
				context.RegisterFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Replace with call to OfType<T>()", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}