// 
// IntroduceConstantAction.cs
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
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Introduce constant")]
    [NotPortedYet]
    public class IntroduceConstantAction : CodeRefactoringProvider
	{
		public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			//var document = context.Document;
			//var span = context.Span;
			//var cancellationToken = context.CancellationToken;
			//var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			//if (model.IsFromGeneratedCode(cancellationToken))
			//	return;
			//var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
			return Task.FromResult (0);
		}
//		public async Task ComputeRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
//		{
//			var pexpr = context.GetNode<PrimitiveExpression>();
//			if (pexpr == null)
//				yield break;
//
//			var visitor = new DeclareLocalVariableAction.SearchNodeVisitior(pexpr);
//
//			if (pexpr.Parent is VariableInitializer) {
//				var varDec = pexpr.Parent.Parent as VariableDeclarationStatement;
//				if (varDec != null && (varDec.Modifiers & Modifiers.Const) != 0)
//					yield break;
//				var fieldDecl = pexpr.Parent.Parent as FieldDeclaration;
//				if (fieldDecl != null && (fieldDecl.Modifiers & Modifiers.Const) != 0)
//					yield break;
//			}
//
//			var node = context.GetNode <BlockStatement>();
//			if (node != null)
//				node.AcceptVisitor(visitor);
//
//			var resolveResult = context.Resolve(pexpr);
//			var statement = context.GetNode<Statement>();
//			if (statement != null) {
//				yield return new CodeAction(context.TranslateString("Create local constant"), script => {
//					string name = CreateMethodDeclarationAction.CreateBaseName(pexpr, resolveResult.Type);
//					var service = (NamingConventionService)context.GetService(typeof(NamingConventionService));
//					if (service != null)
//						name = service.CheckName(context, name, AffectedEntity.LocalConstant);
//					
//					var initializer = new VariableInitializer(name, pexpr.Clone());
//					var decl = new VariableDeclarationStatement() {
//						Type = context.CreateShortType(resolveResult.Type),
//						Modifiers = Modifiers.Const,
//						Variables = { initializer }
//					};
//					
//					script.InsertBefore(statement, decl);
//					var variableUsage = new IdentifierExpression(name);
//					script.Replace(pexpr, variableUsage);
//					script.Link(initializer.NameToken, variableUsage);
//				}, pexpr);
//			}
//			
//			yield return new CodeAction(context.TranslateString("Create constant field"), script => {
//				string name = CreateMethodDeclarationAction.CreateBaseName(pexpr, resolveResult.Type);
//				var service = (NamingConventionService)context.GetService(typeof(NamingConventionService));
//				if (service != null)
//					name = service.CheckName(context, name, AffectedEntity.ConstantField);
//				
//				var initializer = new VariableInitializer(name, pexpr.Clone());
//				
//				var decl = new FieldDeclaration() {
//					ReturnType = context.CreateShortType(resolveResult.Type),
//					Modifiers = Modifiers.Const,
//					Variables = { initializer }
//				};
//				
//				var variableUsage = new IdentifierExpression(name);
//				script.Replace(pexpr, variableUsage);
//				//				script.Link(initializer.NameToken, variableUsage);
//				script.InsertWithCursor(context.TranslateString("Create constant"), Script.InsertPosition.Before, decl);
//			}, pexpr);
//
//			if (visitor.Matches.Count > 1) {
//				yield return new CodeAction(string.Format(context.TranslateString("Create local constant (replace '{0}' occurrences)"), visitor.Matches.Count), script => {
//					string name = CreateMethodDeclarationAction.CreateBaseName(pexpr, resolveResult.Type);
//					var service = (NamingConventionService)context.GetService(typeof(NamingConventionService));
//					if (service != null)
//						name = service.CheckName(context, name, AffectedEntity.LocalConstant);
//					
//					var initializer = new VariableInitializer(name, pexpr.Clone());
//					var decl = new VariableDeclarationStatement() {
//						Type = context.CreateShortType(resolveResult.Type),
//						Modifiers = Modifiers.Const,
//						Variables = { initializer }
//					};
//					
//					script.InsertBefore(statement, decl);
//
//					var linkedNodes = new List<AstNode>();
//					linkedNodes.Add(initializer.NameToken);
//					for (int i = 0; i < visitor.Matches.Count; i++) {
//						var identifierExpression = new IdentifierExpression(name);
//						linkedNodes.Add(identifierExpression);
//						script.Replace(visitor.Matches [i], identifierExpression);
//					}
//					script.Link(linkedNodes.ToArray ());
//				}, pexpr);
//				
//				yield return new CodeAction(string.Format(context.TranslateString("Create constant field (replace '{0}' occurrences)"), visitor.Matches.Count), script => {
//					string name = CreateMethodDeclarationAction.CreateBaseName(pexpr, resolveResult.Type);
//					var service = (NamingConventionService)context.GetService(typeof(NamingConventionService));
//					if (service != null)
//						name = service.CheckName(context, name, AffectedEntity.ConstantField);
//					
//					var initializer = new VariableInitializer(name, pexpr.Clone());
//					
//					var decl = new FieldDeclaration() {
//						ReturnType = context.CreateShortType(resolveResult.Type),
//						Modifiers = Modifiers.Const,
//						Variables = { initializer }
//					};
//					
//					var linkedNodes = new List<AstNode>();
//					linkedNodes.Add(initializer.NameToken);
//					for (int i = 0; i < visitor.Matches.Count; i++) {
//						var identifierExpression = new IdentifierExpression(name);
//						linkedNodes.Add(identifierExpression);
//						script.Replace(visitor.Matches [i], identifierExpression);
//					}
//					script.InsertWithCursor(context.TranslateString("Create constant"), Script.InsertPosition.Before, decl);
//				}, pexpr);
//			}
//		}
	}
}

