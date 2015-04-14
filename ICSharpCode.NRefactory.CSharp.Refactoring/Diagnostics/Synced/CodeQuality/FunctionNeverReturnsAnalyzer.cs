// 
// MethodNeverReturnsAnalyzer.cs
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
	public class FunctionNeverReturnsAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.FunctionNeverReturnsAnalyzerID, 
			GettextCatalog.GetString("Function does not reach its end or a 'return' statement by any of possible execution paths"),
			GettextCatalog.GetString("{0} never reaches its end or a 'return' statement"), 
			DiagnosticAnalyzerCategories.CodeQualityIssues, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.FunctionNeverReturnsAnalyzerID)
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
			if (nodeContext.IsFromGeneratedCode())
				return false;
			//var node = nodeContext.Node as ;
			//diagnostic = Diagnostic.Create (descriptor, node.GetLocation ());
			//return true;
			return false;
		}

//		class GatherVisitor : GatherVisitorBase<FunctionNeverReturnsAnalyzer>
//		{
//			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
//				: base (semanticModel, addDiagnostic, cancellationToken)
//			{
//			}

////			public override void VisitMethodDeclaration (MethodDeclaration methodDeclaration)
////			{
////				var body = methodDeclaration.Body;
////
////				// partial method
////				if (body.IsNull)
////					return;
////
////				var memberResolveResult = ctx.Resolve(methodDeclaration) as MemberResolveResult;
////				VisitBody("Method", methodDeclaration.NameToken, body,
////				          memberResolveResult == null ? null : memberResolveResult.Member, null);
////
////				base.VisitMethodDeclaration (methodDeclaration);
////			}
////
////			public override void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
////			{
////				VisitBody("Delegate", anonymousMethodExpression.DelegateToken,
////				          anonymousMethodExpression.Body, null, null);
////
////				base.VisitAnonymousMethodExpression(anonymousMethodExpression);
////			}
////
////			public override void VisitAccessor(Accessor accessor)
////			{
////				if (accessor.Body.IsNull)
////					return;
////				var parentProperty = accessor.GetParent<PropertyDeclaration>();
////				var resolveResult = ctx.Resolve(parentProperty);
////				var memberResolveResult = resolveResult as MemberResolveResult;
////
////				VisitBody("Accessor", accessor.Keyword, accessor.Body,
////				          memberResolveResult == null ? null : memberResolveResult.Member,
////				          accessor.Keyword.Role);
////
////				base.VisitAccessor (accessor);
////			}
////
////			public override void VisitLambdaExpression(LambdaExpression lambdaExpression)
////			{
////				var body = lambdaExpression.Body as BlockStatement;
////				if (body != null) {
////					VisitBody("Lambda expression", lambdaExpression.ArrowToken, body, null, null);
////				}
////
////				//Even if it is an expression, we still need to check for children
////				//for cases like () => () => { while (true) {}}
////				base.VisitLambdaExpression(lambdaExpression);
////			}
////
////			void VisitBody(string entityType, AstNode node, BlockStatement body, IMember member, Role accessorRole)
////			{
////				var recursiveDetector = new RecursiveDetector(ctx, member, accessorRole);
////				var reachability = ctx.CreateReachabilityAnalysis(body, recursiveDetector);
////				bool hasReachableReturn = false;
////				foreach (var statement in reachability.ReachableStatements) {
////					if (statement is ReturnStatement || statement is ThrowStatement || statement is YieldBreakStatement) {
////						if (!statement.AcceptVisitor(recursiveDetector)) {
////							hasReachableReturn = true;
////							break;
////						}
////					}
////				}
////				if (!hasReachableReturn && !reachability.IsEndpointReachable(body)) {
//			//					AddDiagnosticAnalyzer(new CodeIssue(node, ctx.TranslateString(string.Format("{0} never reaches its end or a 'return' statement.", entityType))));
////				}
////			}
////
////			class RecursiveDetector : ReachabilityAnalysis.RecursiveDetectorVisitor
////			{
////				BaseSemanticModel ctx;
////				IMember member;
////				Role accessorRole;
////
////				internal RecursiveDetector(BaseSemanticModel ctx, IMember member, Role accessorRole) {
////					this.ctx = ctx;
////					this.member = member;
////					this.accessorRole = accessorRole;
////				}
////
////				public override bool VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
////				{
////					switch (binaryOperatorExpression.Operator) {
////						case BinaryOperatorType.ConditionalAnd:
////						case BinaryOperatorType.ConditionalOr:
////							return binaryOperatorExpression.Left.AcceptVisitor(this);
////					}
////					return base.VisitBinaryOperatorExpression(binaryOperatorExpression);
////				}
////
////				public override bool VisitAssignmentExpression(AssignmentExpression assignmentExpression)
////				{
////					if (accessorRole != null) {
////						if (accessorRole == PropertyDeclaration.SetKeywordRole) {
////							return assignmentExpression.Left.AcceptVisitor(this); 
////						}
////						return assignmentExpression.Right.AcceptVisitor(this); 
////					}
////					return base.VisitAssignmentExpression(assignmentExpression);
////				}
////
////				public override bool VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
////				{
////					return false;
////				}
////
////				public override bool VisitLambdaExpression(LambdaExpression lambdaExpression)
////				{
////					return false;
////				}
////
////				public override bool VisitIdentifierExpression(IdentifierExpression identifierExpression)
////				{
////					return CheckRecursion(identifierExpression);
////				}
////
////				public override bool VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
////				{
////					if (base.VisitMemberReferenceExpression(memberReferenceExpression))
////						return true;
////
////					return CheckRecursion(memberReferenceExpression);
////				}
////
////				public override bool VisitInvocationExpression(InvocationExpression invocationExpression)
////				{
////					if (base.VisitInvocationExpression(invocationExpression))
////						return true;
////
////					return CheckRecursion(invocationExpression);
////				}
////
////				bool CheckRecursion(AstNode node) {
////					if (member == null) {
////						return false;
////					}
////
////					var resolveResult = ctx.Resolve(node);
////
////					//We'll ignore Method groups here
////					//If the invocation expressions will be dealt with later anyway
////					//and properties are never in "method groups".
////					var memberResolveResult = resolveResult as MemberResolveResult;
////					if (memberResolveResult == null || memberResolveResult.Member != this.member) {
////						return false;
////					}
////
////					//Now check for virtuals
////					if (memberResolveResult.Member.IsVirtual && !memberResolveResult.Member.DeclaringTypeDefinition.IsSealed) {
////						return false;
////					}
////
////					var parentAssignment = node.Parent as AssignmentExpression;
////					if (parentAssignment != null) {
////						if (accessorRole == CustomEventDeclaration.AddKeywordRole) {
////							return parentAssignment.Operator == AssignmentOperatorType.Add;
////						}
////						if (accessorRole == CustomEventDeclaration.RemoveKeywordRole) {
////							return parentAssignment.Operator == AssignmentOperatorType.Subtract;
////						}
////						if (accessorRole == PropertyDeclaration.GetKeywordRole) {
////							return parentAssignment.Operator != AssignmentOperatorType.Assign;
////						}
////
////						return true;
////					}
////
////					var parentUnaryOperation = node.Parent as UnaryOperatorExpression;
////					if (parentUnaryOperation != null) {
////						var operatorType = parentUnaryOperation.Operator;
////						if (operatorType == UnaryOperatorType.Increment ||
////							operatorType == UnaryOperatorType.Decrement ||
////							operatorType == UnaryOperatorType.PostIncrement ||
////							operatorType == UnaryOperatorType.PostDecrement) {
////
////							return true;
////						}
////					}
////
////					return accessorRole == null || accessorRole == PropertyDeclaration.GetKeywordRole;
////				}
////			}
//		}
	}
}