// 
// RedundantLambdaParameterTypeIssue.cs
//  
// Author:
//       Ji Kun <jikun.nus@gmail.com>
//       Mike Krüger <mkrueger@xamarin.com>
//
//
// Copyright (c) 2013  Ji Kun <jikun.nus@gmail.com>
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
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "RedundantLambdaParameterType")]
	public class RedundantLambdaParameterTypeIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "RedundantLambdaParameterTypeIssue";
		const string Description            = "Explicit type specification can be removed as it can be implicitly inferred";
		const string MessageFormat          = "Redundant lambda explicit type specification";
		const string Category               = IssueCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Redundant lambda explicit type specification");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<RedundantLambdaParameterTypeIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitLambdaExpression(LambdaExpression lambdaExpression)
//			{
//				base.VisitLambdaExpression(lambdaExpression);
//
//				var arguments = lambdaExpression.Parameters.ToList();
//				if (arguments.Any(f => f.Type.IsNull))
//					return;
//				if (!LambdaTypeCanBeInferred(ctx, lambdaExpression, arguments))
//					return;
//
//				foreach (var argument in arguments) {
//					AddIssue(new CodeIssue(
//						argument.Type,
//						ctx.TranslateString(""), 
//						ctx.TranslateString(""),
//						script => {
//							if (arguments.Count == 1) {
//								if (argument.NextSibling.ToString().Equals(")") && argument.PrevSibling.ToString().Equals("(")) {
//									script.Remove(argument.NextSibling);
//									script.Remove(argument.PrevSibling);
//								}
//							}
//							foreach (var arg in arguments)
//								script.Replace(arg, new ParameterDeclaration(arg.Name));
//						}) { IssueMarker = IssueMarker.GrayOut });
//				}
//			}
		}

//		public static bool LambdaTypeCanBeInferred(BaseSemanticModel ctx, Expression expression, List<ParameterDeclaration> parameters)
//		{
//			var validTypes = TypeGuessing.GetValidTypes(ctx.Resolver, expression).ToList();
//			foreach (var type in validTypes) {
//				if (type.Kind != TypeKind.Delegate)
//					continue;
//				var invokeMethod = type.GetDelegateInvokeMethod();
//				if (invokeMethod == null || invokeMethod.Parameters.Count != parameters.Count)
//					continue;
//				for (int p = 0; p < invokeMethod.Parameters.Count; p++) {
//					var resolvedArgument = ctx.Resolve(parameters[p].Type);
//					if (!invokeMethod.Parameters [p].Type.Equals(resolvedArgument.Type))
//						return false;
//				}
//			}
//			return true;
//		}
	}

	[ExportCodeFixProvider(RedundantLambdaParameterTypeIssue.DiagnosticId, LanguageNames.CSharp)]
	public class RedundantLambdaParameterTypeFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return RedundantLambdaParameterTypeIssue.DiagnosticId;
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
				//if (!node.IsKind(SyntaxKind.BaseList))
				//	continue;
				var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Remove parameter type specification", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}