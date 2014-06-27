// 
// AddAnotherAccessor.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	/// <summary>
	/// Add another accessor to a property declaration that has only one.
	/// </summary>
	[NRefactoryCodeRefactoringProvider(Description = "Adds second accessor to a property.")]
	[ExportCodeRefactoringProvider("Add another accessor", LanguageNames.CSharp)]
	public class AddAnotherAccessorAction : ICodeRefactoringProvider
	{
        public static BlockSyntax GetNotImplementedBlock()
        {
            return SyntaxFactory.Block(
                        SyntaxFactory.ThrowStatement(
                            SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName(@"System"), SyntaxFactory.IdentifierName(@"NotImplementedException")))
                                .WithArgumentList(SyntaxFactory.ArgumentList())));
        }

		public async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
		{
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
            var token = root.FindToken(span.Start);
            var propertyDeclaration = token.Parent as PropertyDeclarationSyntax;

            var accessors = propertyDeclaration.AccessorList.Accessors;
            //ignore if it has both accessors
            if (propertyDeclaration == null || accessors.Count == 2)
                return Enumerable.Empty<CodeAction>();
            //ignore interfaces
            if(propertyDeclaration.Parent is InterfaceDeclarationSyntax)
                return Enumerable.Empty<CodeAction>();
            //if it has a getter, then we need a setter (we've checked for 2 accessors)
            bool needsSetter = accessors.Any(m => m.IsKind(SyntaxKind.GetAccessorDeclaration));

            return new[] { CodeActionFactory.Create(token.Span, DiagnosticSeverity.Info, "Add another accessor", PerformAction(document, model, root, propertyDeclaration, needsSetter)) };
		}

        private Document PerformAction(Document document, SemanticModel model, SyntaxNode root, PropertyDeclarationSyntax propertyDeclaration, bool needsSetter)
        {
            AccessorDeclarationSyntax accessor = null;
            PropertyDeclarationSyntax newProp = null;
            if (needsSetter)
            {
                accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration);

                var getter = propertyDeclaration.AccessorList.Accessors.First(m => m.IsKind(SyntaxKind.GetAccessorDeclaration));
                var getField = ScanGetter(model, getter);

                if(getField == null && getter.Body == null)
                {
                    //get;
                    accessor = accessor.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)).WithTrailingTrivia(getter.GetTrailingTrivia());
                }
                else if (getField == null || getField.IsReadOnly)
                {
                    //readonly or no field can be found
                    accessor = accessor.WithBody(GetNotImplementedBlock());
                }
                else
                {
                    //now we add a 'field = value'.
                    accessor = accessor.WithBody(SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.BinaryExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName(getField.Name), SyntaxFactory.IdentifierName("value")))));
                }
                newProp = propertyDeclaration.WithAccessorList(propertyDeclaration.AccessorList.AddAccessors(accessor));
            }
            else
            {
                accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration);

                var setter = propertyDeclaration.AccessorList.Accessors.First(m => m.IsKind(SyntaxKind.SetAccessorDeclaration));
                var setField = ScanSetter(model, setter);

                if (setField == null && setter.Body == null)
                {
                    //set;
                    accessor = accessor.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)).WithTrailingTrivia(setter.GetTrailingTrivia());
                }
                else if (setField == null)
                {
                    //no field can be found
                    accessor = accessor.WithBody(GetNotImplementedBlock());
                }
                else
                {
                    //now we add a 'return field;'.
                    accessor = accessor.WithBody(SyntaxFactory.Block(
                        SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(setField.Name))));
                }
                var accessorDeclList = new SyntaxList<AccessorDeclarationSyntax>();
                accessorDeclList = accessorDeclList.Add(accessor);
                accessorDeclList = accessorDeclList.Add(propertyDeclaration.AccessorList.Accessors.First(m => m.IsKind(SyntaxKind.SetAccessorDeclaration)));
                var accessorList = SyntaxFactory.AccessorList(accessorDeclList);
                newProp = propertyDeclaration.WithAccessorList(accessorList);
            }
            var newRoot = root.ReplaceNode(propertyDeclaration, newProp).WithAdditionalAnnotations(Formatter.Annotation);
            return document.WithSyntaxRoot(newRoot);
        }

        private IFieldSymbol ScanGetter(SemanticModel model, AccessorDeclarationSyntax getter)
        {
            if (getter == null || getter.Body == null || getter.Body.Statements.Count != 1) //no getter/get;/get we can't easily work out
                return null;
            var retStatement = getter.Body.Statements.First() as ReturnStatementSyntax;
            if (retStatement == null)
                return null;
            if (!IsPossibleExpression(retStatement.Expression))
                return null;
            var retSymbol = model.GetSymbolInfo(retStatement.Expression).Symbol;
            return ((IFieldSymbol)retSymbol);
        }

        private IFieldSymbol ScanSetter(SemanticModel model, AccessorDeclarationSyntax setter)
        {
            if (setter == null || setter.Body == null || setter.Body.Statements.Count != 1) //no getter/get;/get we can't easily work out
                return null;
            var setAssignment = setter.Body.Statements.First().ChildNodes().OfType<ExpressionSyntax>().First();
            var assignment = setAssignment != null ? setAssignment as BinaryExpressionSyntax : null;
            if (assignment == null || !assignment.OperatorToken.IsKind(SyntaxKind.EqualsToken))
                return null;
            var id = assignment.Right as IdentifierNameSyntax;
            if (id == null || id.Identifier.ValueText != "value")
                return null;
            if (!IsPossibleExpression(assignment.Left))
                return null;
            var retSymbol = model.GetSymbolInfo(assignment.Left).Symbol;
            return ((IFieldSymbol)retSymbol);

        }
        private bool IsPossibleExpression(ExpressionSyntax left)
        {
            if (left is IdentifierNameSyntax)
                return true;
            var mr = left as MemberAccessExpressionSyntax;
            if (mr == null)
                return false;
            return mr.Expression is ThisExpressionSyntax;
        }
	}
}
