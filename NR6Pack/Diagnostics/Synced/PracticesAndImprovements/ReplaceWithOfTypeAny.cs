//
// ReplaceWithOfTypeAny.cs
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
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ReplaceWithOfTypeAnyAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ReplaceWithOfTypeAnyAnalyzerID, 
			GettextCatalog.GetString("Replace with call to OfType<T>().Any()"),
			GettextCatalog.GetString("Replace with 'OfType<T>().Any()'"), 
			DiagnosticAnalyzerCategories.PracticesAndImprovements, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.ReplaceWithOfTypeAnyAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryGetDiagnostic(nodeContext, out diagnostic))
						nodeContext.ReportDiagnostic(diagnostic);
				},
				SyntaxKind.InvocationExpression
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;
			var anyInvoke = nodeContext.Node as InvocationExpressionSyntax;

			var info = nodeContext.SemanticModel.GetSymbolInfo(anyInvoke);

			IMethodSymbol anyResolve = info.Symbol as IMethodSymbol;
			if (anyResolve == null) {
				anyResolve = info.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault(candidate => HasPredicateVersion(candidate));
			} 

			if (anyResolve == null || !HasPredicateVersion(anyResolve))
				return false;

			ExpressionSyntax target, followUp;
			TypeSyntax type;
			ParameterSyntax param;
			if (MatchSelect(anyInvoke, out target, out type, out param, out followUp)) {
				// if (member == "Where" && followUp == null) return;

				diagnostic = Diagnostic.Create (
					descriptor,
					anyInvoke.GetLocation ()
				);
				return true;
			}
			return false;

		}

		static bool CompareNames(ParameterSyntax param, IdentifierNameSyntax expr)
		{
			if (param == null || expr == null)
				return false;
			return param.Identifier.ValueText == expr.Identifier.ValueText;
		}

		//		static readonly AstNode selectPattern =
		//			new InvocationExpression(
		//				new MemberReferenceExpression(
		//					new InvocationExpression(
		//						new MemberReferenceExpression(new AnyNode("targetExpr"), "Select"),
		//						new LambdaExpression {
		//							Parameters = { PatternHelper.NamedParameter ("param1", PatternHelper.AnyType ("paramType", true), Pattern.AnyString) },
		//							Body = PatternHelper.OptionalParentheses (new AsExpression(new AnyNode("expr1"), PatternHelper.AnyType("type")))
		//						}
		//					), 
		//					Pattern.AnyString
		//				),
		//				new LambdaExpression {
		//					Parameters = { PatternHelper.NamedParameter ("param2", PatternHelper.AnyType ("paramType", true), Pattern.AnyString) },
		//					Body = PatternHelper.CommutativeOperatorWithOptionalParentheses(new AnyNode("expr2"), BinaryOperatorType.InEquality, new NullReferenceExpression())
		//				}
		//			);
		//
		//		static readonly AstNode selectPatternWithFollowUp =
		//			new InvocationExpression(
		//				new MemberReferenceExpression(
		//					new InvocationExpression(
		//						new MemberReferenceExpression(new AnyNode("targetExpr"), "Select"),
		//						new LambdaExpression {
		//							Parameters = { PatternHelper.NamedParameter ("param1", PatternHelper.AnyType ("paramType", true), Pattern.AnyString) },
		//							Body = PatternHelper.OptionalParentheses (new AsExpression(new AnyNode("expr1"), PatternHelper.AnyType("type")))
		//						}
		//					),	 
		//					Pattern.AnyString
		//				),
		//				new NamedNode("lambda", 
		//					new LambdaExpression {
		//						Parameters = { PatternHelper.NamedParameter ("param2", PatternHelper.AnyType ("paramType", true), Pattern.AnyString) },
		//						Body = new BinaryOperatorExpression(
		//							PatternHelper.CommutativeOperatorWithOptionalParentheses(new AnyNode("expr2"), BinaryOperatorType.InEquality, new NullReferenceExpression()),
		//							BinaryOperatorType.ConditionalAnd,
		//							new AnyNode("followUpExpr")
		//						)
		//					}
		//				)
		//			);
		internal static bool MatchSelect(InvocationExpressionSyntax anyInvoke, out ExpressionSyntax target, out TypeSyntax type, out ParameterSyntax lambdaParam, out ExpressionSyntax followUpExpression)
		{
			target = null;
			type = null;
			lambdaParam = null;
			followUpExpression = null;

			if (anyInvoke.ArgumentList.Arguments.Count != 1)
				return false;
			var anyInvokeBase = anyInvoke.Expression as MemberAccessExpressionSyntax;
			if (anyInvokeBase == null)
				return false;
			var selectInvoc = anyInvokeBase.Expression as InvocationExpressionSyntax;
			if (selectInvoc == null || selectInvoc.ArgumentList.Arguments.Count != 1)
				return false;
			var baseMember = selectInvoc.Expression as MemberAccessExpressionSyntax;
			if (baseMember == null || baseMember.Name.Identifier.Text != "Select")
				return false;
			target = baseMember.Expression;

			ParameterSyntax param1, param2;
			BinaryExpressionSyntax expr1, expr2;
			if (!ExtractLambda(selectInvoc.ArgumentList.Arguments[0], out param1, out expr1))
				return false;
			if (!ExtractLambda(anyInvoke.ArgumentList.Arguments[0], out param2, out expr2))
				return false;
			lambdaParam = param2;
			if (!CompareNames(param1, expr1.Left as IdentifierNameSyntax))
				return false;
			if (expr2.IsKind(SyntaxKind.LogicalAndExpression)) {
				if (CheckNotEqualsNullExpr(expr2.Left as BinaryExpressionSyntax, param2))
					followUpExpression = expr2.Right;
				else if (CheckNotEqualsNullExpr(expr2.Right as BinaryExpressionSyntax, param2))
					followUpExpression = expr2.Left;
				else
					return false;
			} else if (!CheckNotEqualsNullExpr(expr2, param2))
				return false;

			if (expr1.IsKind(SyntaxKind.AsExpression))
				type = expr1.Right as TypeSyntax;

			return target != null && type != null;
		}

		static bool CheckNotEqualsNullExpr(BinaryExpressionSyntax expr, ParameterSyntax param)
		{
			if (expr == null)
				return false;
			if (!expr.IsKind(SyntaxKind.NotEqualsExpression))
				return false;
			if (expr.Right.IsKind(SyntaxKind.NullLiteralExpression) && CompareNames(param, expr.Left as IdentifierNameSyntax))
				return true;
			if (expr.Left.IsKind(SyntaxKind.NullLiteralExpression) && CompareNames(param, expr.Right as IdentifierNameSyntax))
				return true;
			return false;
		}

		static bool ExtractLambda(ArgumentSyntax argument, out ParameterSyntax parameter, out BinaryExpressionSyntax body)
		{
			if (argument.Expression is SimpleLambdaExpressionSyntax)
			{
				var simple = (SimpleLambdaExpressionSyntax)argument.Expression;
				parameter = simple.Parameter;
				body = simple.Body as BinaryExpressionSyntax;
				if (body == null)
				{
					return false;
				}
				body = body.SkipParens() as BinaryExpressionSyntax;
				return true;
			}

			parameter = null;
			body = null;
			return false;
		}

		internal static InvocationExpressionSyntax MakeOfTypeCall(InvocationExpressionSyntax anyInvoke)
		{
			var member = ((MemberAccessExpressionSyntax)anyInvoke.Expression).Name;
			ExpressionSyntax target, followUp;
			TypeSyntax type;
			ParameterSyntax param;
			if (MatchSelect(anyInvoke, out target, out type, out param, out followUp))
			{
				var ofTypeIdentifier = ((SimpleNameSyntax)SyntaxFactory.ParseName("OfType")).Identifier;
				var typeParams = SyntaxFactory.SeparatedList(new[] { type });
				var ofTypeName = SyntaxFactory.GenericName(ofTypeIdentifier, SyntaxFactory.TypeArgumentList(typeParams));
				InvocationExpressionSyntax ofTypeCall = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, target, ofTypeName));
				var callerExpr = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ofTypeCall, member).WithAdditionalAnnotations(Formatter.Annotation);
				if (followUp == null)
					return SyntaxFactory.InvocationExpression(callerExpr);
				var lambdaExpr = SyntaxFactory.SimpleLambdaExpression(param, followUp);
				var argument = SyntaxFactory.Argument(lambdaExpr).WithAdditionalAnnotations(Formatter.Annotation);
				return SyntaxFactory.InvocationExpression(callerExpr, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[] { argument })));
			}

			return null;
		}

		internal static bool IsQueryExtensionClass(INamedTypeSymbol typeDef)
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

		static bool HasPredicateVersion(IMethodSymbol member)
		{
			if (!IsQueryExtensionClass(member.ContainingType))
				return false;
			return member.Name == "Any";
		}
	}
}