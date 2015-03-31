//
// ConvertToLambdaExpressionAnalyzer.cs
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ConvertToLambdaExpression")]
	public class ConvertToLambdaExpressionAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "ConvertToLambdaExpressionAnalyzer";
		const string Description            = "Convert to lambda with expression";
		const string MessageFormat          = "Can be converted to expression";
		const string Category               = DiagnosticAnalyzerCategories.Opportunities;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "Convert to lambda expression");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}
 
		class GatherVisitor : GatherVisitorBase<ConvertToLambdaExpressionAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}
//
//			public override void VisitLambdaExpression(LambdaExpression lambdaExpression)
//			{
//				base.VisitLambdaExpression(lambdaExpression);
//				BlockStatement block;
//				Expression expr;
//				if (!ConvertLambdaBodyStatementToExpressionAction.TryGetConvertableExpression(lambdaExpression.Body, out block, out expr))
//					return;
//				var node = block.Statements.FirstOrDefault() ?? block;
//				var expressionStatement = node as ExpressionStatement;
//				if (expressionStatement != null) {
//					if (expressionStatement.Expression is AssignmentExpression)
//						return;
//				}
//				var returnTypes = new List<IType>();
//				foreach (var type in TypeGuessing.GetValidTypes(ctx.Resolver, lambdaExpression)) {
//					if (type.Kind != TypeKind.Delegate)
//						continue;
//					var invoke = type.GetDelegateInvokeMethod();
//					if (!returnTypes.Contains(invoke.ReturnType))
//						returnTypes.Add(invoke.ReturnType);
//				}
//				if (returnTypes.Count > 1)
//					return;
//
//				AddDiagnosticAnalyzer(new CodeIssue(
//					node,
//					ctx.TranslateString(""),
//					ConvertLambdaBodyStatementToExpressionAction.CreateAction(ctx, node, block, expr)
//				));
//			}
//
//			public override void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
//			{
//				base.VisitAnonymousMethodExpression(anonymousMethodExpression);
//				if (!anonymousMethodExpression.HasParameterList)
//					return;
//				BlockStatement block;
//				Expression expr;
//				if (!ConvertLambdaBodyStatementToExpressionAction.TryGetConvertableExpression(anonymousMethodExpression.Body, out block, out expr))
//					return;
//				var node = block.Statements.FirstOrDefault() ?? block;
//				var returnTypes = new List<IType>();
//				foreach (var type in TypeGuessing.GetValidTypes(ctx.Resolver, anonymousMethodExpression)) {
//					if (type.Kind != TypeKind.Delegate)
//						continue;
//					var invoke = type.GetDelegateInvokeMethod();
//					if (!returnTypes.Contains(invoke.ReturnType))
//						returnTypes.Add(invoke.ReturnType);
//				}
//				if (returnTypes.Count > 1)
//					return;
//
//				AddDiagnosticAnalyzer(new CodeIssue(
//					node,
//					ctx.TranslateString(""),
//					ctx.TranslateString("Convert to lambda expression"),
//					script => {
//						var lambdaExpression = new LambdaExpression();
//						foreach (var parameter in anonymousMethodExpression.Parameters)
//							lambdaExpression.Parameters.Add(parameter.Clone());
//						lambdaExpression.Body = expr.Clone();
//						script.Replace(anonymousMethodExpression, lambdaExpression);
//					}
//				));
//			}
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class ConvertToLambdaExpressionFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ConvertToLambdaExpressionAnalyzer.DiagnosticId;
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
			var diagnostic = diagnostics.First ();
			var node = root.FindNode(context.Span);
			//if (!node.IsKind(SyntaxKind.BaseList))
			//	continue;
			var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
			context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Convert to expression", document.WithSyntaxRoot(newRoot)), diagnostic);
		}
	}
}