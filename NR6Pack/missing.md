CodeActions
===========

Actions not implemented (and added)
-----------------------------------

* AddExceptionDescriptionAction
* AutoLinqSumAction
* ChangeAccessModifierAction
* ConvertIfStatementToSwitchStatementAction
* CopyCommentsFromBase
* CopyCommentsFromInterface
* CreateClassDeclarationAction
* CreateConstructorDeclarationAction
* CreateDelegateAction
* CreateEnumValue
* CreateEventInvocatorAction
* CreateFieldAction
* CreateIndexerAction
* CreateMethodDeclarationAction
* CreateOverloadWithoutParameterAction
* CreatePropertyAction
* CS1520MethodMustHaveAReturnTypeAction
* ExtractAnonymousMethodAction
* ExtractFieldAction
* GenerateGetterAction
* GeneratePropertyAction
* ImplementAbstractMembersAction
* ImplementInterfaceAction
* ImplementInterfaceExplicitAction
* IntroduceConstantAction
* IterateViaForeachAction
* LinqFluentToQueryAction
* LinqQueryToFluentAction
* MergeNestedIfAction
* MoveToOuterScopeAction
* PutInsideUsingAction
* RemoveRedundantCatchTypeAction
* ReverseDirectionForForLoopAction
* SplitDeclarationListAction
* SplitStringAction
* UseStringFormatAction

Broken Actions (buggy)
----------------------

* InlineLocalVariableAction
* IntroduceFormatItemAction
* InsertAnonymousMethodSignatureAction

Duplication
-----------

* ImplementNotImplementedProperty vs. CreateBackingStore

CodeIssues
==========

Issues not implemented
----------------------

*Code Quality*

* CanBeReplacedWithTryCastAndCheckForNullIssue
* EqualExpressionComparisonIssue
* EventUnsubscriptionViaAnonymousDelegateIssue
* ForControlVariableIsNeverModifiedIssue
* FormatStringProblemIssue
* LocalVariableHidesMemberIssue
* MemberHidesStaticFromOuterClassIssue
* MethodOverloadWithOptionalParameterIssue
* NotResolvedInTextIssue
* OptionalParameterHierarchyMismatchIssue
* ParameterHidesMemberIssue
* PartialMethodParameterNameMismatchIssue
* PolymorphicFieldLikeEventInvocationIssue
* PossibleAssignmentToReadonlyFieldIssue
* PossibleMultipleEnumerationIssue
* StaticFieldInGenericTypeIssue
* ThreadStaticAtInstanceFieldIssue
* ValueParameterNotUsedIssue

*Compiler Errors*

* ProhibitedModifiersIssue

*Compiler Warnings*

* CS0183ExpressionIsAlwaysOfProvidedTypeIssue
* CS1573ParameterHasNoMatchingParamTagIssue
* CS1717AssignmentMadeToSameVariableIssue
* UnassignedReadonlyFieldIssue

*Opportunities*

* ConvertIfStatementToConditionalTernaryExpressionIssue
* ConvertIfStatementToNullCoalescingExpressionIssue
* ConvertIfStatementToSwitchStatementIssue
* ConvertToAutoPropertyIssue
* ConvertToLambdaExpressionIssue
* ForCanBeConvertedToForeachIssue
* RewriteIfReturnToReturnIssue
* SuggestUseVarKeywordEvidentIssue

*Practices and Improvements*

* ConvertIfToOrExpressionIssue
* ConvertToConstantIssue
* FieldCanBeMadeReadOnlyIssue
* MemberCanBeMadeStaticIssue
* ParameterCanBeDeclaredWithBaseTypeIssue
* PublicConstructorInAbstractClassIssue
* ReferenceEqualsWithValueTypeIssue
* ReplaceWithOfTypeAnyIssue
* ReplaceWithOfTypeIssue
* ReplaceWithSingleCallToAnyIssue
* ReplaceWithStringIsNullOrEmptyIssue
* SimplifyConditionalTernaryExpressionIssue
* SimplifyLinqExpressionIssue
* StringCompareIsCultureSpecificIssue
* StringCompareToIsCultureSpecificIssue
* StringIndexOfIsCultureSpecificIssue
* UseArrayCreationExpressionIssue
* UseIsOperatorIssue
* UseMethodAnyIssue
* UseMethodIsInstanceOfTypeIssue

*Redundancies in Code*

* ConstantNullCoalescingConditionIssue
* RedundantArgumentDefaultValueIssue
* RedundantBoolCompareIssue
* RedundantCatchClauseIssue
* RedundantCheckBeforeAssignmentIssue
* RedundantCommaInArrayInitializerIssue
* RedundantComparisonWithNullIssue
* RedundantDelegateCreationIssue
* RedundantEmptyFinallyBlockIssue
* RedundantEnumerableCastCallIssue
* RedundantExplicitArrayCreationIssue
* RedundantExplicitArraySizeIssue
* RedundantExplicitNullableCreationIssue
* RedundantExtendsListEntryIssue
* RedundantIfElseBlockIssue
* RedundantLambdaParameterTypeIssue
* RedundantLambdaSignatureParenthesesIssue
* RedundantLogicalConditionalExpressionOperandIssue
* RedundantObjectCreationArgumentListIssue
* RedundantObjectOrCollectionInitializerIssue
* RedundantStringToCharArrayCallIssue
* RedundantToStringCallForValueTypesIssue
* RedundantToStringCallIssue
* RedundantUnsafeContextIssue
* RedundantUsingDirectiveIssue
* UnusedAnonymousMethodSignatureIssue

*Redundancies in Declaration*

* PartialTypeWithSinglePartIssue
* RedundantBaseConstructorCallIssue
* RedundantDefaultFieldInitializerIssue
* RedundantOverridenMemberIssue
* RedundantParamsIssue
* UnusedLabelIssue
* UnusedParameterIssue
* UnusedTypeParameterIssue

Duplication
-----------

* FunctionNeverReturnsIssue
* ProhibitedModifiersIssue
* RedundantThisQualifierIssue
* LocalVariableNotUsedIssue
