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
using Microsoft.CodeAnalysis.Editing;


namespace ICSharpCode.NRefactory6.CSharp
{
	/// <summary>
	/// Generates symbols that describe declarations to be generated.
	/// </summary>
	#if NR6
	public
	#endif
	static class CodeGenerationSymbolFactory
	{
		readonly static Type typeInfo;

		static CodeGenerationSymbolFactory ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationSymbolFactory" + ReflectionNamespaces.WorkspacesAsmName, true);
			isCodeGenerationSymbolMethod = typeInfo.GetMethod ("IsCodeGenerationSymbol", BindingFlags.Static | BindingFlags.Public);
			createParameterSymbolMethod = typeInfo.GetMethods ().First (m => m.Name == "CreateParameterSymbol" && m.GetParameters ().Length == 8);
			createTypeParameterSymbolMethod = typeInfo.GetMethod ("CreateTypeParameterSymbol", BindingFlags.Static | BindingFlags.Public);
			createTypeParameterMethod = typeInfo.GetMethod ("CreateTypeParameter", BindingFlags.Static | BindingFlags.Public);
			createMethodSymbolMethod = typeInfo.GetMethod ("CreateMethodSymbol", BindingFlags.Static | BindingFlags.Public);
			createMethodSymbolMethod2 = typeInfo.GetMethod ("CreateMethodSymbol", BindingFlags.Static | BindingFlags.NonPublic, null, new [] { typeof(INamedTypeSymbol), typeof(IList<AttributeData>), typeof(Accessibility), typeof(DeclarationModifiers), typeof(ITypeSymbol), typeof(IMethodSymbol) , typeof(string), typeof(IList<ITypeParameterSymbol>), typeof(IList<IParameterSymbol>), typeof(IList<SyntaxNode>), typeof(IList<SyntaxNode>), typeof(IList<AttributeData>), typeof(MethodKind) }, null);
			createConstructorSymbolMethod = typeInfo.GetMethod ("CreateConstructorSymbol", BindingFlags.Static | BindingFlags.Public);
			createAccessorSymbolMethod = typeInfo.GetMethod ("CreateAccessorSymbol", BindingFlags.Static | BindingFlags.Public);
			createPropertySymbolMethod = typeInfo.GetMethod ("CreatePropertySymbol", BindingFlags.Static | BindingFlags.Public);
			createFieldSymbolMethod = typeInfo.GetMethod ("CreateFieldSymbol", BindingFlags.Static | BindingFlags.Public);
			createPointerTypeSymbolMethod = typeInfo.GetMethod ("CreatePointerTypeSymbol", BindingFlags.Static | BindingFlags.Public);
			createArrayTypeSymbolMethod = typeInfo.GetMethod ("CreateArrayTypeSymbol", BindingFlags.Static | BindingFlags.Public);
			createNamespaceSymbolMethod = typeInfo.GetMethod ("CreateNamespaceSymbol", BindingFlags.Static | BindingFlags.Public);
			createNamedTypeSymbolMethod = typeInfo.GetMethod ("CreateNamedTypeSymbol", BindingFlags.Static | BindingFlags.Public);
			createDelegateTypeSymbolMethod = typeInfo.GetMethod ("CreateDelegateTypeSymbol", BindingFlags.Static | BindingFlags.Public);
			createAttributeDataMethod = typeInfo.GetMethod ("CreateAttributeData", BindingFlags.Static | BindingFlags.Public);
			createEventSymbol = typeInfo.GetMethod ("CreateEventSymbol", BindingFlags.Static | BindingFlags.Public);

			createPropertySymbolMethod2 = typeInfo.GetMethod ("CreatePropertySymbol", BindingFlags.Static | BindingFlags.NonPublic, null, new [] { 
				typeof(INamedTypeSymbol),
				typeof(IList<AttributeData>),
				typeof(Accessibility),
				typeof(DeclarationModifiers),
				typeof(ITypeSymbol),
				typeof(IPropertySymbol),
				typeof(string),
				typeof(IList<IParameterSymbol>),
				typeof(IMethodSymbol),
				typeof(IMethodSymbol),
				typeof(bool),
				typeof(SyntaxNode) 
			}, null);

		}

		static MethodInfo createPropertySymbolMethod2;

		public static IPropertySymbol CreatePropertySymbol(
			INamedTypeSymbol containingType,
			IList<AttributeData> attributes,
			Accessibility accessibility,
			DeclarationModifiers modifiers,
			ITypeSymbol type,
			IPropertySymbol explicitInterfaceSymbol,
			string name,
			IList<IParameterSymbol> parameters,
			IMethodSymbol getMethod,
			IMethodSymbol setMethod,
			bool isIndexer = false,
			SyntaxNode initializer = null)
		{
			return (IPropertySymbol)createPropertySymbolMethod2.Invoke (null, new object[] { containingType,
				attributes,
				accessibility,
				modifiers,
				type,
				explicitInterfaceSymbol,
				name,
				parameters,
				getMethod,
				setMethod,
				isIndexer,
				initializer 
			});	
		}


		static MethodInfo createEventSymbol;

		public static IEventSymbol CreateEventSymbol(IList<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers, ITypeSymbol type, IEventSymbol explicitInterfaceSymbol, string name, IMethodSymbol addMethod = null, IMethodSymbol removeMethod = null, IMethodSymbol raiseMethod = null, IList<IParameterSymbol> parameterList = null)
		{
			return (IEventSymbol)createEventSymbol.Invoke (null, new object[] { attributes, accessibility, modifiers, type, explicitInterfaceSymbol, name, addMethod, removeMethod, raiseMethod, parameterList });
		}

		public static IEventSymbol CreateEventSymbol(
			IEventSymbol @event,
			IList<AttributeData> attributes = null,
			Accessibility? accessibility = null,
			DeclarationModifiers? modifiers = null,
			IEventSymbol explicitInterfaceSymbol = null,
			string name = null,
			IMethodSymbol addMethod = null,
			IMethodSymbol removeMethod = null)
		{
			return CodeGenerationSymbolFactory.CreateEventSymbol(
				attributes,
				accessibility ?? @event.DeclaredAccessibility,
				modifiers ?? @event.GetSymbolModifiers(),
				@event.Type,
				explicitInterfaceSymbol,
				name ?? @event.Name,
				addMethod,
				removeMethod);
		}

		public static IMethodSymbol CreateMethodSymbol(
			IMethodSymbol method,
			IList<AttributeData> attributes = null,
			Accessibility? accessibility = null,
			DeclarationModifiers? modifiers = null,
			IMethodSymbol explicitInterfaceSymbol = null,
			string name = null,
			IList<SyntaxNode> statements = null)
		{
			return CodeGenerationSymbolFactory.CreateMethodSymbol(
				attributes,
				accessibility ?? method.DeclaredAccessibility,
				modifiers ?? method.GetSymbolModifiers(),
				method.ReturnType,
				explicitInterfaceSymbol,
				name ?? method.Name,
				method.TypeParameters,
				method.Parameters,
				statements,
				returnTypeAttributes: method.GetReturnTypeAttributes());
		}

		public static IPropertySymbol CreatePropertySymbol(
			IPropertySymbol property,
			IList<AttributeData> attributes = null,
			Accessibility? accessibility = null,
			DeclarationModifiers? modifiers = null,
			IPropertySymbol explicitInterfaceSymbol = null,
			string name = null,
			bool? isIndexer = null,
			IMethodSymbol getMethod = null,
			IMethodSymbol setMethod = null)
		{
			return CodeGenerationSymbolFactory.CreatePropertySymbol(
				attributes,
				accessibility ?? property.DeclaredAccessibility,
				modifiers ?? property.GetSymbolModifiers(),
				property.Type,
				explicitInterfaceSymbol,
				name ?? property.Name,
				property.Parameters,
				getMethod,
				setMethod,
				isIndexer ?? property.IsIndexer);
		}

		public static IMethodSymbol CreateAccessorSymbol(
			IMethodSymbol accessor,
			IList<AttributeData> attributes = null,
			Accessibility? accessibility = null,
			IMethodSymbol explicitInterfaceSymbol = null,
			IList<SyntaxNode> statements = null)
		{
			return CodeGenerationSymbolFactory.CreateMethodSymbol(
				attributes,
				accessibility ?? accessor.DeclaredAccessibility,
				accessor.GetSymbolModifiers().WithIsAbstract(statements == null),
				accessor.ReturnType,
				explicitInterfaceSymbol ?? accessor.ExplicitInterfaceImplementations.FirstOrDefault(),
				accessor.Name,
				accessor.TypeParameters,
				accessor.Parameters,
				statements,
				returnTypeAttributes: accessor.GetReturnTypeAttributes());
		}


		static MethodInfo createAttributeDataMethod;

		public static AttributeData CreateAttributeData(
			INamedTypeSymbol attributeClass,
			ImmutableArray<TypedConstant> constructorArguments = default(ImmutableArray<TypedConstant>),
			ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments = default(ImmutableArray<KeyValuePair<string, TypedConstant>>))
		{
			return (AttributeData)createAttributeDataMethod.Invoke (null, new object[] { attributeClass, constructorArguments, namedArguments });
		}

		static MethodInfo createNamedTypeSymbolMethod;
		public static INamedTypeSymbol CreateNamedTypeSymbol(IList<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers, TypeKind typeKind, string name, IList<ITypeParameterSymbol> typeParameters = null, INamedTypeSymbol baseType = null, IList<INamedTypeSymbol> interfaces = null, SpecialType specialType = SpecialType.None, IList<ISymbol> members = null)
		{
			return (INamedTypeSymbol)createNamedTypeSymbolMethod.Invoke (null, new object[] { attributes, accessibility, modifiers, typeKind, name, typeParameters, baseType, interfaces,  specialType, members });
		}

		static MethodInfo createDelegateTypeSymbolMethod;
		public static INamedTypeSymbol CreateDelegateTypeSymbol(IList<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers, ITypeSymbol returnType, string name, IList<ITypeParameterSymbol> typeParameters = null, IList<IParameterSymbol> parameters = null)
		{
			return (INamedTypeSymbol)createDelegateTypeSymbolMethod.Invoke (null, new object[] { attributes, accessibility, modifiers, returnType, name, typeParameters, parameters });
		}

		static MethodInfo createNamespaceSymbolMethod;

		public static INamespaceSymbol CreateNamespaceSymbol(string name, IList<ISymbol> imports = null, IList<INamespaceOrTypeSymbol> members = null)
		{
			return (INamespaceSymbol)createNamespaceSymbolMethod.Invoke (null, new object[] { name, imports, members });
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

		static MethodInfo createMethodSymbolMethod;
		public static IMethodSymbol CreateMethodSymbol(IList<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers, ITypeSymbol returnType, IMethodSymbol explicitInterfaceSymbol, string name, IList<ITypeParameterSymbol> typeParameters, IList<IParameterSymbol> parameters, IList<SyntaxNode> statements = null, IList<SyntaxNode> handlesExpressions = null, IList<AttributeData> returnTypeAttributes = null, MethodKind methodKind = MethodKind.Ordinary)
		{
			return (IMethodSymbol)createMethodSymbolMethod.Invoke (null, new object[] { attributes, accessibility, modifiers, returnType, explicitInterfaceSymbol, name, typeParameters, parameters, statements, handlesExpressions, returnTypeAttributes, methodKind });
		}

		static MethodInfo createMethodSymbolMethod2;
		public static IMethodSymbol CreateMethodSymbol(INamedTypeSymbol containingType, IList<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers, ITypeSymbol returnType, IMethodSymbol explicitInterfaceSymbol, string name, IList<ITypeParameterSymbol> typeParameters, IList<IParameterSymbol> parameters, IList<SyntaxNode> statements = null, IList<SyntaxNode> handlesExpressions = null, IList<AttributeData> returnTypeAttributes = null, MethodKind methodKind = MethodKind.Ordinary)
		{
			return (IMethodSymbol)createMethodSymbolMethod2.Invoke (null, new object[] { containingType, attributes, accessibility, modifiers, returnType, explicitInterfaceSymbol, name, typeParameters, parameters, statements, null, returnTypeAttributes, methodKind });
		}

		static MethodInfo createPointerTypeSymbolMethod;
		public static IPointerTypeSymbol CreatePointerTypeSymbol(ITypeSymbol pointedAtType)
		{
			return (IPointerTypeSymbol)createPointerTypeSymbolMethod.Invoke (null, new object[] { pointedAtType });
		}

		static MethodInfo createArrayTypeSymbolMethod;
		public static IArrayTypeSymbol CreateArrayTypeSymbol(ITypeSymbol elementType, int rank = 1)
		{
			return (IArrayTypeSymbol)createArrayTypeSymbolMethod.Invoke (null, new object[] { elementType, rank });
		}

		static MethodInfo createConstructorSymbolMethod;
		public static IMethodSymbol CreateConstructorSymbol(IList<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers, string typeName, IList<IParameterSymbol> parameters, IList<SyntaxNode> statements = null, IList<SyntaxNode> baseConstructorArguments = null, IList<SyntaxNode> thisConstructorArguments = null)
		{
			return (IMethodSymbol)createConstructorSymbolMethod.Invoke (null, new object[] { attributes, accessibility, modifiers, typeName, parameters, statements, baseConstructorArguments, thisConstructorArguments });	
		}

		static MethodInfo createAccessorSymbolMethod;
		public static IMethodSymbol CreateAccessorSymbol(IList<AttributeData> attributes, Accessibility accessibility, IList<SyntaxNode> statements)
		{
			return (IMethodSymbol)createAccessorSymbolMethod.Invoke (null, new object[] { attributes, accessibility, statements });	
		}

		static MethodInfo createPropertySymbolMethod;

		/// <summary>
		/// Creates a property symbol that can be used to describe a property declaration.
		/// </summary>
		public static IPropertySymbol CreatePropertySymbol(IList<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers, ITypeSymbol type, IPropertySymbol explicitInterfaceSymbol, string name, IList<IParameterSymbol> parameters, IMethodSymbol getMethod, IMethodSymbol setMethod, bool isIndexer = false)
		{
			return (IPropertySymbol)createPropertySymbolMethod.Invoke (null, new object[] { attributes, accessibility, modifiers, type, explicitInterfaceSymbol, name, parameters, getMethod, setMethod, isIndexer });	
		}

		static MethodInfo createFieldSymbolMethod;

		public static IFieldSymbol CreateFieldSymbol(IList<AttributeData> attributes, Accessibility accessibility, DeclarationModifiers modifiers, ITypeSymbol type, string name, bool hasConstantValue = false, object constantValue = null, SyntaxNode initializer = null)
		{
			return (IFieldSymbol)createFieldSymbolMethod.Invoke (null, new object[] { attributes, accessibility, modifiers, type, name, hasConstantValue, constantValue, initializer });	
		}
	}
}
