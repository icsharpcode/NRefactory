using System;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;


namespace ICSharpCode.NRefactory6.CSharp
{
	public static class ICodeDefinitionFactoryExtensions
	{
		readonly static Type typeInfo;

		static ICodeDefinitionFactoryExtensions ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.Shared.Extensions.ICodeDefinitionFactoryExtensions" + ReflectionNamespaces.WorkspacesAsmName, true);
			createFieldDelegatingConstructorMethod = typeInfo.GetMethod ("CreateFieldDelegatingConstructor", BindingFlags.Static | BindingFlags.Public);
		}

		readonly static MethodInfo createFieldDelegatingConstructorMethod;

		public static IEnumerable<ISymbol> CreateFieldDelegatingConstructor(
			this SyntaxGenerator factory,
			string typeName,
			INamedTypeSymbol containingTypeOpt,
			IList<IParameterSymbol> parameters,
			IDictionary<string, ISymbol> parameterToExistingFieldMap,
			IDictionary<string, string> parameterToNewFieldMap,
			CancellationToken cancellationToken)
		{
			return (IEnumerable<ISymbol>)createFieldDelegatingConstructorMethod.Invoke (null, new object[] {
				factory,
				typeName,
				containingTypeOpt,
				parameters,
				parameterToExistingFieldMap,
				parameterToNewFieldMap,
				cancellationToken 
			});
		}

	}
}
