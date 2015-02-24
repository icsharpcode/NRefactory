// 
// CreateMethodDeclarationAction.cs
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
using System.Text;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Creates a method declaration out of an invocation")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Create method")]
	public class CreateMethodDeclarationAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait (false);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait (false);
			var node = root.FindNode(span);
			if (node.IsKind(SyntaxKind.Argument)) {
				var argumentSyntax = (ArgumentSyntax)node;
				if (!argumentSyntax.Expression.IsKind(SyntaxKind.IdentifierName))
					return;
				node = argumentSyntax.Expression;
			} 
			var ma = node.Parent as MemberAccessExpressionSyntax;
			if (ma != null) {
				if (ma.Parent is InvocationExpressionSyntax) {
					GetActionsFromInvocation(context, model, root, (InvocationExpressionSyntax)ma.Parent);
					return;
				}
				if (ma.Name == node)
					GetActionsFromMemberReferenceExpression(context, model, root, ma);
			}

			if (node is IdentifierNameSyntax)
				GetActionsFromIdentifier(context, model, root, (IdentifierNameSyntax)node);

			if (node.Parent is InvocationExpressionSyntax)
				GetActionsFromInvocation(context, model, root, (InvocationExpressionSyntax)node.Parent);
		}

		static void GetActionsFromMemberReferenceExpression(CodeRefactoringContext context, SemanticModel model, SyntaxNode root, MemberAccessExpressionSyntax memberAccess)
		{
//			if (!(context.Resolve(invocation).IsError)) 
//				return;
//
			var methodName = memberAccess.Name.ToString ();
			var guessedType = TypeGuessing.GuessType(model, memberAccess);
			if (guessedType.TypeKind != TypeKind.Delegate)
				return;
			
			var invocationMethod = guessedType.GetDelegateInvokeMethod();
//			var state = context.GetResolverStateBefore(invocation);
//			if (state.CurrentTypeDefinition == null)
//				return;
			
			var targetResolveResult = model.GetTypeInfo (memberAccess.Expression);
			var enclosingType = model.GetEnclosingNamedType(memberAccess.SpanStart, context.CancellationToken);
			var enclosingMember = model.GetEnclosingSymbol(memberAccess.SpanStart, context.CancellationToken);

			bool createInOtherType = enclosingType != targetResolveResult.Type;

			bool isStatic;
			if (createInOtherType) {
				if (targetResolveResult.Type as INamedTypeSymbol == null || targetResolveResult.Type.Locations.First ().IsInMetadata)
					return;
				isStatic = model.GetSymbolInfo(memberAccess.Expression).Symbol is ITypeSymbol;
				if (isStatic && targetResolveResult.Type.TypeKind == TypeKind.Interface || targetResolveResult.Type.TypeKind == TypeKind.Enum)
					return;
			} else {
				if (enclosingMember == null)
					return;
				isStatic = enclosingMember.IsStatic || enclosingType.IsStatic;
			}

//			var service = (NamingConventionService)context.GetService(typeof(NamingConventionService));
//			if (service != null && !service.IsValidName(methodName, AffectedEntity.Method, Modifiers.Private, isStatic)) { 
//				yield break;
//			}

			CreateAction(
				context, 
				model,
				memberAccess,
				methodName, 
				invocationMethod.ReturnType.GenerateTypeSyntax (),
				SyntaxFactory.ParameterList (SyntaxFactory.SeparatedList<ParameterSyntax>(invocationMethod.Parameters.Select (p => p.GenerateParameterSyntax ()))),
				createInOtherType,
				isStatic,
				targetResolveResult.Type as INamedTypeSymbol
			);
		}
		
		static void GetActionsFromIdentifier(CodeRefactoringContext context, SemanticModel model, SyntaxNode root, IdentifierNameSyntax identifier)
		{
			var guessedType = TypeGuessing.GuessType(model, identifier);
			if (guessedType.TypeKind != TypeKind.Delegate)
				return;
			var methodName = identifier.ToString();
			if (methodName == null)
				return;

			var invocationMethod = guessedType.GetDelegateInvokeMethod();
			if (invocationMethod == null)
				return;

			var enclosingType = model.GetEnclosingNamedType(identifier.SpanStart, context.CancellationToken);
			var enclosingMember = model.GetEnclosingSymbol(identifier.SpanStart, context.CancellationToken);
			bool isStatic = enclosingMember.IsStatic || enclosingType.IsStatic;;

			CreateAction(
				context,
				model, 
				identifier,
				methodName, 
				invocationMethod.ReturnType.GenerateTypeSyntax (),
				SyntaxFactory.ParameterList (SyntaxFactory.SeparatedList<ParameterSyntax>(invocationMethod.Parameters.Select (p => p.GenerateParameterSyntax ()))),
				false,
				isStatic,
				enclosingType
			);
		}

		static void GetActionsFromInvocation(CodeRefactoringContext context, SemanticModel model, SyntaxNode root, InvocationExpressionSyntax invocation)
		{
			var methodName = GetMethodName(invocation);
			if (methodName == null)
				return;
			var guessedType = TypeGuessing.GuessAstType(model, invocation);

			bool createInOtherType = false;
			TypeInfo targetResolveResult;
			var enclosingType = model.GetEnclosingNamedType(invocation.SpanStart, context.CancellationToken);
			var enclosingMember = model.GetEnclosingSymbol(invocation.SpanStart, context.CancellationToken);
			var targetType = enclosingType;
			if (invocation.Expression is MemberAccessExpressionSyntax) {
				targetResolveResult = model.GetTypeInfo (((MemberAccessExpressionSyntax)invocation.Expression).Expression);
				createInOtherType = enclosingType != targetResolveResult.Type;
				targetType = targetResolveResult.Type as INamedTypeSymbol ?? enclosingType;
			}

			bool isStatic;
			if (createInOtherType) {
				if (targetResolveResult.Type == null || targetResolveResult.Type.Locations.First ().IsInMetadata)
					return;
				isStatic = model.GetSymbolInfo (((MemberAccessExpressionSyntax)invocation.Expression).Expression).Symbol is INamedTypeSymbol;

				if (isStatic && targetResolveResult.Type.TypeKind == TypeKind.Interface || targetResolveResult.Type.TypeKind == TypeKind.Enum)
					return;
			} else {
				isStatic = enclosingMember.IsStatic || enclosingType.IsStatic;
			}

			CreateAction(
				context,
				model, 
				invocation,
				methodName, 
				guessedType,
				GenerateParameters(model, invocation.ArgumentList),
				createInOtherType,
				isStatic,
				targetType);
		}

		static void CreateAction(CodeRefactoringContext context, SemanticModel model, SyntaxNode invocation, string methodName, TypeSyntax returnType, ParameterListSyntax parameters, bool createInOtherType, bool isStatic, INamedTypeSymbol targetType)
		{
			context.RegisterRefactoring(
				CodeActionFactory.CreateInsertion(
					context.Span, 
					DiagnosticSeverity.Error, 
					"Create method", 
					t2 => {

						var decl = SyntaxFactory.MethodDeclaration(returnType, methodName);
						decl = decl.WithParameterList (parameters);
						if (targetType.TypeKind != TypeKind.Interface) {
							decl = decl.WithBody(SyntaxFactory.Block (SyntaxFactory.ParseStatement ("throw new System.NotImplementedException();")));
							var enclosingType = model.GetEnclosingNamedType(invocation.SpanStart, context.CancellationToken);
							if (enclosingType != targetType) {
								if (isStatic) {
									decl = decl.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)));
								} else {
									decl = decl.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
								}
							} else {
								if (isStatic)
									decl = decl.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)));
							}
						} else {
							decl = decl.WithSemicolonToken (SyntaxFactory.Token (SyntaxKind.SemicolonToken));
						}

						decl = decl.WithAdditionalAnnotations (Simplifier.Annotation, Formatter.Annotation);

						return Task.FromResult(new InsertionResult (context, decl, targetType, InsertionResult.GuessCorrectLocation (context, targetType.Locations)));
					}
				) 
			);
		}


		public static ParameterListSyntax GenerateParameters(SemanticModel model, ArgumentListSyntax argumentList)
		{
			var parameters = new List<ParameterSyntax>();
			var nameCounter = new Dictionary<string, int>();
			foreach (var argument in argumentList.Arguments) {
//				var direction = ParameterModifier.None;
//				AstNode node;
//				if (argument is DirectionExpression) {
//					var de = (DirectionExpression)argument;
//					direction = de.FieldDirection == FieldDirection.Out ? ParameterModifier.Out : ParameterModifier.Ref;
//					node = de.Expression;
//				} else {
//					node = argument;
//				}
//
				var resolveResult = model.GetTypeInfo (argument.Expression);

				string name = CreateBaseName(argument, resolveResult.Type);
				if (!nameCounter.ContainsKey(name)) {
					nameCounter [name] = 1;
				} else {
					nameCounter [name]++;
					name += nameCounter [name].ToString();
				}
//				var type = resolveResult.Type.Kind == TypeKind.Unknown || resolveResult.Type.Kind == TypeKind.Null ? new PrimitiveType("object") : context.CreateShortType(resolveResult.Type);


				var param = SyntaxFactory.Parameter(SyntaxFactory.Identifier (name));
				if (argument.RefOrOutKeyword.IsKind (SyntaxKind.RefKeyword))
					param = param.WithModifiers(SyntaxFactory.TokenList (SyntaxFactory.Token (SyntaxKind.RefKeyword)));
				if (argument.RefOrOutKeyword.IsKind (SyntaxKind.OutKeyword))
					param = param.WithModifiers(SyntaxFactory.TokenList (SyntaxFactory.Token (SyntaxKind.OutKeyword)));

				param = param.WithType(resolveResult.Type != null ? resolveResult.Type.GenerateTypeSyntax () : SyntaxFactory.ParseTypeName ("object"));
				parameters.Add(param);
			}

			return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList<ParameterSyntax>(parameters));
		}

		static string CreateBaseNameFromString(string str)
		{
			if (string.IsNullOrEmpty(str)) {
				return "empty";
			}
			var sb = new StringBuilder();
			bool firstLetter = true, wordStart = false;
			foreach (char ch in str) {
				if (char.IsWhiteSpace(ch)) {
					wordStart = true;
					continue;
				}
				if (!char.IsLetter(ch))
					continue;
				if (firstLetter) {
					sb.Append(char.ToLower(ch));
					firstLetter = false;
					continue;
				}
				if (wordStart) {
					sb.Append(char.ToUpper(ch));
					wordStart = false;
					continue;
				}
				sb.Append(ch);
			}
			return sb.Length == 0 ? "str" : sb.ToString();
		}

		public static string CreateBaseName(SyntaxNode node, ITypeSymbol type)
		{
			string name = null;

			if (node.IsKind(SyntaxKind.Argument))
				node = ((ArgumentSyntax)node).Expression;

			if (node.IsKind(SyntaxKind.NullLiteralExpression))
				return "o";
			if (node.IsKind(SyntaxKind.InvocationExpression))
				return CreateBaseName(((InvocationExpressionSyntax)node).Expression, type);
			if (node.IsKind(SyntaxKind.IdentifierName)) {
				name = node.ToString();
			} else if (node is MemberAccessExpressionSyntax) {
				name = ((MemberAccessExpressionSyntax)node).Name.ToString();
			} else if (node is LiteralExpressionSyntax) {
				var pe = (LiteralExpressionSyntax)node;
				if (pe.IsKind(SyntaxKind.StringLiteralExpression)) {
					name = CreateBaseNameFromString(pe.Token.ToString());
				} else {
					return char.ToLower(type.Name [0]).ToString();
				}
			} else if (node is ArrayCreationExpressionSyntax) {
				name = "arr";
			} else {
				if (type.TypeKind == TypeKind.Error)
					return "par";
				name = GuessNameFromType(type);
			}
			var sb = new StringBuilder ();
			sb.Append (char.ToLower(name [0]));
			for (int i = 1; i < name.Length; i++) {
				var ch = name[i];
				if (char.IsLetterOrDigit (ch) || ch == '_')
					sb.Append (ch);
			}
			return sb.ToString ();
		}

		internal static string GuessNameFromType(ITypeSymbol returnType)
		{
			switch (returnType.SpecialType) {
				case SpecialType.System_Object:
					return "obj";
				case SpecialType.System_Boolean:
					return "b";
				case SpecialType.System_Char:
					return "ch";
				case SpecialType.System_SByte:
				case SpecialType.System_Byte:
					return "b";
				case SpecialType.System_Int16:
				case SpecialType.System_UInt16:
				case SpecialType.System_Int32:
				case SpecialType.System_UInt32:
				case SpecialType.System_Int64:
				case SpecialType.System_UInt64:
					return "i";
				case SpecialType.System_Decimal:
					return "d";
				case SpecialType.System_Single:
					return "f";
				case SpecialType.System_Double:
					return "d";
				case SpecialType.System_String:
					return "str";
				case SpecialType.System_IntPtr:
				case SpecialType.System_UIntPtr:
					return "ptr";
				case SpecialType.System_DateTime:
					return "date";
			}
			if (returnType.TypeKind == TypeKind.Array)
				return "arr";
			switch (returnType.GetFullName()) {
				case "System.Exception":
					return "e";
				case "System.Object":
				case "System.Func":
				case "System.Action":
					return "action";
			}
			return string.IsNullOrEmpty(returnType.Name) ? "obj" : returnType.Name;
		}
		
		static string GetMethodName(InvocationExpressionSyntax invocation)
		{
			if (invocation.Expression is IdentifierNameSyntax)
				return invocation.Expression.ToString ();
			if (invocation.Expression is MemberAccessExpressionSyntax)
				return ((MemberAccessExpressionSyntax)invocation.Expression).Name.ToString ();
			return null;
		}
	}
}
