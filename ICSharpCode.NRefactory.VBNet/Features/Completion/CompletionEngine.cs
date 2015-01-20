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
using Microsoft.CodeAnalysis.VisualBasic;

using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Text;
using Microsoft.CodeAnalysis.Recommendations;

namespace ICSharpCode.NRefactory6.VisualBasic.Completion
{
	public partial class CompletionEngine 
	{
		static readonly CompletionContextHandler[] handlers = {
			new RoslynRecommendationsCompletionContextHandler (),
			new KeywordContextHandler(),
			new EnumMemberContextHandler()
		};

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
			Stack<INamespaceOrTypeSymbol> ns = new Stack<INamespaceOrTypeSymbol>();
			ns.Push(semanticModel.Compilation.GlobalNamespace);
			
			semanticModel.LookupNamespacesAndTypes(position);
			
			/*
			Enumerable.SelectMany(semanticModel.Compilation.GlobalNamespace,
			ns); 
			.GetMembers()
*/
//			/// <summary>
//		/// Gets the types that needs to be imported via using or full type name.
//		/// </summary>
//		public IEnumerable<ICompletionData> GetImportCompletionData(int offset)
//		{
//			var generalLookup = new MemberLookup(null, Compilation.MainAssembly);
//			SetOffset(offset);
//
//			// flatten usings
//			var namespaces = new List<INamespace>();
//			for (var n = ctx.CurrentUsingScope; n != null; n = n.Parent) {
//				namespaces.Add(n.Namespace);
//				foreach (var u in n.Usings)
//					namespaces.Add(u);
//			}
//
//			foreach (var type in Compilation.GetAllTypeDefinitions ()) {
//				if (!generalLookup.IsAccessible(type, false))
//					continue;	
//				if (namespaces.Any(n => n.FullName == type.Namespace))
//					continue;
//				bool useFullName = false;
//				foreach (var ns in namespaces) {
//					if (ns.GetTypeDefinition(type.Name, type.TypeParameterCount) != null) {
//						useFullName = true;
//						break;
//					}
//				}
//				yield return factory.CreateImportCompletionData(type, useFullName, false);
//			}
//		}
		}
	
		public CompletionResult GetCompletionData(Document document, SemanticModel semanticModel, int position, bool forceCompletion, CancellationToken cancellationToken = default(CancellationToken))
		{
			var ctx = SyntaxContext.Create(workspace, document, semanticModel, position, cancellationToken);
			/*
			if (ctx.ContainingTypeDeclaration != null &&
			    ctx.ContainingTypeDeclaration.IsKind(SyntaxKind.EnumDeclaration)) {
				return CompletionResult.Empty;
			}*/
			
			/*var trivia = semanticModel.SyntaxTree.GetRoot(cancellationToken).FindTrivia(position - 1);
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
			}*/
			
//			if (ctx.LeftToken.CSharpContextualKind() == SyntaxKind.IdentifierToken &&
//				(ctx.LeftToken.Parent.Parent.CSharpKind() == SyntaxKind.Argument || 
//					ctx.LeftToken.Parent.Parent.CSharpKind() == SyntaxKind.TypeParameterList || 
//					ctx.LeftToken.Parent.Parent.CSharpKind() == SyntaxKind.CatchClause || 
//					ctx.LeftToken.Parent.Parent.CSharpKind() == SyntaxKind.VariableDeclaration || 
//					ctx.LeftToken.Parent.Parent.CSharpKind() == SyntaxKind.NamespaceDeclaration || 
//					ctx.LeftToken.Parent.Parent.CSharpKind() == SyntaxKind.CompilationUnit || 
//					ctx.LeftToken.Parent.Parent.CSharpKind() == SyntaxKind.Block || 
//					ctx.LeftToken.Parent.Parent.CSharpKind() == SyntaxKind.BracketedParameterList || 
//					ctx.LeftToken.Parent.Parent.CSharpKind() == SyntaxKind.QualifiedName || 
//					ctx.LeftToken.Parent.Parent.CSharpKind() == SyntaxKind.AnonymousObjectMemberDeclarator && ctx.TargetToken.CSharpKind() != SyntaxKind.EqualsToken || 
//					ctx.LeftToken.Parent.Parent.CSharpKind() == SyntaxKind.ParameterList
//				) ||
//				ctx.LeftToken.Parent is QualifiedNameSyntax &&
//				ctx.LeftToken.Parent.Parent != null &&
//				ctx.LeftToken.Parent.Parent.CSharpKind() == SyntaxKind.NamespaceDeclaration) {
//				return CompletionResult.Empty;
//			}
			var incompleteMemberSyntax = ctx.TargetToken.Parent as IncompleteMemberSyntax;
			if (incompleteMemberSyntax != null) {
				var mod = incompleteMemberSyntax.Modifiers.LastOrDefault();
				if (mod.IsParentKind(SyntaxKind.OverloadsKeyword))
					return HandleOverrideContext(ctx, semanticModel);
			}
			if (ctx.TargetToken.Parent != null) {
				incompleteMemberSyntax = ctx.TargetToken.Parent.Parent as IncompleteMemberSyntax;
				if (incompleteMemberSyntax != null) {
					var mod = incompleteMemberSyntax.Modifiers.LastOrDefault();
					if (incompleteMemberSyntax.ToString().ToLower().StartsWith("partial"))
						return HandlePartialContext(ctx, incompleteMemberSyntax, semanticModel);
				}


			}
			
			var text = document.GetTextAsync(cancellationToken).Result; 
			char lastLastChar = position >= 2 ? text [position - 2] : '\0';
			char lastChar = text [position - 1];
			/*
			if (trivia.IsKind(SyntaxKind.CommentTrivia)) {
				// should be SyntaxKind.SingleLineDocumentationCommentTrivia - but seems to be broken/not working
				// if that works in later version this work around can be removed.
				if (lastChar == '<' && trivia.ToFullString().StartsWith("'", StringComparison.Ordinal)) {
					return CompletionResult.Create(GetXmlDocumentationCompletionData(document, semanticModel, position, trivia));
				}
				return CompletionResult.Empty;
			}*/
			/*
			if (trivia.RawKind==(SyntaxKind.SingleLineDocumentationCommentTrivia)) {
				if (lastChar == '<') {
					return CompletionResult.Create(GetXmlDocumentationCompletionData(document, semanticModel, position, trivia));
				}
				return CompletionResult.Empty;
			}*/
			/*
			if (ctx.TargetToken.Parent != null && ctx.TargetToken.Parent.IsParentKind( SyntaxKind.ObjectCreationExpression)) {
				if (lastChar == ' ' || forceCompletion) {
					return HandleObjectCreationExpression(ctx, semanticModel, position, forceCompletion, cancellationToken);
				}
			}
			if (!forceCompletion && (char.IsLetter(lastLastChar) || lastLastChar == '_') &&
			    (char.IsLetterOrDigit(lastChar) || lastChar == '_')) {
				return CompletionResult.Empty;
			}

			if ((lastChar == '"' || lastChar == ':') && ctx.TargetToken.Parent != null && ctx.TargetToken.Parent.Parent != null && 
				ctx.TargetToken.Parent.Parent.IsParentKind(SyntaxKind.ArgumentList)) {
				return HandleStringFormatItems(document, semanticModel, position, ctx);
			}

			if (lastChar == ' ' && !char.IsWhiteSpace(lastLastChar)) {
				var str = ctx.TargetToken.ToFullString().Trim();
				switch (str) {
					case "yield":
						return HandleYieldStatementExpression();
				}

				// auto popup enum base types
				var parent = ctx.TargetToken.Parent;
				if (parent != null && parent.Parent != null && parent.IsParentKind(SyntaxKind.List ) && parent.Parent.IsParentKind(SyntaxKind.EnumBlock)) {
					var result2 = new CompletionResult();
					foreach (var handler in handlers)
						handler.GetCompletionData(result2, this, ctx, semanticModel, position, cancellationToken);
					return result2;
				}
				if (ctx.TargetToken.IsParentKind(SyntaxKind.UsingKeyword))
					forceCompletion = true;
			}
			
			if (!forceCompletion && lastChar != '#' && lastChar != '.' && !(lastChar == '>' && lastLastChar == '-') && !char.IsLetter(lastChar) && lastChar != '_')
				return CompletionResult.Empty;

			if (ctx.IsInsideNamingContext (lastChar == ' ' && !char.IsWhiteSpace(lastLastChar))) {
				if (forceCompletion)
					return HandleNamingContext(ctx);
				return CompletionResult.Empty;
			}*/
			/*
			if (ctx.TargetToken.Parent is AttributeArgumentListSyntax) {
				return HandleAttributeArgumentListSyntax(semanticModel, ctx);
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
			}*/

			// case lambda parameter (n1, $
			/*if (ctx.TargetToken.Parent != null && ctx.TargetToken.Parent.Parent != null &&
				ctx.TargetToken.Parent.Parent.IsParentKind(SyntaxKind.SingleLineFunctionLambdaExpression)) 
				return CompletionResult.Empty;
*/
//			if (ctx.TargetToken.IsKind(SyntaxKind.OpenBraceToken) && 
//				ctx.TargetToken.Parent != null && ctx.TargetToken.Parent.IsKind(SyntaxKind.AnonymousObjectCreationExpression)) {
//				return CompletionResult.Empty;
//			}
			/*
			if ((ctx.TargetToken.RawKind==(SyntaxKind.OpenBraceToken) || ctx.TargetToken.IsKind(SyntaxKind.CommaToken)) && 
				ctx.TargetToken.Parent != null && (
					ctx.TargetToken.Parent.RawKind==(SyntaxKind.CollectionInitializerExpression) ||
					ctx.TargetToken.Parent.RawKind==(SyntaxKind.ObjectInitializerExpression)
				)) {
				return HandleObjectInitializer(semanticModel, position, ctx, cancellationToken);
			}*/

			var result = new CompletionResult();

			foreach (var handler in handlers)
				if (handler is RoslynRecommendationsCompletionContextHandler)
					handler.GetCompletionData(result, this, ctx, semanticModel, position, cancellationToken);

			// prevent auto selection for "<number>." case
			if (ctx.TargetToken.IsParentKind(SyntaxKind.DotToken)) {
			//if(ctx.TargetToken.RawKind==(int)SyntaxKind.DotToken){
				var accessExpr = ctx.TargetToken.Parent as MemberAccessExpressionSyntax;
				if (accessExpr != null &&
					accessExpr.Expression != null &&
					accessExpr.Expression.RawKind==(int)SyntaxKind.NumericLiteralExpression)  {
					result.AutoSelect = false;
				}
			}
			/*
			if (ctx.LeftToken.Parent != null &&
				ctx.LeftToken.Parent.Parent != null &&
				ctx.TargetToken.Parent != null && !ctx.TargetToken.Parent.IsKind(SyntaxKind.NameEquals) &&
				ctx.LeftToken.Parent.Parent.RawKind==(SyntaxKind.AnonymousObjectMemberDeclarator))
				result.AutoSelect = false;
			*/
	
			if (ctx.TargetToken.IsParentKind(SyntaxKind.OpenParenToken))
				result.AutoSelect = false;

			foreach (var type in ctx.InferredTypes) {
				if (type.TypeKind == TypeKind.Delegate) {
					result.AutoSelect = false;
					break;
				}
			}
			
			return result;
		}

		CompletionResult HandleAttributeArgumentListSyntax(SemanticModel semanticModel, SyntaxContext ctx)
		{
			var result = new CompletionResult();
			var node = ctx.TargetToken.Parent.Parent;
			var typeNameText = node.ToFullString();
			var idx = typeNameText.IndexOf('(');
			if (idx >= 0)
				typeNameText = typeNameText.Substring(0, idx).Trim();
			var typeSyntax = SyntaxFactory.ParseTypeName(typeNameText);
			var info = semanticModel.GetSpeculativeTypeInfo(node.SpanStart, typeSyntax, SpeculativeBindingOption.BindAsTypeOrNamespace); 
			if (info.Type.TypeKind == TypeKind.Error)
				info = semanticModel.GetSpeculativeTypeInfo(node.SpanStart, SyntaxFactory.ParseTypeName(typeSyntax.ToFullString() + "Attribute"), SpeculativeBindingOption.BindAsTypeOrNamespace); 
			if (info.Type.TypeKind == TypeKind.Error)
				return result;
			var cache = new HashSet<string>();
			foreach (var member in info.Type.GetMembers()) {
				var property = member as IPropertySymbol;
				if (property != null) {
					var data = factory.CreateSymbolCompletionData(property);
					data.DisplayFlags |= DisplayFlags.NamedArgument;
					result.AddData(data);
					continue;
				}
				var field = member as IFieldSymbol;
				if (field != null) {
					var data = factory.CreateSymbolCompletionData(field);
					data.DisplayFlags |= DisplayFlags.NamedArgument;
					result.AddData(data);
					continue;
				}
				var method = member as IMethodSymbol;
				if (method != null && method.MethodKind == MethodKind.Constructor) {
					foreach (var p in method.Parameters) {
						var data = factory.CreateSymbolCompletionData(p);
						data.DisplayFlags |= DisplayFlags.NamedArgument;
						result.AddData(data);
						AddNamedParameterData(result, cache, p);
					}
				}
			}
			return result;
		}

		void AddNamedParameterData(CompletionResult result, HashSet<string> parametersAddedCache, IParameterSymbol p)
		{
			if (parametersAddedCache.Contains(p.Name))
				return;
			parametersAddedCache.Add(p.Name);
			result.AddData(Factory.CreateGenericData(p.Name + ":", GenericDataType.NamedParameter));
		}

		CompletionResult HandleEventAccessorContext()
		{
			var result = new CompletionResult();
			result.AddData(factory.CreateGenericData("add", GenericDataType.Keyword));
			result.AddData(factory.CreateGenericData("remove", GenericDataType.Keyword));
			return result;
		}

		static ITypeSymbol GetCurrentType(SyntaxContext ctx, SemanticModel semanticModel)
		{
			foreach (var f in semanticModel.Compilation.GlobalNamespace.GetMembers()) {
				foreach (var loc in f.Locations) {
					if (loc.SourceTree.FilePath == ctx.SyntaxTree.FilePath) {
						//if (loc.SourceSpan == ctx.ContainingTypeDeclaration.Identifier.Span) {
							return f as ITypeSymbol;
						//}
					}
				}
			}
			return null;
		}

		static bool HasOverridden(ISymbol original, ISymbol testSymbol)
		{
			if (original.Kind != testSymbol.Kind)
				return false;
			switch (testSymbol.Kind) {
				case SymbolKind.Method:
					return ((IMethodSymbol)testSymbol).OverriddenMethod == original;
				case SymbolKind.Property:
					return ((IPropertySymbol)testSymbol).OverriddenProperty == original;
				case SymbolKind.Event:
					return ((IEventSymbol)testSymbol).OverriddenEvent == original;
			}
			return false;
		}

		CompletionResult HandleOverrideContext(SyntaxContext ctx, SemanticModel semanticModel)
		{
			var result = new CompletionResult();
			//if (ctx.ContainingTypeDeclaration == null)
			//	return result;
			var curType = GetCurrentType(ctx, semanticModel);
			if (curType == null)
				return result;
			var incompleteMemberSyntax = ctx.TargetToken.Parent as IncompleteMemberSyntax;
			foreach (var m in curType.BaseType.GetMembers ()) {
				if (!m.IsOverride && !m.IsVirtual || m.ContainingType == curType)
					continue;
				// filter out the "Finalize" methods, because finalizers should be done with destructors.
				if (m.Kind == SymbolKind.Method && m.Name == "Finalize") {
					continue;
				}

				// check if the member is already implemented
				bool foundMember = curType.GetMembers().Any(cm => HasOverridden (m, cm));
				if (foundMember)
					continue;

/*				if (alreadyInserted.Contains(m))
					continue;
				alreadyInserted.Add(m);*/

				var data = factory.CreateNewOverrideCompletionData(
					incompleteMemberSyntax.SpanStart,
					curType,
					m
				);
				//data.CompletionCategory = col.GetCompletionCategory(m.DeclaringTypeDefinition);
				result.AddData(data); 
			}
			return result;
		}

		CompletionResult HandlePartialContext(SyntaxContext ctx, IncompleteMemberSyntax incompleteMemberSyntax, SemanticModel semanticModel)
		{
			var result = new CompletionResult();
			//if (ctx.ContainingTypeDeclaration == null)
			//	return result;
			var curType = GetCurrentType(ctx, semanticModel);
			if (curType == null)
				return result;
			foreach (var method in curType.GetMembers().OfType<IMethodSymbol>()) {
				// TODO: seems to be broken in roslyn :/
				if (method.PartialDefinitionPart != null && method.PartialImplementationPart == null) {
					var data = factory.CreatePartialCompletionData(
						incompleteMemberSyntax.SpanStart,
						curType,
						method
					);
					//data.CompletionCategory = col.GetCompletionCategory(m.DeclaringTypeDefinition);
					result.AddData(data); 
				}
			}

			return result;
//			var wrapper = new CompletionDataWrapper(this);
//			int declarationBegin = offset;
//			int j = declarationBegin;
//			for (int i = 0; i < 3; i++) {
//				switch (GetPreviousToken(ref j, true)) {
//					case "public":
//					case "protected":
//					case "private":
//					case "internal":
//					case "sealed":
//					case "override":
//					case "partial":
//					case "async":
//						declarationBegin = j;
//						break;
//					case "static":
//						return null; // don't add override completion for static members
//				}
//			}
//
//			var methods = new List<IUnresolvedMethod>();
//
//			foreach (var part in type.Parts) {
//				foreach (var method in part.Methods) {
//					if (method.BodyRegion.IsEmpty) {
//						if (GetImplementation(type, method) != null) {
//							continue;
//						}
//						methods.Add(method);
//					}
//				}	
//			}
//
//			foreach (var method in methods) {
//			} 
//
//			return wrapper.Result;
		}



		CompletionResult HandleObjectCreationExpression(SyntaxContext ctx, SemanticModel semanticModel, int position, bool isCtrlSpace, CancellationToken cancellationToken)
		{
			var result = new CompletionResult();
			foreach (var symbol in Recommender.GetRecommendedSymbolsAtPosition(semanticModel, position, Workspace, null, cancellationToken)) {
				result.AddData(Factory.CreateSymbolCompletionData(symbol));
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

		CompletionResult HandleNamingContext(SyntaxContext ctx)
		{
			var result = new CompletionResult();
			var token = ctx.TargetToken;
			/*
			if (token.Parent.IsKind(SyntaxKind.IdentifierName)) {
				var prev = ctx.TargetToken.GetPreviousToken();
				if (prev.Parent.IsKind(SyntaxKind.IdentifierName))
					token = prev;
			}
			if (token.Parent.IsKind(SyntaxKind.PredefinedType)) {			
				switch (token.RawKind) {
					case SyntaxKind.ObjectKeyword:
						result.AddData (Factory.CreateGenericData("o", GenericDataType.NameProposal));
						result.AddData (Factory.CreateGenericData("obj", GenericDataType.NameProposal));
						return result;
					case SyntaxKind.BoolKeyword:
						result.AddData (Factory.CreateGenericData("b", GenericDataType.NameProposal));
						result.AddData (Factory.CreateGenericData("pred", GenericDataType.NameProposal));
						return result;
					case SyntaxKind.DoubleKeyword:
					case SyntaxKind.FloatKeyword:
					case SyntaxKind.DecimalKeyword:
						result.AddData (Factory.CreateGenericData("d", GenericDataType.NameProposal));
						result.AddData (Factory.CreateGenericData("f", GenericDataType.NameProposal));
						result.AddData (Factory.CreateGenericData("m", GenericDataType.NameProposal));
						return result;
					default:
						result.AddData (Factory.CreateGenericData("i", GenericDataType.NameProposal));
						result.AddData (Factory.CreateGenericData("j", GenericDataType.NameProposal));
						result.AddData (Factory.CreateGenericData("k", GenericDataType.NameProposal));
						return result;
				}
			}*/
			var names = WordParser.BreakWords(token.ToFullString().Trim());

			var possibleName = new StringBuilder();
			for (int i = 0; i < names.Count; i++) {
				possibleName.Length = 0;
				for (int j = i; j < names.Count; j++) {
					if (string.IsNullOrEmpty(names [j])) {
						continue;
					}
					if (j == i) { 
						names [j] = Char.ToLower(names [j] [0]) + names [j].Substring(1);
					}
					possibleName.Append(names [j]);
				}
				result.AddData (Factory.CreateGenericData(possibleName.ToString(), GenericDataType.NameProposal));
			}
			return result;
		}

		CompletionResult HandleYieldStatementExpression()
		{
			var result = new CompletionResult();
			result.DefaultCompletionString = "return";

			result.AddData(factory.CreateGenericData("break", GenericDataType.Keyword));
			result.AddData(factory.CreateGenericData("return", GenericDataType.Keyword));
			return result;
		}
		
		CompletionResult HandlePropertyAccessorContext(bool isInsideAccessorDeclaration)
		{
			var result = new CompletionResult();
			result.AddData(factory.CreateGenericData("get", GenericDataType.Keyword));
			result.AddData(factory.CreateGenericData("set", GenericDataType.Keyword));
			foreach (var accessorModifier in new [] { "public", "internal", "protected", "private", "async" }) {
				result.AddData(factory.CreateGenericData(accessorModifier, GenericDataType.Keyword));
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

		CompletionResult HandleObjectInitializer(SemanticModel semanticModel, int position, SyntaxContext ctx, CancellationToken cancellationToken)
		{
			var objectCreationExpression = ctx.TargetToken.Parent.Parent;
			var info = semanticModel.GetSymbolInfo(objectCreationExpression);
			var result = new CompletionResult();

			bool hasObjectInitializers = false;
			bool hasArrayInitializers  = false;

			var objectCreation = objectCreationExpression as ObjectCreationExpressionSyntax;
			/*if (objectCreation != null && objectCreation.Initializer != null && objectCreation.Initializer.Expressions != null) {
				hasObjectInitializers = objectCreation.Initializer.Expressions.Count > 0 && objectCreation.Initializer.Expressions.Take(objectCreation.Initializer.Expressions.Count - 1).Any(arg => {
					return arg.IsKind(SyntaxKind.SimpleAssignmentExpression);
				});

				hasArrayInitializers = objectCreation.Initializer.Expressions.Count > 0 && objectCreation.Initializer.Expressions.Take(objectCreation.Initializer.Expressions.Count - 1).Any(arg => {
					return !arg.IsKind(SyntaxKind.SimpleAssignmentExpression);
				});
			}*/

			//	case 'new initalizer { xpr' or 'new initalizer { foo, ... , xpr'
			if (ctx.TargetToken.IsKind(SyntaxKind.OpenBraceToken) || ctx.TargetToken.IsKind(SyntaxKind.CommaToken)) {
				ITypeSymbol initializerType;
				if (info.CandidateReason == CandidateReason.LateBound) {
					if (info.CandidateSymbols.Length == 0)
						return CompletionResult.Empty;
					initializerType = info.CandidateSymbols.FirstOrDefault().GetReturnType();
				} else {
					if (info.Symbol == null)
						return CompletionResult.Empty;
					initializerType = info.Symbol.GetReturnType();
				}

				if (initializerType == null)
					return CompletionResult.Empty;

				if (!hasArrayInitializers) {
					foreach (var m in GetAllMembers (initializerType).Where(m => m.Kind == SymbolKind.Field)) {
						var f = m as IFieldSymbol;
						if (f != null && (f.IsReadOnly || f.IsConst))
							continue;

						if (semanticModel.IsAccessible(position, f)) {
							result.AddData(factory.CreateSymbolCompletionData(m));
							/*var data = contextList.AddMember(m);
							if (data != null)
								data.DisplayFlags |= DisplayFlags.NamedArgument;*/
						}
					}

					foreach (var m in GetAllMembers (initializerType).Where(m => m.Kind == SymbolKind.Property)) {
						var p = m as IPropertySymbol;
						if (p == null || p.SetMethod == null)
							continue;
						if (semanticModel.IsAccessible(position, p.SetMethod)) {
							result.AddData(factory.CreateSymbolCompletionData(p));
						}
						/*
							var data = contextList.AddMember(m);
							if (data != null)
								data.DisplayFlags |= DisplayFlags.NamedArgument;*/
					}
				}

				var type = semanticModel.Compilation.GetTypeSymbol("System.Collections", "IList", 0, cancellationToken); 
				if (type == null)
					return result;
				/*if (initializerType.AllInterfaces.Any(i => i == type) && (objectCreationExpression.IsKind(SyntaxKind.ArrayInitializerExpression) || !hasObjectInitializers)) {
					foreach (var handler in handlers)
						handler.GetCompletionData(result, this, ctx, semanticModel, position, cancellationToken);
				}*/
			}
		
			return result;
		}

		/*
		IEnumerable<ICompletionData> HandleMemberReferenceCompletion(ExpressionResult expr)
		{
			if (expr == null)
				return null;

			// do not auto select <number>. (but <number>.<number>.) (0.ToString() is valid)
			if (expr.Node is PrimitiveExpression) {
				var pexpr = (PrimitiveExpression)expr.Node;
				if (!(pexpr.Value is string || pexpr.Value is char) && !pexpr.LiteralValue.Contains('.')) {
					AutoSelect = false;
				}
			}
			var resolveResult = ResolveExpression(expr);

			if (resolveResult == null) {
				return null;
			}
			if (expr.Node is AstType) {

				// check for namespace names
				if (expr.Node.AncestorsAndSelf
					.TakeWhile(n => n is AstType)
					.Any(m => m.Role == NamespaceDeclaration.NamespaceNameRole))
					return null;

				// need to look at paren.parent because of "catch (<Type>.A" expression
				if (expr.Node.Parent != null && expr.Node.Parent.Parent is CatchClause)
					return HandleCatchClauseType(expr);
				return CreateTypeAndNamespaceCompletionData(
					location,
					resolveResult.Result,
					expr.Node,
					resolveResult.Resolver
				);
			}


			return CreateCompletionData(
				location,
				resolveResult.Result,
				expr.Node,
				resolveResult.Resolver
			);
		}

		bool IsInPreprocessorDirective()
		{
			var text = GetMemberTextToCaret().Item1;
			var miniLexer = new MiniLexer(text);
			miniLexer.Parse();
			return miniLexer.IsInPreprocessorDirective;
		}




		IEnumerable<ICompletionData> HandleToStringFormatItems()
		{
			var unit = ParseStub("\");", false);

			var invoke = unit.GetNodeAt<InvocationExpression>(location);
			if (invoke == null)
				return Enumerable.Empty<ICompletionData>();

			var resolveResult = ResolveExpression(new ExpressionResult(invoke, unit));
			var invokeResult = resolveResult.Result as InvocationResolveResult;
			if (invokeResult == null)
				return Enumerable.Empty<ICompletionData>();
			if (invokeResult.Member.Name == "ToString")
				return GetFormatCompletionData(invokeResult.Member.DeclaringType) ?? Enumerable.Empty<ICompletionData>();
			return Enumerable.Empty<ICompletionData>();
		}

		IEnumerable<ICompletionData> MagicKeyCompletion(char completionChar, bool controlSpace, out bool isComplete)
		{
			isComplete = false;
			ExpressionResolveResult resolveResult;
			switch (completionChar) {
				// Magic key completion
				case ':':
					var text = GetMemberTextToCaret();
					var lexer = new MiniLexer(text.Item1);
					lexer.Parse();
					if (lexer.IsInSingleComment ||
						lexer.IsInChar ||
						lexer.IsInMultiLineComment ||
						lexer.IsInPreprocessorDirective) {
						return Enumerable.Empty<ICompletionData>();
					}

					return HandleMemberReferenceCompletion(GetExpressionBeforeCursor());
				case '.':
					if (IsInsideCommentStringOrDirective()) {
						return Enumerable.Empty<ICompletionData>();
					}
					return HandleMemberReferenceCompletion(GetExpressionBeforeCursor());
				case '#':
					if (!IsInPreprocessorDirective())
						return null;
					return GetDirectiveCompletionData();
					// XML doc completion
				case '<':
					if (controlSpace) {
						return DefaultControlSpaceItems(ref isComplete);
					}
					return null;
				case '>':
					if (!IsInsideDocComment()) {
						if (offset > 2 && document.GetCharAt(offset - 2) == '-' && !IsInsideCommentStringOrDirective()) {
							return HandleMemberReferenceCompletion(GetExpressionBeforeCursor());
						}
						return null;
					}
					return null;

					// Parameter completion
				case '(':
					if (IsInsideCommentStringOrDirective()) {
						return null;
					}
					var invoke = GetInvocationBeforeCursor(true);
					if (invoke == null) {
						if (controlSpace)
							return DefaultControlSpaceItems(ref isComplete, invoke);
						return null;
					}
					if (invoke.Node is TypeOfExpression) {
						return CreateTypeList();
					}
					var invocationResult = ResolveExpression(invoke);
					if (invocationResult == null) {
						return null;
					}
					var methodGroup = invocationResult.Result as MethodGroupResolveResult;
					if (methodGroup != null) {
						return CreateParameterCompletion(
							methodGroup,
							invocationResult.Resolver,
							invoke.Node,
							invoke.Unit,
							0,
							controlSpace
						);
					}

					if (controlSpace) {
						return DefaultControlSpaceItems(ref isComplete, invoke);
					}
					return null;
				case '=':
					return controlSpace ? DefaultControlSpaceItems(ref isComplete) : null;
				case ',':
					int cpos2;
					if (!GetParameterCompletionCommandOffset(out cpos2)) { 
						return null;
					}
					//	completionContext = CompletionWidget.CreateCodeCompletionContext (cpos2);
					//	int currentParameter2 = MethodParameterDataProvider.GetCurrentParameterIndex (CompletionWidget, completionContext) - 1;
					//				return CreateParameterCompletion (CreateResolver (), location, ExpressionContext.MethodBody, provider.Methods, currentParameter);	
					break;

					// Completion on space:
				case ' ':
					int tokenIndex = offset;
					string token = GetPreviousToken(ref tokenIndex, false);
					if (IsInsideCommentStringOrDirective()) {
						return null;
					}
					// check propose name, for context <variable name> <ctrl+space> (but only in control space context)
					//IType isAsType = null;
					var isAsExpression = GetExpressionAt(offset);
					if (controlSpace && isAsExpression != null && isAsExpression.Node is VariableDeclarationStatement && token != "new") {
						var parent = isAsExpression.Node as VariableDeclarationStatement;
						var proposeNameList = new CompletionDataWrapper(this);
						if (parent.Variables.Count != 1)
							return DefaultControlSpaceItems(ref isComplete, isAsExpression, controlSpace);

						foreach (var possibleName in GenerateNameProposals (parent.Type)) {
							if (possibleName.Length > 0) {
								proposeNameList.Result.Add(factory.CreateLiteralCompletionData(possibleName.ToString()));
							}
						}

						AutoSelect = false;
						AutoCompleteEmptyMatch = false;
						isComplete = true;
						return proposeNameList.Result;
					}
					//				int tokenIndex = offset;
					//				string token = GetPreviousToken (ref tokenIndex, false);
					//				if (result.ExpressionContext == ExpressionContext.ObjectInitializer) {
					//					resolver = CreateResolver ();
					//					ExpressionContext exactContext = new NewCSharpExpressionFinder (dom).FindExactContextForObjectInitializer (document, resolver.Unit, Document.FileName, resolver.CallingType);
					//					IReturnType objectInitializer = ((ExpressionContext.TypeExpressionContext)exactContext).UnresolvedType;
					//					if (objectInitializer != null && objectInitializer.ArrayDimensions == 0 && objectInitializer.PointerNestingLevel == 0 && (token == "{" || token == ","))
					//						return CreateCtrlSpaceCompletionData (completionContext, result); 
					//				}
					if (token == "=") {
						int j = tokenIndex;
						string prevToken = GetPreviousToken(ref j, false);
						if (prevToken == "=" || prevToken == "+" || prevToken == "-" || prevToken == "!") {
							token = prevToken + token;
							tokenIndex = j;
						}
					}
					switch (token) {
						case "(":
						case ",":
							int cpos;
							if (!GetParameterCompletionCommandOffset(out cpos)) { 
								break;
							}
							int currentParameter = GetCurrentParameterIndex(cpos - 1, this.offset) - 1;
							if (currentParameter < 0) {
								return null;
							}
							invoke = GetInvocationBeforeCursor(token == "(");
							if (invoke == null) {
								return null;
							}
							invocationResult = ResolveExpression(invoke);
							if (invocationResult == null) {
								return null;
							}
							methodGroup = invocationResult.Result as MethodGroupResolveResult;
							if (methodGroup != null) {
								return CreateParameterCompletion(
									methodGroup,
									invocationResult.Resolver,
									invoke.Node,
									invoke.Unit,
									currentParameter,
									controlSpace);
							}
							return null;
						case "=":
						case "==":
						case "!=":
							GetPreviousToken(ref tokenIndex, false);
							var expressionOrVariableDeclaration = GetExpressionAt(tokenIndex);
							if (expressionOrVariableDeclaration == null) {
								return null;
							}
							resolveResult = ResolveExpression(expressionOrVariableDeclaration);
							if (resolveResult == null) {
								return null;
							}
							if (resolveResult.Result.Type.Kind == TypeKind.Enum) {
								var wrapper = new CompletionDataWrapper(this);
								AddContextCompletion(
									wrapper,
									resolveResult.Resolver,
									expressionOrVariableDeclaration.Node);
								AddEnumMembers(wrapper, resolveResult.Result.Type, resolveResult.Resolver);
								AutoCompleteEmptyMatch = false;
								return wrapper.Result;
							}
							//				
							//					if (resolvedType.FullName == DomReturnType.Bool.FullName) {
							//						CompletionDataList completionList = new ProjectDomCompletionDataList ();
							//						CompletionDataCollector cdc = new CompletionDataCollector (this, dom, completionList, Document.CompilationUnit, resolver.CallingType, location);
							//						completionList.AutoCompleteEmptyMatch = false;
							//						resolver.AddAccessibleCodeCompletionData (result.ExpressionContext, cdc);
							//						return completionList;
							//					}
							//					if (resolvedType.ClassType == ClassType.Delegate && token == "=") {
							//						CompletionDataList completionList = new ProjectDomCompletionDataList ();
							//						string parameterDefinition = AddDelegateHandlers (completionList, resolvedType);
							//						string varName = GetPreviousMemberReferenceExpression (tokenIndex);
							//						completionList.Add (new EventCreationCompletionData (document, varName, resolvedType, null, parameterDefinition, resolver.CallingMember, resolvedType));
							//						
							//						CompletionDataCollector cdc = new CompletionDataCollector (this, dom, completionList, Document.CompilationUnit, resolver.CallingType, location);
							//						resolver.AddAccessibleCodeCompletionData (result.ExpressionContext, cdc);
							//						foreach (var data in completionList) {
							//							if (data is MemberCompletionData) 
							//								((MemberCompletionData)data).IsDelegateExpected = true;
							//						}
							//						return completionList;
							//					}
							return null;
						case "+=":
						case "-=":
							var curTokenIndex = tokenIndex;
							GetPreviousToken(ref tokenIndex, false);

							expressionOrVariableDeclaration = GetExpressionAt(tokenIndex);
							if (expressionOrVariableDeclaration == null) {
								return null;
							}

							resolveResult = ResolveExpression(expressionOrVariableDeclaration);
							if (resolveResult == null) {
								return null;
							}


							var mrr = resolveResult.Result as MemberResolveResult;
							if (mrr != null) {
								var evt = mrr.Member as IEvent;
								if (evt == null) {
									return null;
								}
								var delegateType = evt.ReturnType;
								if (delegateType.Kind != TypeKind.Delegate) {
									return null;
								}

								var wrapper = new CompletionDataWrapper(this);
								if (currentType != null) {
									//							bool includeProtected = DomType.IncludeProtected (dom, typeFromDatabase, resolver.CallingType);
									foreach (var method in ctx.CurrentTypeDefinition.Methods) {
										if (MatchDelegate(delegateType, method)) {
											wrapper.AddMember(method);
											//									data.SetText (data.CompletionText + ";");
										}
									}
								}
								if (token == "+=") {
									string parameterDefinition = AddDelegateHandlers(
										wrapper,
										delegateType,
										optDelegateName: GuessEventHandlerMethodName(curTokenIndex)
									);
								}

								return wrapper.Result;
							}
							return null;
						case ":":
							if (currentMember == null) {
								token = GetPreviousToken(ref tokenIndex, false);
								token = GetPreviousToken(ref tokenIndex, false);
								if (token == "enum")
									return HandleEnumContext();
								var wrapper = new CompletionDataWrapper(this);
								AddTypesAndNamespaces(
									wrapper,
									GetState(),
									null,
									t =>  {
										if (currentType != null && currentType.ReflectionName.Equals(t.ReflectionName))
											return null;
										var def = t.GetDefinition();
										if (def != null && t.Kind != TypeKind.Interface && (def.IsSealed ||def.IsStatic))
											return null;
										return t;
									}
								);
								return wrapper.Result;
							}
							return null;
					}

					var keywordCompletion = HandleKeywordCompletion(tokenIndex, token);
					if (keywordCompletion == null && controlSpace) {
						goto default;
					}
					return keywordCompletion;
					// Automatic completion
				default:
					if (IsInsideCommentStringOrDirective()) {
						tokenIndex = offset;
						token = GetPreviousToken(ref tokenIndex, false);
						if (IsInPreprocessorDirective() && (token.Length == 1 && char.IsLetter(completionChar) || controlSpace)) {
							while (token != null && document.GetCharAt(tokenIndex - 1) != '#') {
								token = GetPreviousToken(ref tokenIndex, false);
							}
							if (token != null)
								return HandleKeywordCompletion(tokenIndex, token);
						}
						return null;
					}
					char prevCh = offset > 2 ? document.GetCharAt(offset - 2) : ';';
					char nextCh = offset < document.TextLength ? document.GetCharAt(offset) : ' ';
					const string allowedChars = ";,.[](){*}+-/%^?:&|~!<>=";

					if ((!Char.IsWhiteSpace(nextCh) && allowedChars.IndexOf(nextCh) < 0) || !(Char.IsWhiteSpace(prevCh) || allowedChars.IndexOf(prevCh) >= 0)) {
						if (!controlSpace)
							return null;
					}

					if (IsInLinqContext(offset)) {
						if (!controlSpace && !(char.IsLetter(completionChar) || completionChar == '_')) {
							return null;
						}
						tokenIndex = offset;
						token = GetPreviousToken(ref tokenIndex, false);
						// token last typed
						if (!char.IsWhiteSpace(completionChar) && !linqKeywords.Contains(token)) {
							token = GetPreviousToken(ref tokenIndex, false);
						}
						// token last typed

						if (linqKeywords.Contains(token)) {
							if (token == "from") {
								// after from no auto code completion.
								return null;
							}
							return DefaultControlSpaceItems(ref isComplete);
						}
						var dataList = new CompletionDataWrapper(this);
						AddKeywords(dataList, linqKeywords);
						return dataList.Result;
					}
					if (currentType != null && currentType.Kind == TypeKind.Enum) {
						if (!char.IsLetter(completionChar))
							return null;
						return HandleEnumContext();
					}
					var contextList = new CompletionDataWrapper(this);
					var identifierStart = GetExpressionAtCursor();
					if (!(char.IsLetter(completionChar) || completionChar == '_') && (!controlSpace || identifierStart == null)) {
						return controlSpace ? HandleAccessorContext() ?? DefaultControlSpaceItems(ref isComplete, identifierStart) : null;
					}

					if (identifierStart != null) {
						if (identifierStart.Node is TypeParameterDeclaration) {
							return null;
						}

						if (identifierStart.Node is MemberReferenceExpression) {
							return HandleMemberReferenceCompletion(
								new ExpressionResult(
									((MemberReferenceExpression)identifierStart.Node).Target,
									identifierStart.Unit
								)
							);
						}

						if (identifierStart.Node is Identifier) {
							if (identifierStart.Node.Parent is GotoStatement)
								return null;

							// May happen in variable names
							return controlSpace ? DefaultControlSpaceItems(ref isComplete, identifierStart) : null;
						}
						if (identifierStart.Node is VariableInitializer && location <= ((VariableInitializer)identifierStart.Node).NameToken.EndLocation) {
							return controlSpace ? HandleAccessorContext() ?? DefaultControlSpaceItems(ref isComplete, identifierStart) : null;
						}
						if (identifierStart.Node is CatchClause) {
							if (((CatchClause)identifierStart.Node).VariableNameToken.IsInside(location)) {
								return null;
							}
						}
						if (identifierStart.Node is AstType && identifierStart.Node.Parent is CatchClause) {
							return HandleCatchClauseType(identifierStart);
						}

						var pDecl = identifierStart.Node as ParameterDeclaration;
						if (pDecl != null && pDecl.Parent is LambdaExpression) {
							return null;
						}
					}


					// Do not pop up completion on identifier identifier (should be handled by keyword completion).
					tokenIndex = offset - 1;
					token = GetPreviousToken(ref tokenIndex, false);
					if (token == "class" || token == "interface" || token == "struct" || token == "enum" || token == "namespace") {
						// after these always follows a name
						return null;
					}
					var keywordresult = HandleCompletion(tokenIndex, token);
					if (keywordresult != null) {
						return keywordresult;
					}

					if ((!Char.IsWhiteSpace(nextCh) && allowedChars.IndexOf(nextCh) < 0) || !(Char.IsWhiteSpace(prevCh) || allowedChars.IndexOf(prevCh) >= 0)) {
						if (controlSpace)
							return DefaultControlSpaceItems(ref isComplete, identifierStart);
					}

					int prevTokenIndex = tokenIndex;
					var prevToken2 = GetPreviousToken(ref prevTokenIndex, false);
					if (prevToken2 == "delegate") {
						// after these always follows a name
						return null;
					}

					if (identifierStart == null && !string.IsNullOrEmpty(token) && !IsInsideCommentStringOrDirective() && (prevToken2 == ";" || prevToken2 == "{" || prevToken2 == "}")) {
						char last = token [token.Length - 1];
						if (char.IsLetterOrDigit(last) || last == '_' || token == ">") {
							return HandleKeywordCompletion(tokenIndex, token);
						}
					}
					if (identifierStart == null) {
						var accCtx = HandleAccessorContext();
						if (accCtx != null) {
							return accCtx;
						}
						return DefaultControlSpaceItems(ref isComplete, null, controlSpace);
					}
					CSharpResolver csResolver;
					AstNode n = identifierStart.Node;
					if (n.Parent is NamedArgumentExpression)
						n = n.Parent;

					if (n != null && n.Parent is AnonymousTypeCreateExpression) {
						AutoSelect = false;
					}

					// new { b$ } 
					if (n is IdentifierExpression && n.Parent is AnonymousTypeCreateExpression)
						return null;

					// Handle foreach (type name _
					if (n is IdentifierExpression) {
						var prev = n.GetPrevNode() as ForeachStatement;
						while (prev != null && prev.EmbeddedStatement is ForeachStatement)
							prev = (ForeachStatement)prev.EmbeddedStatement;
						if (prev != null && prev.InExpression.IsNull) {
							if (IncludeKeywordsInCompletionList)
								contextList.AddCustom("in");
							return contextList.Result;
						}
					}
					// Handle object/enumerable initialzer expressions: "new O () { P$"
					if (n is IdentifierExpression && n.Parent is ArrayInitializerExpression && !(n.Parent.Parent is ArrayCreateExpression)) {
						var result = HandleObjectInitializer(identifierStart.Unit, n);
						if (result != null)
							return result;
					}

					if (n != null && n.Parent is InvocationExpression ||
						n.Parent is ParenthesizedExpression && n.Parent.Parent is InvocationExpression) {
						if (n.Parent is ParenthesizedExpression)
							n = n.Parent;
						var invokeParent = (InvocationExpression)n.Parent;
						var invokeResult = ResolveExpression(
							invokeParent.Target
						);
						var mgr = invokeResult != null ? invokeResult.Result as MethodGroupResolveResult : null;
						if (mgr != null) {
							int idx = 0;
							foreach (var arg in invokeParent.Arguments) {
								if (arg == n) {
									break;
								}
								idx++;
							}

							foreach (var method in mgr.Methods) {
								if (idx < method.Parameters.Count && method.Parameters [idx].Type.Kind == TypeKind.Delegate) {
									AutoSelect = false;
									AutoCompleteEmptyMatch = false;
								}
								foreach (var p in method.Parameters) {
									contextList.AddNamedParameterVariable(p);
								}
							}
							idx++;
							foreach (var list in mgr.GetEligibleExtensionMethods (true)) {
								foreach (var method in list) {
									if (idx < method.Parameters.Count && method.Parameters [idx].Type.Kind == TypeKind.Delegate) {
										AutoSelect = false;
										AutoCompleteEmptyMatch = false;
									}
								}
							}
						}
					}

					if (n != null && n.Parent is ObjectCreateExpression) {
						var invokeResult = ResolveExpression(n.Parent);
						var mgr = invokeResult != null ? invokeResult.Result as ResolveResult : null;
						if (mgr != null) {
							foreach (var constructor in mgr.Type.GetConstructors ()) {
								foreach (var p in constructor.Parameters) {
									contextList.AddVariable(p);
								}
							}
						}
					}

					if (n is IdentifierExpression) {
						var bop = n.Parent as BinaryOperatorExpression;
						Expression evaluationExpr = null;

						if (bop != null && bop.Right == n && (bop.Operator == BinaryOperatorType.Equality || bop.Operator == BinaryOperatorType.InEquality)) {
							evaluationExpr = bop.Left;
						}
						// check for compare to enum case 
						if (evaluationExpr != null) {
							resolveResult = ResolveExpression(evaluationExpr);
							if (resolveResult != null && resolveResult.Result.Type.Kind == TypeKind.Enum) {
								var wrapper = new CompletionDataWrapper(this);
								AddContextCompletion(
									wrapper,
									resolveResult.Resolver,
									evaluationExpr
								);
								AddEnumMembers(wrapper, resolveResult.Result.Type, resolveResult.Resolver);
								AutoCompleteEmptyMatch = false;
								return wrapper.Result;
							}
						}
					}

					if (n is Identifier && n.Parent is ForeachStatement) {
						if (controlSpace) {
							return DefaultControlSpaceItems(ref isComplete);
						}
						return null;
					}

					if (n is ArrayInitializerExpression) {
						// check for new [] {...} expression -> no need to resolve the type there
						var parent = n.Parent as ArrayCreateExpression;
						if (parent != null && parent.Type.IsNull) {
							return DefaultControlSpaceItems(ref isComplete);
						}

						var initalizerResult = ResolveExpression(n.Parent);

						var concreteNode = identifierStart.Unit.GetNodeAt<IdentifierExpression>(location);
						// check if we're on the right side of an initializer expression
						if (concreteNode != null && concreteNode.Parent != null && concreteNode.Parent.Parent != null && concreteNode.Identifier != "a" && concreteNode.Parent.Parent is NamedExpression) {
							return DefaultControlSpaceItems(ref isComplete);
						}
						if (initalizerResult != null && initalizerResult.Result.Type.Kind != TypeKind.Unknown) { 

							foreach (var property in initalizerResult.Result.Type.GetProperties ()) {
								if (!property.IsPublic) {
									continue;
								}
								var data = contextList.AddMember(property);
								if (data != null)
									data.DisplayFlags |= DisplayFlags.NamedArgument;
							}
							foreach (var field in initalizerResult.Result.Type.GetFields ()) {       
								if (!field.IsPublic) {
									continue;
								}
								var data = contextList.AddMember(field);
								if (data != null)
									data.DisplayFlags |= DisplayFlags.NamedArgument;
							}
							return contextList.Result;
						}
						return DefaultControlSpaceItems(ref isComplete);
					}

					if (n is MemberType) {
						resolveResult = ResolveExpression(
							((MemberType)n).Target
						);
						return CreateTypeAndNamespaceCompletionData(
							location,
							resolveResult.Result,
							((MemberType)n).Target,
							resolveResult.Resolver
						);
					}
					if (n != null) {
						csResolver = new CSharpResolver(ctx);
						var nodes = new List<AstNode>();
						nodes.Add(n);
						if (n.Parent is IICSharpCode.NRefactory6.CSharp.Attribute) {
							nodes.Add(n.Parent);
						}
						var astResolver = CompletionContextProvider.GetResolver(csResolver, identifierStart.Unit);
						astResolver.ApplyNavigator(new NodeListResolveVisitorNavigator(nodes));
						try {
							csResolver = astResolver.GetResolverStateBefore(n);
						} catch (Exception) {
							csResolver = GetState();
						}
						// add attribute properties.
						if (n.Parent is IICSharpCode.NRefactory6.CSharp.Attribute) {
							var rr = ResolveExpression(n.Parent);
							if (rr != null)
								AddAttributeProperties(contextList, rr.Result);
						}
					} else {
						csResolver = GetState();
					}
					// identifier has already started with the first letter
					offset--;
					AddContextCompletion(
						contextList,
						csResolver,
						identifierStart.Node
					);
					return contextList.Result;
					//				if (stub.Parent is BlockStatement)

					//				result = FindExpression (dom, completionContext, -1);
					//				if (result == null)
					//					return null;
					//				 else if (result.ExpressionContext != ExpressionContext.IdentifierExpected) {
					//					triggerWordLength = 1;
					//					bool autoSelect = true;
					//					IType returnType = null;
					//					if ((prevCh == ',' || prevCh == '(') && GetParameterCompletionCommandOffset (out cpos)) {
					//						ctx = CompletionWidget.CreateCodeCompletionContext (cpos);
					//						NRefactoryParameterDataProvider dataProvider = ParameterCompletionCommand (ctx) as NRefactoryParameterDataProvider;
					//						if (dataProvider != null) {
					//							int i = dataProvider.GetCurrentParameterIndex (CompletionWidget, ctx) - 1;
					//							foreach (var method in dataProvider.Methods) {
					//								if (i < method.Parameters.Count) {
					//									returnType = dom.GetType (method.Parameters [i].ReturnType);
					//									autoSelect = returnType == null || returnType.ClassType != ClassType.Delegate;
					//									break;
					//								}
					//							}
					//						}
					//					}
					//					// Bug 677531 - Auto-complete doesn't always highlight generic parameter in method signature
					//					//if (result.ExpressionContext == ExpressionContext.TypeName)
					//					//	autoSelect = false;
					//					CompletionDataList dataList = CreateCtrlSpaceCompletionData (completionContext, result);
					//					AddEnumMembers (dataList, returnType);
					//					dataList.AutoSelect = autoSelect;
					//					return dataList;
					//				} else {
					//					result = FindExpression (dom, completionContext, 0);
					//					tokenIndex = offset;
					//					
					//					// check foreach case, unfortunately the expression finder is too dumb to handle full type names
					//					// should be overworked if the expression finder is replaced with a mcs ast based analyzer.
					//					var possibleForeachToken = GetPreviousToken (ref tokenIndex, false); // starting letter
					//					possibleForeachToken = GetPreviousToken (ref tokenIndex, false); // varname
					//				
					//					// read return types to '(' token
					//					possibleForeachToken = GetPreviousToken (ref tokenIndex, false); // varType
					//					if (possibleForeachToken == ">") {
					//						while (possibleForeachToken != null && possibleForeachToken != "(") {
					//							possibleForeachToken = GetPreviousToken (ref tokenIndex, false);
					//						}
					//					} else {
					//						possibleForeachToken = GetPreviousToken (ref tokenIndex, false); // (
					//						if (possibleForeachToken == ".")
					//							while (possibleForeachToken != null && possibleForeachToken != "(")
					//								possibleForeachToken = GetPreviousToken (ref tokenIndex, false);
					//					}
					//					possibleForeachToken = GetPreviousToken (ref tokenIndex, false); // foreach
					//				
					//					if (possibleForeachToken == "foreach") {
					//						result.ExpressionContext = ExpressionContext.ForeachInToken;
					//					} else {
					//						return null;
					//						//								result.ExpressionContext = ExpressionContext.IdentifierExpected;
					//					}
					//					result.Expression = "";
					//					result.Region = DomRegion.Empty;
					//				
					//					return CreateCtrlSpaceCompletionData (completionContext, result);
					//				}
					//				break;
			}
			return null;

		}

		IEnumerable<ICompletionData> HandleCatchClauseType(ExpressionResult identifierStart)
		{
			Func<IType, IType> typePred = delegate (IType type) {
				if (type.GetAllBaseTypes().Any(t => t.ReflectionName == "System.Exception"))
					return type;
				return null;
			};
			if (identifierStart.Node.Parent is CatchClause) {
				var wrapper = new CompletionDataWrapper(this);
				AddTypesAndNamespaces(
					wrapper,
					GetState(),
					identifierStart.Node,
					typePred,
					m => false
				);
				return wrapper.Result;
			}

			var resolveResult = ResolveExpression(identifierStart);
			return CreateCompletionData(
				location,
				resolveResult.Result,
				identifierStart.Node,
				resolveResult.Resolver,
				typePred
			);
		}

		IEnumerable<ICompletionData> HandleEnumContext()
		{
			var syntaxTree = ParseStub("a", false);
			if (syntaxTree == null) {
				return null;
			}

			var curType = syntaxTree.GetNodeAt<TypeDeclaration>(location);
			if (curType == null || curType.ClassType != ClassType.Enum) {
				syntaxTree = ParseStub("a {}", false);
				var node = syntaxTree.GetNodeAt<AstType>(location);
				if (node != null) {
					var wrapper = new CompletionDataWrapper(this);
					AddKeywords(wrapper, validEnumBaseTypes);
					return wrapper.Result;
				}
			}

			var member = syntaxTree.GetNodeAt<EnumMemberDeclaration>(location);
			if (member != null && member.NameToken.EndLocation < location) {
				if (currentMember == null && currentType != null) {
					foreach (var a in currentType.Members)
						if (a.Region.Begin < location && (currentMember == null || a.Region.Begin > currentMember.Region.Begin))
							currentMember = a;
				}
				bool isComplete = false;
				return DefaultControlSpaceItems(ref isComplete);
			}

			var attribute = syntaxTree.GetNodeAt<Attribute>(location);
			if (attribute != null) {
				var contextList = new CompletionDataWrapper(this);
				var astResolver = CompletionContextProvider.GetResolver(GetState(), syntaxTree);
				var csResolver = astResolver.GetResolverStateBefore(attribute);
				AddContextCompletion(
					contextList,
					csResolver,
					attribute
				);
				return contextList.Result;
			}
			return null;
		}

		bool IsInLinqContext(int offset)
		{
			string token;
			while (null != (token = GetPreviousToken(ref offset, true)) && !IsInsideCommentStringOrDirective()) {

				if (token == "from") {
					return !IsInsideCommentStringOrDirective(offset);
				}
				if (token == ";" || token == "{") {
					return false;
				}
			}
			return false;
		}

		class IfVisitor :DepthFirstAstVisitor
		{
			TextLocation loc;
			ICompletionContextProvider completionContextProvider;
			public bool IsValid;

			public IfVisitor(TextLocation loc, ICompletionContextProvider completionContextProvider)
			{
				this.loc = loc;
				this.completionContextProvider = completionContextProvider;

				this.IsValid = true;
			}

			void Check(string argument)
			{
				// TODO: evaluate #if epressions
				if (argument.Any(c => !(char.IsLetterOrDigit(c) || c == '_')))
					return;
				IsValid &= completionContextProvider.ConditionalSymbols.Contains(argument);
			}

			Stack<PreProcessorDirective> ifStack = new Stack<PreProcessorDirective>();

			public override void VisitPreProcessorDirective(PreProcessorDirective preProcessorDirective)
			{
				if (preProcessorDirective.Type == PreProcessorDirectiveType.If) {
					ifStack.Push(preProcessorDirective);
				} else if (preProcessorDirective.Type == PreProcessorDirectiveType.Endif) {
					if (ifStack.Count == 0)
						return;
					var ifDirective = ifStack.Pop();
					if (ifDirective.StartLocation < loc && loc < preProcessorDirective.EndLocation) {
						Check(ifDirective.Argument);
					}

				}

				base.VisitPreProcessorDirective(preProcessorDirective);
			}

			public void End()
			{
				while (ifStack.Count > 0) {
					Check(ifStack.Pop().Argument);
				}
			}
		}

		IEnumerable<ICompletionData> DefaultControlSpaceItems(ref bool isComplete, ExpressionResult xp = null, bool controlSpace = true)
		{
			var wrapper = new CompletionDataWrapper(this);
			if (offset >= document.TextLength) {
				offset = document.TextLength - 1;
			}
			while (offset > 1 && char.IsWhiteSpace(document.GetCharAt(offset))) {
				offset--;
			}
			location = document.GetLocation(offset);

			if (xp == null) {
				xp = GetExpressionAtCursor();
			}
			AstNode node;
			SyntaxTree unit;
			ExpressionResolveResult rr;
			if (xp != null) {
				node = xp.Node;
				rr = ResolveExpression(node);
				unit = xp.Unit;
			} else {
				unit = ParseStub("foo", false);
				node = unit.GetNodeAt(
					location.Line,
					location.Column + 2,
					n => n is Expression || n is AstType || n is NamespaceDeclaration || n is Attribute
				);
				rr = ResolveExpression(node);
			}
			var ifvisitor = new IfVisitor(location, CompletionContextProvider);
			unit.AcceptVisitor(ifvisitor);
			ifvisitor.End();
			if (!ifvisitor.IsValid)
				return null;
			// namespace name case
			var ns = node as NamespaceDeclaration;
			if (ns != null) {
				var last = ns.NamespaceName;
				if (last != null && location < last.EndLocation)
					return null;
			}
			if (node is Identifier && node.Parent is ForeachStatement) {
				var foreachStmt = (ForeachStatement)node.Parent;
				foreach (var possibleName in GenerateNameProposals (foreachStmt.VariableType)) {
					if (possibleName.Length > 0) {
						wrapper.Result.Add(factory.CreateLiteralCompletionData(possibleName.ToString()));
					}
				}

				AutoSelect = false;
				AutoCompleteEmptyMatch = false;
				isComplete = true;
				return wrapper.Result;
			}

			if (node is Identifier && node.Parent is ParameterDeclaration) {
				if (!controlSpace) {
					return null;
				}
				// Try Parameter name case 
				var param = node.Parent as ParameterDeclaration;
				if (param != null) {
					foreach (var possibleName in GenerateNameProposals (param.Type)) {
						if (possibleName.Length > 0) {
							wrapper.Result.Add(factory.CreateLiteralCompletionData(possibleName.ToString()));
						}
					}
					AutoSelect = false;
					AutoCompleteEmptyMatch = false;
					isComplete = true;
					return wrapper.Result;
				}
			}
			var pDecl = node as ParameterDeclaration;
			if (pDecl != null && pDecl.Parent is LambdaExpression) {
				return null;
			}


			var initializer = node != null ? node.Parent as ArrayInitializerExpression : null;
			if (initializer != null) {
				var result = HandleObjectInitializer(unit, initializer);
				if (result != null)
					return result;
			}
			CSharpResolver csResolver = null;
			if (rr != null) {
				csResolver = rr.Resolver;
			}

			if (csResolver == null) {
				if (node != null) {
					csResolver = GetState();
					//var astResolver = new CSharpAstResolver (csResolver, node, xp != null ? xp.Item1 : CSharpUnresolvedFile);

					try {
						//csResolver = astResolver.GetResolverStateBefore (node);
						Console.WriteLine(csResolver.LocalVariables.Count());
					} catch (Exception  e) {
						Console.WriteLine("E!!!" + e);
					}

				} else {
					csResolver = GetState();
				}
			}

			if (node is Attribute) {
				// add attribute properties.
				var astResolver = CompletionContextProvider.GetResolver(csResolver, unit);
				var resolved = astResolver.Resolve(node);
				AddAttributeProperties(wrapper, resolved);
			}


			if (node == null) {
				// try lambda
				unit = ParseStub("foo) => {}", true);
				var pd = unit.GetNodeAt<ParameterDeclaration>(
					location.Line,
					location.Column
				);
				if (pd != null) {
					var astResolver = unit != null ? CompletionContextProvider.GetResolver(GetState(), unit) : null;
					var parameterType = astResolver.Resolve(pd.Type);
					// Type <name> is always a name context -> return null
					if (parameterType != null && !parameterType.IsError)
						return null;
				}
			}

			AddContextCompletion(wrapper, csResolver, node);

			return wrapper.Result;
		}

		void AddContextCompletion(CompletionDataWrapper wrapper, CSharpResolver state, AstNode node)
		{
			int i = offset - 1;
			var isInGlobalDelegate = node == null && state.CurrentTypeDefinition == null && GetPreviousToken(ref i, true) == "delegate";

			if (state != null && !(node is AstType)) {
				foreach (var variable in state.LocalVariables) {
					if (variable.Region.IsInside(location.Line, location.Column - 1)) {
						continue;
					}
					wrapper.AddVariable(variable);
				}
			}

			if (state.CurrentMember is IParameterizedMember && !(node is AstType)) {
				var param = (IParameterizedMember)state.CurrentMember;
				foreach (var p in param.Parameters) {
					wrapper.AddVariable(p);
				}
			}

			if (state.CurrentMember is IMethod) {
				var method = (IMethod)state.CurrentMember;
				foreach (var p in method.TypeParameters) {
					wrapper.AddTypeParameter(p);
				}
			}

			Func<IType, IType> typePred = null;
			if (IsAttributeContext(node)) {
				var attribute = Compilation.FindType(KnownTypeCode.Attribute);
				typePred = t => t.GetAllBaseTypeDefinitions().Any(bt => bt.Equals(attribute)) ? t : null;
			}
			if (node != null && node.Role == Roles.BaseType) {
				typePred = t => {
					var def = t.GetDefinition();
					if (def != null && t.Kind != TypeKind.Interface && (def.IsSealed || def.IsStatic))
						return null;
					return t;
				};
			}

			if (node != null && !(node is NamespaceDeclaration) || state.CurrentTypeDefinition != null || isInGlobalDelegate) {
				AddTypesAndNamespaces(wrapper, state, node, typePred);

				wrapper.Result.Add(factory.CreateLiteralCompletionData("global"));
			}

			if (!(node is AstType)) {
				if (currentMember != null || node is Expression) {
					AddKeywords(wrapper, statementStartKeywords);
					if (LanguageVersion.Major >= 5)
						AddKeywords(wrapper, new [] { "await" });
					AddKeywords(wrapper, expressionLevelKeywords);
					if (node == null || node is TypeDeclaration)
						AddKeywords(wrapper, typeLevelKeywords);
				} else if (currentType != null) {
					AddKeywords(wrapper, typeLevelKeywords);
				} else {
					if (!isInGlobalDelegate && !(node is Attribute))
						AddKeywords(wrapper, globalLevelKeywords);
				}
				var prop = currentMember as IUnresolvedProperty;
				if (prop != null && prop.Setter != null && prop.Setter.Region.IsInside(location)) {
					wrapper.AddCustom("value");
				} 
				if (currentMember is IUnresolvedEvent) {
					wrapper.AddCustom("value");
				} 

				if (IsInSwitchContext(node)) {
					if (IncludeKeywordsInCompletionList)
						wrapper.AddCustom("case"); 
				}
			} else {
				if (((AstType)node).Parent is ParameterDeclaration) {
					AddKeywords(wrapper, parameterTypePredecessorKeywords);
				}
			}

			if (node != null || state.CurrentTypeDefinition != null || isInGlobalDelegate)
				AddKeywords(wrapper, primitiveTypesKeywords);
			if (currentMember != null && (node is IdentifierExpression || node is SimpleType) && (node.Parent is ExpressionStatement || node.Parent is ForeachStatement || node.Parent is UsingStatement)) {
=			} 
			wrapper.Result.AddRange(factory.CreateCodeTemplateCompletionData());
			if (node != null && node.Role == Roles.Argument) {
				var resolved = ResolveExpression(node.Parent);
				var invokeResult = resolved != null ? resolved.Result as CSharpInvocationResolveResult : null;
				if (invokeResult != null) {
					int argNum = 0;
					foreach (var arg in node.Parent.Children.Where (c => c.Role == Roles.Argument)) {
						if (arg == node) {
							break;
						}
						argNum++;
					}
					var param = argNum < invokeResult.Member.Parameters.Count ? invokeResult.Member.Parameters [argNum] : null;
					if (param != null && param.Type.Kind == TypeKind.Enum) {
						AddEnumMembers(wrapper, param.Type, state);
					}
				}
			}

			if (node is Expression) {
				var root = node;
				while (root.Parent != null)
					root = root.Parent;
				var astResolver = CompletionContextProvider.GetResolver(state, root);
				foreach (var type in TypeGuessing.GetValidTypes(astResolver, (Expression)node)) {
					if (type.Kind == TypeKind.Enum) {
						AddEnumMembers(wrapper, type, state);
					} else if (type.Kind == TypeKind.Delegate) {
						AddDelegateHandlers(wrapper, type, false, true);
						AutoSelect = false;
						AutoCompleteEmptyMatch = false;
					}
				}
			}

			// Add 'this' keyword for first parameter (extension method case)
			if (node != null && node.Parent is ParameterDeclaration &&
				node.Parent.PrevSibling != null && node.Parent.PrevSibling.Role == Roles.LPar && IncludeKeywordsInCompletionList) {
				wrapper.AddCustom("this");
			}
		}

		static bool IsInSwitchContext(AstNode node)
		{
			var n = node;
			while (n != null && !(n is EntityDeclaration)) {
				if (n is SwitchStatement) {
					return true;
				}
				if (n is BlockStatement) {
					return false;
				}
				n = n.Parent;
			}
			return false;
		}

		static bool ListEquals(List<INamespace> curNamespaces, List<INamespace> oldNamespaces)
		{
			if (oldNamespaces == null || curNamespaces.Count != oldNamespaces.Count)
				return false;
			for (int i = 0; i < curNamespaces.Count; i++) {
				if (curNamespaces [i].FullName != oldNamespaces [i].FullName) {
					return false;
				}
			}
			return true;
		}

		void AddTypesAndNamespaces(CompletionDataWrapper wrapper, CSharpResolver state, AstNode node, Func<IType, IType> typePred = null, Predicate<IMember> memberPred = null, Action<ICompletionData, IType> callback = null, bool onlyAddConstructors = false)
		{
			var lookup = new MemberLookup(ctx.CurrentTypeDefinition, Compilation.MainAssembly);

			if (currentType != null) {
				for (var ct = ctx.CurrentTypeDefinition; ct != null; ct = ct.DeclaringTypeDefinition) {
					foreach (var nestedType in ct.GetNestedTypes ()) {
						if (!lookup.IsAccessible(nestedType.GetDefinition(), true))
							continue;
						if (onlyAddConstructors) {
							if (!nestedType.GetConstructors().Any(c => lookup.IsAccessible(c, true)))
								continue;
						}

						if (typePred == null) {
							if (onlyAddConstructors)
								wrapper.AddConstructors(nestedType, false, IsAttributeContext(node));
							else
								wrapper.AddType(nestedType, false, IsAttributeContext(node));
							continue;
						}

						var type = typePred(nestedType);
						if (type != null) {
							var a2 = onlyAddConstructors ? wrapper.AddConstructors(type, false, IsAttributeContext(node)) : wrapper.AddType(type, false, IsAttributeContext(node));
							if (a2 != null && callback != null) {
								callback(a2, type);
							}
						}
						continue;
					}
				}

				if (this.currentMember != null && !(node is AstType)) {
					var def = ctx.CurrentTypeDefinition;
					if (def == null && currentType != null)
						def = Compilation.MainAssembly.GetTypeDefinition(currentType.FullTypeName);
					if (def != null) {
						bool isProtectedAllowed = true;

						foreach (var member in def.GetMembers (m => currentMember.IsStatic ? m.IsStatic : true)) {
							if (member is IMethod && ((IMethod)member).FullName == "System.Object.Finalize") {
								continue;
							}
							if (member.SymbolKind == SymbolKind.Operator) {
								continue;
							}
							if (member.IsExplicitInterfaceImplementation) {
								continue;
							}
							if (!lookup.IsAccessible(member, isProtectedAllowed)) {
								continue;
							}
							if (memberPred == null || memberPred(member)) {
								wrapper.AddMember(member);
							}
						}
						var declaring = def.DeclaringTypeDefinition;
						while (declaring != null) {
							foreach (var member in declaring.GetMembers (m => m.IsStatic)) {
								if (memberPred == null || memberPred(member)) {
									wrapper.AddMember(member);
								}
							}
							declaring = declaring.DeclaringTypeDefinition;
						}
					}
				}
				if (ctx.CurrentTypeDefinition != null) {
					foreach (var p in ctx.CurrentTypeDefinition.TypeParameters) {
						wrapper.AddTypeParameter(p);
					}
				}
			}
			var scope = ctx.CurrentUsingScope;

			for (var n = scope; n != null; n = n.Parent) {
				foreach (var pair in n.UsingAliases) {
					wrapper.AddAlias(pair.Key);
				}
				foreach (var alias in n.ExternAliases) {
					wrapper.AddAlias(alias);
				}
				foreach (var u in n.Usings) {
					foreach (var type in u.Types) {
						if (!lookup.IsAccessible(type, false))
							continue;

						IType addType = typePred != null ? typePred(type) : type;

						if (onlyAddConstructors && addType != null) {
							if (!addType.GetConstructors().Any(c => lookup.IsAccessible(c, true)))
								continue;
						}

						if (addType != null) {
							var a = onlyAddConstructors ? wrapper.AddConstructors(addType, false, IsAttributeContext(node)) : wrapper.AddType(addType, false, IsAttributeContext(node));
							if (a != null && callback != null) {
								callback(a, type);
							}
						}
					}
				}

				foreach (var type in n.Namespace.Types) {
					if (!lookup.IsAccessible(type, false))
						continue;
					IType addType = typePred != null ? typePred(type) : type;

					if (onlyAddConstructors && addType != null) {
						if (!addType.GetConstructors().Any(c => lookup.IsAccessible(c, true)))
							continue;
					}

					if (addType != null) {
						var a2 = onlyAddConstructors ? wrapper.AddConstructors(addType, false, IsAttributeContext(node)) : wrapper.AddType(addType, false);
						if (a2 != null && callback != null) {
							callback(a2, type);
						}
					}
				}
			}

			for (var n = scope; n != null; n = n.Parent) {
				foreach (var curNs in n.Namespace.ChildNamespaces) {
					wrapper.AddNamespace(lookup, curNs);
				}
			}

			if (AutomaticallyAddImports) {
				state = GetState();
				ICompletionData[] importData;

				var namespaces = new List<INamespace>();
				for (var n = ctx.CurrentUsingScope; n != null; n = n.Parent) {
					namespaces.Add(n.Namespace);
					foreach (var u in n.Usings)
						namespaces.Add(u);
				}

				if (this.CompletionEngineCache != null && ListEquals(namespaces, CompletionEngineCache.namespaces)) {
					importData = CompletionEngineCache.importCompletion;
				} else {
					// flatten usings
					var importList = new List<ICompletionData>();
					var dict = new Dictionary<string, Dictionary<string, ICompletionData>>();
					foreach (var type in Compilation.GetTopLevelTypeDefinitons ()) {
						if (!lookup.IsAccessible(type, false))
							continue;
						if (namespaces.Any(n => n.FullName == type.Namespace))
							continue;
						bool useFullName = false;
						foreach (var ns in namespaces) {
							if (ns.GetTypeDefinition(type.Name, type.TypeParameterCount) != null) {
								useFullName = true;
								break;
							}
						}

						if (onlyAddConstructors) {
							if (!type.GetConstructors().Any(c => lookup.IsAccessible(c, true)))
								continue;
						}
						var data = factory.CreateImportCompletionData(type, useFullName, onlyAddConstructors);
						Dictionary<string, ICompletionData> createdDict;
						if (!dict.TryGetValue(type.Name, out createdDict)) {
							createdDict = new Dictionary<string, ICompletionData>();
							dict.Add(type.Name, createdDict);
						}
						ICompletionData oldData;
						if (!createdDict.TryGetValue(type.Namespace, out oldData)) {
							importList.Add(data);
							createdDict.Add(type.Namespace, data);
						} else {
							oldData.AddOverload(data); 
						}
					}

					importData = importList.ToArray();
					if (CompletionEngineCache != null) {
						CompletionEngineCache.namespaces = namespaces;
						CompletionEngineCache.importCompletion = importData;
					}
				}
				foreach (var data in importData) {
					wrapper.Result.Add(data);
				}


			}

		}

		IEnumerable<ICompletionData> HandleKeywordCompletion(int wordStart, string word)
		{
			if (IsInsideCommentStringOrDirective()) {
				if (IsInPreprocessorDirective()) {
					if (word == "if" || word == "elif") {
						if (wordStart > 0 && document.GetCharAt(wordStart - 1) == '#') {
							return factory.CreatePreProcessorDefinesCompletionData();
						}
					}
				}
				return null;
			}
			switch (word) {
				case "namespace":
					return null;
				case "using":
					if (currentType != null) {
						return null;
					}
					var wrapper = new CompletionDataWrapper(this);
					AddTypesAndNamespaces(wrapper, GetState(), null, t => null);
					return wrapper.Result;
				case "case":
					return CreateCaseCompletionData(location);
					//				case ",":
					//				case ":":
					//					if (result.ExpressionContext == ExpressionContext.InheritableType) {
					//						IType cls = NRefactoryResolver.GetTypeAtCursor (Document.CompilationUnit, Document.FileName, new TextLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
					//						CompletionDataList completionList = new ProjectDomCompletionDataList ();
					//						List<string > namespaceList = GetUsedNamespaces ();
					//						var col = new CSharpTextEditorCompletion.CompletionDataCollector (this, dom, completionList, Document.CompilationUnit, null, location);
					//						bool isInterface = false;
					//						HashSet<string > baseTypeNames = new HashSet<string> ();
					//						if (cls != null) {
					//							baseTypeNames.Add (cls.Name);
					//							if (cls.ClassType == ClassType.Struct)
					//								isInterface = true;
					//						}
					//						int tokenIndex = offset;
					//	
					//						// Search base types " : [Type1, ... ,TypeN,] <Caret>"
					//						string token = null;
					//						do {
					//							token = GetPreviousToken (ref tokenIndex, false);
					//							if (string.IsNullOrEmpty (token))
					//								break;
					//							token = token.Trim ();
					//							if (Char.IsLetterOrDigit (token [0]) || token [0] == '_') {
					//								IType baseType = dom.SearchType (Document.CompilationUnit, cls, result.Region.Start, token);
					//								if (baseType != null) {
					//									if (baseType.ClassType != ClassType.Interface)
					//										isInterface = true;
					//									baseTypeNames.Add (baseType.Name);
					//								}
					//							}
					//						} while (token != ":");
					//						foreach (object o in dom.GetNamespaceContents (namespaceList, true, true)) {
					//							IType type = o as IType;
					//							if (type != null && (type.IsStatic || type.IsSealed || baseTypeNames.Contains (type.Name) || isInterface && type.ClassType != ClassType.Interface)) {
					//								continue;
					//							}
					//							if (o is Namespace && !namespaceList.Any (ns => ns.StartsWith (((Namespace)o).FullName)))
					//								continue;
					//							col.Add (o);
					//						}
					//						// Add inner classes
					//						Stack<IType > innerStack = new Stack<IType> ();
					//						innerStack.Push (cls);
					//						while (innerStack.Count > 0) {
					//							IType curType = innerStack.Pop ();
					//							if (curType == null)
					//								continue;
					//							foreach (IType innerType in curType.InnerTypes) {
					//								if (innerType != cls)
					//									// don't add the calling class as possible base type
					//									col.Add (innerType);
					//							}
					//							if (curType.DeclaringType != null)
					//								innerStack.Push (curType.DeclaringType);
					//						}
					//						return completionList;
					//					}
					//					break;
				case "is":
				case "as":
					if (currentType == null) {
						return null;
					}
					IType isAsType = null;
					var isAsExpression = GetExpressionAt(wordStart);
					if (isAsExpression != null) {
						var parent = isAsExpression.Node.Parent;
						if (parent is VariableInitializer) {
							parent = parent.Parent;
						}
						if (parent is VariableDeclarationStatement) {
							var resolved = ResolveExpression(parent);
							if (resolved != null) {
								isAsType = resolved.Result.Type;
							}
						}
					}
					var isAsWrapper = new CompletionDataWrapper(this);
					var def = isAsType != null ? isAsType.GetDefinition() : null;
					AddTypesAndNamespaces(
						isAsWrapper,
						GetState(),
						null,
						t => t.GetDefinition() == null || def == null || t.GetDefinition().IsDerivedFrom(def) ? t : null,
						m => false);
					AddKeywords(isAsWrapper, primitiveTypesKeywords);
					return isAsWrapper.Result;
					//					{
					//						CompletionDataList completionList = new ProjectDomCompletionDataList ();
					//						ExpressionResult expressionResult = FindExpression (dom, completionContext, wordStart - document.Caret.Offset);
					//						NRefactoryResolver resolver = CreateResolver ();
					//						ResolveResult resolveResult = resolver.Resolve (expressionResult, new TextLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset));
					//						if (resolveResult != null && resolveResult.ResolvedType != null) {
					//							CompletionDataCollector col = new CompletionDataCollector (this, dom, completionList, Document.CompilationUnit, resolver.CallingType, location);
					//							IType foundType = null;
					//							if (word == "as") {
					//								ExpressionContext exactContext = new NewCSharpExpressionFinder (dom).FindExactContextForAsCompletion (document, Document.CompilationUnit, Document.FileName, resolver.CallingType);
					//								if (exactContext is ExpressionContext.TypeExpressionContext) {
					//									foundType = resolver.SearchType (((ExpressionContext.TypeExpressionContext)exactContext).Type);
					//									AddAsCompletionData (col, foundType);
					//								}
					//							}
					//						
					//							if (foundType == null)
					//								foundType = resolver.SearchType (resolveResult.ResolvedType);
					//						
					//							if (foundType != null) {
					//								if (foundType.ClassType == ClassType.Interface)
					//									foundType = resolver.SearchType (DomReturnType.Object);
					//							
					//								foreach (IType type in dom.GetSubclasses (foundType)) {
					//									if (type.IsSpecialName || type.Name.StartsWith ("<"))
					//										continue;
					//									AddAsCompletionData (col, type);
					//								}
					//							}
					//							List<string > namespaceList = GetUsedNamespaces ();
					//							foreach (object o in dom.GetNamespaceContents (namespaceList, true, true)) {
					//								if (o is IType) {
					//									IType type = (IType)o;
					//									if (type.ClassType != ClassType.Interface || type.IsSpecialName || type.Name.StartsWith ("<"))
					//										continue;
					//	//								if (foundType != null && !dom.GetInheritanceTree (foundType).Any (x => x.FullName == type.FullName))
					//	//									continue;
					//									AddAsCompletionData (col, type);
					//									continue;
					//								}
					//								if (o is Namespace)
					//									continue;
					//								col.Add (o);
					//							}
					//							return completionList;
					//						}
					//						result.ExpressionContext = ExpressionContext.TypeName;
					//						return CreateCtrlSpaceCompletionData (completionContext, result);
					//					}
				case "public":
				case "protected":
				case "private":
				case "internal":
				case "sealed":
				case "static":
					var accessorContext = HandleAccessorContext();
					if (accessorContext != null) {
						return accessorContext;
					}
					return null;
				case "new":
					int j = offset - 4;
					//				string token = GetPreviousToken (ref j, true);

					IType hintType = null;
					var expressionOrVariableDeclaration = GetNewExpressionAt(j);
					if (expressionOrVariableDeclaration == null)
						return null;
					var astResolver = CompletionContextProvider.GetResolver(GetState(), expressionOrVariableDeclaration.Node.Ancestors.FirstOrDefault(n => n is EntityDeclaration || n is SyntaxTree));
					hintType = TypeGuessing.GetValidTypes(
						astResolver,
						expressionOrVariableDeclaration.Node
					).FirstOrDefault();

					return CreateConstructorCompletionData(hintType);
				case "in":
					var inList = new CompletionDataWrapper(this);

					var expr = GetExpressionAtCursor();
					if (expr == null)
						return null;
					var rr = ResolveExpression(expr);

					AddContextCompletion(
						inList,
						rr != null ? rr.Resolver : GetState(),
						expr.Node
					);
					return inList.Result;
			}
			return null;
		}

		bool IsLineEmptyUpToEol()
		{
			var line = document.GetLineByNumber(location.Line);
			for (int j = offset; j < line.EndOffset; j++) {
				char ch = document.GetCharAt(j);
				if (!char.IsWhiteSpace(ch)) {
					return false;
				}
			}
			return true;
		}

		string GetLineIndent(int lineNr)
		{
			var line = document.GetLineByNumber(lineNr);
			for (int j = line.Offset; j < line.EndOffset; j++) {
				char ch = document.GetCharAt(j);
				if (!char.IsWhiteSpace(ch)) {
					return document.GetText(line.Offset, j - line.Offset);
				}
			}
			return "";
		}
		//		static CSharpAmbience amb = new CSharpAmbience();
		class Category : CompletionCategory
		{
			public Category(string displayText, string icon) : base(displayText, icon)
			{
			}

			public override int CompareTo(CompletionCategory other)
			{
				return 0;
			}
		}


		bool IsTypeParameterInScope(IType hintType)
		{
			var tp = hintType as ITypeParameter;
			var ownerName = tp.Owner.ReflectionName;
			if (currentMember != null && ownerName == currentMember.ReflectionName)
				return true;
			var ot = currentType;
			while (ot != null) {
				if (ownerName == ot.ReflectionName)
					return true;
				ot = ot.DeclaringTypeDefinition;
			}
			return false;
		}


		IMethod GetImplementation(ITypeDefinition type, IUnresolvedMethod method)
		{
			foreach (var cur in type.Methods) {
				if (cur.Name == method.Name && cur.Parameters.Count == method.Parameters.Count && !cur.BodyRegion.IsEmpty) {
					bool equal = true;

					if (equal) {
						return cur;
					}
				}
			}
			return null;
		}

		void AddKeywords(CompletionDataWrapper wrapper, IEnumerable<string> keywords)
		{
			if (!IncludeKeywordsInCompletionList)
				return;
			foreach (string keyword in keywords) {
				if (wrapper.Result.Any(data => data.DisplayText == keyword))
					continue;
				wrapper.AddCustom(keyword);
			}
		}

		public string GuessEventHandlerMethodName(int tokenIndex)
		{
			string result = GetPreviousToken(ref tokenIndex, false);
			return "Handle" + result;
		}

		bool MatchDelegate(IType delegateType, IMethod method)
		{
			if (method.SymbolKind != SymbolKind.Method)
				return false;
			var delegateMethod = delegateType.GetDelegateInvokeMethod();
			if (delegateMethod == null || delegateMethod.Parameters.Count != method.Parameters.Count) {
				return false;
			}

			for (int i = 0; i < delegateMethod.Parameters.Count; i++) {
				if (!delegateMethod.Parameters [i].Type.Equals(method.Parameters [i].Type)) {
					return false;
				}
			}
			return true;
		}

		string AddDelegateHandlers(CompletionDataWrapper completionList, IType delegateType, bool addSemicolon = true, bool addDefault = true, string optDelegateName = null)
		{
			IMethod delegateMethod = delegateType.GetDelegateInvokeMethod();
			PossibleDelegates.Add(delegateMethod);
			var thisLineIndent = GetLineIndent(location.Line);
			string delegateEndString = EolMarker + thisLineIndent + "}" + (addSemicolon ? ";" : "");
			//bool containsDelegateData = completionList.Result.Any(d => d.DisplayText.StartsWith("delegate("));
			if (addDefault && !completionList.AnonymousDelegateAdded) {
				completionList.AnonymousDelegateAdded = true;
				var oldDelegate = completionList.Result.FirstOrDefault(cd => cd.DisplayText == "delegate");
				if (oldDelegate != null)
					completionList.Result.Remove(oldDelegate);
				completionList.AddCustom(
					"delegate",
					"Creates anonymous delegate.",
					"delegate {" + EolMarker + thisLineIndent + IndentString + "|" + delegateEndString
				).DisplayFlags |= DisplayFlags.MarkedBold;
				if (LanguageVersion.Major >= 5) {
					completionList.AddCustom(
						"async delegate",
						"Creates anonymous async delegate.",
						"async delegate {" + EolMarker + thisLineIndent + IndentString + "|" + delegateEndString
					).DisplayFlags |= DisplayFlags.MarkedBold;
				}
			}
			var sb = new StringBuilder("(");
			var sbWithoutTypes = new StringBuilder("(");
			var state = GetState();
			var builder = new TypeSystemAstBuilder(state);

			for (int k = 0; k < delegateMethod.Parameters.Count; k++) {

				if (k > 0) {
					sb.Append(", ");
					sbWithoutTypes.Append(", ");
				}
				var convertedParameter = builder.ConvertParameter(delegateMethod.Parameters [k]);
				if (convertedParameter.ParameterModifier == ParameterModifier.Params)
					convertedParameter.ParameterModifier = ParameterModifier.None;
				sb.Append(convertedParameter.ToString(FormattingPolicy));
				sbWithoutTypes.Append(delegateMethod.Parameters [k].Name);
			}

			sb.Append(")");
			sbWithoutTypes.Append(")");
			var signature = sb.ToString();
			if (!completionList.HasAnonymousDelegateAdded(signature)) {
				completionList.AddAnonymousDelegateAdded(signature);

				completionList.AddCustom(
					"delegate" + signature,
					"Creates anonymous delegate.",
					"delegate" + signature + " {" + EolMarker + thisLineIndent + IndentString + "|" + delegateEndString
				).DisplayFlags |= DisplayFlags.MarkedBold;
				if (LanguageVersion.Major >= 5) {
					completionList.AddCustom(
						"async delegate" + signature,
						"Creates anonymous async delegate.",
						"async delegate" + signature + " {" + EolMarker + thisLineIndent + IndentString + "|" + delegateEndString
					).DisplayFlags |= DisplayFlags.MarkedBold;
				}
				if (!completionList.Result.Any(data => data.DisplayText == sb.ToString())) {
					completionList.AddCustom(
						signature,
						"Creates typed lambda expression.",
						signature + " => |" + (addSemicolon ? ";" : "")
					).DisplayFlags |= DisplayFlags.MarkedBold;
					if (LanguageVersion.Major >= 5) {
						completionList.AddCustom(
							"async " + signature,
							"Creates typed async lambda expression.",
							"async " + signature + " => |" + (addSemicolon ? ";" : "")
						).DisplayFlags |= DisplayFlags.MarkedBold;
					}

					if (!delegateMethod.Parameters.Any(p => p.IsOut || p.IsRef) && !completionList.Result.Any(data => data.DisplayText == sbWithoutTypes.ToString())) {
						completionList.AddCustom(
							sbWithoutTypes.ToString(),
							"Creates lambda expression.",
							sbWithoutTypes + " => |" + (addSemicolon ? ";" : "")
						).DisplayFlags |= DisplayFlags.MarkedBold;
						if (LanguageVersion.Major >= 5) {
							completionList.AddCustom(
								"async " + sbWithoutTypes,
								"Creates async lambda expression.",
								"async " + sbWithoutTypes + " => |" + (addSemicolon ? ";" : "")
							).DisplayFlags |= DisplayFlags.MarkedBold;
						}
					}
				}

			}

			string varName = optDelegateName ?? "Handle" + delegateType.Name;

			var ecd = factory.CreateEventCreationCompletionData(varName, delegateType, null, signature, currentMember, currentType);
			ecd.DisplayFlags |= DisplayFlags.MarkedBold;
			completionList.Add(ecd);

			return sb.ToString();
		}

		bool IsAccessibleFrom(IEntity member, ITypeDefinition calledType, IMember currentMember, bool includeProtected)
		{
			if (currentMember == null) {
				return member.IsStatic || member.IsPublic;
			}
			//			if (currentMember is MonoDevelop.Projects.Dom.BaseResolveResult.BaseMemberDecorator) 
			//				return member.IsPublic | member.IsProtected;
			//		if (member.IsStatic && !IsStatic)
			//			return false;
			if (member.IsPublic || calledType != null && calledType.Kind == TypeKind.Interface && !member.IsProtected) {
				return true;
			}
			if (member.DeclaringTypeDefinition != null) {
				if (member.DeclaringTypeDefinition.Kind == TypeKind.Interface) { 
					return IsAccessibleFrom(
						member.DeclaringTypeDefinition,
						calledType,
						currentMember,
						includeProtected
					);
				}

				if (member.IsProtected && !(member.DeclaringTypeDefinition.IsProtectedOrInternal && !includeProtected)) {
					return includeProtected;
				}
			}
			if (member.IsInternal || member.IsProtectedAndInternal || member.IsProtectedOrInternal) {
				//var type1 = member is ITypeDefinition ? (ITypeDefinition)member : member.DeclaringTypeDefinition;
				//var type2 = currentMember is ITypeDefinition ? (ITypeDefinition)currentMember : currentMember.DeclaringTypeDefinition;
				bool result = true;
				// easy case, projects are the same

				return member.IsProtectedAndInternal ? includeProtected && result : result;
			}

			if (!(currentMember is IType) && (currentMember.DeclaringTypeDefinition == null || member.DeclaringTypeDefinition == null)) {
				return false;
			}

			// inner class 
			var declaringType = currentMember.DeclaringTypeDefinition;
			while (declaringType != null) {
				if (declaringType.ReflectionName == currentMember.DeclaringType.ReflectionName) {
					return true;
				}
				declaringType = declaringType.DeclaringTypeDefinition;
			}


			return currentMember.DeclaringTypeDefinition != null && member.DeclaringTypeDefinition.FullName == currentMember.DeclaringTypeDefinition.FullName;
		}


		IEnumerable<ICompletionData> CreateTypeAndNamespaceCompletionData(TextLocation location, ResolveResult resolveResult, AstNode resolvedNode, CSharpResolver state)
		{
			if (resolveResult == null || resolveResult.IsError) {
				return null;
			}
			var exprParent = resolvedNode.GetParent<Expression>();
			var unit = exprParent != null ? exprParent.GetParent<SyntaxTree>() : null;

			var astResolver = unit != null ? CompletionContextProvider.GetResolver(state, unit) : null;
			IType hintType = exprParent != null && astResolver != null ? 
				TypeGuessing.GetValidTypes(astResolver, exprParent).FirstOrDefault() :
				null;
			var result = new CompletionDataWrapper(this);
			var lookup = new MemberLookup(
				ctx.CurrentTypeDefinition,
				Compilation.MainAssembly
			);
			if (resolveResult is NamespaceResolveResult) {
				var nr = (NamespaceResolveResult)resolveResult;
				if (!(resolvedNode.Parent is UsingDeclaration || resolvedNode.Parent != null && resolvedNode.Parent.Parent is UsingDeclaration)) {
					foreach (var cl in nr.Namespace.Types) {
						if (hintType != null && hintType.Kind != TypeKind.Array && cl.Kind == TypeKind.Interface) {
							continue;
						}
						if (!lookup.IsAccessible(cl, false))
							continue;
						result.AddType(cl, false, IsAttributeContext(resolvedNode));
					}
				}
				foreach (var ns in nr.Namespace.ChildNamespaces) {
					result.AddNamespace(lookup, ns);
				}
			} else if (resolveResult is TypeResolveResult) {
				var type = resolveResult.Type;
				foreach (var nested in type.GetNestedTypes ()) {
					if (hintType != null && hintType.Kind != TypeKind.Array && nested.Kind == TypeKind.Interface) {
						continue;
					}
					var def = nested.GetDefinition();
					if (def != null && !lookup.IsAccessible(def, false))
						continue;
					result.AddType(nested, false);
				}
			}
			return result.Result;
		}

		IEnumerable<ICompletionData> CreateTypeList()
		{
			foreach (var cl in Compilation.RootNamespace.Types) {
				yield return factory.CreateTypeCompletionData(cl, false, false, false);
			}

			foreach (var ns in Compilation.RootNamespace.ChildNamespaces) {
				yield return factory.CreateNamespaceCompletionData(ns);
			}
		}

		void CreateParameterForInvocation(CompletionDataWrapper result, IMethod method, CSharpResolver state, int parameter, HashSet<string> addedEnums, HashSet<string> addedDelegates)
		{
			if (method.Parameters.Count <= parameter) {
				return;
			}
			var resolvedType = method.Parameters [parameter].Type;
			if (resolvedType.Kind == TypeKind.Enum) {
				if (addedEnums.Contains(resolvedType.ReflectionName)) {
					return;
				}
				addedEnums.Add(resolvedType.ReflectionName);
				AddEnumMembers(result, resolvedType, state);
				return;
			}

			if (resolvedType.Kind == TypeKind.Delegate) {
				if (addedDelegates.Contains(resolvedType.ReflectionName))
					return;
				AddDelegateHandlers(result, resolvedType, false, true, "Handle" + method.Parameters [parameter].Type.Name + method.Parameters [parameter].Name);
			}
		}

		IEnumerable<ICompletionData> CreateParameterCompletion(MethodGroupResolveResult resolveResult, CSharpResolver state, AstNode invocation, SyntaxTree unit, int parameter, bool controlSpace)
		{
			var result = new CompletionDataWrapper(this);
			var addedEnums = new HashSet<string>();
			var addedDelegates = new HashSet<string>();

			foreach (var method in resolveResult.Methods) {
				CreateParameterForInvocation(result, method, state, parameter, addedEnums, addedDelegates);
			}
			foreach (var methods in resolveResult.GetEligibleExtensionMethods (true)) {
				foreach (var method in methods) {
					if (resolveResult.Methods.Contains(method))
						continue;
					CreateParameterForInvocation(result, new ReducedExtensionMethod(method), state, parameter, addedEnums, addedDelegates);
				}
			}

			foreach (var method in resolveResult.Methods) {
				if (parameter < method.Parameters.Count && method.Parameters [parameter].Type.Kind == TypeKind.Delegate) {
					AutoSelect = false;
					AutoCompleteEmptyMatch = false;
				}
				foreach (var p in method.Parameters) {
					result.AddNamedParameterVariable(p);
				}
			}

			if (!controlSpace) {
				if (addedEnums.Count + addedDelegates.Count == 0) {
					return Enumerable.Empty<ICompletionData>();
				}
				AutoCompleteEmptyMatch = false;
				AutoSelect = false;
			}
			AddContextCompletion(result, state, invocation);

			//			resolver.AddAccessibleCodeCompletionData (ExpressionContext.MethodBody, cdc);
			//			if (addedDelegates.Count > 0) {
			//				foreach (var data in result.Result) {
			//					if (data is MemberCompletionData) 
			//						((MemberCompletionData)data).IsDelegateExpected = true;
			//				}
			//			}
			return result.Result;
		}

		IEnumerable<ICompletionData> CreateCompletionData(TextLocation location, ResolveResult resolveResult, AstNode resolvedNode, CSharpResolver state, Func<IType, IType> typePred = null)
		{
			if (resolveResult == null) {
				return null;
			}

			var lookup = new MemberLookup(
				ctx.CurrentTypeDefinition,
				Compilation.MainAssembly
			);

			if (resolveResult is NamespaceResolveResult) {
				var nr = (NamespaceResolveResult)resolveResult;
				var namespaceContents = new CompletionDataWrapper(this);

				foreach (var cl in nr.Namespace.Types) {
					if (!lookup.IsAccessible(cl, false))
						continue;
					IType addType = typePred != null ? typePred(cl) : cl;
					if (addType != null)
						namespaceContents.AddType(addType, false);
				}

				foreach (var ns in nr.Namespace.ChildNamespaces) {
					namespaceContents.AddNamespace(lookup, ns);
				}
				return namespaceContents.Result;
			}
			IType type = resolveResult.Type;

			if (type.Namespace == "System" && type.Name == "Void")
				return null;

			if (resolvedNode.Parent is PointerReferenceExpression && (type is PointerType)) {
				resolveResult = new OperatorResolveResult(((PointerType)type).ElementType, System.Linq.Expressions.ExpressionType.Extension, resolveResult);
			}

			//var typeDef = resolveResult.Type.GetDefinition();
			var result = new CompletionDataWrapper(this);
			bool includeStaticMembers = false;

			if (resolveResult is LocalResolveResult) {
				if (resolvedNode is IdentifierExpression) {
					var mrr = (LocalResolveResult)resolveResult;
					includeStaticMembers = mrr.Variable.Name == mrr.Type.Name;
				}
			}
			if (resolveResult is TypeResolveResult && type.Kind == TypeKind.Enum) {
				foreach (var field in type.GetFields ()) {
					if (!lookup.IsAccessible(field, false))
						continue;
					result.AddMember(field);
				}
				return result.Result;
			}

			bool isProtectedAllowed = lookup.IsProtectedAccessAllowed(resolveResult);
			bool skipNonStaticMembers = (resolveResult is TypeResolveResult);

			if (resolveResult is MemberResolveResult && resolvedNode is IdentifierExpression) {
				var mrr = (MemberResolveResult)resolveResult;
				includeStaticMembers = mrr.Member.Name == mrr.Type.Name;

				TypeResolveResult trr;
				if (state.IsVariableReferenceWithSameType(
					resolveResult,
					((IdentifierExpression)resolvedNode).Identifier,
					out trr
				)) {
					if (currentMember != null && mrr.Member.IsStatic ^ currentMember.IsStatic) {
						skipNonStaticMembers = true;

						if (trr.Type.Kind == TypeKind.Enum) {
							foreach (var field in trr.Type.GetFields ()) {
								if (lookup.IsAccessible(field, false))
									result.AddMember(field);
							}
							return result.Result;
						}
					}
				}
				// ADD Aliases
				var scope = ctx.CurrentUsingScope;

				for (var n = scope; n != null; n = n.Parent) {
					foreach (var pair in n.UsingAliases) {
						if (pair.Key == mrr.Member.Name) {
							foreach (var r in CreateCompletionData (location, pair.Value, resolvedNode, state)) {
								if (r is IEntityCompletionData && ((IEntityCompletionData)r).Entity is IMember) {
									result.AddMember((IMember)((IEntityCompletionData)r).Entity);
								} else {
									result.Add(r);
								}
							}
						}
					}
				}				


			}
			if (resolveResult is TypeResolveResult && (resolvedNode is IdentifierExpression || resolvedNode is MemberReferenceExpression)) {
				includeStaticMembers = true;
			}

			//			Console.WriteLine ("type:" + type +"/"+type.GetType ());
			//			Console.WriteLine ("current:" + ctx.CurrentTypeDefinition);
			//			Console.WriteLine ("IS PROT ALLOWED:" + isProtectedAllowed + " static: "+ includeStaticMembers);
			//			Console.WriteLine (resolveResult);
			//			Console.WriteLine ("node:" + resolvedNode);
			//			Console.WriteLine (currentMember !=  null ? currentMember.IsStatic : "currentMember == null");

			if (resolvedNode.Annotation<ObjectCreateExpression>() == null) {
				//tags the created expression as part of an object create expression.

				foreach (var member in lookup.GetAccessibleMembers (resolveResult)) {
					if (member.SymbolKind == SymbolKind.Indexer || member.SymbolKind == SymbolKind.Operator || member.SymbolKind == SymbolKind.Constructor || member.SymbolKind == SymbolKind.Destructor) {
						continue;
					}
					if (resolvedNode is BaseReferenceExpression && member.IsAbstract) {
						continue;
					}
					if (member is IType) {
						if (resolveResult is TypeResolveResult || includeStaticMembers) {
							if (!lookup.IsAccessible(member, isProtectedAllowed))
								continue;
							result.AddType((IType)member, false);
							continue;
						}
					}
					bool memberIsStatic = member.IsStatic;
					if (!includeStaticMembers && memberIsStatic && !(resolveResult is TypeResolveResult)) {
						//						Console.WriteLine ("skip static member: " + member.FullName);
						continue;
					}

					var field = member as IField;
					if (field != null) {
						memberIsStatic |= field.IsConst;
					}
					if (!memberIsStatic && skipNonStaticMembers) {
						continue;
					}

					if (member is IMethod && ((IMethod)member).FullName == "System.Object.Finalize") {
						continue;
					}
					if (member.SymbolKind == SymbolKind.Operator) {
						continue;
					}

					if (member is IMember) {
						result.AddMember((IMember)member);
					}
				}
			}

			if (!(resolveResult is TypeResolveResult || includeStaticMembers)) {
				foreach (var meths in state.GetExtensionMethods (type)) {
					foreach (var m in meths) {
						if (!lookup.IsAccessible(m, isProtectedAllowed))
							continue;
						result.AddMember(new ReducedExtensionMethod(m));
					}
				}
			}

			//			IEnumerable<object> objects = resolveResult.CreateResolveResult (dom, resolver != null ? resolver.CallingMember : null);
			//			CompletionDataCollector col = new CompletionDataCollector (this, dom, result, Document.CompilationUnit, resolver != null ? resolver.CallingType : null, location);
			//			col.HideExtensionParameter = !resolveResult.StaticResolve;
			//			col.NamePrefix = expressionResult.Expression;
			//			bool showOnlyTypes = expressionResult.Contexts.Any (ctx => ctx == ExpressionContext.InheritableType || ctx == ExpressionContext.Constraints);
			//			if (objects != null) {
			//				foreach (object obj in objects) {
			//					if (expressionResult.ExpressionContext != null && expressionResult.ExpressionContext.FilterEntry (obj))
			//						continue;
			//					if (expressionResult.ExpressionContext == ExpressionContext.NamespaceNameExcepted && !(obj is Namespace))
			//						continue;
			//					if (showOnlyTypes && !(obj is IType))
			//						continue;
			//					CompletionData data = col.Add (obj);
			//					if (data != null && expressionResult.ExpressionContext == ExpressionContext.Attribute && data.CompletionText != null && data.CompletionText.EndsWith ("Attribute")) {
			//						string newText = data.CompletionText.Substring (0, data.CompletionText.Length - "Attribute".Length);
			//						data.SetText (newText);
			//					}
			//				}
			//			}

			return result.Result;
		}

		IEnumerable<ICompletionData> CreateCaseCompletionData(TextLocation location)
		{
			var unit = ParseStub("a: break;");
			if (unit == null) {
				return null;
			}
			var s = unit.GetNodeAt<SwitchStatement>(location);
			if (s == null) {
				return null;
			}

			var offset = document.GetOffset(s.Expression.StartLocation);
			var expr = GetExpressionAt(offset);
			if (expr == null) {
				return null;
			}

			var resolveResult = ResolveExpression(expr);
			if (resolveResult == null || resolveResult.Result.Type.Kind != TypeKind.Enum) { 
				return null;
			}
			var wrapper = new CompletionDataWrapper(this);
			AddEnumMembers(wrapper, resolveResult.Result.Type, resolveResult.Resolver);
			AutoCompleteEmptyMatch = false;
			return wrapper.Result;
		}

		#region Parsing methods

		ExpressionResult GetExpressionBeforeCursor()
		{
			SyntaxTree baseUnit;
			if (currentMember == null) {
				baseUnit = ParseStub("a", false);
				var type = baseUnit.GetNodeAt<MemberType>(location);
				if (type == null) {
					baseUnit = ParseStub("a;", false);
					type = baseUnit.GetNodeAt<MemberType>(location);
				}

				if (type == null) {
					baseUnit = ParseStub("A a;", false);
					type = baseUnit.GetNodeAt<MemberType>(location);
				}
				if (type != null) {
					return new ExpressionResult((AstNode)type.Target, baseUnit);
				}
			}

			baseUnit = ParseStub("ToString()", false);
			var curNode = baseUnit.GetNodeAt(location);
			// hack for local variable declaration missing ';' issue - remove that if it works.
			if (curNode is EntityDeclaration || baseUnit.GetNodeAt<Expression>(location) == null && baseUnit.GetNodeAt<MemberType>(location) == null) {
				baseUnit = ParseStub("a");
				curNode = baseUnit.GetNodeAt(location);
			}

			// Hack for handle object initializer continuation expressions
			if (curNode is EntityDeclaration || baseUnit.GetNodeAt<Expression>(location) == null && baseUnit.GetNodeAt<MemberType>(location) == null) {
				baseUnit = ParseStub("a};");
			}
			var mref = baseUnit.GetNodeAt<MemberReferenceExpression>(location); 
			if (currentMember == null && currentType == null) {
				if (mref != null) {
					return new ExpressionResult((AstNode)mref.Target, baseUnit);
				}
				return null;
			}

			//var memberLocation = currentMember != null ? currentMember.Region.Begin : currentType.Region.Begin;
			if (mref == null) {
				var type = baseUnit.GetNodeAt<MemberType>(location); 
				if (type != null) {
					return new ExpressionResult((AstNode)type.Target, baseUnit);
				}

				var pref = baseUnit.GetNodeAt<PointerReferenceExpression>(location); 
				if (pref != null) {
					return new ExpressionResult((AstNode)pref.Target, baseUnit);
				}
			}

			if (mref == null) {
				baseUnit = ParseStub("A a;", false);
				var type = baseUnit.GetNodeAt<MemberType>(location);
				if (type != null) {
					return new ExpressionResult((AstNode)type.Target, baseUnit);
				}
			}

			AstNode expr = null;
			if (mref != null) {
				expr = mref.Target;
			} else {
				Expression tref = baseUnit.GetNodeAt<TypeReferenceExpression>(location); 
				MemberType memberType = tref != null ? ((TypeReferenceExpression)tref).Type as MemberType : null;
				if (memberType == null) {
					memberType = baseUnit.GetNodeAt<MemberType>(location); 
					if (memberType != null) {
						if (memberType.Parent is ObjectCreateExpression) {
							var mt = memberType.Target.Clone();
							memberType.ReplaceWith(mt);
							expr = mt;
							goto exit;
						} else {
							tref = baseUnit.GetNodeAt<Expression>(location); 
							if (tref == null) {
								tref = new TypeReferenceExpression(memberType.Clone());
								memberType.Parent.AddChild(tref, Roles.Expression);
							}
							if (tref is ObjectCreateExpression) {
								expr = memberType.Target.Clone();
								expr.AddAnnotation(new ObjectCreateExpression());
							}
						}
					}
				}

				if (memberType == null) {
					return null;
				}
				if (expr == null) {
					expr = memberType.Target.Clone();
				}
				tref.ReplaceWith(expr);
			}
			exit:
			return new ExpressionResult((AstNode)expr, baseUnit);
		}

		ExpressionResult GetExpressionAtCursor()
		{
			//			TextLocation memberLocation;
			//			if (currentMember != null) {
			//				memberLocation = currentMember.Region.Begin;
			//			} else if (currentType != null) {
			//				memberLocation = currentType.Region.Begin;
			//			} else {
			//				memberLocation = location;
			//			}
			var baseUnit = ParseStub("a");
			var tmpUnit = baseUnit;
			AstNode expr = baseUnit.GetNodeAt(
				location,
				n => n is IdentifierExpression || n is MemberReferenceExpression
			);

			if (expr == null) {
				expr = baseUnit.GetNodeAt<AstType>(location.Line, location.Column - 1);
			}
			if (expr == null)
				expr = baseUnit.GetNodeAt<Identifier>(location.Line, location.Column - 1);
			// try insertStatement
			if (expr == null && baseUnit.GetNodeAt<EmptyStatement>(location.Line, location.Column) != null) {
				tmpUnit = baseUnit = ParseStub("a();", false);
				expr = baseUnit.GetNodeAt<InvocationExpression>(
					location.Line,
					location.Column + 1
				); 
			}

			if (expr == null) {
				baseUnit = ParseStub("()");
				expr = baseUnit.GetNodeAt<IdentifierExpression>(
					location.Line,
					location.Column - 1
				); 
				if (expr == null) {
					expr = baseUnit.GetNodeAt<MemberType>(location.Line, location.Column - 1); 
				}
			}

			if (expr == null) {
				baseUnit = ParseStub("a", false);
				expr = baseUnit.GetNodeAt(
					location,
					n => n is IdentifierExpression || n is MemberReferenceExpression || n is CatchClause
				);
			}

			// try statement 
			if (expr == null) {
				expr = tmpUnit.GetNodeAt<SwitchStatement>(
					location.Line,
					location.Column - 1
				); 
				baseUnit = tmpUnit;
			}

			if (expr == null) {
				var block = tmpUnit.GetNodeAt<BlockStatement>(location); 
				var node = block != null ? block.Statements.LastOrDefault() : null;

				var forStmt = node != null ? node.PrevSibling as ForStatement : null;
				if (forStmt != null && forStmt.EmbeddedStatement.IsNull) {
					expr = forStmt;
					var id = new IdentifierExpression("stub");
					forStmt.EmbeddedStatement = new BlockStatement() { Statements = { new ExpressionStatement(id) } };
					expr = id;
					baseUnit = tmpUnit;
				}
			}

			if (expr == null) {
				var forStmt = tmpUnit.GetNodeAt<ForeachStatement>(
					location.Line,
					location.Column - 3
				); 
				if (forStmt != null && forStmt.EmbeddedStatement.IsNull) {
					forStmt.VariableNameToken = Identifier.Create("stub");
					expr = forStmt.VariableNameToken;
					baseUnit = tmpUnit;
				}
			}
			if (expr == null) {
				expr = tmpUnit.GetNodeAt<VariableInitializer>(
					location.Line,
					location.Column - 1
				);
				baseUnit = tmpUnit;
			}

			// try parameter declaration type
			if (expr == null) {
				baseUnit = ParseStub(">", false, "{}");
				expr = baseUnit.GetNodeAt<TypeParameterDeclaration>(
					location.Line,
					location.Column - 1
				); 
			}

			// try parameter declaration method
			if (expr == null) {
				baseUnit = ParseStub("> ()", false, "{}");
				expr = baseUnit.GetNodeAt<TypeParameterDeclaration>(
					location.Line,
					location.Column - 1
				); 
			}

			// try expression in anonymous type "new { sample = x$" case
			if (expr == null) {
				baseUnit = ParseStub("a", false);
				expr = baseUnit.GetNodeAt<AnonymousTypeCreateExpression>(
					location.Line,
					location.Column
				); 
				if (expr != null) {
					expr = baseUnit.GetNodeAt<Expression>(location.Line, location.Column) ?? expr;
				} 
				if (expr == null) {
					expr = baseUnit.GetNodeAt<AstType>(location.Line, location.Column);
				} 
			}

			// try lambda 
			if (expr == null) {
				baseUnit = ParseStub("foo) => {}", false);
				expr = baseUnit.GetNodeAt<ParameterDeclaration>(
					location.Line,
					location.Column
				); 
			}

			if (expr == null)
				return null;
			return new ExpressionResult(expr, baseUnit);
		}

		ExpressionResult GetExpressionAt(int offset)
		{
			var parser = new CSharpParser();
			var text = GetMemberTextToCaret(); 

			int closingBrackets = 0, generatedLines = 0;
			var sb = CreateWrapper("a;", false, "", text.Item1, text.Item2, ref closingBrackets, ref generatedLines);

			var completionUnit = parser.Parse(sb.ToString());
			var offsetLocation = document.GetLocation(offset);
			var loc = new TextLocation(offsetLocation.Line - text.Item2.Line + generatedLines + 1, offsetLocation.Column);

			var expr = completionUnit.GetNodeAt(
				loc,
				n => n is Expression || n is VariableDeclarationStatement
			);
			if (expr == null)
				return null;
			return new ExpressionResult(expr, completionUnit);
		}

		ExpressionResult GetNewExpressionAt(int offset)
		{
			var parser = new CSharpParser();
			var text = GetMemberTextToCaret();
			int closingBrackets = 0, generatedLines = 0;
			var sb = CreateWrapper("a ();", false, "", text.Item1, text.Item2, ref closingBrackets, ref generatedLines);

			var completionUnit = parser.Parse(sb.ToString());
			var offsetLocation = document.GetLocation(offset);
			var loc = new TextLocation(offsetLocation.Line - text.Item2.Line + generatedLines + 1, offsetLocation.Column);

			var expr = completionUnit.GetNodeAt(loc, n => n is Expression);
			if (expr == null) {
				// try without ";"
				sb = CreateWrapper("a ()", false, "", text.Item1, text.Item2, ref closingBrackets, ref generatedLines);
				completionUnit = parser.Parse(sb.ToString());

				expr = completionUnit.GetNodeAt(loc, n => n is Expression);
				if (expr == null) {
					return null;
				}
			}
			return new ExpressionResult(expr, completionUnit);
		}

		#endregion

		#region Helper methods

		string GetPreviousToken(ref int i, bool allowLineChange)
		{
			char c;
			if (i <= 0) {
				return null;
			}

			do {
				c = document.GetCharAt(--i);
			} while (i > 0 && char.IsWhiteSpace(c) && (allowLineChange ? true : c != '\n'));

			if (i == 0) {
				return null;
			}

			if (!char.IsLetterOrDigit(c)) {
				return new string(c, 1);
			}

			int endOffset = i + 1;

			do {
				c = document.GetCharAt(i - 1);
				if (!(char.IsLetterOrDigit(c) || c == '_')) {
					break;
				}

				i--;
			} while (i > 0);

			return document.GetText(i, endOffset - i);
		}

		#endregion

		#region Xml Comments


		#endregion


*/
	}

}

