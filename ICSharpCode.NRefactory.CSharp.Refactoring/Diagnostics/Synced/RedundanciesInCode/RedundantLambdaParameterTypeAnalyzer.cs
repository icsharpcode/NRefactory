// 
// RedundantLambdaParameterTypeAnalyzer.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class RedundantLambdaParameterTypeAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.RedundantLambdaParameterTypeAnalyzerID, 
			GettextCatalog.GetString("Explicit type specification can be removed as it can be implicitly inferred"),
			GettextCatalog.GetString("Redundant lambda explicit type specification"), 
			DiagnosticAnalyzerCategories.RedundanciesInCode, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.RedundantLambdaParameterTypeAnalyzerID),
			customTags: DiagnosticCustomTags.Unnecessary
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			//context.RegisterSyntaxNodeAction(
			//	(nodeContext) => {
			//		Diagnostic diagnostic;
			//		if (TryGetDiagnostic (nodeContext, out diagnostic)) {
			//			nodeContext.ReportDiagnostic(diagnostic);
			//		}
			//	}, 
			//	new SyntaxKind[] { SyntaxKind.None }
			//);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			//var node = nodeContext.Node as ;
			//diagnostic = Diagnostic.Create (descriptor, node.GetLocation ());
			//return true;
			return false;
		}

//		class GatherVisitor : GatherVisitorBase<RedundantLambdaParameterTypeAnalyzer>
//		{
//			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
//				: base (semanticModel, addDiagnostic, cancellationToken)
//			{
//			}

////			public override void VisitLambdaExpression(LambdaExpression lambdaExpression)
////			{
////				base.VisitLambdaExpression(lambdaExpression);
////
////				var arguments = lambdaExpression.Parameters.ToList();
////				if (arguments.Any(f => f.Type.IsNull))
////					return;
////				if (!LambdaTypeCanBeInferred(ctx, lambdaExpression, arguments))
////					return;
////
////				foreach (var argument in arguments) {
////					AddDiagnosticAnalyzer(new CodeIssue(
////						argument.Type,
////						ctx.TranslateString(""), 
////						ctx.TranslateString(""),
////						script => {
////							if (arguments.Count == 1) {
////								if (argument.NextSibling.ToString().Equals(")") && argument.PrevSibling.ToString().Equals("(")) {
////									script.Remove(argument.NextSibling);
////									script.Remove(argument.PrevSibling);
////								}
////							}
////							foreach (var arg in arguments)
////								script.Replace(arg, new ParameterDeclaration(arg.Name));
////						}) { IssueMarker = IssueMarker.GrayOut });
////				}
////			}
//		}

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
}