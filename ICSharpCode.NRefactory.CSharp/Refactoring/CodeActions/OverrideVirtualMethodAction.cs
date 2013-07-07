// 
// OverrideVirtualMethodAction.cs
//  
// Author:
//       Ji Kun <jikun.nus@gmail.com>
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
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.PatternMatching;
using System;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Override virtual methods", Description = "Works on a type declaration. Override all the virtual methods in its directed type")]
	public class OverrideVirtualMemberAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var node = context.GetNode<AstType>();
			if (node == null || node.Role != Roles.BaseType)
				yield break;
			var state = context.GetResolverStateBefore(node);
			if (state.CurrentTypeDefinition == null)
				yield break;

			var resolveResult = context.Resolve(node);
			if (resolveResult.Type.Kind != TypeKind.Class || resolveResult.Type.GetDefinition() == null)
				yield break;

			List<IMember> unimplemented = CollectNonImplementedVirtualMembers(state.CurrentTypeDefinition, resolveResult.Type);
			if (unimplemented.Count == 0)
				yield break;

			yield return new CodeAction(context.TranslateString("Override virtual methods"), script => {
				script.InsertWithCursor(
					context.TranslateString("Override virtual methods"),
					state.CurrentTypeDefinition,
					ImplementInterfaceAction.GenerateImplementation(context, unimplemented.Select(m => Tuple.Create(m, false))).Select(entity => {
					var decl = entity as EntityDeclaration;
					if (decl != null)
						decl.Modifiers |= Modifiers.Override;
					return entity;
				})
				);
			}, node);
		}

		public static List<IMember> CollectNonImplementedVirtualMembers(ITypeDefinition type, IType baseType)
		{
			List<IMember> results = new List<IMember>();
			var methods = baseType.GetDefinition().Methods;
			foreach (IMethod method in methods) {
				if (method.IsSynthetic || !method.IsVirtual)
					continue;
				if (!type.Methods.Any(f => SignatureComparer.Ordinal.Equals(method, f))) {
					results.Add(method);
				}
			}
			return results;
		}
	}
}
