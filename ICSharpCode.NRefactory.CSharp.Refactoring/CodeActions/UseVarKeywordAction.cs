// 
// UseVarKeyword.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Converts local variable declaration to be implicit typed.")]
	[ExportCodeRefactoringProvider("Use 'var' keyword", LanguageNames.CSharp)]
	public class UseVarKeywordAction : CodeRefactoringProvider
	{
		internal static VariableDeclarationSyntax GetVariableDeclarationStatement(SyntaxNode token)
		{
			return token.Parent as VariableDeclarationSyntax;
		}

		internal static ForEachStatementSyntax GetForeachStatement(SyntaxNode token)
		{
			return token.Parent as ForEachStatementSyntax;
		}

		#region CodeRefactoringProvider implementation

		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var parseOptions = root.SyntaxTree.Options as CSharpParseOptions;
			if (parseOptions != null && parseOptions.LanguageVersion < LanguageVersion.CSharp3)
				return;

			var token = root.FindToken(span.Start);

			TypeSyntax type = null;
			var varDecl = GetVariableDeclarationStatement(token.Parent);
			if (varDecl != null && varDecl.Parent is FieldDeclarationSyntax)
				return;
			if (varDecl != null)
				type = varDecl.Type;
			var foreachStmt = GetForeachStatement(token.Parent);
			if (foreachStmt != null)
				type = foreachStmt.Type;
			if (type == null || type.IsVar)
				return;
			context.RegisterRefactoring(
				CodeActionFactory.Create(
					token.Span, 
					DiagnosticSeverity.Info, 
					"Use 'var' keyword", 
					t2 => Task.FromResult(PerformAction(document, root, type))
				)
			);
		}

		#endregion

		static Document PerformAction(Document document, SyntaxNode root, TypeSyntax type)
		{
			var newRoot = root.ReplaceNode((SyntaxNode)
				type,
				SyntaxFactory.IdentifierName("var")
				.WithLeadingTrivia(type.GetLeadingTrivia())
				.WithTrailingTrivia(type.GetTrailingTrivia())
			);
			return document.WithSyntaxRoot(newRoot);
		}
	}
}
