// 
// CreateEventInvocatorCodeRefactoringProvider.cs
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

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Create event invocator")]
	public class CreateEventInvocatorCodeRefactoringProvider : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
				return;
			var span = context.Span;
			if (!span.IsEmpty)
				return;
			var cancellationToken = context.CancellationToken;
			if (cancellationToken.IsCancellationRequested)
				return;
			var root = await document.GetSyntaxRootAsync (cancellationToken).ConfigureAwait (false);
			var token = root.FindToken (span.Start);
			if (!token.IsKind (SyntaxKind.IdentifierToken))
				return;

			var node = token.Parent as VariableDeclaratorSyntax;
			if (node == null)
				return;
			var model = await document.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
			var declaredSymbol = model.GetDeclaredSymbol (node, cancellationToken);
			if (declaredSymbol == null || !declaredSymbol.IsKind (SymbolKind.Event))
				return;
			var invokeMethod = declaredSymbol.GetReturnType ().GetDelegateInvokeMethod ();
			if (invokeMethod == null)
				return;

			context.RegisterRefactoring(
				CodeActionFactory.CreateInsertion(
					span, 
					DiagnosticSeverity.Info, 
					GettextCatalog.GetString ("Create event invocator"), 
					t2 => {
						SyntaxNode eventInvocator = CreateEventInvocator(document, declaredSymbol);
						return Task.FromResult (new InsertionResult (context, eventInvocator, declaredSymbol.ContainingType, declaredSymbol.Locations.First ()));
					}
				) 
			);
		}

		public static MethodDeclarationSyntax CreateEventInvocator(Document document, ISymbol eventMember, bool useExplictType = false)
		{
			var options = document.Project.ParseOptions as CSharpParseOptions;

			if (options != null && options.LanguageVersion < LanguageVersion.CSharp6)
				return CreateOldEventInvocator (eventMember.ContainingType.Name, eventMember.ContainingType.IsSealed, eventMember.IsStatic, eventMember.Name, eventMember.GetReturnType ().GetDelegateInvokeMethod (), useExplictType);
			
			return CreateEventInvocator (eventMember.ContainingType.Name, eventMember.ContainingType.IsSealed, eventMember.IsStatic, eventMember.Name, eventMember.GetReturnType ().GetDelegateInvokeMethod (), useExplictType);
		}

		public static MethodDeclarationSyntax CreateEventInvocator(Document document, string declaringTypeName, bool isSealed, bool isStatic, string eventName, IMethodSymbol invokeMethod, bool useExplictType = false)
		{
			var options = document.Project.ParseOptions as CSharpParseOptions;

			if (options != null && options.LanguageVersion < LanguageVersion.CSharp6)
				return CreateOldEventInvocator (declaringTypeName, isSealed, isStatic, eventName, invokeMethod, useExplictType);
			return CreateEventInvocator (declaringTypeName, isSealed, isStatic, eventName, invokeMethod, useExplictType);
		}

		static MethodDeclarationSyntax CreateMethodStub (bool isSealed, bool isStatic, string eventName, IMethodSymbol invokeMethod)
		{
			var node = SyntaxFactory.MethodDeclaration (SyntaxFactory.PredefinedType (SyntaxFactory.Token (SyntaxKind.VoidKeyword)), SyntaxFactory.Identifier (GetEventMethodName (eventName)));
			if (isStatic) {
				node = node.WithModifiers (SyntaxFactory.TokenList (SyntaxFactory.Token (SyntaxKind.StaticKeyword))); 
			} else if (isSealed) {
				
			} else {
				node = node.WithModifiers (SyntaxFactory.TokenList (SyntaxFactory.Token (SyntaxKind.ProtectedKeyword), SyntaxFactory.Token (SyntaxKind.VirtualKeyword))); 
			}
			node = node.WithParameterList (SyntaxFactory.ParameterList (SyntaxFactory.SeparatedList<ParameterSyntax> (new[] {
				SyntaxFactory.Parameter (SyntaxFactory.Identifier (invokeMethod.Parameters [1].Name)).WithType (invokeMethod.Parameters [1].Type.GenerateTypeSyntax ())
			})));
			return node;
		}

		static ExpressionSyntax GetTargetExpression (string declaringTypeName, bool isStatic, string eventName)
		{
			if (eventName != "e")
				return (ExpressionSyntax)SyntaxFactory.IdentifierName (eventName);

			if (isStatic)
				return SyntaxFactory.MemberAccessExpression (SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName (declaringTypeName), SyntaxFactory.IdentifierName (eventName));
			
			return SyntaxFactory.MemberAccessExpression (SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression (), SyntaxFactory.IdentifierName (eventName));
		}

		static ArgumentSyntax GetInvokeArgument (bool isStatic)
		{
			return SyntaxFactory.Argument (isStatic ? (ExpressionSyntax)SyntaxFactory.LiteralExpression (SyntaxKind.NullLiteralExpression) : SyntaxFactory.ThisExpression ());
		}

		static MethodDeclarationSyntax CreateEventInvocator (string declaringTypeName, bool isSealed, bool isStatic, string eventName, IMethodSymbol invokeMethod, bool useExplictType)
		{
			var result = CreateMethodStub (isSealed, isStatic, eventName, invokeMethod);
			var targetExpr = GetTargetExpression (declaringTypeName, isStatic, eventName);
			result = result.WithBody (SyntaxFactory.Block (
				SyntaxFactory.ExpressionStatement (
					SyntaxFactory.InvocationExpression (
						SyntaxFactory.ConditionalAccessExpression (
							targetExpr,
							SyntaxFactory.MemberBindingExpression (SyntaxFactory.IdentifierName ("Invoke"))
						),
						SyntaxFactory.ArgumentList (
							SyntaxFactory.SeparatedList<ArgumentSyntax> (new [] {
								GetInvokeArgument (isStatic),
								SyntaxFactory.Argument (SyntaxFactory.IdentifierName (invokeMethod.Parameters [1].Name))
							})
						)
					)
				)
			));

			return result;
		}


		static MethodDeclarationSyntax CreateOldEventInvocator (string declaringTypeName, bool isSealed, bool isStatic, string eventName, IMethodSymbol invokeMethod, bool useExplictType)
		{
		//	var invokeMethod = member.GetReturnType ().GetDelegateInvokeMethod ();
			var result = CreateMethodStub (isSealed, isStatic, eventName, invokeMethod);
			const string handlerName = "handler";
			var targetExpr = GetTargetExpression (declaringTypeName, isStatic, eventName);
			result = result.WithBody (SyntaxFactory.Block (
				SyntaxFactory.LocalDeclarationStatement (
					SyntaxFactory.VariableDeclaration (
						SyntaxFactory.ParseTypeName ("var"),
						SyntaxFactory.SeparatedList<VariableDeclaratorSyntax> (new [] {
							SyntaxFactory.VariableDeclarator (SyntaxFactory.Identifier (handlerName)).WithInitializer (
								SyntaxFactory.EqualsValueClause (
									targetExpr
								)
							)
						})
					)
				),
				SyntaxFactory.IfStatement (
					SyntaxFactory.BinaryExpression (
						SyntaxKind.NotEqualsExpression,
						SyntaxFactory.IdentifierName (handlerName),
						SyntaxFactory.LiteralExpression (SyntaxKind.NullLiteralExpression)
					),
					SyntaxFactory.ExpressionStatement (
						SyntaxFactory.InvocationExpression (
							SyntaxFactory.IdentifierName (handlerName), 
							SyntaxFactory.ArgumentList (
								SyntaxFactory.SeparatedList<ArgumentSyntax> (new [] {
									GetInvokeArgument (isStatic),
									SyntaxFactory.Argument (SyntaxFactory.IdentifierName (invokeMethod.Parameters [1].Name))
								})
							)
					)
					)
				)
			));
			return result;
		}

		static string GetEventMethodName(string eventName)
		{
			return "On" + char.ToUpper(eventName[0]) + eventName.Substring(1);
		}
	}
}