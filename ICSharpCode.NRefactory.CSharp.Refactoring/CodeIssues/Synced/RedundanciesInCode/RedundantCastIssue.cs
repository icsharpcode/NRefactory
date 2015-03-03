// 
// RedundantCastIssue.cs
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
using System.Linq;
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
using System.ComponentModel;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzerAttribute(AnalysisDisableKeyword = "RedundantCast")]
	[Description("Type cast is redundant")]
	public class RedundantCastIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "RedundantCastIssue";
		const string Category               = IssueCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, "Type cast can be safely removed.", "Type cast is redundant", Category, DiagnosticSeverity.Warning, true, "Redundant cast");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<RedundantCastIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			void CheckTypeCast(SyntaxNode node, ExpressionSyntax expressionSyntax, ExpressionSyntax typeSyntax)
			{
				var expressionSymbol = semanticModel.GetSymbolInfo(expressionSyntax);
				var fullSymbol = semanticModel.GetSymbolInfo(typeSyntax).Symbol as ITypeSymbol;
				if (expressionSymbol.Symbol == null || fullSymbol == null)
					return;
				var rt = expressionSymbol.Symbol.GetReturnType();
				if (rt == null)
					return;
				var conversion = semanticModel.Compilation.ClassifyConversion(rt, fullSymbol);
				if (conversion.IsIdentity) {
					AddIssue(Diagnostic.Create(Rule, node.GetLocation()));
					return;
				}
				if (!conversion.Exists)
					return;
			}

			public override void VisitCastExpression(CastExpressionSyntax node)
			{
				base.VisitCastExpression(node);
				CheckTypeCast(node, node.Expression, node.Type);
			}

			public override void VisitBinaryExpression(BinaryExpressionSyntax node)
			{
				base.VisitBinaryExpression(node);
				if (node.IsKind(SyntaxKind.AsExpression)) {
					CheckTypeCast(node, node.Left, node.Right);
				}
			}

			/*
			ITypeSymbol GetExpectedType(ExpressionSyntax typeCastNode, out ISymbol accessingMember)
			{
				var memberRefExpr = typeCastNode.Parent as MemberAccessExpressionSyntax;
				if (memberRefExpr != null) {
					var invocationExpr = memberRefExpr.Parent as InvocationExpressionSyntax;
					if (invocationExpr != null && invocationExpr.Expression == memberRefExpr) {
						var invocationResolveResult = semanticModel.GetSymbolInfo(invocationExpr);
						if (invocationResolveResult.Symbol != null) {
							accessingMember = invocationResolveResult.Symbol;
							return invocationResolveResult.Symbol.ContainingType;
						}
					} else {
						var memberResolveResult = semanticModel.GetSymbolInfo(memberRefExpr);
						if (memberResolveResult.Symbol != null) {
							accessingMember = memberResolveResult.Symbol;
							return memberResolveResult.Symbol.ContainingType;
						}
					}
				}
				accessingMember = null;
				return ctx.GetExpectedType(typeCastNode);
			}

			bool IsExplicitImplementation(IType exprType, IType interfaceType, Expression typeCastNode)
			{
				var memberRefExpr = typeCastNode.Parent as MemberReferenceExpression;
				if (memberRefExpr != null) {
					var rr = ctx.Resolve(memberRefExpr);
					var memberResolveResult = rr as MemberResolveResult;
					if (memberResolveResult != null) {
						foreach (var member in exprType.GetMembers (m => m.SymbolKind == memberResolveResult.Member.SymbolKind)) {
							if (member.IsExplicitInterfaceImplementation && member.ImplementedInterfaceMembers.Contains(memberResolveResult.Member)) {
								return true;
							}
						}
					}

					var methodGroupResolveResult = rr as MethodGroupResolveResult;
					if (methodGroupResolveResult != null) {
						foreach (var member in exprType.GetMethods ()) {
							if (member.IsExplicitInterfaceImplementation && member.ImplementedInterfaceMembers.Any(m => methodGroupResolveResult.Methods.Contains((IMethod)m))) {
								return true;
							}
						}
					}
				}
				return false;
			}

			void AddIssue(Expression outerTypeCastNode, Expression typeCastNode, Expression expr, TextLocation start, TextLocation end)
			{
				AstNode type;
				if (typeCastNode is CastExpression)
					type = ((CastExpression)typeCastNode).Type;
				else
					type = ((AsExpression)typeCastNode).Type;
				AddIssue(new CodeIssue(start, end, ctx.TranslateString("Type cast is redundant"), string.Format(ctx.TranslateString("Remove cast to '{0}'"), type),
					script => script.Replace(outerTypeCastNode, expr.Clone())) { IssueMarker = IssueMarker.GrayOut });
			}

			bool IsRedundantInBinaryExpression(BinaryOperatorExpression bop, Expression outerTypeCastNode, IType exprType)
			{
				if (bop.Operator == BinaryOperatorType.NullCoalescing) {
					if (outerTypeCastNode == bop.Left) {
						var rr = ctx.Resolve(bop.Right).Type;
						if (rr != exprType)
							return true;
					}
					return false;
				}

				return ctx.Resolve(bop.Left).Type != ctx.Resolve(bop.Right).Type;
			}

			bool IsBreaking(IType exprType, IType expectedType)
			{
				if (exprType.IsReferenceType == true &&
					expectedType.IsReferenceType == false &&
					exprType != expectedType)
					return true;
				if (exprType.IsReferenceType == false &&
					expectedType.IsReferenceType == true &&
					exprType != expectedType)
					return true;
				return false;
			}

			bool IsRequiredToSelectOverload(IEnumerable<IMethod> methods, IType expectedType, int nArg)
			{
				// TODO: Check method accessibility & other parameter match.
				int count = 0;
				foreach (var method in methods) {
					if (method.Parameters.Count < nArg)
						continue;
					var baseTypes = method.Parameters[nArg].Type.GetAllBaseTypes();
					if (expectedType == method.Parameters[nArg].Type || baseTypes.Any(t => t.Equals(expectedType)))
						count++;
				}
				return count > 1;
			}

			void CheckTypeCast(Expression typeCastNode, Expression expr, TextLocation castStart, TextLocation castEnd)
			{
				var outerTypeCastNode = typeCastNode;
				while (outerTypeCastNode.Parent is ParenthesizedExpression)
					outerTypeCastNode = (Expression)outerTypeCastNode.Parent;

				IMember accessingMember;
				var expectedType = GetExpectedType(outerTypeCastNode, out accessingMember);
				var exprType = ctx.Resolve(expr).Type;
				if (expectedType.Kind == TypeKind.Interface && IsExplicitImplementation(exprType, expectedType, outerTypeCastNode))
					return;
				var baseTypes = exprType.GetAllBaseTypes().ToList();
				if (!baseTypes.Any(t => t.Equals(expectedType)))
					return;

				if (IsBreaking(exprType, expectedType))
					return;

				var cond = outerTypeCastNode.Parent as ConditionalExpression;
				if (cond != null) {
					if (outerTypeCastNode == cond.TrueExpression) {
						var rr = ctx.Resolve(cond.FalseExpression).Type;
						if (rr != exprType)
							return;
					}
				}

				var bop = outerTypeCastNode.Parent as BinaryOperatorExpression;
				if (bop != null && IsRedundantInBinaryExpression(bop, outerTypeCastNode, exprType))
					return;

				// check if the called member doesn't change it's virtual slot when changing types
				if (accessingMember != null) {
					var baseMember = InheritanceHelper.GetBaseMember(accessingMember);
					foreach (var bt in baseTypes) {
						foreach (var member in bt.GetMembers(m => m.Name == accessingMember.Name)) {
							if (InheritanceHelper.GetBaseMember(member) != baseMember)
								return;
						}
					}
				}

				var mrr = ctx.Resolve(typeCastNode.Parent) as CSharpInvocationResolveResult;
				if (mrr != null) {
					if (mrr.Member.SymbolKind == SymbolKind.Constructor) {
						int nArg = typeCastNode.Parent.Children
							.Where(n => n.Role == Roles.Argument)
							.TakeWhile(n => n.DescendantNodesAndSelf().All(c => c != typeCastNode))
							.Count();

						if (IsRequiredToSelectOverload(mrr.Member.DeclaringTypeDefinition.GetConstructors(), expectedType, nArg))
							return;
					} else if (mrr.Member.SymbolKind == SymbolKind.Method) {
						int nArg = typeCastNode.Parent.Children
							.Where(n => n.Role == Roles.Argument)
							.TakeWhile(n => n.DescendantNodesAndSelf().All(c => c != typeCastNode))
							.Count();
						if (IsRequiredToSelectOverload(mrr.Member.DeclaringTypeDefinition.GetMethods(m => m.Name == mrr.Member.Name), expectedType, nArg))
							return;
					}
				}

				AddIssue(outerTypeCastNode, typeCastNode, expr, castStart, castEnd);
			}
			*/
		}
	}

	[ExportCodeFixProvider(RedundantCastIssue.DiagnosticId, LanguageNames.CSharp)]
	public class RedundantCastFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return RedundantCastIssue.DiagnosticId;
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
				var ca = node as CastExpressionSyntax;
				if (ca != null) {
					SyntaxNode outerTypeCastNode = ca;
					while (outerTypeCastNode.Parent is ParenthesizedExpressionSyntax)
						outerTypeCastNode = outerTypeCastNode.Parent;
					var newRoot = root.ReplaceNode((SyntaxNode)outerTypeCastNode, ca.Expression.SkipParens());
					context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, string.Format("Remove cast to '{0}'", ca.Type), document.WithSyntaxRoot(newRoot)), diagnostic);
				}
			}
		}
	}
}
