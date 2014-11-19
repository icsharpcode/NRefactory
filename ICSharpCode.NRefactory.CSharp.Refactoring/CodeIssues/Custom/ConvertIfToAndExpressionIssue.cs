//
// ConvertIfToAndExpressionIssue.cs
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
	public class ConvertIfToAndExpressionIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "ConvertIfToAndExpressionIssue";
		const string Description            = "Convert 'if' to '&&' expression";
		const string MessageFormat          = "";
		const string Category               = IssueCategories.Opportunities;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "'if' statement can be re-written as '&&' expression");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<ConvertIfToAndExpressionIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}
//
//			static readonly AstNode ifPattern = 
//				new IfElseStatement(
//					new AnyNode ("condition"),
//					PatternHelper.EmbeddedStatement (
//						new AssignmentExpression(
//							new AnyNode("target"),
//							new PrimitiveExpression (false)
//						)
//					)
//				);
//
//			static readonly AstNode varDelarationPattern = 
//				new VariableDeclarationStatement(new AnyNode("type"), Pattern.AnyString, new AnyNode("initializer"));
//
//			void AddTo(IfElseStatement ifElseStatement, VariableDeclarationStatement varDeclaration, Expression expr)
//			{
//			}
//
//			public override void VisitIfElseStatement(IfElseStatement ifElseStatement)
//			{
//				base.VisitIfElseStatement(ifElseStatement);
//
//				var match = ifPattern.Match(ifElseStatement);
//				if (match.Success) {
//					var varDeclaration = ifElseStatement.GetPrevSibling(s => s.Role == BlockStatement.StatementRole) as VariableDeclarationStatement;
//					var target = match.Get<Expression>("target").Single();
//					var match2 = varDelarationPattern.Match(varDeclaration);
//					if (match2.Success) {
//						var initializer = varDeclaration.Variables.FirstOrDefault();
//						if (initializer != null && target is IdentifierExpression && ((IdentifierExpression)target).Identifier != initializer.Name)
//							return;
//						var expr = match.Get<Expression>("condition").Single();
//						if (!ConvertIfToOrExpressionIssue.CheckTarget(target, expr))
//							return;
//						AddIssue(new CodeIssue(
//							ifElseStatement.IfToken,
//							ctx.TranslateString("Convert to '&&' expresssion"),
//							ctx.TranslateString("Replace with '&&'"),
//							script => {
//								var variable = initializer;
//								var initalizerExpression = variable.Initializer.Clone();
//								var bOp = initalizerExpression as BinaryOperatorExpression;
//								if (bOp != null && bOp.Operator == BinaryOperatorType.ConditionalOr)
//									initalizerExpression = new ParenthesizedExpression (initalizerExpression);
//								script.Replace(
//									varDeclaration, 
//									new VariableDeclarationStatement(
//										varDeclaration.Type.Clone(),
//										variable.Name,
//										new BinaryOperatorExpression(initalizerExpression, BinaryOperatorType.ConditionalAnd, CSharpUtil.InvertCondition(expr)) 
//									)
//								);
//							script.Remove(ifElseStatement); 
//						}
//						) {
//							IssueMarker = IssueMarker.DottedLine
//						});
//						return;
//					} else {
//						var expr = match.Get<Expression>("condition").Single();
//						if (!ConvertIfToOrExpressionIssue.CheckTarget(target, expr))
//							return;
//						AddIssue(new CodeIssue(
//							ifElseStatement.IfToken,
//							ctx.TranslateString("Convert to '&=' expresssion"),
//							ctx.TranslateString("Replace with '&='"),
//							script =>
//								script.Replace(
//									ifElseStatement, 
//									new ExpressionStatement(
//										new AssignmentExpression(
//											target.Clone(),
//											AssignmentOperatorType.BitwiseAnd,
//											CSharpUtil.InvertCondition(expr)
//										) 
//									)
//								)
//						) { IssueMarker = IssueMarker.DottedLine });
//					}
//				}
//			}
		}
	}

	[ExportCodeFixProvider(ConvertIfToAndExpressionIssue.DiagnosticId, LanguageNames.CSharp)]
	public class ConvertIfToAndExpressionFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ConvertIfToAndExpressionIssue.DiagnosticId;
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