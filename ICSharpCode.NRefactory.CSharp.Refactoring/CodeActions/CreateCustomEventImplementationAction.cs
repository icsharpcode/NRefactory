// 
// CreateCustomEventImplementationAction.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang <lightyang0@gmail.com>
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
	[NRefactoryCodeRefactoringProvider(Description = "Create custom event implementation")]
	[ExportCodeRefactoringProvider("Create custom event implementation", LanguageNames.CSharp)]
	public class CreateCustomEventImplementationAction : SpecializedCodeAction<InitializerExpressionSyntax>
	{
		protected override IEnumerable<CodeAction> GetActions(SemanticModel semanticModel, SyntaxNode root, TextSpan span, InitializerExpressionSyntax node, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
//		protected override CodeAction GetAction (SemanticModel context, VariableInitializer node)
//		{
//			var eventDecl = node.Parent as EventDeclaration;
//			if (eventDecl == null)
//				return null;
//			return new CodeAction (context.TranslateString ("Create custom event implementation"),
//				script =>
//				{
//					var accessor = new Accessor
//					{
//						Body = new BlockStatement
//						{
//							new ThrowStatement(
//								new ObjectCreateExpression(context.CreateShortType("System", "NotImplementedException")))	
//						}
//					};
//					var e = new CustomEventDeclaration
//					{
//						Name = node.Name,
//						Modifiers = eventDecl.Modifiers,
//						ReturnType = eventDecl.ReturnType.Clone (),
//						AddAccessor = accessor,
//						RemoveAccessor = (Accessor)accessor.Clone(),
//					};
//					if (eventDecl.Variables.Count > 1) {
//						var newEventDecl = (EventDeclaration)eventDecl.Clone ();
//						newEventDecl.Variables.Remove (
//							newEventDecl.Variables.FirstOrNullObject (v => v.Name == node.Name));
//						script.InsertBefore (eventDecl, newEventDecl);
//					}
//					script.Replace (eventDecl, e);
//				}, node.NameToken);
//		}
	}
}
