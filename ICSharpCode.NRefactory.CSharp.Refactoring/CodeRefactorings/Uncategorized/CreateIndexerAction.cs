// 
// CreateIndexerAction.cs
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

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[NRefactoryCodeRefactoringProvider(Description = "Creates an indexer declaration out of an indexer expression")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Create indexer")]
	public class CreateIndexerAction : CodeRefactoringProvider
	{
		public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			//var document = context.Document;
			//var span = context.Span;
			//var cancellationToken = context.CancellationToken;
			//var model = await document.GetSemanticModelAsync(cancellationToken);
			//var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			return Task.FromResult (0);
		}
//		public async Task ComputeRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
//		{
//			var indexer = context.GetNode<IndexerExpression>();
//			if (indexer == null)
//				yield break;
//			if (!(context.Resolve(indexer).IsError))
//				yield break;
//
//			var state = context.GetResolverStateBefore(indexer);
//			if (state.CurrentTypeDefinition == null)
//				yield break;
//			var guessedType = TypeGuessing.GuessAstType(context, indexer);
//
//			bool createInOtherType = false;
//			ResolveResult targetResolveResult = null;
//			targetResolveResult = context.Resolve(indexer.Target);
//			createInOtherType = !state.CurrentTypeDefinition.Equals(targetResolveResult.Type.GetDefinition());
//
//			bool isStatic;
//			if (createInOtherType) {
//				if (targetResolveResult.Type.GetDefinition() == null || targetResolveResult.Type.GetDefinition().Region.IsEmpty)
//					yield break;
//				isStatic = targetResolveResult is TypeResolveResult;
//				if (isStatic && targetResolveResult.Type.Kind == TypeKind.Interface || targetResolveResult.Type.Kind == TypeKind.Enum)
//					yield break;
//			} else {
//				isStatic = indexer.Target is IdentifierExpression && state.CurrentMember.IsStatic;
//			}
//
//			yield return new CodeAction(context.TranslateString("Create indexer"), script => {
//				var decl = new IndexerDeclaration() {
//					ReturnType = guessedType,
//					Getter = new Accessor() {
//						Body = new BlockStatement() {
//							new ThrowStatement(new ObjectCreateExpression(context.CreateShortType("System", "NotImplementedException")))
//						}
//					},
//					Setter = new Accessor() {
//						Body = new BlockStatement() {
//							new ThrowStatement(new ObjectCreateExpression(context.CreateShortType("System", "NotImplementedException")))
//						}
//					},
//				};
//				decl.Parameters.AddRange(CreateMethodDeclarationAction.GenerateParameters(context, indexer.Arguments));
//				if (isStatic)
//					decl.Modifiers |= Modifiers.Static;
//				
//				if (createInOtherType) {
//					if (targetResolveResult.Type.Kind == TypeKind.Interface) {
//						decl.Getter.Body = null;
//						decl.Setter.Body = null;
//						decl.Modifiers = Modifiers.None;
//					} else {
//						decl.Modifiers |= Modifiers.Public;
//					}
//
//					script.InsertWithCursor(context.TranslateString("Create indexer"), targetResolveResult.Type.GetDefinition(), (s, c) => decl);
//					return;
//				}
//
//				script.InsertWithCursor(context.TranslateString("Create indexer"), Script.InsertPosition.Before, decl);
//			}, indexer) { Severity = ICSharpCode.NRefactory.Refactoring.Severity.Error };
//		}

	}
}