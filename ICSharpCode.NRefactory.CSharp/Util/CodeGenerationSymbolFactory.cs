//
// CodeGenerationSymbolFactory.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using System.Reflection;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;


namespace ICSharpCode.NRefactory6.CSharp
{
	/// <summary>
	/// Generates symbols that describe declarations to be generated.
	/// </summary>
	public static class CodeGenerationSymbolFactory
	{
		readonly static Type typeInfo;

		static CodeGenerationSymbolFactory ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationSymbolFactory" + ReflectionNamespaces.WorkspacesAsmName, true);
			isCodeGenerationSymbolMethod = typeInfo.GetMethod ("IsCodeGenerationSymbol", BindingFlags.Static | BindingFlags.Public);
			createParameterSymbolMethod = typeInfo.GetMethods ().First (m => m.Name == "CreateParameterSymbol" && m.GetParameters ().Length == 8);
			createTypeParameterSymbolMethod = typeInfo.GetMethod ("CreateTypeParameterSymbol", BindingFlags.Static | BindingFlags.Public);

			createTypeParameterMethod = typeInfo.GetMethod ("CreateTypeParameter", BindingFlags.Static | BindingFlags.Public);
		}

		static readonly MethodInfo isCodeGenerationSymbolMethod;
		/// <summary>
		/// Determines if the symbol is purely a code generation symbol.
		/// </summary>
		public static bool IsCodeGenerationSymbol(this ISymbol symbol)
		{
			return (bool)isCodeGenerationSymbolMethod.Invoke (null, new object[] { symbol });
		}

		static readonly MethodInfo createParameterSymbolMethod;
		public static IParameterSymbol CreateParameterSymbol(ITypeSymbol type, string name)
		{
			return CreateParameterSymbol(attributes: null, refKind: RefKind.None, isParams: false, type: type, name: name, isOptional: false);
		}

		public static IParameterSymbol CreateParameterSymbol(IList<AttributeData> attributes, RefKind refKind, bool isParams, ITypeSymbol type, string name, bool isOptional = false, bool hasDefaultValue = false, object defaultValue = null)
		{
			return (IParameterSymbol)createParameterSymbolMethod.Invoke (null, new object[] { attributes, refKind, isParams, type, name, isOptional, hasDefaultValue, defaultValue });
		}

		static readonly MethodInfo createTypeParameterSymbolMethod;
		public static ITypeParameterSymbol CreateTypeParameterSymbol(string name, int ordinal = 0)
		{
			return (ITypeParameterSymbol)createTypeParameterSymbolMethod.Invoke (null, new object[] { name, ordinal });
		}

		static MethodInfo createTypeParameterMethod;
		public static ITypeParameterSymbol CreateTypeParameter(IList<AttributeData> attributes, VarianceKind varianceKind, string name, ImmutableArray<ITypeSymbol> constraintTypes, bool hasConstructorConstraint = false, bool hasReferenceConstraint = false, bool hasValueConstraint = false, int ordinal = 0)
		{
			return (ITypeParameterSymbol)createTypeParameterSymbolMethod.Invoke (null, new object[] { attributes, varianceKind, name, constraintTypes, hasConstructorConstraint, hasReferenceConstraint, hasValueConstraint, ordinal});
		}

	}
}
