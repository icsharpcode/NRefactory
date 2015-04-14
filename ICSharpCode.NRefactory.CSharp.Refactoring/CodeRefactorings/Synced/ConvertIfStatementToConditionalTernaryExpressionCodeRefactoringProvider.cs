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
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Convert 'if' to '?:'")]
	public class ConvertIfStatementToConditionalTernaryExpressionCodeRefactoringProvider : CodeRefactoringProvider
	{
		internal static bool ParseIfStatement(IfStatementSyntax node, out ExpressionSyntax condition, out ExpressionSyntax target, out AssignmentExpressionSyntax whenTrue, out AssignmentExpressionSyntax whenFalse)
		{
			condition = null;
			target = null;
			whenTrue = null;
			whenFalse = null;

			if (node == null || node.Else == null || node.Parent is IfStatementSyntax || node.Else.Statement is IfStatementSyntax)
				return false;

			condition = node.Condition;
			//make sure to check for multiple statements
			ExpressionStatementSyntax whenTrueExprStatement, whenFalseExprStatement;
			var embeddedBlock = node.Statement as BlockSyntax;
			if (embeddedBlock != null) {
				if (embeddedBlock.Statements.Count > 1)
					return false;
				whenTrueExprStatement = node.Statement.DescendantNodesAndSelf().OfType<ExpressionStatementSyntax>().FirstOrDefault();
			} else {
				whenTrueExprStatement = node.Statement as ExpressionStatementSyntax;
			}

			var elseBlock = node.Else.Statement as BlockSyntax;
			if (elseBlock != null) {
				if (elseBlock.Statements.Count > 1)
					return false;
				whenFalseExprStatement = node.Else.Statement.DescendantNodesAndSelf().OfType<ExpressionStatementSyntax>().FirstOrDefault();
			} else {
				whenFalseExprStatement = node.Else.Statement as ExpressionStatementSyntax;
			}

			if (whenTrueExprStatement == null || whenFalseExprStatement == null)
				return false;

			whenTrue = whenTrueExprStatement.Expression as AssignmentExpressionSyntax;
			whenFalse = whenFalseExprStatement.Expression as AssignmentExpressionSyntax;
			if (whenTrue == null || whenFalse == null || whenTrue.Kind() != whenFalse.Kind() ||
				!whenTrue.Left.IsEquivalentTo(whenFalse.Left))
				return false;

			return true;
		}

		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
				return;
			var span = context.Span;
			if (!span.IsEmpty)
				return;
			var cancellationToken = context.CancellationToken;
			if (cancellationToken.IsCancellationRequested)
				return;
			var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			if (model.IsFromGeneratedCode(cancellationToken))
				return;
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
			var node = root.FindNode(span) as IfStatementSyntax;

			ExpressionSyntax condition, target;
			AssignmentExpressionSyntax trueAssignment, falseAssignment;
			if (!ParseIfStatement(node, out condition, out target, out trueAssignment, out falseAssignment))
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(span, DiagnosticSeverity.Info, GettextCatalog.GetString ("To '?:' expression"), 
					t2 => {
						var newRoot = root.ReplaceNode((SyntaxNode)node, 
							SyntaxFactory.ExpressionStatement(
								SyntaxFactory.AssignmentExpression(
									trueAssignment.Kind(),
									trueAssignment.Left,
									SyntaxFactory.ConditionalExpression(condition, trueAssignment.Right, falseAssignment.Right)
								)
							).WithAdditionalAnnotations(Formatter.Annotation).WithLeadingTrivia(node.GetLeadingTrivia())
						);
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				) 
			);
		}
	}
}