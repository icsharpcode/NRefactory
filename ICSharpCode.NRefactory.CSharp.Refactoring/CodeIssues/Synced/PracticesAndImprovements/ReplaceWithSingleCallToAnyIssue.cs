// 
// ReplaceWithSingleCallToAnyIssue.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2013 Xamarin <http://xamarin.com>
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ReplaceWithSingleCallToAny")]
	public class ReplaceWithSingleCallToAnyIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "ReplaceWithSingleCallToAnyIssue";
		const string Description            = "Replace with single call to Any(...)";
		const string MessageFormat          = "Redundant Where() call with predicate followed by {0}()";
		const string Category               = IssueCategories.PracticesAndImprovements;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "Replace with single call to Any(...)");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor<ReplaceWithSingleCallToAnyIssue>(semanticModel, addDiagnostic, cancellationToken, "Any");
		}

		//		static readonly AstNode pattern =
		//			new InvocationExpression (
		//				new MemberReferenceExpression (
		//					new NamedNode ("whereInvoke",
		//					               new InvocationExpression (
		//					               	new MemberReferenceExpression (new AnyNode ("target"), "Where"),
		//					               	new AnyNode ())),
		//					Pattern.AnyString));
		static bool MatchWhere(InvocationExpressionSyntax anyInvoke, out ExpressionSyntax target, out InvocationExpressionSyntax whereInvoke)
		{
			target = null;
			whereInvoke = null;

			if (anyInvoke.ArgumentList.Arguments.Count != 0)
				return false;
			var anyInvokeBase = anyInvoke.Expression as MemberAccessExpressionSyntax;
			if (anyInvokeBase == null)
				return false;
			whereInvoke = anyInvokeBase.Expression as InvocationExpressionSyntax;
			if (whereInvoke == null || whereInvoke.ArgumentList.Arguments.Count != 1)
				return false;
			var baseMember = whereInvoke.Expression as MemberAccessExpressionSyntax;
			if (baseMember == null || baseMember.Name.Identifier.Text != "Where")
				return false;
			target = baseMember.Expression;

			return target != null;
		}

		//				AddIssue(new CodeIssue(
		//					anyInvoke, string.Format(ctx.TranslateString("Redundant Where() call with predicate followed by {0}()"), anyResolve.Member.Name),
		//					new CodeAction (
		//						string.Format(ctx.TranslateString("Replace with single call to '{0}'"), anyResolve.Member.Name),
		//						script => {
		//							var arg = whereInvoke.Arguments.Single ().Clone ();
		//							var target = match.Get<Expression> ("target").Single ().Clone ();
		//							script.Replace (anyInvoke, new InvocationExpression (new MemberReferenceExpression (target, anyResolve.Member.Name), arg));
		//						},
		//						anyInvoke
		//					)
		//				));
		internal static InvocationExpressionSyntax MakeSingleCall(InvocationExpressionSyntax anyInvoke)
		{
			var member = ((MemberAccessExpressionSyntax)anyInvoke.Expression).Name;
			ExpressionSyntax target;
			InvocationExpressionSyntax whereInvoke;
			if (MatchWhere(anyInvoke, out target, out whereInvoke))
			{
				var callerExpr = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, target, member).WithAdditionalAnnotations(Formatter.Annotation);
				var argument = whereInvoke.ArgumentList.Arguments[0].WithAdditionalAnnotations(Formatter.Annotation);
				return SyntaxFactory.InvocationExpression(callerExpr, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[] { argument })));
			}

			return null;
		}

		internal class GatherVisitor<T> : GatherVisitorBase<T> where T : GatherVisitorCodeIssueProvider
		{
			readonly string member;

			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken, string member)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
				this.member = member;
			}

			public override void VisitInvocationExpression(InvocationExpressionSyntax anyInvoke)
			{
				var info = semanticModel.GetSymbolInfo(anyInvoke);
				IMethodSymbol anyResolve = info.Symbol as IMethodSymbol;
				if (anyResolve == null) {
					anyResolve = info.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault(candidate => HasPredicateVersion(candidate));
				}

				if (anyResolve == null || !HasPredicateVersion(anyResolve))
					return;

				ExpressionSyntax target;
				InvocationExpressionSyntax whereInvoke;
				if (!MatchWhere(anyInvoke, out target, out whereInvoke))
					return;
				info = semanticModel.GetSymbolInfo(whereInvoke);
				IMethodSymbol whereResolve = info.Symbol as IMethodSymbol;
				if (whereResolve == null){
					whereResolve = info.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault(candidate => candidate.Name == "Where" && IsQueryExtensionClass(candidate.ContainingType));
				}

				if (whereResolve == null || whereResolve.Name != "Where" || !IsQueryExtensionClass(whereResolve.ContainingType))
					return;
				if (whereResolve.Parameters.Length != 1)
					return;
				var predResolve = whereResolve.Parameters[0];
				if (predResolve.Type.GetTypeParameters().Length != 2)
					return;

				AddIssue(Diagnostic.Create(Rule, anyInvoke.GetLocation(), member));
			}

			static bool IsQueryExtensionClass(INamedTypeSymbol typeDef)
			{
				if (typeDef == null || typeDef.ContainingNamespace == null || typeDef.ContainingNamespace.GetFullName() != "System.Linq")
					return false;
				switch (typeDef.Name)
				{
					case "Enumerable":
					case "ParallelEnumerable":
					case "Queryable":
						return true;
					default:
						return false;
				}
			}

			bool HasPredicateVersion(IMethodSymbol member)
			{
				if (!IsQueryExtensionClass(member.ContainingType))
					return false;
				return member.Name == this.member;
			}
		}
	}

	[ExportCodeFixProvider(ReplaceWithSingleCallToAnyIssue.DiagnosticId, LanguageNames.CSharp)]
	public class ReplaceWithSingleCallToAnyFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ReplaceWithSingleCallToAnyIssue.DiagnosticId;
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
				var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie:true) as InvocationExpressionSyntax;
				var newRoot = root.ReplaceNode(node, ReplaceWithSingleCallToAnyIssue.MakeSingleCall(node));
				var member = ((MemberAccessExpressionSyntax)node.Expression).Name;
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, string.Format("Replace with single call to '{0}'", member.Identifier.ValueText), document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}