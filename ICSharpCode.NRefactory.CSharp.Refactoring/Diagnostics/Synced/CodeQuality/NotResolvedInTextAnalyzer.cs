//
// ExceptionParameterCantBeResolvedAnalyzer.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NotResolvedInTextAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.NotResolvedInTextAnalyzerID, 
			GettextCatalog.GetString("Cannot resolve symbol in text argument"),
			GettextCatalog.GetString("The parameter '{0}' can't be resolved"), 
			DiagnosticAnalyzerCategories.CodeQualityIssues, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.NotResolvedInTextAnalyzerID)
		);

		static readonly DiagnosticDescriptor descriptor2 = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.NotResolvedInTextAnalyzer_SwapID, 
			GettextCatalog.GetString("The parameter name is on the wrong argument"),
			GettextCatalog.GetString("The parameter name is on the wrong argument"), 
			DiagnosticAnalyzerCategories.CodeQualityIssues, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.NotResolvedInTextAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor, descriptor2);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryGetDiagnostic (nodeContext, out diagnostic)) {
						nodeContext.ReportDiagnostic(diagnostic);
					}
				}, 
				new SyntaxKind[] { SyntaxKind.ObjectCreationExpression }
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;
			var objectCreateExpression = nodeContext.Node as ObjectCreationExpressionSyntax;

			ExpressionSyntax paramNode;
			ExpressionSyntax altParamNode;
			bool canAddParameterName;
			if (!CheckExceptionType(nodeContext.SemanticModel, objectCreateExpression, out paramNode, out altParamNode, out canAddParameterName))
				return false;

			var paramName = GetArgumentParameterName(paramNode);
			if (paramName == null)
				return false;
			var validNames = GetValidParameterNames(objectCreateExpression);

			if (!validNames.Contains(paramName)) {
				// Case 1: Parameter name is swapped
				var altParamName = GetArgumentParameterName(altParamNode);
				if (altParamName != null && validNames.Contains(altParamName)) {
					diagnostic = Diagnostic.Create (descriptor2, altParamNode.GetLocation ());
					return true;
				}
				//var guessName = GuessParameterName(nodeContext.SemanticModel, objectCreateExpression, validNames);

				// General case: mark only
				diagnostic = Diagnostic.Create (descriptor, paramNode.GetLocation (), paramName);
				return true;
			}
			return false;
		}

		internal static string GetArgumentParameterName(SyntaxNode expression)
		{
			var pExpr = expression as LiteralExpressionSyntax;
			if (pExpr != null)
				return pExpr.Token.Value.ToString ();
			return null;
		}

		internal static bool CheckExceptionType(SemanticModel model, ObjectCreationExpressionSyntax objectCreateExpression, out ExpressionSyntax paramNode, out ExpressionSyntax altParam, out bool canAddParameterName)
		{
			paramNode = null;
			altParam = null;
			canAddParameterName = false;
			var type = model.GetTypeInfo(objectCreateExpression).Type;
			if (type == null)
				return false;
			if (type.Name == typeof(ArgumentException).Name) {
				if (objectCreateExpression.ArgumentList.Arguments.Count >= 2) {
					altParam = objectCreateExpression.ArgumentList.Arguments[0].Expression;
					paramNode = objectCreateExpression.ArgumentList.Arguments[1].Expression;
				}
				return paramNode != null;
			}
			if (type.Name == typeof(ArgumentNullException).Name ||
			    type.Name == typeof(ArgumentOutOfRangeException).Name ||
			    type.Name == typeof(DuplicateWaitObjectException).Name) {
				canAddParameterName = objectCreateExpression.ArgumentList.Arguments.Count == 1;
				if (objectCreateExpression.ArgumentList.Arguments.Count >= 1) {
					paramNode = objectCreateExpression.ArgumentList.Arguments[0].Expression;
					if (objectCreateExpression.ArgumentList.Arguments.Count == 2) {
						altParam = objectCreateExpression.ArgumentList.Arguments[1].Expression;
						if (model.GetTypeInfo(altParam).Type.SpecialType != SpecialType.System_String)
							paramNode = null;
					}
					if (objectCreateExpression.ArgumentList.Arguments.Count == 3)
						altParam = objectCreateExpression.ArgumentList.Arguments[2].Expression;
				}
				return paramNode != null;
			}
			return false;
		}

		internal static List<string> GetValidParameterNames(ObjectCreationExpressionSyntax objectCreateExpression)
		{
			var names = new List<string>();
			var node = objectCreateExpression.Parent;
			while (node != null && !(node is BaseTypeDeclarationSyntax) && !(node is AnonymousObjectCreationExpressionSyntax)) {
				var lambda = node as ParenthesizedLambdaExpressionSyntax;
				if (lambda != null)
					names.AddRange(lambda.ParameterList.Parameters.Select(p => p.Identifier.ToString ()));

				var lambda2 = node as SimpleLambdaExpressionSyntax;
				if (lambda2 != null && lambda2.Parameter != null)
					names.Add(lambda2.Parameter.Identifier.ToString ());
				
				var anonymousMethod = node as AnonymousMethodExpressionSyntax;
				if (anonymousMethod != null)
					names.AddRange(anonymousMethod.ParameterList.Parameters.Select(p => p.Identifier.ToString ()));

				var indexer = node as IndexerDeclarationSyntax;
				if (indexer != null) {
					names.AddRange(indexer.ParameterList.Parameters.Select(p => p.Identifier.ToString ()));
					break;
				}

				var methodDeclaration = node as MethodDeclarationSyntax;
				if (methodDeclaration != null) {
					names.AddRange(methodDeclaration.ParameterList.Parameters.Select(p => p.Identifier.ToString ()));
					break;
				}

				var constructorDeclaration = node as ConstructorDeclarationSyntax;
				if (constructorDeclaration != null) {
					names.AddRange(constructorDeclaration.ParameterList.Parameters.Select(p => p.Identifier.ToString ()));
					break;
				}
				var accessor = node as AccessorDeclarationSyntax;
				if (accessor != null) {
					if (accessor.IsKind (SyntaxKind.SetAccessorDeclaration) ||
					    accessor.IsKind (SyntaxKind.AddAccessorDeclaration) ||
					    accessor.IsKind (SyntaxKind.RemoveAccessorDeclaration)) {
						names.Add("value");
					}
					break;
				}
				node = node.Parent;
			}
			return names;
		}

		static string GetParameterName(SemanticModel model, ExpressionSyntax expr)
		{
			foreach (var node in expr.DescendantNodesAndSelf ()) {
				if (!(node is ExpressionSyntax))
					continue;
				var rr = model.GetSymbolInfo (node).Symbol as IParameterSymbol;
				if (rr != null)
					return rr.Name;
			}
			return null;
		}

		internal static string GuessParameterName(SemanticModel model, ObjectCreationExpressionSyntax objectCreateExpression, List<string> validNames)
		{
			if (validNames.Count == 1)
				return validNames[0];
			var parent = objectCreateExpression.Ancestors ().OfType<IfStatementSyntax> ().FirstOrDefault ();
			if (parent == null)
				return null;
			return GetParameterName(model, parent.Condition);
		}
	}
}