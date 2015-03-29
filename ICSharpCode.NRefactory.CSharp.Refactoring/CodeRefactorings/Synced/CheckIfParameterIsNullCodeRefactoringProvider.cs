// 
// CheckIfParameterIsNull.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[NRefactoryCodeRefactoringProvider(Description = "Checks function parameter is not null")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Check if parameter is null")]
	/// <summary>
	/// Creates a 'if (param == null) throw new System.ArgumentNullException ();' contruct for a parameter.
	/// </summary>
	public class CheckIfParameterIsNullCodeRefactoringProvider : SpecializedCodeRefactoringProvider<ParameterSyntax>
	{
		protected override IEnumerable<CodeAction> GetActions(Document document, SemanticModel semanticModel, SyntaxNode root, TextSpan span, ParameterSyntax node, CancellationToken cancellationToken)
		{
			if (!node.Identifier.Span.Contains(span))
				return Enumerable.Empty<CodeAction>();
			var parameter = node;
			var bodyStatement = parameter.Parent.Parent.ChildNodes().OfType<BlockSyntax>().FirstOrDefault();
			if (bodyStatement == null)
				return Enumerable.Empty<CodeAction>();

			var parameterSymbol = semanticModel.GetDeclaredSymbol(node);
			var type = parameterSymbol.Type;
			if (type == null || type.IsValueType || HasNullCheck(semanticModel, parameterSymbol, bodyStatement)) 
				return Enumerable.Empty<CodeAction>();
			return new [] { CodeActionFactory.Create(
				node.Identifier.Span,
				DiagnosticSeverity.Info,
				GettextCatalog.GetString ("Add null check for parameter"),
				t2 => {
					var paramName = node.Identifier.ToString();
					var ifStatement = SyntaxFactory.IfStatement(
						SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, SyntaxFactory.IdentifierName(paramName), SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)), 
						SyntaxFactory.ThrowStatement(
							SyntaxFactory.ObjectCreationExpression(
								SyntaxFactory.ParseTypeName("System.ArgumentNullException"), 
								SyntaxFactory.ArgumentList(
									SyntaxFactory.SeparatedList(new [] {
										SyntaxFactory.Argument(
											SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(paramName))
										)}
									)
								),
								null
							)
						)
					);

					var newBody = bodyStatement.WithStatements (SyntaxFactory.List<StatementSyntax>(new [] { ifStatement.WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation) }.Concat (bodyStatement.Statements)));
					var newRoot = root.ReplaceNode((SyntaxNode)bodyStatement, newBody);
					return Task.FromResult(document.WithSyntaxRoot(newRoot));
				}
			) };
		}

		static bool HasNullCheck(SemanticModel semanticModel, IParameterSymbol parameterSymbol, BlockSyntax bodyStatement)
		{
			foreach (var ifStmt in bodyStatement.DescendantNodes().OfType<IfStatementSyntax>()) {
				var cond = ifStmt.Condition as BinaryExpressionSyntax;
				if (cond == null || !cond.IsKind(SyntaxKind.EqualsExpression) && !cond.IsKind(SyntaxKind.NotEqualsExpression))
					continue;
				ExpressionSyntax checkParam;
				if (cond.Left.IsKind(SyntaxKind.NullLiteralExpression)) {
					checkParam = cond.Right;
				} else if (cond.Right.IsKind(SyntaxKind.NullLiteralExpression)) {
					checkParam = cond.Left;
				} else {
					continue;
				}
				var stmt = ifStmt.Statement;
				while (stmt is BlockSyntax)
					stmt = ((BlockSyntax)stmt).Statements.FirstOrDefault();
				if (!(stmt is ThrowStatementSyntax))
					continue;

				var param = semanticModel.GetSymbolInfo(checkParam);
				if (param.Symbol == parameterSymbol)
					return true;
			}
			return false;
		}
	}
}
