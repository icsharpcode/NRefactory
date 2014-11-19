// 
// CreateEventInvocator.cs
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
	[NRefactoryCodeRefactoringProvider(Description = "Creates a standard OnXXX event method.")]
	[ExportCodeRefactoringProvider("Create event invocator", LanguageNames.CSharp)]
	public class CreateEventInvocatorAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
		}
//		/// <summary>
//		/// If <c>true</c> an explicit type will be used for the handler variable; otherwise, 'var' will be used as type.
//		/// Default value is <c>false</c>
//		/// </summary>
//		public bool UseExplictType {
//			get;
//			set;
//		}

		public static MethodDeclarationSyntax CreateEventInvocator (SemanticModel model, TypeDeclarationSyntax declaringType, bool isStatic, string eventName, IMethodSymbol invokeMethod, bool useExplictType)
		{
			bool hasSenderParam = false;
			var pars = invokeMethod.Parameters;
			if (invokeMethod.Parameters.Any()) {
				var first = invokeMethod.Parameters [0];
				if (first.Name == "sender" /*&& first.Type == "System.Object"*/) {
					hasSenderParam = true;
					pars = pars.RemoveAt(0);
				}
			}

			const string handlerName = "handler";

			var arguments = new List<ArgumentSyntax>();
			if (hasSenderParam)
				arguments.Add(SyntaxFactory.Argument(isStatic ? (ExpressionSyntax)SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression) : SyntaxFactory.ThisExpression()));

			bool useThisMemberReference = false;
			foreach (var par in pars) {
				arguments.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(par.Name)));
				useThisMemberReference |= par.Name == eventName;
			}
			var proposedHandlerName = GetNameProposal(eventName);
			var modifiers = isStatic ? SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)) : SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.VirtualKeyword));
			if (declaringType.Modifiers.Any(m => m.IsKind(SyntaxKind.SealedKeyword))) {
				modifiers = SyntaxFactory.TokenList();
			}
			var parameters = new List<ParameterSyntax>(pars.Select(p => SyntaxFactory.Parameter(SyntaxFactory.Identifier(p.Name)).WithType(ConvertType(model, declaringType.SpanStart, p.Type))));
			var methodDeclaration = SyntaxFactory.MethodDeclaration (
				SyntaxFactory.List<AttributeListSyntax>(),
				modifiers,
				SyntaxFactory.ParseTypeName("void"),
				null,
				SyntaxFactory.Identifier(proposedHandlerName),
				null,
				SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)),
				SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
				SyntaxFactory.Block(
					SyntaxFactory.LocalDeclarationStatement(
						SyntaxFactory.VariableDeclaration(
							// TODO:
//						useExplictType ? eventDeclaration.ReturnType.Clone () : new PrimitiveType ("var"), handlerName, 							SyntaxFactory.ParseTypeName("var"),
							SyntaxFactory.ParseTypeName("var"),
							SyntaxFactory.SeparatedList(new [] {
								SyntaxFactory.VariableDeclarator(
									SyntaxFactory.Identifier(handlerName), 
									null,
									// TODO:
//						useThisMemberReference ?  //						(Expression)new MemberReferenceExpression (new ThisReferenceExpression (), eventName)  //						: new IdentifierExpression (eventName)
									SyntaxFactory.EqualsValueClause(SyntaxFactory.IdentifierName(eventName))
								)
							})
						)
					),
					SyntaxFactory.IfStatement(
						SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, SyntaxFactory.IdentifierName(handlerName), SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
						SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(handlerName), SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))))
					)
				),
				null
			);
			return methodDeclaration;
		}

		public static TypeSyntax ConvertType(SemanticModel model, int position, ITypeSymbol type)
		{
			return SyntaxFactory.ParseTypeName(type.ToMinimalDisplayString(model, position));
		}

		static string GetNameProposal(string eventName)
		{
			return "On" + char.ToUpper(eventName[0]) + eventName.Substring(1);
		}

//		public async Task ComputeRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
//		{
//			VariableInitializer initializer;
//			var eventDeclaration = GetEventDeclaration(context, out initializer);
//			if (eventDeclaration == null) {
//				yield break;
//			}
//			var type = (TypeDeclaration)eventDeclaration.Parent;
//			var proposedHandlerName = GetNameProposal(initializer);
//			if (type.Members.Any(m => m is MethodDeclaration && m.Name == proposedHandlerName)) {
//				yield break;
//			}
//			var resolvedType = context.Resolve(eventDeclaration.ReturnType).Type;
//			if (resolvedType.Kind == TypeKind.Unknown) {
//				yield break;
//			}
//			var invokeMethod = resolvedType.GetDelegateInvokeMethod();
//			if (invokeMethod == null) {
//				yield break;
//			}
//			yield return new CodeAction(context.TranslateString("Create event invocator"), script => {
//				var methodDeclaration = CreateEventInvocator (context, type, eventDeclaration, initializer, invokeMethod, UseExplictType);
//				script.InsertWithCursor(
//					context.TranslateString("Create event invocator"),
//					Script.InsertPosition.After,
//					methodDeclaration
//				);
//			}, initializer);
//		}
//
//		static EventDeclaration GetEventDeclaration (SemanticModel context, out VariableInitializer initializer)
//		{
//			var result = context.GetNode<EventDeclaration> ();
//			if (result == null) {
//				initializer = null;
//				return null;
//			}
//			initializer = result.Variables.FirstOrDefault (v => v.NameToken.Contains (context.Location));
//			return initializer != null ? result : null;
//		}
	}
}

