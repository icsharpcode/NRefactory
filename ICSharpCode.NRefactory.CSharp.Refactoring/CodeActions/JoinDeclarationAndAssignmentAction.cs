// 
// JoinDeclarationAndAssignmentAction.cs
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
	[NRefactoryCodeRefactoringProvider(Description = "Join local variable declaration and assignment")]
	[ExportCodeRefactoringProvider("Join local variable declaration and assignment", LanguageNames.CSharp)]
	public class JoinDeclarationAndAssignmentAction : SpecializedCodeAction<VariableDeclaratorSyntax>
	{
		protected override IEnumerable<CodeAction> GetActions(SemanticModel semanticModel, SyntaxNode root, TextSpan span, VariableDeclaratorSyntax node, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
//		protected override CodeAction GetAction (SemanticModel context, VariableInitializer node)
//		{
//			var variableDecl = node.Parent as VariableDeclarationStatement;
//			if (variableDecl == null || !node.Initializer.IsNull)
//				return null;
//
//			var assignmentPattern = new ExpressionStatement(
//				new AssignmentExpression (new IdentifierExpression (node.Name), new AnyNode ("value")));
//			var nextSibling = variableDecl.GetNextSibling(n => n is Statement);
//			var match = assignmentPattern.Match(nextSibling);
//			if (!match.Success)
//				return null;
//
//			return new CodeAction (context.TranslateString ("Join local variable declaration and assignment"), script => {
//				var jointVariableDecl = new VariableDeclarationStatement (variableDecl.Type.Clone (),
//					node.Name, match.Get<Expression> ("value").First ().Clone ());
//				script.Replace (nextSibling, jointVariableDecl);
//				if (variableDecl.Variables.Count == 1) {
//					script.Remove (variableDecl);
//				} else {
//					var newVariableDecl = new VariableDeclarationStatement { Type = variableDecl.Type.Clone () };
//					foreach (var variable in variableDecl.Variables.Where (variable => variable != node))
//						newVariableDecl.Variables.Add ((VariableInitializer) variable.Clone ());
//					script.Replace (variableDecl, newVariableDecl);
//				}
//			}, node.NameToken);
//		}
	}
}
