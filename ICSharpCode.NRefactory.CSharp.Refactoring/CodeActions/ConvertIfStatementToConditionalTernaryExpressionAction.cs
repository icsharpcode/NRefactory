//
// ConvertIfStatementToConditionalTernaryExpressionAction.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Convert 'if' to '?:'")]
	[ExportCodeRefactoringProvider("Convert 'if' to '?:'", LanguageNames.CSharp)]
	public class ConvertIfStatementToConditionalTernaryExpressionAction : CodeRefactoringProvider
	{
		public override async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await document.GetSyntaxRootAsync(cancellationToken);

			var node = root.FindNode(span) as IfStatementSyntax;
			if (node == null || node.Else == null || node.Parent is IfStatementSyntax || node.Else.Statement is IfStatementSyntax)
				return Enumerable.Empty<CodeAction>();

			var condition = node.Condition;
			//make sure to check for multiple statements
			ExpressionStatementSyntax whenTrueExprStatement, whenFalseExprStatement;
			if (node.Statement is BlockSyntax) {
				var block = node.Statement as BlockSyntax;
				if (block.Statements.Count > 1)
					return Enumerable.Empty<CodeAction>();
				whenTrueExprStatement = node.Statement.DescendantNodesAndSelf().OfType<ExpressionStatementSyntax>().FirstOrDefault();
			} else {
				whenTrueExprStatement = node.Statement as ExpressionStatementSyntax;
			}

			if (node.Else.Statement is BlockSyntax) {
				var block = node.Else.Statement as BlockSyntax;
				if (block.Statements.Count > 1)
					return Enumerable.Empty<CodeAction>();
				whenFalseExprStatement = node.Else.Statement.DescendantNodesAndSelf().OfType<ExpressionStatementSyntax>().FirstOrDefault();
			} else {
				whenFalseExprStatement = node.Else.Statement as ExpressionStatementSyntax;
			}

			if (whenTrueExprStatement == null || whenFalseExprStatement == null)
				return Enumerable.Empty<CodeAction>();

			var trueAssignment = whenTrueExprStatement.Expression as AssignmentExpressionSyntax;
			var falseAssignment = whenFalseExprStatement.Expression as AssignmentExpressionSyntax;
			if (trueAssignment == null /*|| !ConvertAssignmentToIfAction.IsAssignment(trueAssignment)*/ || 
				falseAssignment == null /*|| !ConvertAssignmentToIfAction.IsAssignment(falseAssignment)*/ || trueAssignment.CSharpKind() != falseAssignment.CSharpKind() ||
				!trueAssignment.Left.IsEquivalentTo(falseAssignment.Left))
				return Enumerable.Empty<CodeAction>();

			var newRoot = root.ReplaceNode((StatementSyntax)node, SyntaxFactory.ExpressionStatement(SyntaxFactory.BinaryExpression(trueAssignment.CSharpKind(), trueAssignment.Left,
				SyntaxFactory.ConditionalExpression(condition, trueAssignment.Right, falseAssignment.Right))));
			return new[] { CodeActionFactory.Create(span, DiagnosticSeverity.Info, "Replace with '?:' expression", document.WithSyntaxRoot(newRoot)) };
		}
	}
}