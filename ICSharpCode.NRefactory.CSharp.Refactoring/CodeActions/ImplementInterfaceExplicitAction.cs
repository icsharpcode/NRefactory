// 
// ImplementInterfaceExplicitAction.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
	[NRefactoryCodeRefactoringProvider(Description = "Creates an interface implementation")]
	[ExportCodeRefactoringProvider("Implement interface explicit", LanguageNames.CSharp)]
	public class ImplementInterfaceExplicitAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
		}
//		public async Task ComputeRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
//		{
//			var service = (CodeGenerationService)context.GetService(typeof(CodeGenerationService)); 
//			if (service == null)
//				yield break;
//
//			var type = context.GetNode<AstType>();
//			if (type == null || type.Role != Roles.BaseType)
//				yield break;
//			var state = context.GetResolverStateBefore(type);
//			if (state.CurrentTypeDefinition == null)
//				yield break;
//
//			var resolveResult = context.Resolve(type);
//			if (resolveResult.Type.Kind != TypeKind.Interface)
//				yield break;
//
//			bool interfaceMissing;
//			var toImplement = ImplementInterfaceAction.CollectMembersToImplement(
//				state.CurrentTypeDefinition,
//				resolveResult.Type,
//				false,
//				out interfaceMissing
//			);
//			if (toImplement.Count == 0)
//				yield break;
//
//			yield return new CodeAction(context.TranslateString("Implement interface explicit"), script =>
//				script.InsertWithCursor(
//					context.TranslateString("Implement Interface"),
//					state.CurrentTypeDefinition,
//					(s, c) => ImplementInterfaceAction.GenerateImplementation (c, toImplement.Select (t => Tuple.Create (t.Item1, true)), interfaceMissing).ToList()
//				)
//			, type);
//		}
	}
}
