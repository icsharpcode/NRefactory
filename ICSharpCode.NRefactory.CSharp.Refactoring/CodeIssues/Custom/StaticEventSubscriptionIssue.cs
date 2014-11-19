//
// StaticEventSubscriptionIssue.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
	[DiagnosticAnalyzer]
	public class StaticEventSubscriptionIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "StaticEventSubscriptionIssue";
		const string Description            = "Checks if static events are removed";
		const string MessageFormat          = "";
		const string Category               = IssueCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Static event removal check");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<StaticEventSubscriptionIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitSyntaxTree(SyntaxTree syntaxTree)
//			{
//				base.VisitSyntaxTree(syntaxTree);
//
//				foreach (var assignedEvent in assignedEvents) {
//					addedEvents.Remove(assignedEvent); 
//				}
//
//				foreach (var hs in removedEvents) {
//					HashSet<Tuple<IMember, AssignmentExpression>> h;
//					if (!addedEvents.TryGetValue(hs.Key, out h))
//						continue;
//					foreach (var evt in hs.Value) {
//						restart:
//						foreach (var m in h) {
//							if (m.Item1 == evt) {
//								h.Remove(m);
//								goto restart;
//							}
//						}
//					}
//				}
//
//				foreach (var added in addedEvents) {
//					foreach (var usage in added.Value) {
//						AddIssue(new CodeIssue(usage.Item2.OperatorToken,
//							ctx.TranslateString("Subscription to static events without unsubscription may cause memory leaks."))); 
//					}
//				}
//			}
//
//			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
//			{
//				if (methodDeclaration.HasModifier(Modifiers.Static))
//					return;
//
//				base.VisitMethodDeclaration(methodDeclaration);
//			}
//
//			public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
//			{
//				if (constructorDeclaration.HasModifier(Modifiers.Static))
//					return;
//				base.VisitConstructorDeclaration(constructorDeclaration);
//			}
//			readonly Dictionary<IMember, HashSet<Tuple<IMember, AssignmentExpression>>> addedEvents = new Dictionary<IMember, HashSet<Tuple<IMember, AssignmentExpression>>>();
//			readonly Dictionary<IMember, HashSet<IMember>> removedEvents = new Dictionary<IMember, HashSet<IMember>>();
//			readonly HashSet<IMember> assignedEvents = new HashSet<IMember> ();
//
//			public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
//			{
//				base.VisitAssignmentExpression(assignmentExpression);
//
//				if (assignmentExpression.Operator == AssignmentOperatorType.Add) {
//					var left = ctx.Resolve(assignmentExpression.Left) as MemberResolveResult;
//					if (left == null || left.Member.SymbolKind != SymbolKind.Event || !left.Member.IsStatic)
//						return;
//
//					if (assignmentExpression.Right is AnonymousMethodExpression || assignmentExpression.Right is LambdaExpression) {
//						AddIssue(new CodeIssue(assignmentExpression.OperatorToken,
//							ctx.TranslateString("Subscription to static events with an anonymous method may cause memory leaks."))); 
//					}
//
//
//					var right = ctx.Resolve(assignmentExpression.Right) as MethodGroupResolveResult;
//					if (right == null || right.Methods.Count() != 1)
//						return;
//					HashSet<Tuple<IMember, AssignmentExpression>> hs;
//					if (!addedEvents.TryGetValue(left.Member, out hs))
//						addedEvents[left.Member] = hs = new HashSet<Tuple<IMember, AssignmentExpression>>();
//					hs.Add(Tuple.Create((IMember)right.Methods.First(), assignmentExpression));
//				} else if (assignmentExpression.Operator == AssignmentOperatorType.Subtract) {
//					var left = ctx.Resolve(assignmentExpression.Left) as MemberResolveResult;
//					if (left == null || left.Member.SymbolKind != SymbolKind.Event || !left.Member.IsStatic)
//						return;
//					var right = ctx.Resolve(assignmentExpression.Right) as MethodGroupResolveResult;
//					if (right == null || right.Methods.Count() != 1)
//						return;
//					HashSet<IMember> hs;
//					if (!removedEvents.TryGetValue(left.Member, out hs))
//						removedEvents[left.Member] = hs = new HashSet<IMember>();
//					hs.Add(right.Methods.First());
//				} else if (assignmentExpression.Operator == AssignmentOperatorType.Assign) {
//					var left = ctx.Resolve(assignmentExpression.Left) as MemberResolveResult;
//					if (left == null || left.Member.SymbolKind != SymbolKind.Event || !left.Member.IsStatic)
//						return;
//					assignedEvents.Add(left.Member); 
//				}
//			}
		}
	}

	[ExportCodeFixProvider(StaticEventSubscriptionIssue.DiagnosticId, LanguageNames.CSharp)]
	public class StaticEventSubscriptionFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return StaticEventSubscriptionIssue.DiagnosticId;
		}

		public override async Task ComputeFixesAsync(CodeFixContext context)
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
				context.RegisterFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, diagnostic.GetMessage(), document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}