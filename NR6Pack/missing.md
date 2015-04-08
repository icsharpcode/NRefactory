Refactorings
============

Refactorings not implemented (and added)
-----------------------------------------

* AutoLinqSumAction
* ChangeAccessModifierAction
* CreateDelegateAction
* CreateIndexerAction
* CreateOverloadWithoutParameterAction
* ExtractAnonymousMethodAction
* ExtractFieldAction
* GenerateGetterAction
* GeneratePropertyAction
* IntroduceConstantAction
* IterateViaForeachAction
* LinqFluentToQueryAction
* LinqQueryToFluentAction
* MergeNestedIfAction
* MoveToOuterScopeAction
* PutInsideUsingAction
* SplitVariableIntoSeveralOnesCodeRefactoringProvider
* UseStringFormatAction


Analyzers
=========

Analyzers not implemented
--------------------------

*Code Quality*

* CanBeReplacedWithTryCastAndCheckForNullAnalyzer
* EqualExpressionComparisonAnalyzer
* **ForControlVariableIsNeverModifiedAnalyzer**
* FormatStringProblemAnalyzer
* FunctionNeverReturnsAnalyzer
* LocalVariableHidesMemberAnalyzer
* MemberHidesStaticFromOuterClassAnalyzer
* MethodOverloadWithOptionalParameterAnalyzer
* **NotResolvedInTextAnalyzer**
* **OptionalParameterHierarchyMismatchAnalyzer**
* ParameterHidesMemberAnalyzer
* PartialMethodParameterNameMismatchAnalyzer
* PolymorphicFieldLikeEventInvocationAnalyzer
* PossibleAssignmentToReadonlyFieldAnalyzer
* **PossibleMultipleEnumerationAnalyzer**
* StaticFieldInGenericTypeAnalyzer
* ThreadStaticAtInstanceFieldAnalyzer

*Compiler Errors*

* ProhibitedModifiersAnalyzer

*Compiler Warnings*

* CS0183ExpressionIsAlwaysOfProvidedTypeAnalyzer
* CS1573ParameterHasNoMatchingParamTagAnalyzer
* CS1717AssignmentMadeToSameVariableAnalyzer
* UnassignedReadonlyFieldAnalyzer

*Opportunities*

* ConvertIfStatementToNullCoalescingExpressionAnalyzer
* ConvertToAutoPropertyAnalyzer
* ConvertToLambdaExpressionAnalyzer
* ForCanBeConvertedToForeachAnalyzer
* RewriteIfReturnToReturnAnalyzer
* SuggestUseVarKeywordEvidentAnalyzer

*Practices and Improvements*

* ConvertToConstantAnalyzer
* FieldCanBeMadeReadOnlyAnalyzer
* MemberCanBeMadeStaticAnalyzer
* ParameterCanBeDeclaredWithBaseTypeAnalyzer
* PublicConstructorInAbstractClassAnalyzer
* ReferenceEqualsWithValueTypeAnalyzer
* ReplaceWithOfTypeAnyAnalyzer
* ReplaceWithOfTypeAnalyzer
* ReplaceWithStringIsNullOrEmptyAnalyzer
* SimplifyLinqExpressionAnalyzer
* **StringCompareIsCultureSpecificAnalyzer**
* **StringCompareToIsCultureSpecificAnalyzer**
* **StringIndexOfIsCultureSpecificAnalyzer**
* **UseArrayCreationExpressionAnalyzer**
* UseIsOperatorAnalyzer
* **UseMethodAnyAnalyzer**
* UseMethodIsInstanceOfTypeAnalyzer

*Redundancies in Code*

* **ConstantNullCoalescingConditionAnalyzer**
* RedundantArgumentDefaultValueAnalyzer
* RedundantBoolCompareAnalyzer
* **RedundantCatchClauseAnalyzer**
* RedundantCheckBeforeAssignmentAnalyzer
* RedundantCommaInArrayInitializerAnalyzer
* RedundantComparisonWithNullAnalyzer
* RedundantDelegateCreationAnalyzer
* **RedundantEmptyFinallyBlockAnalyzer**
* RedundantEnumerableCastCallAnalyzer
* RedundantExplicitArrayCreationAnalyzer
* RedundantExplicitArraySizeAnalyzer
* RedundantExplicitNullableCreationAnalyzer
* RedundantExtendsListEntryAnalyzer
* **RedundantIfElseBlockAnalyzer**
* RedundantLambdaParameterTypeAnalyzer
* RedundantLambdaSignatureParenthesesAnalyzer
* RedundantLogicalConditionalExpressionOperandAnalyzer
* RedundantObjectCreationArgumentListAnalyzer
* RedundantObjectOrCollectionInitializerAnalyzer
* RedundantStringToCharArrayCallAnalyzer
* RedundantToStringCallForValueTypesAnalyzer
* RedundantToStringCallAnalyzer
* RedundantUnsafeContextAnalyzer
* **UnusedAnonymousMethodSignatureAnalyzer**

*Redundancies in Declaration*

* PartialTypeWithSinglePartAnalyzer
* RedundantBaseConstructorCallAnalyzer
* RedundantDefaultFieldInitializerAnalyzer
* RedundantOverridenMemberAnalyzer
* RedundantParamsAnalyzer
* UnusedParameterAnalyzer
* **UnusedTypeParameterAnalyzer**

*Custom*

* AdditionalOfTypeAnalyzer
* CheckNamespaceAnalyzer
* LockThisAnalyzer
* NegativeRelationalExpressionAnalyzer
* ParameterOnlyAssignedAnalyzer
* RedundantAssignmentAnalyzer
* StaticEventSubscriptionAnalyzer
* XmlDocAnalyzer
