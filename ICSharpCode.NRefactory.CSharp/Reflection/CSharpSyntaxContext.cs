using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.NRefactory6.CSharp
{
	class ReflectionNamespaces
	{
		internal const string WorkspacesAsmName = ", Microsoft.CodeAnalysis.Workspaces, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
		internal const string CSWorkspacesAsmName = ", Microsoft.CodeAnalysis.CSharp.Workspaces, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
		internal const string CAAsmName = ", Microsoft.CodeAnalysis, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
		internal const string CACSharpAsmName = ", Microsoft.CodeAnalysis.CSharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
	}

	public class CSharpSyntaxContext
	{
		readonly static Type typeInfo;
		readonly static MethodInfo createContextMethod;
		readonly static PropertyInfo leftTokenProperty;
		readonly static PropertyInfo targetTokenProperty;
		readonly static FieldInfo isIsOrAsTypeContextField;
		readonly static FieldInfo isInstanceContextField;
		readonly static FieldInfo isNonAttributeExpressionContextField;
		readonly static FieldInfo isPreProcessorKeywordContextField;
		readonly static FieldInfo isPreProcessorExpressionContextField;
		readonly static FieldInfo containingTypeDeclarationField;
		readonly static FieldInfo isGlobalStatementContextField;
		readonly static FieldInfo isParameterTypeContextField;
		readonly static PropertyInfo syntaxTreeProperty;


		object instance;

		public SyntaxToken LeftToken {
			get {
				return (SyntaxToken)leftTokenProperty.GetValue (instance);
			}
		}

		public SyntaxToken TargetToken {
			get {
				return (SyntaxToken)targetTokenProperty.GetValue (instance);
			}
		}

		public bool IsIsOrAsTypeContext {
			get {
				return (bool)isIsOrAsTypeContextField.GetValue (instance);
			}
		}

		public bool IsInstanceContext {
			get {
				return (bool)isInstanceContextField.GetValue (instance);
			}
		}

		public bool IsNonAttributeExpressionContext {
			get {
				return (bool)isNonAttributeExpressionContextField.GetValue (instance);
			}
		}

		public bool IsPreProcessorKeywordContext {
			get {
				return (bool)isPreProcessorKeywordContextField.GetValue (instance);
			}
		}

		public bool IsPreProcessorExpressionContext {
			get {
				return (bool)isPreProcessorExpressionContextField.GetValue (instance);
			}
		}

		public TypeDeclarationSyntax ContainingTypeDeclaration {
			get {
				return (TypeDeclarationSyntax)containingTypeDeclarationField.GetValue (instance);
			}
		}

		public bool IsGlobalStatementContext {
			get {
				return (bool)isGlobalStatementContextField.GetValue (instance);
			}
		}

		public bool IsParameterTypeContext {
			get {
				return (bool)isParameterTypeContextField.GetValue (instance);
			}
		}

		public SyntaxTree SyntaxTree {
			get {
				return (SyntaxTree)syntaxTreeProperty.GetValue (instance);
			}
		}


		readonly static MethodInfo isMemberDeclarationContextMethod;

		public bool IsMemberDeclarationContext (
			ISet<SyntaxKind> validModifiers = null,
			ISet<SyntaxKind> validTypeDeclarations = null,
			bool canBePartial = false,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			return (bool)isMemberDeclarationContextMethod.Invoke (instance, new object[] {
				validModifiers,
				validTypeDeclarations,
				canBePartial,
				cancellationToken
			});
		}

		readonly static MethodInfo isTypeDeclarationContextMethod;

		public bool IsTypeDeclarationContext (
			ISet<SyntaxKind> validModifiers = null,
			ISet<SyntaxKind> validTypeDeclarations = null,
			bool canBePartial = false,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			return (bool)isTypeDeclarationContextMethod.Invoke (instance, new object[] {
				validModifiers,
				validTypeDeclarations,
				canBePartial,
				cancellationToken
			});
		}

		readonly static PropertyInfo isPreProcessorDirectiveContextProperty;

		public bool IsPreProcessorDirectiveContext {
			get {
				return (bool)isPreProcessorDirectiveContextProperty.GetValue (instance);
			}
		}

		readonly static FieldInfo isInNonUserCodeField;

		public bool IsInNonUserCode {
			get {
				return (bool)isInNonUserCodeField.GetValue (instance);
			}
		}

		readonly static FieldInfo isIsOrAsContextField;

		public bool IsIsOrAsContext {
			get {
				return (bool)isIsOrAsContextField.GetValue (instance);
			}
		}

		readonly static MethodInfo isTypeAttributeContextMethod;

		public bool IsTypeAttributeContext (CancellationToken cancellationToken)
		{
			return (bool)isTypeAttributeContextMethod.Invoke (instance, new object[] { cancellationToken });
		}

		readonly static PropertyInfo isAnyExpressionContextProperty;

		public bool IsAnyExpressionContext {
			get {
				return (bool)isAnyExpressionContextProperty.GetValue (instance);
			}
		}

		readonly static PropertyInfo isStatementContextProperty;

		public bool IsStatementContext {
			get {
				return (bool)isStatementContextProperty.GetValue (instance);
			}
		}

		readonly static FieldInfo isDefiniteCastTypeContextField;

		public bool IsDefiniteCastTypeContext {
			get {
				return (bool)isDefiniteCastTypeContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo isObjectCreationTypeContextField;

		public bool IsObjectCreationTypeContext {
			get {
				return (bool)isObjectCreationTypeContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo isGenericTypeArgumentContextField;

		public bool IsGenericTypeArgumentContext {
			get {
				return (bool)isGenericTypeArgumentContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo isLocalVariableDeclarationContextField;

		public bool IsLocalVariableDeclarationContext {
			get {
				return (bool)isLocalVariableDeclarationContextField.GetValue (instance);
			}
		}


		readonly static FieldInfo isFixedVariableDeclarationContextField;

		public bool IsFixedVariableDeclarationContext {
			get {
				return (bool)isFixedVariableDeclarationContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo isPossibleLambdaOrAnonymousMethodParameterTypeContextField;

		public bool IsPossibleLambdaOrAnonymousMethodParameterTypeContext {
			get {
				return (bool)isPossibleLambdaOrAnonymousMethodParameterTypeContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo isImplicitOrExplicitOperatorTypeContextField;

		public bool IsImplicitOrExplicitOperatorTypeContext {
			get {
				return (bool)isImplicitOrExplicitOperatorTypeContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo isPrimaryFunctionExpressionContextField;

		public bool IsPrimaryFunctionExpressionContext {
			get {
				return (bool)isPrimaryFunctionExpressionContextField.GetValue (instance);
			}
		}


		readonly static FieldInfo isCrefContextField;

		public bool IsCrefContext {
			get {
				return (bool)isCrefContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo isDelegateReturnTypeContextField;

		public bool IsDelegateReturnTypeContext {
			get {
				return (bool)isDelegateReturnTypeContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo isEnumBaseListContextField;

		public bool IsEnumBaseListContext {
			get {
				return (bool)isEnumBaseListContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo isConstantExpressionContextField;

		public bool IsConstantExpressionContext {
			get {
				return (bool)isConstantExpressionContextField.GetValue (instance);
			}
		}

		readonly static MethodInfo isMemberAttributeContextMethod;
		public bool IsMemberAttributeContext(ISet<SyntaxKind> validTypeDeclarations, CancellationToken cancellationToken)
		{
			return (bool)isMemberAttributeContextMethod.Invoke (instance, new object[] {
				validTypeDeclarations,
				cancellationToken
			});

		}

		readonly static FieldInfo precedingModifiersField;

		public ISet<SyntaxKind> PrecedingModifiers {
			get {
				return (ISet<SyntaxKind>)precedingModifiersField.GetValue (instance);
			}
		}

		readonly static FieldInfo isTypeOfExpressionContextField;

		public bool IsTypeOfExpressionContext {
			get {
				return (bool)isTypeOfExpressionContextField.GetValue (instance);
			}
		}

		readonly static FieldInfo containingTypeOrEnumDeclarationField;

		public BaseTypeDeclarationSyntax ContainingTypeOrEnumDeclaration {
			get {
				return (BaseTypeDeclarationSyntax)containingTypeOrEnumDeclarationField.GetValue (instance);
			}
		}


		static CSharpSyntaxContext ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery.CSharpSyntaxContext" + ReflectionNamespaces.CSWorkspacesAsmName, true);

			createContextMethod = typeInfo.GetMethod ("CreateContext", BindingFlags.Static | BindingFlags.Public);
			leftTokenProperty = typeInfo.GetProperty ("LeftToken");
			targetTokenProperty = typeInfo.GetProperty ("TargetToken");
			isIsOrAsTypeContextField = typeInfo.GetField ("IsIsOrAsTypeContext");
			isInstanceContextField = typeInfo.GetField ("IsInstanceContext");
			isNonAttributeExpressionContextField = typeInfo.GetField ("IsNonAttributeExpressionContext");
			isPreProcessorKeywordContextField = typeInfo.GetField ("IsPreProcessorKeywordContext");
			isPreProcessorExpressionContextField = typeInfo.GetField ("IsPreProcessorExpressionContext");
			containingTypeDeclarationField = typeInfo.GetField ("ContainingTypeDeclaration");
			isGlobalStatementContextField = typeInfo.GetField ("IsGlobalStatementContext");
			isParameterTypeContextField = typeInfo.GetField ("IsParameterTypeContext");
			isMemberDeclarationContextMethod = typeInfo.GetMethod ("IsMemberDeclarationContext", BindingFlags.Instance | BindingFlags.Public);
			isTypeDeclarationContextMethod = typeInfo.GetMethod ("IsTypeDeclarationContext", BindingFlags.Instance | BindingFlags.Public);
			syntaxTreeProperty = typeInfo.GetProperty ("SyntaxTree");
			isPreProcessorDirectiveContextProperty = typeInfo.GetProperty ("IsPreProcessorDirectiveContext");
			isInNonUserCodeField = typeInfo.GetField ("IsInNonUserCode");
			isIsOrAsContextField = typeInfo.GetField ("IsIsOrAsContext");
			isTypeAttributeContextMethod = typeInfo.GetMethod ("IsTypeAttributeContext", BindingFlags.Instance | BindingFlags.Public);
			isAnyExpressionContextProperty = typeInfo.GetProperty ("IsAnyExpressionContext");
			isStatementContextProperty = typeInfo.GetProperty ("IsStatementContext");
			isDefiniteCastTypeContextField = typeInfo.GetField ("IsDefiniteCastTypeContext");
			isObjectCreationTypeContextField = typeInfo.GetField ("IsObjectCreationTypeContext");
			isGenericTypeArgumentContextField = typeInfo.GetField ("IsGenericTypeArgumentContext");
			isLocalVariableDeclarationContextField = typeInfo.GetField ("IsLocalVariableDeclarationContext");
			isFixedVariableDeclarationContextField = typeInfo.GetField ("IsFixedVariableDeclarationContext");
			isPossibleLambdaOrAnonymousMethodParameterTypeContextField = typeInfo.GetField ("IsPossibleLambdaOrAnonymousMethodParameterTypeContext");
			isImplicitOrExplicitOperatorTypeContextField = typeInfo.GetField ("IsImplicitOrExplicitOperatorTypeContext");
			isPrimaryFunctionExpressionContextField = typeInfo.GetField ("IsPrimaryFunctionExpressionContext");
			isCrefContextField = typeInfo.GetField ("IsCrefContext");
			isDelegateReturnTypeContextField = typeInfo.GetField ("IsDelegateReturnTypeContext");
			isEnumBaseListContextField = typeInfo.GetField ("IsEnumBaseListContext");
			isConstantExpressionContextField = typeInfo.GetField ("IsConstantExpressionContext");
			isMemberAttributeContextMethod = typeInfo.GetMethod ("IsMemberAttributeContext", BindingFlags.Instance | BindingFlags.Public);
			precedingModifiersField = typeInfo.GetField ("PrecedingModifiers");
			isTypeOfExpressionContextField = typeInfo.GetField ("IsTypeOfExpressionContext");
			containingTypeOrEnumDeclarationField = typeInfo.GetField ("ContainingTypeOrEnumDeclaration");
		}

		public SemanticModel SemanticModel {
			get;
			private set;
		}

		public int Position {
			get;
			private set;
		}

		CSharpSyntaxContext (object instance)
		{
			this.instance = instance;
		}

		internal static CSharpSyntaxContext CreateContext (Workspace workspace, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			return new CSharpSyntaxContext (createContextMethod.Invoke (null, new object[] {
				workspace,
				semanticModel,
				position,
				cancellationToken
			})) {
				SemanticModel = semanticModel,
				Position = position
			};
		}
	}

	class CSharpTypeInferenceService
	{
		readonly static Type typeInfo;
		readonly static MethodInfo inferTypesMethod;
		readonly static MethodInfo inferTypes2Method;
		readonly object instance;

		static CSharpTypeInferenceService ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.CSharp.CSharpTypeInferenceService" + ReflectionNamespaces.CSWorkspacesAsmName, true);

			inferTypesMethod = typeInfo.GetMethod ("InferTypes", new[] {
				typeof(SemanticModel),
				typeof(int),
				typeof(CancellationToken)
			});
			inferTypes2Method = typeInfo.GetMethod ("InferTypes", new[] {
				typeof(SemanticModel),
				typeof(SyntaxNode),
				typeof(CancellationToken)
			});
		}

		public CSharpTypeInferenceService ()
		{
			instance = Activator.CreateInstance (typeInfo);
		}

		public IEnumerable<ITypeSymbol> InferTypes (SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			return (IEnumerable<ITypeSymbol>)inferTypesMethod.Invoke (instance, new object[] {
				semanticModel,
				position,
				cancellationToken
			});
		}

		public IEnumerable<ITypeSymbol> InferTypes (SemanticModel semanticModel, SyntaxNode expression, CancellationToken cancellationToken)
		{
			return (IEnumerable<ITypeSymbol>)inferTypes2Method.Invoke (instance, new object[] {
				semanticModel,
				expression,
				cancellationToken
			});
		}


		public ITypeSymbol InferType(
			SemanticModel semanticModel,
			SyntaxNode expression,
			bool objectAsDefault,
			CancellationToken cancellationToken)
		{
			var types = InferTypes(semanticModel, expression, cancellationToken)
				.WhereNotNull();

			if (!types.Any())
			{
				return objectAsDefault ? semanticModel.Compilation.ObjectType : null;
			}

			return types.FirstOrDefault();
		}


	}

	class CaseCorrector
	{
		readonly static Type typeInfo;
		readonly static MethodInfo caseCorrectAsyncMethod;

		static CaseCorrector ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.CaseCorrection.CaseCorrector" + ReflectionNamespaces.WorkspacesAsmName, true);

			Annotation = (SyntaxAnnotation)typeInfo.GetField ("Annotation", BindingFlags.Public | BindingFlags.Static).GetValue (null);

			caseCorrectAsyncMethod = typeInfo.GetMethod ("CaseCorrectAsync", new[] {
				typeof(Document),
				typeof(SyntaxAnnotation),
				typeof(CancellationToken)
			});
		}

		public static readonly SyntaxAnnotation Annotation;

		public static Task<Document> CaseCorrectAsync (Document document, SyntaxAnnotation annotation, CancellationToken cancellationToken)
		{
			return (Task<Document>)caseCorrectAsyncMethod.Invoke (null, new object[] { document, annotation, cancellationToken });
		}
	}


}
