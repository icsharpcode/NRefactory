// 
// UnreachableCodeIssue.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	public class UnreachableCodeIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "UnreachableCodeIssue";
		const string Description            = "Code is unreachable";
		const string MessageFormat          = "";
		const string Category               = IssueCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Code is unreachable");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<UnreachableCodeIssue>
		{
			//HashSet<AstNode> unreachableNodes;

			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
				//unreachableNodes = new HashSet<AstNode>();
			}

//			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
//			{
//				StatementIssueCollector.Collect(this, methodDeclaration.Body);
//
//				base.VisitMethodDeclaration(methodDeclaration);
//			}
//
//			public override void VisitLambdaExpression(LambdaExpression lambdaExpression)
//			{
//				var blockStatement = lambdaExpression.Body as BlockStatement;
//				if (blockStatement != null)
//					StatementIssueCollector.Collect(this, blockStatement);
//
//				base.VisitLambdaExpression(lambdaExpression);
//			}
//
//			public override void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
//			{
//				StatementIssueCollector.Collect(this, anonymousMethodExpression.Body);
//
//				base.VisitAnonymousMethodExpression(anonymousMethodExpression);
//			}
//
//			public override void VisitConditionalExpression(ConditionalExpression conditionalExpression)
//			{
//				var resolveResult = ctx.Resolve(conditionalExpression.Condition);
//				if (resolveResult.ConstantValue is bool) {
//					var condition = (bool)resolveResult.ConstantValue;
//					Expression resultExpr, unreachableExpr;
//					if (condition) {
//						resultExpr = conditionalExpression.TrueExpression;
//						unreachableExpr = conditionalExpression.FalseExpression;
//					} else {
//						resultExpr = conditionalExpression.FalseExpression;
//						unreachableExpr = conditionalExpression.TrueExpression;
//					}
//					unreachableNodes.Add(unreachableExpr);
//
//					AddIssue(new CodeIssue(unreachableExpr, ctx.TranslateString("Code is unreachable"), ctx.TranslateString("Remove unreachable code"),
//						script => script.Replace(conditionalExpression, resultExpr.Clone())) { IssueMarker = IssueMarker.GrayOut });
//				}
//				base.VisitConditionalExpression(conditionalExpression);
//			}
//
//			protected override void VisitChildren(AstNode node)
//			{
//				// skip unreachable nodes
//				if (!unreachableNodes.Contains(node))
//					base.VisitChildren(node);
//			}
//
//			class StatementIssueCollector : DepthFirstAstVisitor
//			{
//				GatherVisitor visitor;
//				ReachabilityAnalysis reachability;
//				List<AstNode> collectedStatements;
//
//				private StatementIssueCollector(GatherVisitor visitor, ReachabilityAnalysis reachability)
//				{
//					collectedStatements = new List<AstNode>();
//					this.visitor = visitor;
//					this.reachability = reachability;
//				}
//
//				public static void Collect(GatherVisitor visitor, BlockStatement body)
//				{
//					if (body.IsNull)
//						return;
//					var reachability = visitor.ctx.CreateReachabilityAnalysis(body);
//
//					var collector = new StatementIssueCollector(visitor, reachability);
//					collector.VisitBlockStatement(body);
//				}
//
//				protected override void VisitChildren(AstNode node)
//				{
//					var statement = node as Statement;
//					if (statement == null || !AddStatement(statement))
//						base.VisitChildren(node);
//				}
//
//				bool AddStatement(Statement statement)
//				{
//					if (reachability.IsReachable(statement))
//						return false;
//					if (collectedStatements.Contains(statement))
//						return true;
//
//					if (statement is BlockStatement && statement.GetChildrenByRole<Statement>(BlockStatement.StatementRole).Any(reachability.IsReachable)) {
//						//There's reachable content
//						return false;
//					}
//
//
//					var prevEnd = statement.GetPrevNode(n => !(n is NewLineNode)).EndLocation;
//					TextLocation start;
//					TextLocation end;
//
//					if (statement.Role == IfElseStatement.TrueRole) {
//						var ife = (IfElseStatement)statement.Parent;
//						start = ife.IfToken.StartLocation;
//						collectedStatements.Add(ife.IfToken);
//						prevEnd = ife.IfToken.StartLocation;
//						collectedStatements.Add(statement);
//						visitor.unreachableNodes.Add(statement);
//						collectedStatements.Add(ife.ElseToken);
//						end = ife.ElseToken.GetNextNode(n => !(n is NewLineNode)).StartLocation;
//					} else if (statement.Role == IfElseStatement.FalseRole) {
//						var ife = (IfElseStatement)statement.Parent;
//						start = ife.ElseToken.StartLocation;
//						collectedStatements.Add(ife.ElseToken);
//						prevEnd = ife.ElseToken.GetPrevNode(n => !(n is NewLineNode)).EndLocation;
//						collectedStatements.Add(statement);
//						visitor.unreachableNodes.Add(statement);
//						end = statement.EndLocation;
//					} else {
//						// group multiple continuous statements into one issue
//						start = statement.StartLocation;
//						collectedStatements.Add(statement);
//						visitor.unreachableNodes.Add(statement);
//						Statement nextStatement;
//						while ((nextStatement = (Statement)statement.GetNextSibling(s => s is Statement)) != null && !(nextStatement is LabelStatement)) {
//							if (nextStatement.Role == IfElseStatement.FalseRole)
//								break;
//							statement = nextStatement;
//							collectedStatements.Add(statement);
//							visitor.unreachableNodes.Add(statement);
//						}
//						end = statement.EndLocation;
//					}
//
//					var removeAction = new CodeAction(
//						visitor.ctx.TranslateString("Remove unreachable code"),
//						script => {
//							var startOffset = script.GetCurrentOffset(prevEnd);
//							var endOffset = script.GetCurrentOffset(end);
//							script.RemoveText(startOffset, endOffset - startOffset);
//						}, collectedStatements.First().StartLocation, collectedStatements.Last().EndLocation);
//					var commentAction = new CodeAction(
//						visitor.ctx.TranslateString("Comment unreachable code"),
//						script => {
//							var startOffset = script.GetCurrentOffset(prevEnd);
//							script.InsertText(startOffset, Environment.NewLine + "/*");
//							var endOffset = script.GetCurrentOffset(end);
//							script.InsertText(endOffset, Environment.NewLine + "*/");
//						}, collectedStatements.First().StartLocation, collectedStatements.Last().EndLocation);
//					var actions = new [] { removeAction, commentAction };
//					visitor.AddIssue(new CodeIssue(start, end, visitor.ctx.TranslateString("Code is unreachable"), actions) { IssueMarker = IssueMarker.GrayOut });
//					return true;
//				}
//				// skip lambda and anonymous method
//				public override void VisitLambdaExpression(LambdaExpression lambdaExpression)
//				{
//				}
//
//				public override void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
//				{
//				}
//			}
		}
	}

	[ExportCodeFixProvider(UnreachableCodeIssue.DiagnosticId, LanguageNames.CSharp)]
	public class UnreachableCodeFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return UnreachableCodeIssue.DiagnosticId;
		}

		public override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
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