//
// CompletionExtensionMethods.cs
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
using Microsoft.CodeAnalysis;
using System.ComponentModel;
using System.Collections;
using System.Threading;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	public static class ExtensionMethods
	{
		/// <summary>
		/// Gets the EditorBrowsableState of an entity.
		/// </summary>
		/// <returns>
		/// The editor browsable state.
		/// </returns>
		/// <param name='symbol'>
		/// Entity.
		/// </param>
		public static System.ComponentModel.EditorBrowsableState GetEditorBrowsableState(this ISymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException ("symbol");
			var browsableState = symbol.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.Name == "EditorBrowsableAttribute" && attr.AttributeClass.ContainingNamespace.MetadataName == "System.ComponentModel");
			if (browsableState != null && browsableState.ConstructorArguments.Length == 1) {
				try {
					return (System.ComponentModel.EditorBrowsableState)browsableState.ConstructorArguments [0].Value;
				} catch {
				}
			}
			return System.ComponentModel.EditorBrowsableState.Always;
		}
		
		/// <summary>
		/// Determines if an entity should be shown in the code completion window. This is the same as:
		/// <c>GetEditorBrowsableState (entity) != System.ComponentModel.EditorBrowsableState.Never</c>
		/// </summary>
		/// <returns>
		/// <c>true</c> if the entity should be shown; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='symbol'>
		/// The entity.
		/// </param>
		public static bool IsEditorBrowsable(this ISymbol symbol)
		{
			return GetEditorBrowsableState (symbol) != System.ComponentModel.EditorBrowsableState.Never;
		}
		
		/// <summary>
		/// Returns true if the symbol wasn't tagged with
		/// [System.ComponentModel.BrowsableAttribute (false)]
		/// </summary>
		/// <returns><c>true</c> if is designer browsable the specified symbol; otherwise, <c>false</c>.</returns>
		/// <param name="symbol">Symbol.</param>
		public static bool IsDesignerBrowsable(this ISymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException ("symbol");
			var browsableState = symbol.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.Name == "BrowsableAttribute" && attr.AttributeClass.ContainingNamespace.MetadataName == "System.ComponentModel");
			if (browsableState != null && browsableState.ConstructorArguments.Length == 1) {
				try {
					return (bool)browsableState.ConstructorArguments [0].Value;
				} catch {
				}
			}
			return true;
		}
		
		/// <summary>
		/// Returns the component category.
		/// [System.ComponentModel.CategoryAttribute (CATEGORY)]
		/// </summary>
		/// <param name="symbol">Symbol.</param>
		public static string GetComponentCategory(this ISymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException ("symbol");
			var browsableState = symbol.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.Name == "CategoryAttribute" && attr.AttributeClass.ContainingNamespace.MetadataName == "System.ComponentModel");
			if (browsableState != null && browsableState.ConstructorArguments.Length == 1) {
				try {
					return (string)browsableState.ConstructorArguments [0].Value;
				} catch {
				}
			}
			return null;
		}
		
		/// <summary>
		/// Returns true if the type is public and was tagged with
		/// [System.ComponentModel.ToolboxItem (true)]
		/// </summary>
		/// <returns><c>true</c> if is designer browsable the specified symbol; otherwise, <c>false</c>.</returns>
		/// <param name="symbol">Symbol.</param>
		public static bool IsToolboxItem(this ITypeSymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException ("symbol");
			if (symbol.DeclaredAccessibility != Accessibility.Public)
				return false;
			var toolboxItemAttr = symbol.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.Name == "ToolboxItemAttribute" && attr.AttributeClass.ContainingNamespace.MetadataName == "System.ComponentModel");
			if (toolboxItemAttr != null && toolboxItemAttr.ConstructorArguments.Length == 1) {
				try {
					return (bool)toolboxItemAttr.ConstructorArguments [0].Value;
				} catch {
				}
			}
			return false;
		}
		

		public static IEnumerable<INamedTypeSymbol> GetAllTypes(this INamespaceSymbol namespaceSymbol, CancellationToken cancellationToken = default(CancellationToken))
		{
			var stack = new Stack<INamespaceOrTypeSymbol>();
			stack.Push(namespaceSymbol);

			while (stack.Count > 0) {
				if (cancellationToken.IsCancellationRequested)
					yield break;
				var current = stack.Pop();
				var currentNs = current as INamespaceSymbol;
				if (currentNs != null) {
					foreach (var member in currentNs.GetMembers())
						stack.Push(member);
				} else {
					var namedType = (INamedTypeSymbol)current;
					foreach (var nestedType in namedType.GetTypeMembers())
						stack.Push(nestedType);
					yield return namedType;
				}
			}
		}
		
		public static IEnumerable<INamedTypeSymbol> GetAllTypes(this Compilation compilation, CancellationToken cancellationToken = default(CancellationToken))
		{
			return GetAllTypes(compilation.GlobalNamespace, cancellationToken);
		}
		
	}
}

