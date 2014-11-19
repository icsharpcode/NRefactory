// 
// ConvertCastToAsAction.cs
//  
// Author:
//       Mansheng Yang <lightyang0@gmail.com>
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
using ICSharpCode.NRefactory6.CSharp;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	/// <summary>
	/// Converts a cast expression to an 'as' expression
	/// </summary>
	[NRefactoryCodeRefactoringProvider(Description = "Convert cast to 'as'.")]
	[ExportCodeRefactoringProvider("Convert cast to 'as'.", LanguageNames.CSharp)]
	public class ConvertCastToAsAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			var token = root.FindToken(span.Start);
			var castExpression = token.Parent.AncestorsAndSelf().OfType<CastExpressionSyntax>().FirstOrDefault();
			if (castExpression == null || castExpression.Expression.Span.Contains (span))
				return;
			var type = model.GetTypeInfo(castExpression.Type).Type;
			if (type == null || type.IsValueType && !type.IsNullableType())
				return;
			context.RegisterRefactoring(CodeActionFactory.Create(token.Span, DiagnosticSeverity.Info, "Convert cast to 'as'", t2 => Task.FromResult(PerformAction (document, root, castExpression))));

//			// only works on reference and nullable types
//			var type = context.ResolveType (node.Type);
//			var typeDef = type.GetDefinition ();
//			var isNullable = typeDef != null && typeDef.KnownTypeCode == KnownTypeCode.NullableOfT;
//			if (type.IsReferenceType == true || isNullable) {
//				return new CodeAction (context.TranslateString ("Convert cast to 'as'"), script => {
//					var asExpr = new AsExpression (node.Expression.Clone (), node.Type.Clone ());
//					// if parent is an expression, clone parent and replace the case expression with asExpr,
//					// so that we can inset parentheses
//					var parentExpr = node.Parent.Clone () as Expression;
//					if (parentExpr != null) {
//						var castExpr = parentExpr.GetNodeContaining (node.StartLocation, node.EndLocation);
//						castExpr.ReplaceWith (asExpr);
//						parentExpr.AcceptVisitor (insertParentheses);
//						script.Replace (node.Parent, parentExpr);
//					} else {
//						script.Replace (node, asExpr);
//					}
//				}, node);
//			}
		}


		static Document PerformAction(Document document, SyntaxNode root, CastExpressionSyntax castExpr)
		{
			ExpressionSyntax nodeToReplace = castExpr;
			while (nodeToReplace.Parent is ParenthesizedExpressionSyntax) {
				nodeToReplace = (ExpressionSyntax)nodeToReplace.Parent;
			}

			// The syntax factory doesn't automatically add spaces around the operator !
			var token = SyntaxFactory.ParseToken(" as ");
			var asExpr = (ExpressionSyntax)SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, castExpr.Expression, token, castExpr.Type);
			if (nodeToReplace.Parent is ExpressionSyntax)
				asExpr = SyntaxFactory.ParenthesizedExpression(asExpr);
			var newRoot = root.ReplaceNode((SyntaxNode)nodeToReplace, asExpr);
			return document.WithSyntaxRoot(newRoot);
		}

	}
}
