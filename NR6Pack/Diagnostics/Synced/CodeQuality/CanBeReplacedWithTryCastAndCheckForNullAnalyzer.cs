//
// CanBeReplacedWithTryCastAndCheckForNullAnalyzer.cs
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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ICSharpCode.NRefactory6.CSharp.CodeRefactorings;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class CanBeReplacedWithTryCastAndCheckForNullAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.CanBeReplacedWithTryCastAndCheckForNullAnalyzerID,
			GettextCatalog.GetString ("Type check and casts can be replaced with 'as' and null check"),
			GettextCatalog.GetString ("Type check and casts can be replaced with 'as' and null check"),
			DiagnosticAnalyzerCategories.CodeQualityIssues,
			DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor (NRefactoryDiagnosticIDs.CanBeReplacedWithTryCastAndCheckForNullAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize (AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction (
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryGetDiagnostic (nodeContext, out diagnostic)) {
						nodeContext.ReportDiagnostic (diagnostic);
					}
				},
				new SyntaxKind [] { SyntaxKind.IfStatement }
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode ())
				return false;
			var node = nodeContext.Node as IfStatementSyntax;
			BinaryExpressionSyntax isExpression;

			if (!CheckIfElse (
				nodeContext.SemanticModel,
				nodeContext.SemanticModel.SyntaxTree.GetRoot (nodeContext.CancellationToken),
				node,
				out isExpression))
				return false;

			diagnostic = Diagnostic.Create (descriptor, isExpression.OperatorToken.GetLocation ());
			return true;
		}


		internal static bool CheckIfElse (SemanticModel ctx, SyntaxNode root, IfStatementSyntax ifElseStatement, out BinaryExpressionSyntax isExpression)
		{
			isExpression = null;
			var embeddedStatment = ifElseStatement.Statement;
			TypeInfo rr;
			ExpressionSyntax castToType;

			List<SyntaxNode> foundCasts;
			var innerCondition = ifElseStatement.Condition.SkipParens ();
			if (innerCondition != null && innerCondition.IsKind (SyntaxKind.LogicalNotExpression)) {

				var c2 = ((PrefixUnaryExpressionSyntax)innerCondition).Operand.SkipParens ();
				if (c2.IsKind (SyntaxKind.IsExpression)) {
					isExpression = c2 as BinaryExpressionSyntax;
					castToType = isExpression.Right;
					rr = ctx.GetTypeInfo (castToType);
					if (rr.Type == null || !rr.Type.IsReferenceType)
						return false;

					SyntaxNode searchStmt = embeddedStatment;
					if (UseAsAndNullCheckCodeRefactoringProvider.IsControlFlowChangingStatement (searchStmt)) {
						searchStmt = ifElseStatement.Parent;
						foundCasts = searchStmt.DescendantNodesAndSelf (n => !UseAsAndNullCheckCodeRefactoringProvider.IsCast (ctx, n, rr.Type)).Where (arg => arg.SpanStart >= ifElseStatement.SpanStart && UseAsAndNullCheckCodeRefactoringProvider.IsCast (ctx, arg, rr.Type)).ToList ();
						foundCasts.AddRange (ifElseStatement.Condition.DescendantNodesAndSelf (n => !UseAsAndNullCheckCodeRefactoringProvider.IsCast (ctx, n, rr.Type)).Where (arg => arg.SpanStart > c2.Span.End && UseAsAndNullCheckCodeRefactoringProvider.IsCast (ctx, arg, rr.Type)));
					} else {
						foundCasts = new List<SyntaxNode> ();
					}
					return foundCasts.Count > 0;

				}
				return false;
			}

			isExpression = innerCondition as BinaryExpressionSyntax;
			if (isExpression == null)
				return false;
			castToType = isExpression.Right;
			rr = ctx.GetTypeInfo (castToType);
			if (rr.Type == null || !rr.Type.IsReferenceType)
				return false;

			foundCasts = embeddedStatment.DescendantNodesAndSelf (n => !UseAsAndNullCheckCodeRefactoringProvider.IsCast (ctx, n, rr.Type)).Where (arg => UseAsAndNullCheckCodeRefactoringProvider.IsCast (ctx, arg, rr.Type)).ToList ();
			foundCasts.AddRange (ifElseStatement.Condition.DescendantNodesAndSelf (n => !UseAsAndNullCheckCodeRefactoringProvider.IsCast (ctx, n, rr.Type)).Where (arg => arg.SpanStart > innerCondition.Span.End && UseAsAndNullCheckCodeRefactoringProvider.IsCast (ctx, arg, rr.Type)));
			return foundCasts.Count > 0;
		}
	}
}