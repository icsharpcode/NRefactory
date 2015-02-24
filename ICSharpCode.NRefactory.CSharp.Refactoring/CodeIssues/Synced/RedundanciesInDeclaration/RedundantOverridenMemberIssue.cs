// 
// RedundantOverridenMemberIssue.cs
// 
// Author:
//      Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013 Ji Kun <jikun.nus@gmail.com>
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "RedundantOverridenMember")]
	public class RedundantOverridenMemberIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "RedundantOverridenMemberIssue";
		const string Description            = "The override of a virtual member is redundant because it consists of only a call to the base";
		const string MessageFormat          = "Redundant method override";
		const string Category               = IssueCategories.RedundanciesInDeclarations;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Redundant member override");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<RedundantOverridenMemberIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}
//
//			void AddIssue(MethodDeclaration methodDeclaration)
//			{
//				var title = ctx.TranslateString("");
//				AddIssue(new CodeIssue(methodDeclaration, title, ctx.TranslateString(""), script => script.Remove(methodDeclaration)) {
//					IssueMarker = IssueMarker.GrayOut
//				});
//			}
//			
//			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
//			{
//				base.VisitMethodDeclaration(methodDeclaration);
//				
//				if (!methodDeclaration.HasModifier(Modifiers.Override))
//					return;
//				
//				if (methodDeclaration.Body.Statements.Count != 1)
//					return;
//				
//				var expr = methodDeclaration.Body.Statements.FirstOrNullObject();
//				//Debuger.WriteInFile(expr.FirstChild.ToString());
//				if (expr == null)
//					return;
//
//				var returnStatement = expr as ReturnStatement;
//				if (returnStatement != null) {
//					var invocationExpression = returnStatement.Expression as InvocationExpression;
//					if (invocationExpression == null)
//						return;
//					var memberReferenceExpression = invocationExpression.Target as MemberReferenceExpression;
//					if (memberReferenceExpression == null ||
//					    memberReferenceExpression.MemberName != methodDeclaration.Name ||
//					    !(memberReferenceExpression.FirstChild is BaseReferenceExpression))
//						return;
//					if (methodDeclaration.Name == "GetHashCode" && !methodDeclaration.Parameters.Any()) {
//						var rr = ctx.Resolve(methodDeclaration) as MemberResolveResult;
//						if (rr != null && rr.Member.ReturnType.IsKnownType(KnownTypeCode.Int32)) {
//							if (rr.Member.DeclaringType.GetMethods(m => m.Name == "Equals" && m.IsOverride, GetMemberOptions.IgnoreInheritedMembers).Any())
//								return;
//						}
//					}
//
//					AddIssue(methodDeclaration);
//				}
//				var stmtExpr = expr as ExpressionStatement;
//				if (stmtExpr == null)
//					return;
//				var invocation = stmtExpr.Expression as InvocationExpression;
//				if (invocation != null) {
//					var memberReferenceExpression = invocation.Target as MemberReferenceExpression;
//					if (memberReferenceExpression == null ||
//					    memberReferenceExpression.MemberName != methodDeclaration.Name ||
//					    !(memberReferenceExpression.FirstChild is BaseReferenceExpression))
//						return;
//					AddIssue(methodDeclaration);
//				}
//			}
//
//			static readonly AstNode setterPattern = new ExpressionStatement(
//				new AssignmentExpression (new AnyNode ("left"), new IdentifierExpression("value"))
//			);
//			
//			public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
//			{
//				base.VisitPropertyDeclaration(propertyDeclaration);
//				
//				if (!propertyDeclaration.HasModifier(Modifiers.Override))
//					return;
//				
//				bool hasGetter = !propertyDeclaration.Getter.IsNull;
//				bool hasSetter = !propertyDeclaration.Setter.IsNull;
//				if (!hasGetter && !hasSetter)
//					return;
//				
//				if (hasGetter && propertyDeclaration.Getter.Body.Statements.Count != 1)
//					return;
//				
//				if (hasSetter && propertyDeclaration.Setter.Body.Statements.Count != 1)
//					return;
//				
//				var resultProperty = ctx.Resolve(propertyDeclaration) as MemberResolveResult;
//				if (resultProperty == null)
//					return;
//				var baseProperty = InheritanceHelper.GetBaseMember(resultProperty.Member) as IProperty;
//				if (baseProperty == null)
//					return;
//				
//				bool hasBaseGetter = baseProperty.Getter != null;
//				bool hasBaseSetter = baseProperty.Setter != null;
//				
//				if (hasBaseGetter) {
//					if (hasGetter) {
//						var expr = propertyDeclaration.Getter.Body.Statements.FirstOrNullObject();
//					
//						if (expr == null || !(expr is ReturnStatement))
//							return;
//					
//						var memberReferenceExpression = (expr as ReturnStatement).Expression as MemberReferenceExpression;
//					
//						if (memberReferenceExpression == null || 
//							memberReferenceExpression.MemberName != propertyDeclaration.Name ||
//							!(memberReferenceExpression.FirstChild is BaseReferenceExpression))
//							return;
//					}
//				}
//				
//				if (hasBaseSetter) {
//					if (hasSetter) {
//						var match = setterPattern.Match(propertyDeclaration.Setter.Body.Statements.FirstOrNullObject());
//						if (!match.Success)
//							return;
//						var memberReferenceExpression = match.Get("left").Single() as MemberReferenceExpression;
//						if (memberReferenceExpression == null || 
//							memberReferenceExpression.MemberName != propertyDeclaration.Name ||
//							!(memberReferenceExpression.FirstChild is BaseReferenceExpression))
//							return;
//					}
//				}
//				
//				var title = ctx.TranslateString("Redundant property override");
//				AddIssue(new CodeIssue(propertyDeclaration, title, ctx.TranslateString("Remove redundant property override"), script => script.Remove(propertyDeclaration)) { IssueMarker = IssueMarker.GrayOut });
//			}
//			
//			public override void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
//			{
//				base.VisitIndexerDeclaration(indexerDeclaration);
//				
//				if (!indexerDeclaration.HasModifier(Modifiers.Override))
//					return;
//				
//				bool hasGetter = !indexerDeclaration.Getter.IsNull;
//				bool hasSetter = !indexerDeclaration.Setter.IsNull;
//				if (!hasGetter && !hasSetter)
//					return;
//				
//				if (hasGetter && indexerDeclaration.Getter.Body.Statements.Count != 1)
//					return;
//				
//				if (hasSetter && indexerDeclaration.Setter.Body.Statements.Count != 1)
//					return;
//				
//				var resultIndexer = ctx.Resolve(indexerDeclaration) as MemberResolveResult;
//				if (resultIndexer == null)
//					return;
//				var baseIndexer = InheritanceHelper.GetBaseMember(resultIndexer.Member) as IProperty;
//				if (baseIndexer == null)
//					return;
//
//				bool hasBaseGetter = (baseIndexer.Getter != null);
//				bool hasBaseSetter = (baseIndexer.Setter != null);
//				
//				if (hasBaseGetter) {
//					if (hasGetter) {
//					
//						var expr = indexerDeclaration.Getter.Body.Statements.FirstOrNullObject() as ReturnStatement;
//					
//						if (expr == null)
//							return;
//					
//						Expression indexerExpression = expr.Expression;
//					
//						if (indexerExpression == null || 
//							!(indexerExpression.FirstChild is BaseReferenceExpression))
//							return;
//					}
//				}
//				
//				if (hasBaseSetter) {
//					if (hasSetter) {
//						var match = setterPattern.Match(indexerDeclaration.Setter.Body.Statements.FirstOrNullObject());
//						if (!match.Success)
//							return;
//						var memberReferenceExpression = match.Get("left").Single() as IndexerExpression;
//						if (memberReferenceExpression == null || 
//							!(memberReferenceExpression.FirstChild is BaseReferenceExpression))
//							return;
//					}
//				}
//				
//				var title = ctx.TranslateString("Redundant indexer override");
//				AddIssue(new CodeIssue(indexerDeclaration, title, ctx.TranslateString("Remove redundant indexer override"), script => script.Remove(indexerDeclaration)) { IssueMarker = IssueMarker.GrayOut });
//			}
//
//			static readonly AstNode customEventPattern =
//				new CustomEventDeclaration {
//					Modifiers = Modifiers.Any,
//					Name = Pattern.AnyString,
//					ReturnType = new AnyNode(), 
//					AddAccessor = new Accessor {
//						Body = new BlockStatement {
//							new AssignmentExpression {
//								Left = new NamedNode ("baseRef", new MemberReferenceExpression(new BaseReferenceExpression(), Pattern.AnyString)),
//								Operator = AssignmentOperatorType.Add,
//								Right = new IdentifierExpression("value")
//							}
//						}
//					},
//					RemoveAccessor = new Accessor {
//						Body = new BlockStatement {
//							new AssignmentExpression {
//								Left = new Backreference("baseRef"),
//								Operator = AssignmentOperatorType.Subtract,
//								Right = new IdentifierExpression("value")
//							}
//						}
//					},
//				};
//			
//			public override void VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration)
//			{
//				var m = customEventPattern.Match(eventDeclaration);
//				if (!m.Success)
//					return;
//				var baseRef = m.Get<MemberReferenceExpression>("baseRef").First();
//				if (baseRef == null || baseRef.MemberName != eventDeclaration.Name)
//					return;
//
//				var title = ctx.TranslateString("Redundant event override");
//				AddIssue(new CodeIssue(eventDeclaration, title, ctx.TranslateString("Remove event override"), script => script.Remove(eventDeclaration)) { IssueMarker = IssueMarker.GrayOut });
//			}
		}
	}

	[ExportCodeFixProvider(RedundantOverridenMemberIssue.DiagnosticId, LanguageNames.CSharp)]
	public class RedundantOverridenMemberFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return RedundantOverridenMemberIssue.DiagnosticId;
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
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Remove redundant method override", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}
