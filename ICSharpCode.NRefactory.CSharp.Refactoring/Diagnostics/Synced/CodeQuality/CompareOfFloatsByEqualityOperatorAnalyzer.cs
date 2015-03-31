// 
// CompareFloatWithEqualityOperatorAnalyzer.cs
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "CompareOfFloatsByEqualityOperator")]
	public class CompareOfFloatsByEqualityOperatorAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "CompareOfFloatsByEqualityOperatorAnalyzer";
		const string Description            = "Comparison of floating point numbers with equality operator";
		const string MessageFormat          = "{0}";
		const string Category               = DiagnosticAnalyzerCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Compare floating point numbers with equality operator");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		internal static bool IsFloatingPointType (ITypeSymbol type)
		{
			if (type == null)
				return false;
			return type.SpecialType == SpecialType.System_Single || type.SpecialType == SpecialType.System_Double;
		}

		internal static bool IsFloatingPoint(SemanticModel semanticModel, SyntaxNode node)
		{
			return IsFloatingPointType (semanticModel.GetTypeInfo (node).Type);
		}

		internal static bool IsConstantInfinity(SemanticModel semanticModel, SyntaxNode node)
		{
			var rr = semanticModel.GetConstantValue (node);
			if (!rr.HasValue)
				return false;

			return rr.Value is double && double.IsInfinity((double)rr.Value) || rr.Value is float && float.IsInfinity((float)rr.Value);
		}

		internal static string GetFloatType(SemanticModel semanticModel, ExpressionSyntax left, ExpressionSyntax right)
		{
			var rr2 = semanticModel.GetTypeInfo(left);
			var	rr1 = semanticModel.GetTypeInfo(right);
			if (rr1.Type != null && rr1.Type.SpecialType == SpecialType.System_Single && 
				rr2.Type != null && rr2.Type.SpecialType == SpecialType.System_Single)
				return "float";
			return "double";
		}

		internal static bool IsNaN (SemanticModel semanticModel, SyntaxNode node)
		{
			var rr = semanticModel.GetConstantValue (node);
			if (!rr.HasValue)
				return false;

			return rr.Value is double && double.IsNaN((double)rr.Value) || rr.Value is float && float.IsNaN((float)rr.Value);
		}

		internal static bool IsZero (SyntaxNode node)
		{
			var pe = node as LiteralExpressionSyntax;
			if (pe == null)
				return false;
			var token = pe.Token;
			if (token.Value is char || token.Value is string || token.Value is bool)
				return false;

			if (token.Value is double && (double)token.Value == 0d || 
				token.Value is float && (float)token.Value == 0f || 
				token.Value is decimal && (decimal)token.Value == 0m)
				return true;

			foreach (char c in token.ValueText) {
				if (char.IsDigit (c) && c != '0')
					return false;
			}

			return true;
		}

		internal static bool IsNegativeInfinity (SemanticModel semanticModel, SyntaxNode node)
		{
			var rr = semanticModel.GetConstantValue (node);
			if (!rr.HasValue)
				return false;

			return rr.Value is double && double.IsNegativeInfinity((double)rr.Value) || rr.Value is float && float.IsNegativeInfinity((float)rr.Value);
		}

		internal static bool IsPositiveInfinity (SemanticModel semanticModel, SyntaxNode node)
		{
			var rr = semanticModel.GetConstantValue (node);
			if (!rr.HasValue)
				return false;

			return rr.Value is double && double.IsPositiveInfinity ((double)rr.Value) || rr.Value is float && float.IsPositiveInfinity((float)rr.Value);
		}

		class GatherVisitor : GatherVisitorBase<CompareOfFloatsByEqualityOperatorAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitBinaryExpression(BinaryExpressionSyntax node)
			{
				base.VisitBinaryExpression(node);
				if (!node.IsKind(SyntaxKind.EqualsExpression) && !node.IsKind(SyntaxKind.NotEqualsExpression))
					return;

				string message = null, tag = null;

				if (IsNaN(semanticModel, node.Left)) {
					message = node.IsKind(SyntaxKind.EqualsExpression) ? 
						"NaN doesn't equal to any floating point number including to itself. Use 'IsNaN' instead." :
						"NaN doesn't equal to any floating point number including to itself. Use '!IsNaN' instead.";
					tag = "1";
				} else if (IsNaN (semanticModel, node.Right)) {
					message = node.IsKind(SyntaxKind.EqualsExpression) ? 
						"NaN doesn't equal to any floating point number including to itself. Use 'IsNaN' instead." :
						"NaN doesn't equal to any floating point number including to itself. Use '!IsNaN' instead.";
					tag = "2";
				} else if (IsPositiveInfinity(semanticModel, node.Left)) {
					message = node.IsKind(SyntaxKind.EqualsExpression) ?
						"Comparison of floating point numbers with equality operator. Use 'IsPositiveInfinity' method." :
						"Comparison of floating point numbers with equality operator. Use '!IsPositiveInfinity' method.";
					tag = "3";
				} else if (IsPositiveInfinity (semanticModel, node.Right)) {
					message = node.IsKind(SyntaxKind.EqualsExpression) ?
						"Comparison of floating point numbers with equality operator. Use 'IsPositiveInfinity' method." :
						"Comparison of floating point numbers with equality operator. Use '!IsPositiveInfinity' method.";
					tag = "4";
				} else if (IsNegativeInfinity (semanticModel, node.Left)) {
					message = node.IsKind(SyntaxKind.EqualsExpression) ?
						"Comparison of floating point numbers with equality operator. Use 'IsNegativeInfinity' method." :
						"Comparison of floating point numbers with equality operator. Use '!IsNegativeInfinity' method.";
					tag = "5";
				} else if (IsNegativeInfinity (semanticModel, node.Right)) {
					message = node.IsKind(SyntaxKind.EqualsExpression) ?
						"Comparison of floating point numbers with equality operator. Use 'IsNegativeInfinity' method." :
						"Comparison of floating point numbers with equality operator. Use '!IsNegativeInfinity' method.";
					tag = "6";
				} else if (IsFloatingPoint(semanticModel, node.Left) || IsFloatingPoint(semanticModel, node.Right)) {
					if (IsConstantInfinity(semanticModel, node.Left) || IsConstantInfinity(semanticModel, node.Right))
						return;
					if (CompareOfFloatsByEqualityOperatorAnalyzer.IsZero(node.Left)) {
						message = "Fix floating point number comparing. Compare a difference with epsilon.";
						tag = "7";
					} else if (CompareOfFloatsByEqualityOperatorAnalyzer.IsZero(node.Right)) {
						message = "Fix floating point number comparing. Compare a difference with epsilon.";
						tag = "8";
					} else {
						message = "Comparison of floating point numbers can be unequal due to the differing precision of the two values.";
						tag = "9";
					}
				}

				if (message == null)
					return;

				string floatType = CompareOfFloatsByEqualityOperatorAnalyzer.GetFloatType(semanticModel, node.Left, node.Right);
				AddDiagnosticAnalyzer(Diagnostic.Create(
					Rule.Id,
					Rule.Category,
					message,
					Rule.DefaultSeverity,
					Rule.DefaultSeverity,
					Rule.IsEnabledByDefault,
					4,
					Rule.Title,
					Rule.Description,
					Rule.HelpLinkUri,
					node.GetLocation(),
					null,
					new[] { tag , floatType}
				));
			}
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class CompareOfFloatsByEqualityOperatorFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return CompareOfFloatsByEqualityOperatorAnalyzer.DiagnosticId;
		}

		// Does not make sense here, because the fixes produce code that is not compilable
		//public override FixAllProvider GetFixAllProvider()
		//{
		//	return WellKnownFixAllProviders.BatchFixer;
		//}

		public async override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			var cancellationToken = context.CancellationToken;
			var span = context.Span;
			var diagnostics = context.Diagnostics;
			var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
			var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
			var diagnostic = diagnostics.First ();
			var node = root.FindNode(context.Span) as BinaryExpressionSyntax;
			if (node == null)
				return;
			CodeAction action;
			var floatType = diagnostic.Descriptor.CustomTags.ElementAt(1);
			switch (diagnostic.Descriptor.CustomTags.ElementAt(0))
			{
				case "1":
					action = AddIsNaNIssue(document, semanticModel, root, node, node.Right, floatType);
					break;
				case "2":
					action = AddIsNaNIssue(document, semanticModel, root, node, node.Left, floatType);
					break;
				case "3":
					action = AddIsPositiveInfinityIssue(document, semanticModel, root, node, node.Right, floatType);
					break;
				case "4":
					action = AddIsPositiveInfinityIssue(document, semanticModel, root, node, node.Left, floatType);
					break;
				case "5":
					action = AddIsNegativeInfinityIssue(document, semanticModel, root, node, node.Right, floatType);
					break;
				case "6":
					action = AddIsNegativeInfinityIssue(document, semanticModel, root, node, node.Left, floatType);
					break;
				case "7":
					action = AddIsZeroIssue(document, semanticModel, root, node, node.Right, floatType);
					break;
				case "8":
					action = AddIsZeroIssue(document, semanticModel, root, node, node.Left, floatType);
					break;
				default:
					action = AddCompareIssue(document, semanticModel, root, node, floatType);

					break;
			}

			if (action != null)
			{
				context.RegisterCodeFix(action, diagnostic);
			}
		}

		static CodeAction AddIsNaNIssue(Document document, SemanticModel semanticModel, SyntaxNode root, BinaryExpressionSyntax node, ExpressionSyntax argExpr, string floatType)
		{
			return CodeActionFactory.Create(node.Span, DiagnosticSeverity.Warning, string.Format(node.IsKind(SyntaxKind.EqualsExpression) ? "Replace with '{0}.IsNaN(...)' call" : "Replace with '!{0}.IsNaN(...)' call" , floatType), token => {
				SyntaxNode newRoot;
				ExpressionSyntax expr;
				var arguments = new SeparatedSyntaxList<ArgumentSyntax> ();
				arguments = arguments.Add(SyntaxFactory.Argument(argExpr));
				expr = SyntaxFactory.InvocationExpression(
					SyntaxFactory.MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						SyntaxFactory.ParseExpression(floatType),
						SyntaxFactory.IdentifierName("IsNaN")
					),
					SyntaxFactory.ArgumentList(
						arguments
					)
				);
				if (node.IsKind(SyntaxKind.NotEqualsExpression))
					expr = SyntaxFactory.PrefixUnaryExpression (SyntaxKind.LogicalNotExpression, expr);
				expr = expr.WithAdditionalAnnotations(Formatter.Annotation);
				newRoot = root.ReplaceNode((SyntaxNode)node, expr);
				return Task.FromResult(document.WithSyntaxRoot(newRoot));
			});
		}

		static CodeAction AddIsPositiveInfinityIssue(Document document, SemanticModel semanticModel, SyntaxNode root, BinaryExpressionSyntax node, ExpressionSyntax argExpr, string floatType)
		{
			return CodeActionFactory.Create(node.Span, DiagnosticSeverity.Warning, string.Format(node.IsKind(SyntaxKind.EqualsExpression) ? "Replace with '{0}.IsPositiveInfinity(...)' call" : "Replace with '!{0}.IsPositiveInfinity(...)' call" , floatType), token => {
				SyntaxNode newRoot;
				ExpressionSyntax expr;
				var arguments = new SeparatedSyntaxList<ArgumentSyntax> ();
				arguments = arguments.Add(SyntaxFactory.Argument(argExpr));
				expr = SyntaxFactory.InvocationExpression(
					SyntaxFactory.MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						SyntaxFactory.ParseExpression(floatType),
						SyntaxFactory.IdentifierName("IsPositiveInfinity")
					),
					SyntaxFactory.ArgumentList(
						arguments
					)
				);
				if (node.IsKind(SyntaxKind.NotEqualsExpression))
					expr = SyntaxFactory.PrefixUnaryExpression (SyntaxKind.LogicalNotExpression, expr);
				expr = expr.WithAdditionalAnnotations(Formatter.Annotation);
				newRoot = root.ReplaceNode((SyntaxNode)node, expr);
				return Task.FromResult(document.WithSyntaxRoot(newRoot));
			});
		}

		static CodeAction AddIsNegativeInfinityIssue(Document document, SemanticModel semanticModel, SyntaxNode root, BinaryExpressionSyntax node, ExpressionSyntax argExpr, string floatType)
		{
			return CodeActionFactory.Create(node.Span, DiagnosticSeverity.Warning, string.Format(node.IsKind(SyntaxKind.EqualsExpression) ? "Replace with '{0}.IsNegativeInfinity(...)' call" : "Replace with '!{0}.IsNegativeInfinity(...)' call" , floatType), token => {
				SyntaxNode newRoot;
				ExpressionSyntax expr;
				var arguments = new SeparatedSyntaxList<ArgumentSyntax> ();
				arguments = arguments.Add(SyntaxFactory.Argument(argExpr));
				expr = SyntaxFactory.InvocationExpression(
					SyntaxFactory.MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						SyntaxFactory.ParseExpression(floatType),
						SyntaxFactory.IdentifierName("IsNegativeInfinity")
					),
					SyntaxFactory.ArgumentList(
						arguments
					)
				);
				if (node.IsKind(SyntaxKind.NotEqualsExpression))
					expr = SyntaxFactory.PrefixUnaryExpression (SyntaxKind.LogicalNotExpression, expr);
				expr = expr.WithAdditionalAnnotations(Formatter.Annotation);
				newRoot = root.ReplaceNode((SyntaxNode)node, expr);
				return Task.FromResult(document.WithSyntaxRoot(newRoot));
			});
		}

		static CodeAction AddIsZeroIssue(Document document, SemanticModel semanticModel, SyntaxNode root, BinaryExpressionSyntax node, ExpressionSyntax argExpr, string floatType)
		{
			return CodeActionFactory.Create(node.Span, DiagnosticSeverity.Warning, "Fix floating point number comparison", token => {
				SyntaxNode newRoot;
				ExpressionSyntax expr;
				var arguments = new SeparatedSyntaxList<ArgumentSyntax> ();
				arguments = arguments.Add(SyntaxFactory.Argument(argExpr));
				expr = SyntaxFactory.BinaryExpression(
					node.IsKind(SyntaxKind.EqualsExpression) ? SyntaxKind.LessThanExpression : SyntaxKind.GreaterThanExpression,
					SyntaxFactory.InvocationExpression(
						SyntaxFactory.MemberAccessExpression(
							SyntaxKind.SimpleMemberAccessExpression,
							SyntaxFactory.ParseExpression("System.Math"),
							SyntaxFactory.IdentifierName("Abs")
						),
						SyntaxFactory.ArgumentList(
							arguments
						)
					),
					SyntaxFactory.IdentifierName("EPSILON")
				);
				expr = expr.WithAdditionalAnnotations(Formatter.Annotation);
				newRoot = root.ReplaceNode((SyntaxNode)node, expr);
				return Task.FromResult(document.WithSyntaxRoot(newRoot));
			});
		}

		static CodeAction AddCompareIssue(Document document, SemanticModel semanticModel, SyntaxNode root, BinaryExpressionSyntax node, string floatType)
		{
			return CodeActionFactory.Create(node.Span, DiagnosticSeverity.Warning, "Fix floating point number comparison", token => {
				SyntaxNode newRoot;
				ExpressionSyntax expr;
				var arguments = new SeparatedSyntaxList<ArgumentSyntax> ();
				arguments = arguments.Add(SyntaxFactory.Argument(SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression, node.Left, node.Right)));
				expr = SyntaxFactory.BinaryExpression(
						node.IsKind(SyntaxKind.EqualsExpression) ? SyntaxKind.LessThanExpression : SyntaxKind.GreaterThanExpression,
					SyntaxFactory.InvocationExpression(
						SyntaxFactory.MemberAccessExpression(
							SyntaxKind.SimpleMemberAccessExpression,
							SyntaxFactory.ParseExpression("System.Math"),
							SyntaxFactory.IdentifierName("Abs")
						),
						SyntaxFactory.ArgumentList(
							arguments
						)
					),
					SyntaxFactory.IdentifierName("EPSILON")
				);
				expr = expr.WithAdditionalAnnotations(Formatter.Annotation);
				newRoot = root.ReplaceNode((SyntaxNode)node, expr);
				return Task.FromResult(document.WithSyntaxRoot(newRoot));
			});
		}
	}
}