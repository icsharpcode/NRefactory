//
// NRefactoryDiagnosticIDs.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	public static class NRefactoryDiagnosticIDs
	{
		public const string PartialTypeWithSinglePartDiagnosticID = "NR0001";
		public const string ConvertClosureToMethodDiagnosticID = "NR0002";
		public const string BaseMethodCallWithDefaultParameterDiagnosticID = "NR0003";
		public const string EmptyConstructorAnalyzerID = "NR0004";
		public const string EmptyDestructorAnalyzerID = "NR0005";
		public const string EmptyNamespaceAnalyzerID = "NR0006";
		public const string EnumUnderlyingTypeIsIntAnalyzerID = "NR0007";
		public const string SealedMemberInSealedClassAnalyzerID = "NR0008";
		public const string NonPublicMethodWithTestAttributeAnalyzerID = "NR0009";
		public const string ConvertConditionalTernaryToNullCoalescingAnalyzerID = "NR0010";
		public const string ConvertIfStatementToConditionalTernaryExpressionAnalyzerID = "NR0011";
		public const string ConvertIfStatementToSwitchStatementAnalyzerID = "NR0012";
		public const string ConvertNullableToShortFormAnalyzerID = "NR0013";
		public const string ConvertToStaticTypeAnalyzerID = "NR0014";
		public const string InvokeAsExtensionMethodAnalyzerID = "NR0015";
		public const string BitwiseOperatorOnEnumWithoutFlagsAnalyzerID = "NR0016";
		public const string CompareNonConstrainedGenericWithNullAnalyzerID = "NR0017";
		public const string CompareOfFloatsByEqualityOperatorAnalyzerID = "NR0018";
		public const string ConditionalTernaryEqualBranchAnalyzerID = "NR0019";
		public const string DelegateSubtractionAnalyzerID = "NR0020";
		public const string DoNotCallOverridableMethodsInConstructorAnalyzerID = "NR0021";
		public const string EmptyGeneralCatchClauseAnalyzerID = "NR0022";
		public const string EventUnsubscriptionViaAnonymousDelegateAnalyzerID = "NR0023";
		public const string LongLiteralEndingLowerLAnalyzerID = "NR0024";
		public const string NonReadonlyReferencedInGetHashCodeAnalyzerID = "NR0025";
		public const string ObjectCreationAsStatementAnalyzerID = "NR0026";
		public const string OperatorIsCanBeUsedAnalyzerID = "NR0027";
		public const string OptionalParameterRefOutAnalyzerID = "NR0028";
		public const string ValueParameterNotUsedAnalyzerID = "NR0029";
		public const string AccessToStaticMemberViaDerivedTypeAnalyzerID = "NR0030";
		public const string BaseMemberHasParamsAnalyzerID = "NR0031";
		public const string ConvertIfDoToWhileAnalyzerID = "NR0032";
		public const string ConvertIfToOrExpressionAnalyzerID = "NR0033";
		public const string EmptyEmbeddedStatementAnalyzerID = "NR0034";
		public const string PossibleMistakenCallToGetTypeAnalyzerID = "NR0035";
		public const string ReplaceWithFirstOrDefaultAnalyzerID = "NR0036";
		public const string ReplaceWithLastOrDefaultAnalyzerID = "NR0037";
		public const string ReplaceWithOfTypeAnalyzerID = "NR0038";
		public const string ReplaceWithOfTypeAnyAnalyzerID = "NR0039";
		public const string ReplaceWithOfTypeCountAnalyzerID = "NR0040";
		public const string ReplaceWithOfTypeFirstAnalyzerID = "NR0041";
		public const string ReplaceWithOfTypeFirstOrDefaultAnalyzerID = "NR0042";
		public const string ReplaceWithOfTypeLastAnalyzerID = "NR0043";
		public const string ReplaceWithOfTypeLastOrDefaultAnalyzerID = "NR0044";
		public const string ReplaceWithOfTypeLongCountAnalyzerID = "NR0045";
		public const string ReplaceWithOfTypeSingleAnalyzerID = "NR0046";
		public const string ReplaceWithOfTypeSingleOrDefaultAnalyzerID = "NR0047";
		public const string ReplaceWithOfTypeWhereAnalyzerID = "NR0048";
		public const string ReplaceWithSimpleAssignmentAnalyzerID = "NR0049";
		public const string ReplaceWithSingleCallToAnyAnalyzerID = "NR0050";
		public const string ReplaceWithSingleCallToCountAnalyzerID = "NR0051";
		public const string ReplaceWithSingleCallToFirstAnalyzerID = "NR0052";
		public const string ReplaceWithSingleCallToFirstOrDefaultAnalyzerID = "NR0053";
		public const string ReplaceWithSingleCallToLastAnalyzerID = "NR0054";
		public const string ReplaceWithSingleCallToLastOrDefaultAnalyzerID = "NR0055";
		public const string ReplaceWithSingleCallToLongCountAnalyzerID = "NR0056";
		public const string ReplaceWithSingleCallToSingleAnalyzerID = "NR0057";
		public const string ReplaceWithSingleCallToSingleOrDefaultAnalyzerID = "NR0058";
		public const string SimplifyConditionalTernaryExpressionAnalyzerID = "NR0059";
		public const string StringIndexOfIsCultureSpecificAnalyzerID = "NR0060";
		public const string StringEndsWithIsCultureSpecificAnalyzerID = "NR0061";
		public const string StringLastIndexOfIsCultureSpecificAnalyzerID = "NR0062";
		public const string StringStartsWithIsCultureSpecificAnalyzerID = "NR0063";
		public const string StringCompareToIsCultureSpecificAnalyzerID = "NR0064";
		public const string ConditionIsAlwaysTrueOrFalseAnalyzerID = "NR0065";
		public const string DoubleNegationOperatorAnalyzerID = "NR0066";
		public const string EmptyStatementAnalyzerID = "NR0067";
		public const string ForStatementConditionIsTrueAnalyzerID = "NR0068";
		public const string RedundantAnonymousTypePropertyNameAnalyzerID = "NR0069";
		public const string RedundantArgumentNameAnalyzerID = "NR0070";
		public const string RedundantAttributeParenthesesAnalyzerID = "NR0071";
		public const string RedundantBaseQualifierAnalyzerID = "NR0072";
		public const string RedundantCaseLabelAnalyzerID = "NR0073";
		public const string RedundantEmptyDefaultSwitchBranchAnalyzerID = "NR0074";
		public const string RedundantTernaryExpressionAnalyzerID = "NR0075";
		public const string RemoveRedundantOrStatementAnalyzerID = "NR0076";
		public const string NegativeRelationalExpressionAnalyzerID = "NR0077";
		public const string RedundantExplicitArrayCreationAnalyzerID = "NR0078";
		public const string RedundantLogicalConditionalExpressionOperandAnalyzerID = "NR0079";
		public const string AdditionalOfTypeAnalyzerID = "NR0080";
		public const string XmlDocAnalyzerID = "NR0081";
		public const string ParameterHidesMemberAnalyzerID = "NR0082";
		public const string NotImplementedExceptionAnalyzerID = "NR0083";
		public const string RedundantAssignmentAnalyzerID = "NR0084";
		public const string ArrayCreationCanBeReplacedWithArrayInitializerAnalyzerID = "NR0085";
		public const string RedundantEnumerableCastCallAnalyzerID = "NR0086";
		public const string SimplifyLinqExpressionAnalyzerID = "NR0087";
		public const string EqualExpressionComparisonAnalyzerID = "NR0088";
		public const string PolymorphicFieldLikeEventInvocationAnalyzerID = "NR0089";
		public const string ForCanBeConvertedToForeachAnalyzerID = "NR0090";
		public const string SuggestUseVarKeywordEvidentAnalyzerID = "NR0091";
		public const string FieldCanBeMadeReadOnlyAnalyzerID = "NR0092";
		public const string ConvertIfToAndExpressionAnalyzerID = "NR0093";
		public const string RedundantLambdaParameterTypeAnalyzerID = "NR0094";
		public const string LockThisAnalyzerID = "NR0095";
		public const string UnusedTypeParameterAnalyzerID = "NR0096";
		public const string MemberCanBeMadeStaticAnalyzerID = "NR0097";
		public const string ConstantNullCoalescingConditionAnalyzerID = "NR0098";
		public const string ParameterOnlyAssignedAnalyzerID = "NR0099";
		public const string RedundantComparisonWithNullAnalyzerID = "NR0100";
		public const string RedundantExtendsListEntryAnalyzerID = "NR0101";
		public const string ParameterCanBeDeclaredWithBaseTypeAnalyzerID = "NR0102";
		public const string RedundantExplicitArraySizeAnalyzerID = "NR0103";
		public const string RedundantObjectCreationArgumentListAnalyzerID = "NR0104";
		public const string CanBeReplacedWithTryCastAndCheckForNullAnalyzerID = "NR0105";
		public const string RedundantToStringCallAnalyzerID = "NR0106";
		public const string RedundantDelegateCreationAnalyzerID = "NR0107";
		public const string StaticFieldInGenericTypeAnalyzerID = "NR0108";
		public const string RedundantCatchClauseAnalyzerID = "NR0109";
		public const string ConstantConditionAnalyzerID = "NR0110";
		public const string PossibleAssignmentToReadonlyFieldAnalyzerID = "NR0111";
		public const string StaticEventSubscriptionAnalyzerID = "NR0112";
		public const string RedundantCommaInArrayInitializerAnalyzerID = "NR0113";
		public const string UseMethodIsInstanceOfTypeAnalyzerID = "NR0114";
		public const string UnassignedReadonlyFieldAnalyzerID = "NR0115";
		public const string UseMethodAnyAnalyzerID = "NR0116";
		public const string LocalVariableHidesMemberAnalyzerID = "NR0117";
		public const string RedundantEmptyFinallyBlockAnalyzerID = "NR0118";
		public const string StringCompareIsCultureSpecificAnalyzerID = "NR0119";
		public const string PublicConstructorInAbstractClassAnalyzerID = "NR0120";
		public const string RedundantBoolCompareAnalyzerID = "NR0121";
		public const string RedundantDefaultFieldInitializerAnalyzerID = "NR0122";
		public const string RedundantStringToCharArrayCallAnalyzerID = "NR0123";
		public const string OptionalParameterHierarchyMismatchAnalyzerID = "NR0124";
		public const string CheckNamespaceAnalyzerID = "NR0125";
		public const string RedundantBaseConstructorCallAnalyzerID = "NR0126";
		public const string ReplaceWithStringIsNullOrEmptyAnalyzerID = "NR0127";
		public const string UnusedAnonymousMethodSignatureAnalyzerID = "NR0128";
		public const string RedundantInternalAnalyzerID = "NR0129";
		public const string ForControlVariableIsNeverModifiedAnalyzerID = "NR0130";
		public const string ReferenceEqualsWithValueTypeAnalyzerID = "NR0131";
		public const string ConvertToLambdaExpressionAnalyzerID = "NR0132";
		public const string BaseMethodParameterNameMismatchAnalyzerID = "NR0133";
		public const string RedundantCheckBeforeAssignmentAnalyzerID = "NR0134";
		public const string FunctionNeverReturnsAnalyzerID = "NR0135";
		public const string PartialMethodParameterNameMismatchAnalyzerID = "NR0136";
		public const string MethodOverloadWithOptionalParameterAnalyzerID = "NR0137";
		public const string RedundantExplicitNullableCreationAnalyzerID = "NR0138";
		public const string PossibleMultipleEnumerationAnalyzerID = "NR0139";
		public const string RedundantLambdaSignatureParenthesesAnalyzerID = "NR0140";
		public const string RedundantArgumentDefaultValueAnalyzerID = "NR0141";
		public const string UseArrayCreationExpressionAnalyzerID = "NR0142";
		public const string NotResolvedInTextAnalyzerID = "NR0143";
		public const string RedundantObjectOrCollectionInitializerAnalyzerID = "NR0144";
		public const string RedundantPrivateAnalyzerID = "NR0145";
		public const string MemberHidesStaticFromOuterClassAnalyzerID = "NR0146";
		public const string RedundantIfElseBlockAnalyzerID = "NR0147";
		public const string UseIsOperatorAnalyzerID = "NR0148";
		public const string CallToObjectEqualsViaBaseAnalyzerID = "NR0149";
		public const string RedundantToStringCallForValueTypesAnalyzerID = "NR0150";
		public const string RedundantParamsAnalyzerID = "NR0151";
		public const string ConvertIfStatementToNullCoalescingExpressionAnalyzerID = "NR0152";
		public const string ConvertToConstantAnalyzerID = "NR0153";
		public const string UnusedParameterAnalyzerID = "NR0154";
		public const string FormatStringProblemAnalyzerID = "NR0155";
		public const string LocalVariableNotUsedAnalyzerID = "NR0156";
		public const string ThreadStaticAtInstanceFieldAnalyzerID = "NR0157";
		public const string RedundantUnsafeContextAnalyzerID = "NR0158";
		public const string RedundantOverriddenMemberAnalyzerID = "NR0159";
		public const string RewriteIfReturnToReturnAnalyzerID = "NR0160";
		public const string ConvertToAutoPropertyAnalyzerID = "NR0161";
		public const string NotResolvedInTextAnalyzer_SwapID = "NR0162";
	}
}