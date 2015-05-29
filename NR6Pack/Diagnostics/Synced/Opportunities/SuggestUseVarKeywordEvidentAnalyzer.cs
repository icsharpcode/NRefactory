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
    [NotPortedYet]
    public class SuggestUseVarKeywordEvidentAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor(
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

        private static bool TryGetDiagnostic(SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
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

                if (!TryFindObviousTypeCase(localVariableStatement, nodeContext.SemanticModel))
                    return false;

                diagnostic = Diagnostic.Create(descriptor, localVariableSyntax.Type.GetLocation());
            }
            return true;
        }

        private static bool TryValidateLocalVariableType(LocalDeclarationStatementSyntax localDeclarationStatementSyntax,
            VariableDeclarationSyntax variableDeclarationSyntax)
        {
            //Either we don't have a local variable or we're using constant value
            if (localDeclarationStatementSyntax == null ||
                localDeclarationStatementSyntax.IsConst ||
                localDeclarationStatementSyntax.ChildNodes().OfType<VariableDeclarationSyntax>().Count() != 1)
                return false;

            //We don't want to raise a diagnostic if the local variable is already a var
            return !variableDeclarationSyntax.Type.IsVar;
        }

        private static bool TryFindObviousTypeCase(LocalDeclarationStatementSyntax localVariable, SemanticModel semanticModel)
        {
            var singleVariable = localVariable.Declaration.Variables.First();
            var initializer = singleVariable.Initializer;
            var initializerExpression = initializer.Value;

            var variableTypeName = localVariable.Declaration.Type;
            var variableType = semanticModel.GetTypeInfo(variableTypeName).ConvertedType;

            return TryValidateArrayCreationSyntaxType(initializerExpression, semanticModel, variableType) ||
                   TryValidateObjectCreationSyntaxType(initializerExpression, semanticModel, variableType);
        }

        private static bool TryValidateArrayCreationSyntaxType(ExpressionSyntax initializerExpression,
            SemanticModel semanticModel, ITypeSymbol variableType)
        {
            var arrayCreationExpressionSyntax = initializerExpression as ArrayCreationExpressionSyntax;
            if (arrayCreationExpressionSyntax != null)
            {
                var arrayType = semanticModel.GetTypeInfo(arrayCreationExpressionSyntax).ConvertedType;
                return arrayType != null && variableType.Equals(arrayType);
            }
            return false;
        }

        private static bool TryValidateObjectCreationSyntaxType(ExpressionSyntax initializerExpression,
            SemanticModel semanticModel, ITypeSymbol variableType)
        {
            var objectCreationExpressionSyntax = initializerExpression as ObjectCreationExpressionSyntax;
            if (objectCreationExpressionSyntax != null)
            {
                var objectType = semanticModel.GetTypeInfo(objectCreationExpressionSyntax).ConvertedType;
                return objectType != null && variableType.Equals(objectType);
            }
            return false;
        }


        //		class GatherVisitor : GatherVisitorBase<SuggestUseVarKeywordEvidentAnalyzer>
        //		{
        //			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        //				: base (semanticModel, addDiagnostic, cancellationToken)
        //			{
        //			}
        ////
        ////			public override void VisitSyntaxTree(SyntaxTree syntaxTree)
        ////			{
        ////				if (!ctx.Supports(UseVarKeywordAction.minimumVersion))
        ////					return;
        ////				base.VisitSyntaxTree(syntaxTree);
        ////			}
        ////
        ////			public override void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
        ////			{
        ////				base.VisitVariableDeclarationStatement(variableDeclarationStatement);
        ////				if (variableDeclarationStatement.Type is PrimitiveType) {
        ////					return;
        ////				}
        ////				if (variableDeclarationStatement.Type.IsVar()) {
        ////					return;
        ////				}
        ////				if (variableDeclarationStatement.Variables.Count != 1) {
        ////					return;
        ////				}
        ////
        ////				//only checks for cases where the type would be obvious - assignment of new, cast, etc.
        ////				//also check the type actually matches else the user might want to assign different subclasses later
        ////				var v = variableDeclarationStatement.Variables.Single();
        ////
        ////				var arrCreate = v.Initializer as ArrayCreateExpression;
        ////				if (arrCreate != null) {
        ////					var n = variableDeclarationStatement.Type as ComposedType;
        ////					//FIXME: check the specifier compatibility
        ////					if (n != null && n.ArraySpecifiers.Any() && n.BaseType.IsMatch(arrCreate.Type)) {
        ////						AddDiagnosticAnalyzer(variableDeclarationStatement);
        ////					}
        ////				}
        ////				var objCreate = v.Initializer as ObjectCreateExpression;
        ////				if (objCreate != null && objCreate.Type.IsMatch(variableDeclarationStatement.Type)) {
        ////					AddDiagnosticAnalyzer(variableDeclarationStatement);
        ////				}
        ////				var asCast = v.Initializer as AsExpression;
        ////				if (asCast != null && asCast.Type.IsMatch(variableDeclarationStatement.Type)) {
        ////					AddDiagnosticAnalyzer(variableDeclarationStatement);
        ////				}
        ////				var cast = v.Initializer as CastExpression;
        ////				if (cast != null && cast.Type.IsMatch(variableDeclarationStatement.Type)) {
        ////					AddDiagnosticAnalyzer(variableDeclarationStatement);
        ////				}
        ////			}
        ////
        ////			void AddDiagnosticAnalyzer(VariableDeclarationStatement variableDeclarationStatement)
        ////			{
        ////				AddDiagnosticAnalyzer(new CodeIssue(variableDeclarationStatement.Type, ctx.TranslateString("")) { IssueMarker = IssueMarker.DottedLine, ActionProvider = { typeof(UseVarKeywordAction) } });
        ////			}
        //		}
    }
}