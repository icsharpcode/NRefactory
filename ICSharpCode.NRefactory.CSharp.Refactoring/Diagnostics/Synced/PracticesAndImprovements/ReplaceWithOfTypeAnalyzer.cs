//
// ReplaceWithOfTypeAnalyzer.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ReplaceWithOfType")]
	public class ReplaceWithOfTypeAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "ReplaceWithOfTypeAnalyzer";
		const string Description            = "Replace with call to OfType<T>";
		const string MessageFormat          = "Replace with OfType<T>()";
		const string Category               = DiagnosticAnalyzerCategories.PracticesAndImprovements;

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
		static bool MatchWhereSelect(InvocationExpressionSyntax selectInvoke, out ExpressionSyntax target, out TypeSyntax type)
		{
			target = null;
			type = null;

			if (selectInvoke.ArgumentList.Arguments.Count != 1)
				return false;
			var anyInvokeBase = selectInvoke.Expression as MemberAccessExpressionSyntax;
			if (anyInvokeBase == null || anyInvokeBase.Name.Identifier.Text != "Select")
				return false;
			var whereInvoke = anyInvokeBase.Expression as InvocationExpressionSyntax;
			if (whereInvoke == null || whereInvoke.ArgumentList.Arguments.Count != 1)
				return false;
			var baseMember = whereInvoke.Expression as MemberAccessExpressionSyntax;
			if (baseMember == null || baseMember.Name.Identifier.Text != "Where")
				return false;
			target = baseMember.Expression;

			ParameterSyntax param1, param2;
			ExpressionSyntax expr1, expr2;
			if (!ExtractLambda(whereInvoke.ArgumentList.Arguments[0], out param1, out expr1))
				return false;
			if (!ExtractLambda(selectInvoke.ArgumentList.Arguments[0], out param2, out expr2))
				return false;
			if (!expr1.IsKind(SyntaxKind.IsExpression))
				return false;
			type = (expr1 as BinaryExpressionSyntax)?.Right as TypeSyntax;
			if (type == null)
				return false;
			if (expr2.IsKind(SyntaxKind.AsExpression)) {
				if (!CompareNames(param2, (expr2 as BinaryExpressionSyntax).Left as IdentifierNameSyntax))
					return false;
				if (!type.IsEquivalentTo((expr2 as BinaryExpressionSyntax)?.Right))
					return false;
			} else if (expr2.IsKind(SyntaxKind.CastExpression)) {
				if (!CompareNames(param2, (expr2 as CastExpressionSyntax)?.Expression.SkipParens() as IdentifierNameSyntax))
					return false;
				if (!type.IsEquivalentTo((expr2 as CastExpressionSyntax)?.Type))
					return false;
			} else
				return false;

			if (!CompareNames(param1, (expr1 as BinaryExpressionSyntax)?.Left as IdentifierNameSyntax))
				return false;

			
			return target != null;
		}

		static bool CompareNames(ParameterSyntax param, IdentifierNameSyntax expr)
		{
			if (param == null || expr == null)
				return false;
			return param.Identifier.ValueText == expr.Identifier.ValueText;
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

		static bool ExtractLambda(ArgumentSyntax argument, out ParameterSyntax parameter, out ExpressionSyntax body)
		{
			if (argument.Expression is SimpleLambdaExpressionSyntax)
			{
				var simple = (SimpleLambdaExpressionSyntax)argument.Expression;
				parameter = simple.Parameter;
				body = simple.Body as ExpressionSyntax;
				if (body == null)
					return false;
				body = body.SkipParens();
				return true;
			}

			parameter = null;
			body = null;
			return false;
		}

		internal static InvocationExpressionSyntax MakeOfTypeCall(InvocationExpressionSyntax anyInvoke)
		{
			var member = ((MemberAccessExpressionSyntax)anyInvoke.Expression).Name;
			ExpressionSyntax target;
			TypeSyntax type;
			if (MatchWhereSelect(anyInvoke, out target, out type))
			{
				var ofTypeIdentifier = ((SimpleNameSyntax)SyntaxFactory.ParseName("OfType")).Identifier;
				var typeParams = SyntaxFactory.SeparatedList(new[] { type });
				var ofTypeName = SyntaxFactory.GenericName(ofTypeIdentifier, SyntaxFactory.TypeArgumentList(typeParams));
				return SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, target, ofTypeName));
			}

			return null;
		}

		class GatherVisitor : GatherVisitorBase<ReplaceWithOfTypeAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitInvocationExpression (InvocationExpressionSyntax anyInvoke)
			{
				base.VisitInvocationExpression(anyInvoke);

				ExpressionSyntax target;
				TypeSyntax type;
				if (MatchWhereSelect(anyInvoke, out target, out type)) {
					AddDiagnosticAnalyzer(Diagnostic.Create(Rule, anyInvoke.GetLocation()));
				}
			}
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class ReplaceWithOfTypeFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ReplaceWithOfTypeAnalyzer.DiagnosticId;
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
			var diagnostic = diagnostics.First ();
			var node = root.FindNode(context.Span, getInnermostNodeForTie:true) as InvocationExpressionSyntax;
			var newRoot = root.ReplaceNode(node, ReplaceWithOfTypeAnalyzer.MakeOfTypeCall(node));
			context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Replace with call to OfType<T>()", document.WithSyntaxRoot(newRoot)), diagnostic);
		}
	}
}