// 
// InsertAnonymousMethodSignature.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Inserts a signature to parameterless anonymous methods")]
	[ExportCodeRefactoringProvider("Insert anonymous method signature", LanguageNames.CSharp)]
	public class InsertAnonymousMethodSignatureAction : ICodeRefactoringProvider
	{
		public async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
		{
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			return null;
		}
//		public async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
//		{
//			IType type;
//			var anonymousMethodExpression = GetAnonymousMethodExpression(context, out type);
//			if (anonymousMethodExpression == null) {
//				yield break;
//			}
//			yield return new CodeAction (context.TranslateString("Insert anonymous method signature"), script => {
//				var delegateMethod = type.GetDelegateInvokeMethod();
//				
//				var sb = new StringBuilder ("(");
//				for (int k = 0; k < delegateMethod.Parameters.Count; k++) {
//					if (k > 0) {
//						sb.Append(", ");
//					}
//					
//					var paramType = delegateMethod.Parameters [k].Type;
//					
//					sb.Append(context.CreateShortType(paramType));
//					sb.Append(" ");
//					sb.Append(delegateMethod.Parameters [k].Name);
//				}
//				sb.Append(")");
//				
//				script.InsertText(context.GetOffset(anonymousMethodExpression.DelegateToken.EndLocation), sb.ToString());
//			}, anonymousMethodExpression);
//		}
//		
//		static AnonymousMethodExpression GetAnonymousMethodExpression (SemanticModel context, out IType delegateType)
//		{
//			delegateType = null;
//			
//			var anonymousMethodExpression = context.GetNode<AnonymousMethodExpression> ();
//			if (anonymousMethodExpression == null || !anonymousMethodExpression.DelegateToken.Contains (context.Location) || anonymousMethodExpression.HasParameterList)
//				return null;
//			
//			IType resolvedType = null;
//			var parent = anonymousMethodExpression.Parent;
//			
//			if (parent is AssignmentExpression) {
//				resolvedType = context.Resolve (((AssignmentExpression)parent).Left).Type;
//			} else if (parent is VariableInitializer) {
//				resolvedType = context.Resolve (((VariableDeclarationStatement)parent.Parent).Type).Type;
//			} else if (parent is InvocationExpression) {
//				// TODO: handle invocations
//			}
//			if (resolvedType == null)
//				return null;
//			delegateType = resolvedType;
//			if (delegateType.Kind != TypeKind.Delegate) 
//				return null;
//			
//			return anonymousMethodExpression;
//		}
	}
}

