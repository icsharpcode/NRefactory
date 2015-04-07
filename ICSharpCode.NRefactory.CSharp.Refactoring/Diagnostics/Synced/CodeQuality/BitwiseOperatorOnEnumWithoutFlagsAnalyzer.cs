// 
// BitwiseOperationOnNonFlagsEnumAnalyzer.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang <lightyang0@gmail.com>
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

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class BitwiseOperatorOnEnumWithoutFlagsAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.BitwiseOperatorOnEnumWithoutFlagsAnalyzerID, 
			GettextCatalog.GetString("Bitwise operation on enum which has no [Flags] attribute"),
			GettextCatalog.GetString("Bitwise operation on enum not marked with [Flags] attribute"), 
			DiagnosticAnalyzerCategories.CodeQualityIssues, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.BitwiseOperatorOnEnumWithoutFlagsAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (GetDiagnostic(nodeContext, out diagnostic))
						nodeContext.ReportDiagnostic(diagnostic);
				},
				SyntaxKind.BitwiseNotExpression,

				SyntaxKind.OrAssignmentExpression,
				SyntaxKind.AndAssignmentExpression,
				SyntaxKind.ExclusiveOrAssignmentExpression,

				SyntaxKind.BitwiseAndExpression,
				SyntaxKind.BitwiseOrExpression,
				SyntaxKind.ExclusiveOrExpression
			);
		}

		bool GetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);

			var prefixUnaryExpression = nodeContext.Node as PrefixUnaryExpressionSyntax;
			if (prefixUnaryExpression != null) {
				if (IsNonFlagsEnum (nodeContext.SemanticModel, prefixUnaryExpression.Operand)) {
					diagnostic = Diagnostic.Create (
						descriptor,
						prefixUnaryExpression.OperatorToken.GetLocation ()
					);
					return true;
				}
			}

			var assignmentExpression = nodeContext.Node as AssignmentExpressionSyntax;
			if (assignmentExpression != null) {
				if (IsNonFlagsEnum (nodeContext.SemanticModel, assignmentExpression.Left) || IsNonFlagsEnum (nodeContext.SemanticModel, assignmentExpression.Right)) {
					diagnostic = Diagnostic.Create (
						descriptor,
						assignmentExpression.OperatorToken.GetLocation ()
					);
					return true;
				}
			}

			var binaryExpression = nodeContext.Node as BinaryExpressionSyntax;
			if (binaryExpression != null) {
				if (IsNonFlagsEnum (nodeContext.SemanticModel, binaryExpression.Left) || IsNonFlagsEnum (nodeContext.SemanticModel, binaryExpression.Right)) {
					diagnostic = Diagnostic.Create (
						descriptor,
						binaryExpression.OperatorToken.GetLocation ()
					);
					return true;
				}
			}
			return false;
		}

		static bool IsNonFlagsEnum (SemanticModel semanticModel, ExpressionSyntax expr)
		{
			var type = semanticModel.GetTypeInfo(expr).Type;
			if (type == null || type.TypeKind != TypeKind.Enum)
				return false;

			// check [Flags]
			return !type.GetAttributes().Any (attr => attr.AttributeClass.Name == "FlagsAttribute" && attr.AttributeClass.ContainingNamespace.ToDisplayString() == "System");
		}
	}
}