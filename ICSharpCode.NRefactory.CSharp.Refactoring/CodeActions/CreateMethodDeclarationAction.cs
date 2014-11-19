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
	[ExportCodeRefactoringProvider("Create method", LanguageNames.CSharp)]
	public class CreateMethodDeclarationAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
		}
//		public async Task ComputeRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
//		{
//			var identifier = context.GetNode<IdentifierExpression>();
//			if (identifier != null && !(identifier.Parent is InvocationExpression && ((InvocationExpression)identifier.Parent).Target == identifier))
//				return GetActionsFromIdentifier(context, identifier);
//			
//			var memberReference = context.GetNode<MemberReferenceExpression>();
//			if (memberReference != null && !(memberReference.Parent is InvocationExpression && ((InvocationExpression)memberReference.Parent).Target == memberReference))
//				return GetActionsFromMemberReferenceExpression(context, memberReference);
//
//			var invocation = context.GetNode<InvocationExpression>();
//			if (invocation != null)
//				return GetActionsFromInvocation(context, invocation);
//			return;
//		}
//
//		IEnumerable<CodeAction> GetActionsFromMemberReferenceExpression(SemanticModel context, MemberReferenceExpression invocation)
//		{
//			if (!(context.Resolve(invocation).IsError)) 
//					yield break;
//
//			var methodName = invocation.MemberName;
//			var guessedType = TypeGuessing.GuessType(context, invocation);
//			if (guessedType.Kind != TypeKind.Delegate)
//					yield break;
//			var invocationMethod = guessedType.GetDelegateInvokeMethod();
//			var state = context.GetResolverStateBefore(invocation);
//			if (state.CurrentTypeDefinition == null)
//				yield break;
//			ResolveResult targetResolveResult = context.Resolve(invocation.Target);
//			bool createInOtherType = !state.CurrentTypeDefinition.Equals(targetResolveResult.Type.GetDefinition());
//
//			bool isStatic;
//			if (createInOtherType) {
//				if (targetResolveResult.Type.GetDefinition() == null || targetResolveResult.Type.GetDefinition().Region.IsEmpty)
//					yield break;
//				isStatic = targetResolveResult is TypeResolveResult;
//				if (isStatic && targetResolveResult.Type.Kind == TypeKind.Interface || targetResolveResult.Type.Kind == TypeKind.Enum)
//					yield break;
//			} else {
//				if (state.CurrentMember == null)
//					yield break;
//				isStatic = state.CurrentMember.IsStatic || state.CurrentTypeDefinition.IsStatic;
//			}
//
////			var service = (NamingConventionService)context.GetService(typeof(NamingConventionService));
////			if (service != null && !service.IsValidName(methodName, AffectedEntity.Method, Modifiers.Private, isStatic)) { 
////				yield break;
////			}
//
//			yield return CreateAction(
//				context, 
//				invocation,
//				methodName, 
//				context.CreateShortType(invocationMethod.ReturnType),
//				invocationMethod.Parameters.Select(parameter => new ParameterDeclaration(context.CreateShortType(parameter.Type), parameter.Name) { 
//					ParameterModifier = GetModifiers(parameter)
//				}),
//				createInOtherType,
//				isStatic,
//				targetResolveResult);
//		}
//		
//		IEnumerable<CodeAction> GetActionsFromIdentifier(SemanticModel context, IdentifierExpression identifier)
//		{
//			if (!(context.Resolve(identifier).IsError))
//				yield break;
//			var methodName = identifier.Identifier;
//			var guessedType = TypeGuessing.GuessType(context, identifier);
//			if (guessedType.Kind != TypeKind.Delegate)
//				yield break;
//			var invocationMethod = guessedType.GetDelegateInvokeMethod();
//			if (invocationMethod == null)
//				yield break;
//			var state = context.GetResolverStateBefore(identifier);
//			if (state.CurrentMember == null || state.CurrentTypeDefinition == null)
//				yield break;
//			bool isStatic = state.CurrentMember.IsStatic || state.CurrentTypeDefinition.IsStatic;
//
//			var service = (NamingConventionService)context.GetService(typeof(NamingConventionService));
//			if (service != null && !service.IsValidName(methodName, AffectedEntity.Method, Modifiers.Private, isStatic))
//				yield break;
//
//			yield return CreateAction(
//				context, 
//				identifier,
//				methodName, 
//				context.CreateShortType(invocationMethod.ReturnType),
//				invocationMethod.Parameters.Select(parameter => new ParameterDeclaration(context.CreateShortType(parameter.Type), parameter.Name) { 
//					ParameterModifier = GetModifiers(parameter)
//				}),
//				false,
//				isStatic,
//				null);
//		}
//
//		IEnumerable<CodeAction> GetActionsFromInvocation(SemanticModel context, InvocationExpression invocation)
//		{
//			if (!(context.Resolve(invocation.Target).IsError)) 
//				yield break;
//
//			var methodName = GetMethodName(invocation);
//			if (methodName == null)
//				yield break;
//			var state = context.GetResolverStateBefore(invocation);
//			if (state.CurrentMember == null || state.CurrentTypeDefinition == null)
//				yield break;
//			var guessedType = invocation.Parent is ExpressionStatement ? new PrimitiveType("void") : TypeGuessing.GuessAstType(context, invocation);
//
//			bool createInOtherType = false;
//			ResolveResult targetResolveResult = null;
//			if (invocation.Target is MemberReferenceExpression) {
//				targetResolveResult = context.Resolve(((MemberReferenceExpression)invocation.Target).Target);
//				createInOtherType = !state.CurrentTypeDefinition.Equals(targetResolveResult.Type.GetDefinition());
//			}
//
//			bool isStatic;
//			if (createInOtherType) {
//				if (targetResolveResult.Type.GetDefinition() == null || targetResolveResult.Type.GetDefinition().Region.IsEmpty)
//					yield break;
//				isStatic = targetResolveResult is TypeResolveResult;
//				if (isStatic && targetResolveResult.Type.Kind == TypeKind.Interface || targetResolveResult.Type.Kind == TypeKind.Enum)
//					yield break;
//			} else {
//				isStatic = state.CurrentMember.IsStatic || state.CurrentTypeDefinition.IsStatic;
//			}
//
////			var service = (NamingConventionService)context.GetService(typeof(NamingConventionService));
////			if (service != null && !service.IsValidName(methodName, AffectedEntity.Method, Modifiers.Private, isStatic)) { 
////				yield break;
////			}
//
//
//			yield return CreateAction(
//				context, 
//				invocation,
//				methodName, 
//				guessedType,
//				GenerateParameters(context, invocation.Arguments),
//				createInOtherType,
//				isStatic,
//				targetResolveResult);
//		}
//
//		static ParameterModifier GetModifiers(IParameter parameter)
//		{
//			if (parameter.IsOut)
//				return ParameterModifier.Out;
//			if (parameter.IsRef)
//				return ParameterModifier.Ref;
//			if (parameter.IsParams)
//				return ParameterModifier.Params;
//			return ParameterModifier.None;
//		}
//
//		static CodeAction CreateAction(SemanticModel context, AstNode createFromNode, string methodName, AstType returnType, IEnumerable<ParameterDeclaration> parameters, bool createInOtherType, bool isStatic, ResolveResult targetResolveResult)
//		{
//			return new CodeAction(context.TranslateString("Create method"), script => {
//				var throwStatement = new ThrowStatement();
//				var decl = new MethodDeclaration {
//					ReturnType = returnType,
//					Name = methodName,
//					Body = new BlockStatement {
//						throwStatement
//					}
//				};
//				decl.Parameters.AddRange(parameters);
//				
//				if (isStatic)
//					decl.Modifiers |= Modifiers.Static;
//				
//				if (createInOtherType) {
//					if (targetResolveResult.Type.Kind == TypeKind.Interface) {
//						decl.Body = null;
//						decl.Modifiers = Modifiers.None;
//					} else {
//						decl.Modifiers |= Modifiers.Public;
//					}
//
//					script
//						.InsertWithCursor(context.TranslateString("Create method"), targetResolveResult.Type.GetDefinition(), (s, c) => {
//						throwStatement.Expression = new ObjectCreateExpression(c.CreateShortType("System", "NotImplementedException"));
//						return decl;
//					})
//						.ContinueScript(s => s.Select(throwStatement));
//					return;
//				} else {
//					throwStatement.Expression = new ObjectCreateExpression(context.CreateShortType("System", "NotImplementedException"));
//				}
//
//				script
//					.InsertWithCursor(context.TranslateString("Create method"), Script.InsertPosition.Before, decl)
//					.ContinueScript(() => script.Select(throwStatement));
//			}, createFromNode.GetNodeAt(context.Location) ?? createFromNode)  { Severity = ICSharpCode.NRefactory.Refactoring.Severity.Error };
//		}
//
//		public static IEnumerable<ParameterDeclaration> GenerateParameters(SemanticModel context, IEnumerable<Expression> arguments)
//		{
//			var nameCounter = new Dictionary<string, int>();
//			foreach (var argument in arguments) {
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
//				var resolveResult = context.Resolve(node);
//				string name = CreateBaseName(argument, resolveResult.Type);
//				if (!nameCounter.ContainsKey(name)) {
//					nameCounter [name] = 1;
//				} else {
//					nameCounter [name]++;
//					name += nameCounter [name].ToString();
//				}
//				var type = resolveResult.Type.Kind == TypeKind.Unknown || resolveResult.Type.Kind == TypeKind.Null ? new PrimitiveType("object") : context.CreateShortType(resolveResult.Type);
//
//				yield return new ParameterDeclaration(type, name) { ParameterModifier = direction};
//			}
//		}
//
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
		
//		string GetMethodName(InvocationExpression invocation)
//		{
//			if (invocation.Target is IdentifierExpression)
//				return ((IdentifierExpression)invocation.Target).Identifier;
//			if (invocation.Target is MemberReferenceExpression)
//				return ((MemberReferenceExpression)invocation.Target).MemberName;
//
//			return null;
//		}
	}
}
