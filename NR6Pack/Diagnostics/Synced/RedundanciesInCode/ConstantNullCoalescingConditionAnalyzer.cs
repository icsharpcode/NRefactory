//
// RedundantNullCoalescingExpressionAnalyzer.cs
//
// Author:
//       Luís Reis <luiscubal@gmail.com>
//
// Copyright (c) 2013 Luís Reis
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
    [NotPortedYet]
    public class ConstantNullCoalescingConditionAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ConstantNullCoalescingConditionAnalyzerID, 
			GettextCatalog.GetString("Finds redundant null coalescing expressions such as expr ?? expr"),
			"{0}", 
			DiagnosticAnalyzerCategories.PracticesAndImprovements, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.ConstantNullCoalescingConditionAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);


		//static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (DiagnosticId, "Redundant ??. Right side is always null.", "Remove redundant right side", Category, DiagnosticSeverity.Warning, true, "'??' condition is known to be null or not null");
		//static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (DiagnosticId, "Redundant ??. Left side is always null.", "Remove redundant left side", Category, DiagnosticSeverity.Warning, true, "'??' condition is known to be null or not null");
		//static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (DiagnosticId, "Redundant ??. Left side is never null.", "Remove redundant right side", Category, DiagnosticSeverity.Warning, true, "'??' condition is known to be null or not null");


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
			if (nodeContext.IsFromGeneratedCode())
				return false;
			//var node = nodeContext.Node as ;
			//diagnostic = Diagnostic.Create (descriptor, node.GetLocation ());
			//return true;
			return false;
		}

//		class GatherVisitor : GatherVisitorBase<ConstantNullCoalescingConditionAnalyzer>
//		{
//			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
//				: base (semanticModel, addDiagnostic, cancellationToken)
//			{
//			}

////			Dictionary<AstNode, NullValueAnalysis> cachedNullAnalysis = new Dictionary<AstNode, NullValueAnalysis>();
////
////			public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
////			{
////				base.VisitBinaryOperatorExpression(binaryOperatorExpression);
////
////				if (binaryOperatorExpression.Operator != BinaryOperatorType.NullCoalescing) {
////					//The issue is not applicable
////					return;
////				}
////
////				var parentFunction = GetParentFunctionNode(binaryOperatorExpression);
////				var analysis = GetAnalysis(parentFunction);
////
////				NullValueStatus leftStatus = analysis.GetExpressionResult(binaryOperatorExpression.Left);
////				if (leftStatus == NullValueStatus.DefinitelyNotNull) {
////					AddDiagnosticAnalyzer(new CodeIssue(binaryOperatorExpression.OperatorToken.StartLocation,
////					         binaryOperatorExpression.Right.EndLocation,
////					         ctx.TranslateString(""),
////					         ctx.TranslateString(""),
////					         script => {
////
////						script.Replace(binaryOperatorExpression, binaryOperatorExpression.Left.Clone());
////
////						}) { IssueMarker = IssueMarker.GrayOut });
////					return;
////				}
////				if (leftStatus == NullValueStatus.DefinitelyNull) {
////					AddDiagnosticAnalyzer(new CodeIssue(binaryOperatorExpression.Left.StartLocation,
////					         binaryOperatorExpression.OperatorToken.EndLocation,
////					         ctx.TranslateString(""),
////					         ctx.TranslateString(""),
////					         script => {
////
////						script.Replace(binaryOperatorExpression, binaryOperatorExpression.Right.Clone());
////
////						}));
////					return;
////				}
////				NullValueStatus rightStatus = analysis.GetExpressionResult(binaryOperatorExpression.Right);
////				if (rightStatus == NullValueStatus.DefinitelyNull) {
////					AddDiagnosticAnalyzer(new CodeIssue(binaryOperatorExpression.OperatorToken.StartLocation,
////					         binaryOperatorExpression.Right.EndLocation,
////					         ctx.TranslateString(""),
////					         ctx.TranslateString(""),
////					         script => {
////
////						script.Replace(binaryOperatorExpression, binaryOperatorExpression.Left.Clone());
////
////						}));
////					return;
////				}
//			}

//			NullValueAnalysis GetAnalysis(AstNode parentFunction)
//			{
//				NullValueAnalysis analysis;
//				if (cachedNullAnalysis.TryGetValue(parentFunction, out analysis)) {
//					return analysis;
//				}
//
//				analysis = new NullValueAnalysis(ctx, parentFunction.GetChildByRole(Roles.Body), parentFunction.GetChildrenByRole(Roles.Parameter), ctx.CancellationToken);
//				analysis.IsParametersAreUninitialized = true;
//				analysis.Analyze();
//				cachedNullAnalysis [parentFunction] = analysis;
//				return analysis;
//			}
//		}
//
//		public static AstNode GetParentFunctionNode(AstNode node)
//		{
//			do {
//				node = node.Parent;
//			} while (node != null && !IsFunctionNode(node));
//
//			return node;
//		}
//
//		static bool IsFunctionNode(AstNode node)
//		{
//			return node is EntityDeclaration ||
//				node is LambdaExpression ||
//					node is AnonymousMethodExpression;
//		}
	}

	
}