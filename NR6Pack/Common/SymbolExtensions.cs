//
// SymbolExtensions.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.NRefactory6.CSharp
{
	public static class SymbolExtensions
	{
		//		public static string GetDocumentationId (this ISymbol symbol)
		//		{
		//			if (symbol.GetType().FullName != "Microsoft.CodeAnalysis.CSharp.Symbol")
		//				return null;
		//			var mi = symbol.GetType().GetMethod("GetDocumentationCommentId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		//			if (mi == null)
		//				return null;
		//			return (string)mi.Invoke(symbol, null);
		//		}
		
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
				throw new ArgumentNullException("symbol");
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
			if (symbol == null)
				throw new ArgumentNullException("symbol");
			return GetEditorBrowsableState(symbol) != System.ComponentModel.EditorBrowsableState.Never;
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
				throw new ArgumentNullException("symbol");
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
				throw new ArgumentNullException("symbol");
			var browsableState = symbol.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.Name == "CategoryAttribute" && attr.AttributeClass.ContainingNamespace.MetadataName == "System.ComponentModel");
			if (browsableState != null && browsableState.ConstructorArguments.Length == 1) {
				try {
					return (string)browsableState.ConstructorArguments [0].Value;
				} catch {
				}
			}
			return null;
		}

		public static ImmutableArray<IParameterSymbol> GetParameters(this ISymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException("symbol");
			var method = symbol as IMethodSymbol;
			if (method != null)
				return method.Parameters;
			var property = symbol as IPropertySymbol;
			if (property != null)
				return property.Parameters;
			return ImmutableArray<IParameterSymbol>.Empty;
		}

		public static ImmutableArray<ITypeParameterSymbol> GetTypeParameters(this ISymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException("symbol");
			var type = symbol as INamedTypeSymbol;
			if (type != null)
				return type.TypeParameters;
			var method = symbol as IMethodSymbol;
			if (method != null)
				return method.TypeParameters;
			return ImmutableArray<ITypeParameterSymbol>.Empty;
		}

		public static bool IsAnyConstructor(this ISymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException("symbol");
			var method = symbol as IMethodSymbol;
			return method != null && (method.MethodKind == MethodKind.Constructor || method.MethodKind == MethodKind.StaticConstructor);
		}

		public static bool IsConstructor(this ISymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException("symbol");
			return symbol is IMethodSymbol && ((IMethodSymbol)symbol).MethodKind == MethodKind.Constructor;
		}

		public static bool IsStaticConstructor(this ISymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException("symbol");
			return symbol is IMethodSymbol && ((IMethodSymbol)symbol).MethodKind == MethodKind.StaticConstructor;
		}

		public static bool IsDestructor(this ISymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException("symbol");
			return symbol is IMethodSymbol && ((IMethodSymbol)symbol).MethodKind == MethodKind.Destructor;
		}

		public static bool IsDelegateType(this ISymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException("symbol");
			return symbol is ITypeSymbol && ((ITypeSymbol)symbol).TypeKind == TypeKind.Delegate;
		}

		public static ITypeSymbol GetReturnType(this ISymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException("symbol");
			switch (symbol.Kind) {
				case SymbolKind.Field:
					var field = (IFieldSymbol)symbol;
					return field.Type;
				case SymbolKind.Method:
					var method = (IMethodSymbol)symbol;
					if (method.MethodKind == MethodKind.Constructor)
						return method.ContainingType;
					return method.ReturnType;
				case SymbolKind.Property:
					var property = (IPropertySymbol)symbol;
					return property.Type;
				case SymbolKind.Event:
					var evt = (IEventSymbol)symbol;
					return evt.Type;
				case SymbolKind.Parameter:
					var param = (IParameterSymbol)symbol;
					return param.Type;
				case SymbolKind.Local:
					var local = (ILocalSymbol)symbol;
					return local.Type;
			}
			return null;
		}

		public static ParameterSyntax GenerateParameterSyntax (this IParameterSymbol symbol)
		{
			var result = SyntaxFactory.Parameter (SyntaxFactory.Identifier (symbol.Name));
			result = result.WithType (symbol.Type.GenerateTypeSyntax ());
			if (symbol.IsThis)
				result = result.WithModifiers(SyntaxFactory.TokenList (SyntaxFactory.Token (SyntaxKind.ThisKeyword)));
			if (symbol.IsParams)
				result = result.WithModifiers(SyntaxFactory.TokenList (SyntaxFactory.Token (SyntaxKind.ParamsKeyword)));
			if (symbol.RefKind == RefKind.Out)
				result = result.WithModifiers(SyntaxFactory.TokenList (SyntaxFactory.Token (SyntaxKind.OutKeyword)));
			if (symbol.RefKind == RefKind.Ref)
				result = result.WithModifiers(SyntaxFactory.TokenList (SyntaxFactory.Token (SyntaxKind.RefKeyword)));
			return result;
		}
	}
}