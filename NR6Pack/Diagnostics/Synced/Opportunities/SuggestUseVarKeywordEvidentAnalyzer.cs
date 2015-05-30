//
// UseVarKeywordInspector.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
    /// <summary>
    /// Checks for places where the 'var' keyword can be used. Note that the action is actually done with a context
    /// action.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SuggestUseVarKeywordEvidentAnalyzer : DiagnosticAnalyzer
    {
        static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor(
            NRefactoryDiagnosticIDs.SuggestUseVarKeywordEvidentAnalyzerID,
            GettextCatalog.GetString("Use 'var' keyword when possible"),
            GettextCatalog.GetString("Use 'var' keyword"),
            DiagnosticAnalyzerCategories.Opportunities,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.SuggestUseVarKeywordEvidentAnalyzerID)
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(
                (nodeContext) =>
                {
                    Diagnostic diagnostic;
                    if (TryGetDiagnostic(nodeContext, out diagnostic))
                    {
                        nodeContext.ReportDiagnostic(diagnostic);
                    }
                }, SyntaxKind.LocalDeclarationStatement);
        }

        static bool TryGetDiagnostic(SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
        {
            diagnostic = default(Diagnostic);
            if (nodeContext.IsFromGeneratedCode())
                return false;

            var localVariableStatement = nodeContext.Node as LocalDeclarationStatementSyntax;

            if (localVariableStatement != null)
            {
                var localVariableSyntax = localVariableStatement.Declaration;

                if (!TryValidateLocalVariableType(localVariableStatement, localVariableSyntax))
                    return false;

                if (!DidVariableDeclarationTypeCorrespondToObviousCase(nodeContext, localVariableStatement))
                    return false;

                diagnostic = Diagnostic.Create(descriptor, localVariableSyntax.Type.GetLocation());
            }
            return true;
        }

        static bool TryValidateLocalVariableType(LocalDeclarationStatementSyntax localDeclarationStatementSyntax, VariableDeclarationSyntax variableDeclarationSyntax)
        {
            //Either we don't have a local variable or we're using constant value
            if (localDeclarationStatementSyntax == null ||
                localDeclarationStatementSyntax.IsConst ||
                localDeclarationStatementSyntax.ChildNodes().OfType<VariableDeclarationSyntax>().Count() != 1)
                return false;

            //We don't want to raise a diagnostic if the local variable is already a var
            return !variableDeclarationSyntax.Type.IsVar;
        }

        static bool DidVariableDeclarationTypeCorrespondToObviousCase(SyntaxNodeAnalysisContext nodeContext, LocalDeclarationStatementSyntax localVariable)
        {
            var singleVariable = localVariable.Declaration.Variables.First();
            var initializer = singleVariable.Initializer;
            if (initializer == null)
                return false;
            var initializerExpression = initializer.Value;

            var variableTypeName = localVariable.Declaration.Type;
            var semanticModel = nodeContext.SemanticModel;
            var variableType = semanticModel.GetSymbolInfo(variableTypeName, nodeContext.CancellationToken).Symbol as ITypeSymbol;
            return IsArrayTypeSomeObviousTypeCase(nodeContext, initializerExpression, variableType, localVariable) ||
                IsObjectCreationSomeObviousTypeCase(nodeContext, initializerExpression, variableType) ||
                IsCastingSomeObviousTypeCase(nodeContext, initializerExpression, variableType) /*||
				IsPropertyAccessSomeObviousTypeCase(nodeContext, initializerExpression, variableType)*/;
        }

        static bool IsArrayTypeSomeObviousTypeCase(SyntaxNodeAnalysisContext nodeContext, ExpressionSyntax initializerExpression, ITypeSymbol variableType, LocalDeclarationStatementSyntax localVariable)
        {
            var arrayCreationExpressionSyntax = initializerExpression as ArrayCreationExpressionSyntax;
            if (arrayCreationExpressionSyntax != null)
            {
                if (arrayCreationExpressionSyntax.Type.IsMissing)
                    return false;

                var arrayType = nodeContext.SemanticModel.GetTypeInfo(arrayCreationExpressionSyntax).ConvertedType;
                return arrayType != null && arrayCreationExpressionSyntax.Initializer != null && variableType.Equals(arrayType);
            }

            return false;
        }

        static bool IsObjectCreationSomeObviousTypeCase(SyntaxNodeAnalysisContext nodeContext, ExpressionSyntax initializerExpression, ITypeSymbol variableType)
        {
            var objectCreationExpressionSyntax = initializerExpression as ObjectCreationExpressionSyntax;
            if (objectCreationExpressionSyntax != null)
            {
                var objectType = nodeContext.SemanticModel.GetTypeInfo(objectCreationExpressionSyntax, nodeContext.CancellationToken).ConvertedType;
                return objectType != null && variableType.Equals(objectType);
            }

            return false;
        }

        protected static bool IsPropertyAccessSomeObviousTypeCase(SyntaxNodeAnalysisContext nodeContext, ExpressionSyntax initializerExpression, ITypeSymbol variableType)
        {
            var simpleMemberAccess = initializerExpression as MemberAccessExpressionSyntax;
            if (simpleMemberAccess != null)
            {
                var propertyType = nodeContext.SemanticModel.GetTypeInfo(simpleMemberAccess, nodeContext.CancellationToken).ConvertedType;
                return propertyType != null && variableType.Equals(propertyType);
            }

            return false;
        }

        static bool IsCastingSomeObviousTypeCase(SyntaxNodeAnalysisContext nodeContext, ExpressionSyntax initializerExpression, ITypeSymbol variableType)
        {
            var asBinaryExpression = initializerExpression as BinaryExpressionSyntax;
            if (asBinaryExpression != null && asBinaryExpression.IsKind(SyntaxKind.AsExpression))
            {
                var castType = nodeContext.SemanticModel.GetTypeInfo(asBinaryExpression.Right, nodeContext.CancellationToken).ConvertedType;
                return castType != null && castType.Equals(variableType);
            }
            else if (asBinaryExpression == null)
            {
                var castExpression = initializerExpression as CastExpressionSyntax;
                if (castExpression != null)
                {
                    var castExpressionType = nodeContext.SemanticModel.GetTypeInfo(castExpression, nodeContext.CancellationToken).ConvertedType;
                    return castExpressionType != null && castExpressionType.Equals(variableType);
                }
            }

            return false;
        }
    }
}