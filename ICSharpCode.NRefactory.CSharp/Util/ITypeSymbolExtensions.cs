using System;
using System.ComponentModel;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace ICSharpCode.NRefactory6.CSharp
{
	[EditorBrowsableAttribute (EditorBrowsableState.Never)]
	public static class ITypeSymbolExtensions
	{
		readonly static MethodInfo generateTypeSyntax;

		static ITypeSymbolExtensions()
		{
			
			var typeInfo = Type.GetType("Microsoft.CodeAnalysis.CSharp.Extensions.ITypeSymbolExtensions" + ReflectionNamespaces.CSWorkspacesAsmName, true);
			generateTypeSyntax = typeInfo.GetMethod("GenerateTypeSyntax", new[] { typeof(ITypeSymbol) });
		}

		public static TypeSyntax GenerateTypeSyntax (this ITypeSymbol typeSymbol)
		{
			return (TypeSyntax)generateTypeSyntax.Invoke (null, new [] { typeSymbol });
		}
	}
}

