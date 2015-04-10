//
// PossibleAssignmentToReadonlyFieldAnalyzer.cs
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
	public class PossibleAssignmentToReadonlyFieldAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.PossibleAssignmentToReadonlyFieldAnalyzerID, 
			GettextCatalog.GetString("Check if a readonly field is used as assignment target"),
			"{0}", 
			DiagnosticAnalyzerCategories.CodeQualityIssues, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.PossibleAssignmentToReadonlyFieldAnalyzerID)
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

//		class GatherVisitor : GatherVisitorBase<PossibleAssignmentToReadonlyFieldAnalyzer>
//		{
//			//bool inConstructor;

//			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
//				: base (semanticModel, addDiagnostic, cancellationToken)
//			{
//			}
////
////			public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
////			{
////				inConstructor = true;
////				base.VisitConstructorDeclaration(constructorDeclaration);
////				inConstructor = false;
////			}
////
////			void Check(Expression expr)
////			{
////				var mr = expr as MemberReferenceExpression;
////				if (mr != null) {
////					if (inConstructor && mr.Descendants.Any(d => d.Parent is MemberReferenceExpression && d is ThisReferenceExpression))
////						return;
////					Check(mr.Target);
////				}
////				if (inConstructor && expr is IdentifierExpression)
////					return;
////
////				var rr = ctx.Resolve(expr) as MemberResolveResult;
////
////				if (rr == null || rr.IsError)
////					return;
////				var field = rr.Member as IField;
////
////				if (field == null || !field.IsReadOnly)
////					return;
////
////				if (field.Type.Kind == TypeKind.TypeParameter) {
////					var param = (ITypeParameter)field.Type;
////					if (param.HasReferenceTypeConstraint)
////						return;
////					// TODO: Add resolve actions: Add class constraint + remove readonly modifier
////					AddDiagnosticAnalyzer(new CodeIssue(expr,
////						ctx.TranslateString("Assignment to a property of a readonly field can be useless. Type parameter is not known to be a reference type.")));
////					return;
////				}
////				if (field.Type.Kind == TypeKind.Struct) {
////					// TODO: Add resolve actions: Remove readonly modifier
////					AddDiagnosticAnalyzer(new CodeIssue(expr, ctx.TranslateString("Readonly field can not be used as assignment target.")));
////				}
////			}
////
////			public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
////			{
////				base.VisitAssignmentExpression (assignmentExpression);
////				Check(assignmentExpression.Left);
////			}
////
////			public override void VisitDirectionExpression(DirectionExpression directionExpression)
////			{
////				base.VisitDirectionExpression (directionExpression);
////				Check(directionExpression.Expression);
////			}
////
////			public override void VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression)
////			{
////				base.VisitUnaryOperatorExpression (unaryOperatorExpression);
////				if (unaryOperatorExpression.Operator == UnaryOperatorType.Increment || unaryOperatorExpression.Operator == UnaryOperatorType.Decrement ||
////					unaryOperatorExpression.Operator == UnaryOperatorType.PostIncrement || unaryOperatorExpression.Operator == UnaryOperatorType.PostDecrement) {
////					Check(unaryOperatorExpression.Expression);
////				}
////			}
//		}
	}
}