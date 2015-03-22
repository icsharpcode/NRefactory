//
// RedundantToStringCallAnalyzer.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "RedundantToStringCall")]
	public class RedundantToStringCallAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "RedundantToStringCallAnalyzer.;
		const string Description            = "Finds calls to ToString() which would be generated automatically by the compiler.";
		const string MessageFormat          = "Redundant 'ToString()' call";
		const string Category               = DiagnosticAnalyzerCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Redundant 'object.ToString()' call");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<RedundantToStringCallAnalyzer>
		{
//			static Tuple<int, int> onlyFirst = Tuple.Create (0, 0);
//
//			static IDictionary<Tuple<string, int>, Tuple<int, int>> membersCallingToString = new Dictionary<Tuple<string, int>, Tuple<int, int>> {
//				{ Tuple.Create("System.IO.TextWriter.Write", 1), onlyFirst },
//				{ Tuple.Create("System.IO.TextWriter.WriteLine", 1), onlyFirst },
//				{ Tuple.Create("System.Console.Write", 1), onlyFirst },
//				{ Tuple.Create("System.Console.WriteLine", 1), onlyFirst }
//			};
						
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
				//binOpVisitor = new BinaryExpressionVisitor (this);
			}
			
//			HashSet<AstNode> processedNodes = new HashSet<AstNode>();
//
//			void CheckExpressionInAutoCallContext(Expression expression)
//			{
//				if (expression is InvocationExpression && !processedNodes.Contains(expression)) {
//					CheckInvocationInAutoCallContext((InvocationExpression)expression);
//				}
//			}
//
//			void CheckInvocationInAutoCallContext(InvocationExpression invocationExpression)
//			{
//				var memberExpression = invocationExpression.Target as MemberReferenceExpression;
//				if (memberExpression == null) {
//					return;
//				}
//				if (memberExpression.MemberName != "ToString" || invocationExpression.Arguments.Any ()) {
//					return;
//				}
//
//				var resolveResult = ctx.Resolve(invocationExpression) as CSharpInvocationResolveResult;
//				if (resolveResult == null) {
//					return;
//				}
//				if (ctx.Resolve(memberExpression.Target).Type.Kind != TypeKind.Struct) 
//					AddRedundantToStringIssue(memberExpression, invocationExpression);
//			}
//			
//			void AddRedundantToStringIssue(MemberReferenceExpression memberExpression, InvocationExpression invocationExpression)
//			{
//				// Simon Lindgren 2012-09-14: Previously there was a check here to see if the node had already been processed
//				// This has been moved out to the callers, to check it earlier for a 30-40% run time reduction
//				processedNodes.Add(invocationExpression);
//				
//				AddDiagnosticAnalyzer(new CodeIssue(memberExpression.DotToken.StartLocation, invocationExpression.RParToken.EndLocation,
//				         ctx.TranslateString(""), 
//				         ctx.TranslateString(""), script =>  {
//					script.Replace(invocationExpression, memberExpression.Target.Clone());
//					}) { IssueMarker = IssueMarker.GrayOut });
//			}
//
//			#region Binary operator
//			public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
//			{
//				base.VisitBinaryOperatorExpression(binaryOperatorExpression);
//
//				if (binaryOperatorExpression.Operator != BinaryOperatorType.Add)
//					return;
//				binOpVisitor.Reset();
//				binaryOperatorExpression.AcceptVisitor(binOpVisitor);
//			}
//
//			BinaryExpressionVisitor binOpVisitor;
//			class BinaryExpressionVisitor : DepthFirstAstVisitor
//			{
//				GatherVisitor issue;
//				int stringExpressionCount;
//				Expression firstStringExpression;
//
//				public BinaryExpressionVisitor(GatherVisitor issue)
//				{
//					this.issue = issue;
//				}
//
//				public void Reset()
//				{
//					stringExpressionCount = 0;
//					firstStringExpression = null;
//				}
//
//				void Check (Expression expression)
//				{
//					if (expression is BinaryOperatorExpression) {
//						expression.AcceptVisitor(this);
//						return;
//					}
//					if (stringExpressionCount <= 1) {
//						var resolveResult = issue.ctx.Resolve(expression);
//						if (resolveResult.Type.IsKnownType(KnownTypeCode.String)) {
//							stringExpressionCount++;
//							if (stringExpressionCount == 1) {
//								firstStringExpression = expression;
//							} else {
//								issue.CheckExpressionInAutoCallContext(firstStringExpression);
//								issue.CheckExpressionInAutoCallContext(expression);
//							}
//						}
//					} else {
//						issue.CheckExpressionInAutoCallContext(expression);
//					}
//				}
//				
//				public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
//				{
//					Check(binaryOperatorExpression.Left);
//					Check(binaryOperatorExpression.Right);
//				}
//			}
//
//			#endregion
//
//			#region Invocation expression
//			public override void VisitInvocationExpression(InvocationExpression invocationExpression)
//			{
//				base.VisitInvocationExpression(invocationExpression);
//
//				//var target = invocationExpression.Target as MemberReferenceExpression;
//
//				var invocationResolveResult = ctx.Resolve(invocationExpression) as CSharpInvocationResolveResult;
//				if (invocationResolveResult == null) {
//					return;
//				}
//				IMember member = invocationResolveResult.Member;
//
//				// "".ToString()
//                CheckTargetedObject(invocationExpression, invocationResolveResult.TargetResult.Type, member);
//
//				// Check list of members that call ToString() automatically
//				CheckAutomaticToStringCallers(invocationExpression, member);
//
//				// Check formatting calls
//				CheckFormattingCall(invocationExpression, invocationResolveResult);
//			}
//
//			void CheckTargetedObject(InvocationExpression invocationExpression, IType type, IMember member)
//			{
//				var memberExpression = invocationExpression.Target as MemberReferenceExpression;
//				if (memberExpression != null && !processedNodes.Contains(invocationExpression)) {
//					if (type.IsKnownType(KnownTypeCode.String) && member.Name == "ToString") {
//						AddRedundantToStringIssue(memberExpression, invocationExpression);
//					}
//				}
//			}
//			
//			void CheckAutomaticToStringCallers(InvocationExpression invocationExpression, IMember member)
//			{
//				if (member.IsOverride) {
//					member = InheritanceHelper.GetBaseMember(member);
//					if (member == null) {
//						return;
//					}
//				}
//				var key = new Tuple<string, int>(member.ReflectionName, invocationExpression.Arguments.Count);
//				Tuple<int, int> checkInfo;
//				if (membersCallingToString.TryGetValue(key, out checkInfo)) {
//					var arguments = invocationExpression.Arguments.ToList();
//					for (int i = checkInfo.Item1; i < Math.Min(invocationExpression.Arguments.Count, checkInfo.Item2 + 1); ++i) {
//						CheckExpressionInAutoCallContext(arguments[i]);
//					}
//				}
//			}
//
//			void CheckFormattingCall(InvocationExpression invocationExpression, CSharpInvocationResolveResult invocationResolveResult)
//			{
//				Expression formatArgument;
//				IList<Expression> formatArguments;
//				// Only check parameters that are of type object: String means it is neccessary, others
//				// means that there is another problem (ie no matching overload of the method).
//				Func<IParameter, Expression, bool> predicate = (parameter, argument) => {
//					var type = parameter.Type;
//					if (type is TypeWithElementType && parameter.IsParams) {
//						type = ((TypeWithElementType)type).ElementType;
//					}
//					var typeDefinition = type.GetDefinition();
//					if (typeDefinition == null)
//						return false;
//					return typeDefinition.IsKnownType(KnownTypeCode.Object);
//				};
//				if (FormatStringHelper.TryGetFormattingParameters(invocationResolveResult, invocationExpression,
//				                                                  out formatArgument, out formatArguments, predicate)) {
//					foreach (var argument in formatArguments) {
//						CheckExpressionInAutoCallContext(argument);
//					}
//				}
//			}
//			#endregion
		}
	}

	[ExportCodeFixProvider(RedundantToStringCallAnalyzer.DiagnosticId, LanguageNames.CSharp)]
	public class RedundantToStringCallFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return RedundantToStringCallAnalyzer.DiagnosticId;
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
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Remove redundant '.ToString()'", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}