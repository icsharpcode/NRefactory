// 
// CSharpCompletionEngine.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using Microsoft.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using Microsoft.CodeAnalysis.Recommendations;
using System.Threading.Tasks;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	public partial class CompletionEngine 
	{
		static readonly CompletionContextHandler[] handlers = {
			new RoslynRecommendationsCompletionContextHandler (),
			new KeywordContextHandler(),
			new OverrideContextHandler(),
			new PartialContextHandler(),
			new EnumMemberContextHandler(),
			new XmlDocCommentContextHandler(),
			new ExplicitInterfaceContextHandler(),
			new AttributeNamedParameterContextHandler(),
			new NamedParameterContextHandler(),
			new SpeculativeTContextHandler(),
			new SnippetContextHandler(),
			new ObjectInitializerContextHandler(),
			new FormatItemContextHandler(),
			new SpeculativeNameContextHandler(),
			new DelegateCreationContextHandler()
		};

		static readonly ICompletionKeyHandler DefaultKeyHandler = new RoslynRecommendationsCompletionContextHandler ();

		readonly ICompletionDataFactory factory;
		readonly Workspace workspace;

		public ICompletionDataFactory Factory {
			get {
				return factory;
			}
		}

		public Workspace Workspace {
			get {
				return workspace;
			}
		}

		public CompletionEngine(Workspace workspace, ICompletionDataFactory factory)
		{
			if (workspace == null)
				throw new ArgumentNullException("workspace");
			if (factory == null)
				throw new ArgumentNullException("factory");
			this.workspace = workspace;
			this.factory = factory;
		}
		
		public void AddImportCompletionData (CompletionResult result, Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken = default(CancellationToken))
		{
			var ns = new Stack<INamespaceOrTypeSymbol>();
			ns.Push(semanticModel.Compilation.GlobalNamespace);
			
			semanticModel.LookupNamespacesAndTypes(position);
		}

		public async Task<CompletionResult> GetCompletionDataAsync(CompletionContext completionContext, CompletionTriggerInfo info, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (completionContext == null)
				throw new ArgumentNullException ("completionContext");

			var document = completionContext.Document;
			var semanticModel = await completionContext.GetSemanticModelAsync (cancellationToken).ConfigureAwait(false);
			var position = completionContext.Position;

			var trivia = semanticModel.SyntaxTree.GetRoot(cancellationToken).FindTrivia(position - 1);
			// work around for roslyn bug: missing comments after pre processor directives
			if (trivia.IsKind(SyntaxKind.IfDirectiveTrivia) ||
				trivia.IsKind(SyntaxKind.EndIfDirectiveTrivia) ||
				trivia.IsKind(SyntaxKind.DefineDirectiveTrivia) ||
				trivia.IsKind(SyntaxKind.ElseDirectiveTrivia) ||
				trivia.IsKind(SyntaxKind.RegionDirectiveTrivia) ||
				trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia) ||
				trivia.IsKind(SyntaxKind.ErrorDirectiveTrivia) ||
				trivia.IsKind(SyntaxKind.LineDirectiveTrivia) ||
				trivia.IsKind(SyntaxKind.PragmaChecksumDirectiveTrivia) ||
				trivia.IsKind(SyntaxKind.PragmaWarningDirectiveTrivia) ||
				trivia.IsKind(SyntaxKind.UndefDirectiveTrivia) ||
				trivia.IsKind(SyntaxKind.WarningDirectiveTrivia)) {
				if (trivia.ToFullString().IndexOf("//", StringComparison.Ordinal) >= 0)
					return CompletionResult.Empty;
			}

			var text = document.GetTextAsync(cancellationToken).Result; 
			char lastLastChar = position >= 2 ? text [position - 2] : '\0';
			char lastChar = text [position - 1];
			if (info.CompletionTriggerReason == CompletionTriggerReason.CharTyped) {
				if (lastChar != '.' && 
					lastChar != '#' && 
					lastChar != '>' && lastLastChar == '-' &&
					lastChar != ' ' && lastLastChar != ' ' && lastLastChar != '\t' &&
					!char.IsLetter(lastChar))
					return CompletionResult.Empty;
			}

			var ctx = await completionContext.GetSyntaxContextAsync (workspace, cancellationToken).ConfigureAwait (false);

			if (ctx.ContainingTypeDeclaration != null &&
			    ctx.ContainingTypeDeclaration.IsKind(SyntaxKind.EnumDeclaration)) {
				return CompletionResult.Empty;
			}

			if (ctx.TargetToken.Parent != null && ctx.TargetToken.Parent.CSharpKind() == SyntaxKind.ObjectCreationExpression) {
				if (lastChar == ' ' || info.CompletionTriggerReason == CompletionTriggerReason.CompletionCommand) {
					return HandleObjectCreationExpression(ctx, semanticModel, position, info.CompletionTriggerReason == CompletionTriggerReason.CompletionCommand, cancellationToken);
				}
			}
			if (info.CompletionTriggerReason == CompletionTriggerReason.CharTyped && (char.IsLetter(lastLastChar) || lastLastChar == '_') &&
			    (char.IsLetterOrDigit(lastChar) || lastChar == '_')) {
				return CompletionResult.Empty;
			}

			if (lastChar == ' ' && !char.IsWhiteSpace(lastLastChar)) {
				var str = ctx.TargetToken.ToFullString().Trim();
				switch (str) {
					case "yield":
						return HandleYieldStatementExpression();
				}

				// auto popup enum base types
				var parent = ctx.TargetToken.Parent;
				if (parent != null && parent.Parent != null && parent.IsKind(SyntaxKind.BaseList) && parent.Parent.IsKind(SyntaxKind.EnumDeclaration)) {
					var result2 = new CompletionResult();
					foreach (var handler in handlers.Concat (completionContext.AdditionalContextHandlers)) {
						result2.AddRange (handler.GetCompletionDataAsync (result2, this, completionContext, info, cancellationToken).Result);
					}
					return result2;
				}
			}

			if (ctx.TargetToken.Parent is AccessorListSyntax) {
				if (ctx.TargetToken.Parent.Parent is EventDeclarationSyntax) {
					return HandleEventAccessorContext();
				}
				if (ctx.TargetToken.Parent.Parent is PropertyDeclarationSyntax ||
					ctx.TargetToken.Parent.Parent is IndexerDeclarationSyntax) {
					return HandlePropertyAccessorContext(false);
				}
			}
			if (ctx.TargetToken.Parent is AccessorDeclarationSyntax) {
				if (ctx.TargetToken.Parent.Parent.Parent is PropertyDeclarationSyntax ||
					ctx.TargetToken.Parent.Parent.Parent is IndexerDeclarationSyntax) {
					return HandlePropertyAccessorContext(true);
				}
			}

			// case lambda parameter (n1, $
			if (ctx.TargetToken.Parent != null && ctx.TargetToken.Parent.Parent != null &&
				ctx.TargetToken.Parent.Parent.IsKind(SyntaxKind.ParenthesizedLambdaExpression)) 
				return CompletionResult.Empty;

			var result = new CompletionResult();

			if (position > 0) {
				foreach (var handler in handlers.Concat (completionContext.AdditionalContextHandlers)) {
					if (info.CompletionTriggerReason == CompletionTriggerReason.CompletionCommand || handler.IsTriggerCharacter (text, position - 1)) {
						var handlerResult = handler.GetCompletionDataAsync (result, this, completionContext, info, cancellationToken).Result;
						if (handlerResult != null)
							result.AddRange (handlerResult);
					}
				}
			}
			// prevent auto selection for "<number>." case
			if (ctx.TargetToken.IsKind(SyntaxKind.DotToken)) {
				var accessExpr = ctx.TargetToken.Parent as MemberAccessExpressionSyntax;
				if (accessExpr != null &&
					accessExpr.Expression != null &&
					accessExpr.Expression.IsKind(SyntaxKind.NumericLiteralExpression))  {
					result.AutoSelect = false;
				}
			}

			if (ctx.LeftToken.Parent != null &&
				ctx.LeftToken.Parent.Parent != null &&
				ctx.TargetToken.Parent != null && !ctx.TargetToken.Parent.IsKind(SyntaxKind.NameEquals) &&
				ctx.LeftToken.Parent.Parent.IsKind(SyntaxKind.AnonymousObjectMemberDeclarator))
				result.AutoSelect = false;

			if (ctx.TargetToken.IsKind(SyntaxKind.OpenParenToken))
				result.AutoSelect = false;

			foreach (var type in ctx.InferredTypes) {
				if (type.TypeKind == TypeKind.Delegate) {
					result.AutoSelect = false;
					break;
				}
			}
			
			return result;
		}

		CompletionResult HandleEventAccessorContext()
		{
			var result = new CompletionResult();
			result.AddData(factory.CreateGenericData(DefaultKeyHandler, "add", GenericDataType.Keyword));
			result.AddData(factory.CreateGenericData(DefaultKeyHandler, "remove", GenericDataType.Keyword));
			return result;
		}

		CompletionResult HandleObjectCreationExpression(SyntaxContext ctx, SemanticModel semanticModel, int position, bool isCtrlSpace, CancellationToken cancellationToken)
		{
			var result = new CompletionResult();
			foreach (var symbol in Recommender.GetRecommendedSymbolsAtPosition(semanticModel, position, Workspace, null, cancellationToken)) {
				result.AddData(Factory.CreateSymbolCompletionData(DefaultKeyHandler, symbol));
			}
			
			var inferredType = ctx.InferredTypes.FirstOrDefault();
			if (inferredType != null)
				result.DefaultCompletionString = inferredType.Name;

//					IEnumerable<ICompletionData> CreateConstructorCompletionData(IType hintType)
//		{
//			var wrapper = new CompletionDataWrapper(this);
//			var state = GetState();
//			Func<IType, IType> pred = null;
//			Action<ICompletionData, IType> typeCallback = null;
//			var inferredTypesCategory = new Category("Inferred Types", null);
//			var derivedTypesCategory = new Category("Derived Types", null);
//
//			if (hintType != null && (hintType.Kind != TypeKind.TypeParameter || IsTypeParameterInScope(hintType))) {
//				if (hintType.Kind != TypeKind.Unknown) {
//					var lookup = new MemberLookup(
//						ctx.CurrentTypeDefinition,
//						Compilation.MainAssembly
//					);
//					typeCallback = (data, t) => {
//						//check if type is in inheritance tree.
//						if (hintType.GetDefinition() != null &&
//							t.GetDefinition() != null &&
//							t.GetDefinition().IsDerivedFrom(hintType.GetDefinition())) {
//							data.CompletionCategory = derivedTypesCategory;
//						}
//					};
//					pred = t => {
//						if (t.Kind == TypeKind.Interface && hintType.Kind != TypeKind.Array) {
//							return null;
//						}
//						// check for valid constructors
//						if (t.GetConstructors().Count() > 0) {
//							bool isProtectedAllowed = currentType != null ? 
//								currentType.Resolve(ctx).GetDefinition().IsDerivedFrom(t.GetDefinition()) : false;
//							if (!t.GetConstructors().Any(m => lookup.IsAccessible(m, isProtectedAllowed))) {
//								return null;
//							}
//						}
//
//						// check derived types
//						var typeDef = t.GetDefinition();
//						var hintDef = hintType.GetDefinition();
//						if (typeDef != null && hintDef != null && typeDef.IsDerivedFrom(hintDef)) {
//							var newType = wrapper.AddType(t, true);
//							if (newType != null) {
//								newType.CompletionCategory = inferredTypesCategory;
//							}
//						}
//
//						// check type inference
//						var typeInference = new TypeInference(Compilation);
//						typeInference.Algorithm = TypeInferenceAlgorithm.ImprovedReturnAllResults;
//
//						var inferedType = typeInference.FindTypeInBounds(new [] { t }, new [] { hintType });
//						if (inferedType != SpecialType.UnknownType) {
//							var newType = wrapper.AddType(inferedType, true);
//							if (newType != null) {
//								newType.CompletionCategory = inferredTypesCategory;
//							}
//							return null;
//						}
//						return t;
//					};
//					if (!(hintType.Kind == TypeKind.Interface && hintType.Kind != TypeKind.Array)) {
//						var hint = wrapper.AddType(hintType, true);
//						if (hint != null) {
//							DefaultCompletionString = hint.DisplayText;
//							hint.CompletionCategory = derivedTypesCategory;
//						}
//					}
//					if (hintType is ParameterizedType && hintType.TypeParameterCount == 1 && hintType.FullName == "System.Collections.Generic.IEnumerable") {
//						var arg = ((ParameterizedType)hintType).TypeArguments.FirstOrDefault();
//						if (arg.Kind != TypeKind.TypeParameter) {
//							var array = new ArrayType(ctx.Compilation, arg, 1);
//							wrapper.AddType(array, true);
//						}
//					}
//				} else {
//					var hint = wrapper.AddType(hintType, true);
//					if (hint != null) {
//						DefaultCompletionString = hint.DisplayText;
//						hint.CompletionCategory = derivedTypesCategory;
//					}
//				}
//			} 
//			AddTypesAndNamespaces(wrapper, state, null, pred, m => false, typeCallback, true);
//			if (hintType == null || hintType == SpecialType.UnknownType) {
//				AddKeywords(wrapper, primitiveTypesKeywords.Where(k => k != "void"));
//			}
//
//			return wrapper.Result;
//		}
			result.CloseOnSquareBrackets = true;
			result.AutoCompleteEmptyMatch = true;
			result.AutoCompleteEmptyMatchOnCurlyBracket = false;

			return result;
		}


		CompletionResult HandleYieldStatementExpression()
		{
			var result = new CompletionResult();
			result.DefaultCompletionString = "return";

			result.AddData(factory.CreateGenericData(DefaultKeyHandler, "break", GenericDataType.Keyword));
			result.AddData(factory.CreateGenericData(DefaultKeyHandler, "return", GenericDataType.Keyword));
			return result;
		}
		
		CompletionResult HandlePropertyAccessorContext(bool isInsideAccessorDeclaration)
		{
			var result = new CompletionResult();
			result.AddData(factory.CreateGenericData(DefaultKeyHandler, "get", GenericDataType.Keyword));
			result.AddData(factory.CreateGenericData(DefaultKeyHandler, "set", GenericDataType.Keyword));
			foreach (var accessorModifier in new [] { "public", "internal", "protected", "private", "async" }) {
				result.AddData(factory.CreateGenericData(DefaultKeyHandler, accessorModifier, GenericDataType.Keyword));
			}
			return result;
		}

		IEnumerable<ISymbol> GetAllMembers (ITypeSymbol type)
		{
			if (type == null)
				yield break;
			foreach (var member in type.GetMembers()) {
				yield return member;
			}
			foreach (var baseMember in GetAllMembers(type.BaseType))
				yield return baseMember;
		}

		public static Func<CancellationToken, Task<IEnumerable<ICompletionData>>> SnippetCallback;
	}
}