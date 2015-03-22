//
// ConvertIfStatementToReturnStatementAction.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Convert 'if' to 'return'")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Convert 'if' to 'return'")]
	public class ConvertIfStatementToReturnStatementAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var root = await document.GetSyntaxRootAsync(cancellationToken);

			var node = root.FindNode(span) as IfStatementSyntax;
			if (node == null)
				return;

			ExpressionSyntax condition;
			ReturnStatementSyntax return1, return2, rs;
			if (!ConvertIfStatementToReturnStatementAction.GetMatch(node, out condition, out return1, out return2, out rs))
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					span,
					DiagnosticSeverity.Info, 
					"Replace with 'return'", 
					t2 => {
						var newRoot = root.ReplaceNode((SyntaxNode)node, SyntaxFactory.ReturnStatement(CreateCondition(condition, return1, return2)).WithAdditionalAnnotations(Formatter.Annotation).WithLeadingTrivia(node.GetLeadingTrivia()));
						if (rs != null) {
							var retToRemove = newRoot.DescendantNodes().OfType<ReturnStatementSyntax>().FirstOrDefault(r => r.IsEquivalentTo(rs));
							newRoot = newRoot.RemoveNode(retToRemove, SyntaxRemoveOptions.KeepNoTrivia);
						}

						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					})
			);
		}

		static bool GetMatch(IfStatementSyntax node, out ExpressionSyntax c, out ReturnStatementSyntax e1, out ReturnStatementSyntax e2, out ReturnStatementSyntax rs)
		{
			rs = e1 = e2 = null;
			c = node.Condition;
			//attempt to match if(condition) return else return
			e1 = ConvertIfStatementToNullCoalescingExpressionAction.GetSimpleStatement(node.Statement) as ReturnStatementSyntax;
			if (e1 == null)
				return false;
			e2 = node.Else != null ? ConvertIfStatementToNullCoalescingExpressionAction.GetSimpleStatement(node.Else.Statement) as ReturnStatementSyntax : null;
			//match
			if (e1 != null && e2 != null) {
				return true;
			}

			//attempt to match if(condition) return
			if (e1 != null) {
				rs = node.Parent.ChildThatContainsPosition(node.GetTrailingTrivia().Max(t => t.FullSpan.End) + 1).AsNode() as ReturnStatementSyntax;
				if (rs != null) {
					e2 = rs;
					return true;
				}
			}
			return false;
		}

		static ExpressionSyntax CreateCondition(ExpressionSyntax c, ReturnStatementSyntax e1, ReturnStatementSyntax e2)
		{
			return e1.Expression.IsKind(SyntaxKind.TrueLiteralExpression) && e2.Expression.IsKind(SyntaxKind.FalseLiteralExpression) ? 
					c : SyntaxFactory.ConditionalExpression(c, e1.Expression, e2.Expression);
		}
	}
}

