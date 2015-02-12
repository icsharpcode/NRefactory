//
// ForCanBeConvertedToForeachIssue.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ForCanBeConvertedToForeach")]
	public class ForCanBeConvertedToForeachIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "ForCanBeConvertedToForeachIssue";
		const string Description            = "Foreach loops are more efficient";
		const string MessageFormat          = "'for' loop can be converted to 'foreach'";
		const string Category               = IssueCategories.Opportunities;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "'for' can be converted into 'foreach'");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<ForCanBeConvertedToForeachIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}
//
//			static readonly AstNode forPattern =
//				new Choice {
//					new ForStatement {
//						Initializers = {
//							new VariableDeclarationStatement {
//								Type = new AnyNode("int"),
//								Variables = {
//									new NamedNode("iteratorInitialzer", new VariableInitializer(Pattern.AnyString, new PrimitiveExpression(0)))
//								}
//							}
//						},
//						Condition = PatternHelper.OptionalParentheses(
//							new BinaryOperatorExpression(
//								PatternHelper.OptionalParentheses(new AnyNode("iterator")), 
//								BinaryOperatorType.LessThan,
//								PatternHelper.OptionalParentheses(
//									new NamedNode("upperBound", new MemberReferenceExpression(new AnyNode (), Pattern.AnyString))
//								)
//							)
//						),
//						Iterators = {
//							new ExpressionStatement(
//								new Choice {
//									new UnaryOperatorExpression(UnaryOperatorType.Increment, new Backreference("iterator")), 
//									new UnaryOperatorExpression(UnaryOperatorType.PostIncrement, new Backreference("iterator")) 
//								}
//							)
//						},
//						EmbeddedStatement = new AnyNode("body")
//					},
//					new ForStatement {
//						Initializers = {
//							new VariableDeclarationStatement {
//								Type = new AnyNode("int"),
//								Variables = {
//									new NamedNode("iteratorInitialzer", new VariableInitializer(Pattern.AnyString, new PrimitiveExpression(0))),
//									new NamedNode("upperBoundInitializer", new VariableInitializer(Pattern.AnyString, new NamedNode("upperBound", new MemberReferenceExpression(new AnyNode (), Pattern.AnyString)))),
//								}
//							}
//						},
//						Condition = PatternHelper.OptionalParentheses(
//							new BinaryOperatorExpression(
//								PatternHelper.OptionalParentheses(new AnyNode("iterator")), 
//								BinaryOperatorType.LessThan,
//								PatternHelper.OptionalParentheses(
//									new AnyNode("upperBoundInitializerName")
//								)
//							)
//						),
//						Iterators = {
//							new ExpressionStatement(
//								new Choice {
//									new UnaryOperatorExpression(UnaryOperatorType.Increment, new Backreference("iterator")), 
//									new UnaryOperatorExpression(UnaryOperatorType.PostIncrement, new Backreference("iterator")) 
//								}
//							)
//						},
//						EmbeddedStatement = new AnyNode("body")
//					},
//				};
//			static readonly AstNode varDeclPattern =
//				new VariableDeclarationStatement {
//					Type = new AnyNode(),
//					Variables = {
//						new VariableInitializer(Pattern.AnyString, new NamedNode("indexer", new IndexerExpression(new AnyNode(), new IdentifierExpression(Pattern.AnyString))))
//					}
//				};
//			static readonly AstNode varTypePattern =
//				new SimpleType("var");
//
//			static bool IsEnumerable(IType type)
//			{
//				return type.Name == "IEnumerable" && (type.Namespace == "System.Collections.Generic" || type.Namespace == "System.Collections");
//			}
//
//			public override void VisitForStatement(ForStatement forStatement)
//			{
//				base.VisitForStatement(forStatement);
//				var forMatch = forPattern.Match(forStatement);
//				if (!forMatch.Success)
//					return;
//				var body = forStatement.EmbeddedStatement as BlockStatement;
//				if (body == null || !body.Statements.Any())
//					return;
//				var varDeclStmt = body.Statements.First() as VariableDeclarationStatement;
//				if (varDeclStmt == null)
//					return;
//				var varMatch = varDeclPattern.Match(varDeclStmt);
//				if (!varMatch.Success)
//					return;
//				var typeNode = forMatch.Get<AstNode>("int").FirstOrDefault();
//				var varDecl = forMatch.Get<VariableInitializer>("iteratorInitialzer").FirstOrDefault();
//				var iterator = forMatch.Get<IdentifierExpression>("iterator").FirstOrDefault();
//				var upperBound = forMatch.Get<MemberReferenceExpression>("upperBound").FirstOrDefault();
//				if (typeNode == null || varDecl == null || iterator == null || upperBound == null)
//					return;
//
//				// Check iterator type
//				if (!varTypePattern.IsMatch(typeNode)) {
//					var typeRR = ctx.Resolve(typeNode);
//					if (!typeRR.Type.IsKnownType(KnownTypeCode.Int32))
//						return;
//				}
//
//				if (varDecl.Name != iterator.Identifier)
//					return;
//
//				var upperBoundInitializer = forMatch.Get<VariableInitializer>("upperBoundInitializer").FirstOrDefault();
//				var upperBoundInitializerName = forMatch.Get<IdentifierExpression>("upperBoundInitializerName").FirstOrDefault();
//				if (upperBoundInitializer != null) {
//					if (upperBoundInitializerName == null || upperBoundInitializer.Name != upperBoundInitializerName.Identifier)
//						return;
//				}
//
//				var indexer = varMatch.Get<IndexerExpression>("indexer").Single();
//				if (((IdentifierExpression)indexer.Arguments.First()).Identifier != iterator.Identifier)
//					return;
//				if (!indexer.Target.IsMatch(upperBound.Target))
//					return;
//
//				var rr = ctx.Resolve(upperBound) as MemberResolveResult;
//				if (rr == null || rr.IsError)
//					return;
//
//				if (!(rr.Member.Name == "Length" && rr.Member.DeclaringType.Name == "Array" && rr.Member.DeclaringType.Namespace == "System") &&
//				!(rr.Member.Name == "Count" && (IsEnumerable(rr.TargetResult.Type) || rr.TargetResult.Type.GetAllBaseTypes().Any(IsEnumerable))))
//					return;
//
//				var variableInitializer = varDeclStmt.Variables.First();
//				var lr = ctx.Resolve(variableInitializer) as LocalResolveResult;
//				if (lr == null)
//					return;
//
//				var ir = ctx.Resolve(varDecl) as LocalResolveResult;
//				if (ir == null)
//					return;
//
//				var analyze = new ConvertToConstantIssue.VariableUsageAnalyzation(ctx);
//				analyze.SetAnalyzedRange(
//					varDeclStmt,
//					forStatement.EmbeddedStatement,
//					false
//				);
//				forStatement.EmbeddedStatement.AcceptVisitor(analyze);
//				if (analyze.GetStatus(lr.Variable) == ICSharpCode.NRefactory6.CSharp.Refactoring.ExtractMethod.VariableState.Changed ||
//				    analyze.GetStatus(ir.Variable) == ICSharpCode.NRefactory6.CSharp.Refactoring.ExtractMethod.VariableState.Changed ||
//				    analyze.GetStatus(ir.Variable) == ICSharpCode.NRefactory6.CSharp.Refactoring.ExtractMethod.VariableState.Used)
//					return;
//
//				AddIssue(new CodeIssue(
//					forStatement.ForToken,
//					ctx.TranslateString(""),
//					ctx.TranslateString(""),
//					script => {
//						var foreachBody = (BlockStatement)forStatement.EmbeddedStatement.Clone();
//						foreachBody.Statements.First().Remove();
//
//						var fe = new ForeachStatement {
//							VariableType = new PrimitiveType("var"),
//							VariableName = variableInitializer.Name,
//							InExpression = upperBound.Target.Clone(),
//							EmbeddedStatement = foreachBody
//						};
//						script.Replace(forStatement, fe); 
//					}
//				) { IssueMarker = IssueMarker.DottedLine });
//
//			}
		}
	}

	[ExportCodeFixProvider(ForCanBeConvertedToForeachIssue.DiagnosticId, LanguageNames.CSharp)]
	public class ForCanBeConvertedToForeachFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ForCanBeConvertedToForeachIssue.DiagnosticId;
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
				context.RegisterFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Convert to 'foreach'", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}

}

