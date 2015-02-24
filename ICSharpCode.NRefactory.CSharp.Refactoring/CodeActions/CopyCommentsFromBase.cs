// 
// CopyCommentsFromBase.cs
//  
// Author:
//       Ji Kun <jikun.nus0@gmail.com>
// 
// Copyright (c) 2013 Ji Kun <jikun.nus@gmail.com>
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
	// TODO: Why only methods ?
	[NRefactoryCodeRefactoringProvider(Description = "Copies documented comments from base to overriding methods")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Copy comments from base")]
	public class CopyCommentsFromBase: SpecializedCodeAction <MethodDeclarationSyntax>
	{
		protected override IEnumerable<CodeAction> GetActions(Document document, SemanticModel semanticModel, SyntaxNode root, TextSpan span, MethodDeclarationSyntax node, CancellationToken cancellationToken)
		{
			yield break;
		}
//		protected override CodeAction GetAction(SemanticModel context, MethodDeclaration node)
//		{
//			if (node == null || !node.HasModifier(Modifiers.Override))
//				return null;
//			if (!node.NameToken.Contains(context.Location))
//				return null;
//			
//			IMethod resolvedMember = (IMethod)(context.Resolve(node) as MemberResolveResult).Member;
//			
//			if (resolvedMember == null)
//				return null;
//			
//			IMethod originalMember = (IMethod)InheritanceHelper.GetBaseMember(resolvedMember);
//		
//			if (originalMember == null || originalMember.Documentation == null)
//				return null;
//			string comments = originalMember.Documentation.ToString();
//			
//			if (string.IsNullOrEmpty(comments))
//				return null;
//			
//			string[] lines = comments.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
//			return new CodeAction(context.TranslateString("Copy comments from base"), script => {
//				foreach (string co in lines) {
//					script.InsertBefore(node, new Comment(co, CommentType.Documentation));
//				}
//			}, node.NameToken);
//		}
	}
}