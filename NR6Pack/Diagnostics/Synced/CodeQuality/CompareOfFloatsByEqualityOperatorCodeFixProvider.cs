//
// CompareOfFloatsByEqualityOperatorCodeFixProvider.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class CompareOfFloatsByEqualityOperatorCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds {
			get {
				return ImmutableArray.Create (NRefactoryDiagnosticIDs.CompareOfFloatsByEqualityOperatorAnalyzerID);
			}
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