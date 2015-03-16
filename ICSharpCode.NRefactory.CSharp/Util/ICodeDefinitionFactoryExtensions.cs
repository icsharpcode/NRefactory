using System;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Immutable;
using System.Linq;


namespace ICSharpCode.NRefactory6.CSharp
{
	public static class ICodeDefinitionFactoryExtensions
	{
		readonly static Type typeInfo;

		static ICodeDefinitionFactoryExtensions ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.Shared.Extensions.ICodeDefinitionFactoryExtensions" + ReflectionNamespaces.WorkspacesAsmName, true);
			createFieldDelegatingConstructorMethod = typeInfo.GetMethod ("CreateFieldDelegatingConstructor", BindingFlags.Static | BindingFlags.Public);
			createFieldsForParametersMethod = typeInfo.GetMethod ("CreateFieldsForParameters", BindingFlags.Static | BindingFlags.Public);
			createAssignmentStatementMethod = typeInfo.GetMethod ("CreateAssignmentStatements", BindingFlags.Static | BindingFlags.Public);
			createThrowNotImplementedStatementBlockMethod = typeInfo.GetMethod ("CreateThrowNotImplementedStatementBlock", BindingFlags.Static | BindingFlags.Public);

		}

		public static IList<SyntaxNode> CreateThrowNotImplementedStatementBlock(
			this SyntaxGenerator codeDefinitionFactory,
			Compilation compilation)
		{
			return new[] { CreateThrowNotImplementStatement(codeDefinitionFactory, compilation) };
		}


		static MethodInfo createThrowNotImplementedStatementBlockMethod;
		public static SyntaxNode CreateThrowNotImplementStatement(
			this SyntaxGenerator codeDefinitionFactory,
			Compilation compilation)
		{
			return (SyntaxNode)createThrowNotImplementedStatementBlockMethod.Invoke (null, new object[] { codeDefinitionFactory, compilation });
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

		readonly static MethodInfo createFieldsForParametersMethod;

		public static IEnumerable<IFieldSymbol> CreateFieldsForParameters(
			this SyntaxGenerator factory,
			IList<IParameterSymbol> parameters,
			IDictionary<string, string> parameterToNewFieldMap)
		{
			return (IEnumerable<IFieldSymbol>)createFieldsForParametersMethod.Invoke (null, new object[] {
				factory,
				parameters,
				parameterToNewFieldMap 
			});
		}

		readonly static MethodInfo createAssignmentStatementMethod;
	
		public static IEnumerable<SyntaxNode> CreateAssignmentStatements(
			this SyntaxGenerator factory,
			IList<IParameterSymbol> parameters,
			IDictionary<string, ISymbol> parameterToExistingFieldMap,
			IDictionary<string, string> parameterToNewFieldMap)
		{
			return (IEnumerable<SyntaxNode>)createAssignmentStatementMethod.Invoke (null, new object[] {
				factory,
				parameters,
				parameterToExistingFieldMap,
				parameterToNewFieldMap 
			});
		}

		public static IList<SyntaxNode> CreateArguments(
			this SyntaxGenerator factory,
			ImmutableArray<IParameterSymbol> parameters)
		{
			return parameters.Select(p => CreateArgument(factory, p)).ToList();
		}

		private static SyntaxNode CreateArgument(
			this SyntaxGenerator factory,
			IParameterSymbol parameter)
		{
			return factory.Argument(parameter.RefKind, factory.IdentifierName(parameter.Name));
		}
	}
}
