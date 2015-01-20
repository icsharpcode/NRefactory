// 
// CSharpParameterCompletionEngine.cs
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
using System.CodeDom;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.NRefactory6.VisualBasic.Completion
{
	public class ParameterHintingEngine
	{
		readonly IParameterHintingDataFactory factory;
		readonly Workspace workspace;
	
		public ParameterHintingEngine(Workspace workspace, IParameterHintingDataFactory factory)
		{
			if (workspace == null)
				throw new ArgumentNullException("workspace");
			if (factory == null)
				throw new ArgumentNullException("factory");
			this.workspace = workspace;
			this.factory = factory;
		}
		
		public ParameterHintingResult GetParameterDataProvider(Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken = default(CancellationToken))
		{
			var context = SyntaxContext.Create(workspace, document, semanticModel, position, cancellationToken);
			var targetParent = context.TargetToken.Parent;
			var node = targetParent.Parent;
			// case: identifier<arg1,|
			/*if (node == null) {
				if (context.LeftToken.CSharpKind() == SyntaxKind.CommaToken) {
					targetParent = context.LeftToken.GetPreviousToken().Parent;
					node = targetParent.Parent;
					if (node.CSharpKind() == SyntaxKind.LessThanExpression) {
						return HandlePossibleTypeParameterCase(semanticModel, (BinaryExpressionSyntax)node, cancellationToken);
					}
				}
				return ParameterHintingResult.Empty;
			}
			
			switch (node.CSharpKind()) {
				case SyntaxKind.Attribute:
					return HandleAttribute(semanticModel, node, cancellationToken);					
				case SyntaxKind.ThisConstructorInitializer:
				case SyntaxKind.BaseConstructorInitializer:
					return HandleConstructorInitializer(semanticModel, node, cancellationToken);
				case SyntaxKind.ObjectCreationExpression:
					return HandleObjectCreationExpression(semanticModel, node, cancellationToken);
				case SyntaxKind.InvocationExpression:
					return HandleInvocationExpression(semanticModel, node, cancellationToken);
				case SyntaxKind.ElementAccessExpression:
					return HandleElementAccessExpression(semanticModel, node, cancellationToken);
			}
			switch (targetParent.CSharpKind()) {
				case SyntaxKind.LessThanExpression:
					return HandlePossibleTypeParameterCase(semanticModel, (BinaryExpressionSyntax)targetParent, cancellationToken);
				case SyntaxKind.TypeArgumentList:
					return HandlePossibleTypeParameterCase(semanticModel, (TypeArgumentListSyntax)targetParent, cancellationToken);
			}*/
			return ParameterHintingResult.Empty;
		}

		ParameterHintingResult HandleInvocationExpression(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
		{
			var info = semanticModel.GetSymbolInfo(node, cancellationToken);
			var result = new ParameterHintingResult(node.SpanStart);
			
			var resolvedMethod = info.Symbol as IMethodSymbol;
			if (resolvedMethod != null)
				result.AddData(factory.CreateMethodDataProvider(resolvedMethod));
			result.AddRange(info.CandidateSymbols.OfType<IMethodSymbol>().Select (m => factory.CreateMethodDataProvider(m)));
			return result;
		}

		ParameterHintingResult HandlePossibleTypeParameterCase(SemanticModel semanticModel, BinaryExpressionSyntax node, CancellationToken cancellationToken)
		{
			var info = semanticModel.GetSymbolInfo(node.Left, cancellationToken);
			var result = new ParameterHintingResult(node.SpanStart);
			var resolvedMethod = info.Symbol as IMethodSymbol;
			if (resolvedMethod != null)
				result.AddData(factory.CreateTypeParameterDataProvider(resolvedMethod));
			result.AddRange(info.CandidateSymbols.OfType<IMethodSymbol>().Select (m => factory.CreateTypeParameterDataProvider(m)));
			if (result.Count > 0)
				return result;
			
			var resolvedType = info.Symbol as INamedTypeSymbol;
			if (resolvedType != null)
				result.AddData(factory.CreateTypeParameterDataProvider(resolvedType));
			result.AddRange(info.CandidateSymbols.OfType<INamedTypeSymbol>().Select (m => factory.CreateTypeParameterDataProvider(m)));

			return result;
		}
		
		ParameterHintingResult HandlePossibleTypeParameterCase(SemanticModel semanticModel, TypeArgumentListSyntax node, CancellationToken cancellationToken)
		{
			var info = semanticModel.GetSymbolInfo(node.Parent, cancellationToken);
			var result = new ParameterHintingResult(node.SpanStart);
			
			var resolvedType = info.Symbol as INamedTypeSymbol;
			if (resolvedType != null)
				result.AddData(factory.CreateTypeParameterDataProvider(resolvedType));
			result.AddRange(info.CandidateSymbols.OfType<INamedTypeSymbol>().Select (m => factory.CreateTypeParameterDataProvider(m)));

			return result;
		}

		
		ParameterHintingResult HandlePossibleTypeParameterCase(SemanticModel semanticModel, PredefinedTypeSyntax node, CancellationToken cancellationToken)
		{
			var info = semanticModel.GetSymbolInfo(node, cancellationToken);
			var result = new ParameterHintingResult(node.SpanStart);
			
			var resolvedType = info.Symbol as INamedTypeSymbol;
			if (resolvedType != null)
				result.AddData(factory.CreateTypeParameterDataProvider(resolvedType));
			result.AddRange(info.CandidateSymbols.OfType<INamedTypeSymbol>().Select (m => factory.CreateTypeParameterDataProvider(m)));

			return result;
		}
		
		ParameterHintingResult HandleAttribute(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
		{
			var info = semanticModel.GetSymbolInfo(node, cancellationToken);
			var result = new ParameterHintingResult(node.SpanStart);
			var resolvedMethod = info.Symbol as IMethodSymbol;
			if (resolvedMethod != null)
				result.AddData(factory.CreateConstructorProvider(resolvedMethod));
			result.AddRange(info.CandidateSymbols.OfType<IMethodSymbol>().Select (m => factory.CreateConstructorProvider(m)));
			return result;
		}
		
		ParameterHintingResult HandleConstructorInitializer(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
		{
			var info = semanticModel.GetSymbolInfo(node, cancellationToken);
			var result = new ParameterHintingResult(node.SpanStart);
			
			var resolvedMethod = info.Symbol as IMethodSymbol;
			if (resolvedMethod != null)
				result.AddData(factory.CreateConstructorProvider(resolvedMethod));
			result.AddRange(info.CandidateSymbols.OfType<IMethodSymbol>().Select (m => factory.CreateConstructorProvider(m)));
			return result;
		}
		
		ParameterHintingResult HandleElementAccessExpression(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
		{
			var info = semanticModel.GetSymbolInfo(node, cancellationToken);
			var result = new ParameterHintingResult(node.SpanStart);
			
			// Check for indexers
			var resolvedConstructor = info.Symbol as IPropertySymbol;
			if (resolvedConstructor != null)
				result.AddData(factory.CreateIndexerParameterDataProvider(resolvedConstructor, node));
			result.AddRange(info.CandidateSymbols.OfType<IPropertySymbol>().Select (m => factory.CreateIndexerParameterDataProvider(m, node)));
			if (result.Count > 0)
				return result;
			
			// Array case ?
			/*
			var elementeAccess = node as  ElementAccessExpressionSyntax;
			if (elementeAccess == null)
				return ParameterHintingResult.Empty;
			
			var elementInfo = semanticModel.GetSymbolInfo(elementeAccess.Expression, cancellationToken);
			ITypeSymbol type = null;
			
			switch (elementInfo.Symbol.Kind) {
				case SymbolKind.Local:
					type = ((ILocalSymbol)elementInfo.Symbol).Type;
					break;
				case SymbolKind.Parameter:
					type = ((IParameterSymbol)elementInfo.Symbol).Type;
					break;
				case SymbolKind.Field:
					type = ((IFieldSymbol)elementInfo.Symbol).Type;
					break;
				case SymbolKind.Property:
					type = ((IPropertySymbol)elementInfo.Symbol).Type;
					break;
			}
			if (type == null)
				return ParameterHintingResult.Empty;
			
			if (type.TypeKind == TypeKind.ArrayType)
				result.AddData(factory.CreateArrayDataProvider((IArrayTypeSymbol)type));
			*/
			return result;
		}
		
		ParameterHintingResult HandleObjectCreationExpression (SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
		{
			var info = semanticModel.GetSymbolInfo(node, cancellationToken);
			var result = new ParameterHintingResult(node.SpanStart);
			var resolvedConstructor = info.Symbol as IMethodSymbol;
			if (resolvedConstructor != null)
				result.AddData(factory.CreateConstructorProvider(resolvedConstructor));
			result.AddRange(info.CandidateSymbols.OfType<IMethodSymbol>().Select (m => factory.CreateConstructorProvider(m)));
			
			// work around for adding implicitly declared constructors for structs
			foreach (var d in result) {
				var method = (IMethodSymbol)d.Symbol;
				if (method.ContainingType.TypeKind == TypeKind.Struct) {
					foreach (IMethodSymbol c in method.ContainingType.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Constructor)) {
						if (c.IsImplicitlyDeclared) {
							result.AddData(factory.CreateConstructorProvider(c));
							break;
						}
					}
					break;
				}
			}
			return result;
		}
	}
}
