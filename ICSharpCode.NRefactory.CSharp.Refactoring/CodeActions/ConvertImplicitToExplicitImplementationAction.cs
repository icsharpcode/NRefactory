// 
// ConvertImplicitToExplicitImplementationAction.cs
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
	//TODO: Why only method ?
	[NRefactoryCodeRefactoringProvider(Description = "Convert implict implementation of an interface method to explicit implementation")]
	[ExportCodeRefactoringProvider("Convert implict to explicit implementation", LanguageNames.CSharp)]
	public class ConvertImplicitToExplicitImplementationAction : SpecializedCodeAction<MethodDeclarationSyntax>
	{
		protected override IEnumerable<CodeAction> GetActions(SemanticModel semanticModel, SyntaxNode root, TextSpan span, MethodDeclarationSyntax node, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
//		protected override CodeAction GetAction (SemanticModel context, MethodDeclaration node)
//		{
//			if (!node.PrivateImplementationType.IsNull)
//				return null;
//
//			if (!node.NameToken.Contains (context.Location))
//				return null;
//
//			var memberResolveResult = context.Resolve(node) as MemberResolveResult;
//			if (memberResolveResult == null)
//				return null;
//			var method = memberResolveResult.Member as IMethod;
//			if (method == null || method.ImplementedInterfaceMembers.Count != 1 || method.DeclaringType.Kind == TypeKind.Interface)
//				return null;
//
//			return new CodeAction (context.TranslateString ("Convert implict to explicit implementation"),
//				script =>
//				{
//					var explicitImpl = (MethodDeclaration)node.Clone ();
//					// remove visibility modifier
//					explicitImpl.Modifiers &= ~Modifiers.VisibilityMask;
//					var implementedInterface = method.ImplementedInterfaceMembers [0].DeclaringType;
//					explicitImpl.PrivateImplementationType = context.CreateShortType (implementedInterface);
//					script.Replace (node, explicitImpl);
//				},
//				node.NameToken
//			);
//		}
	}
}
