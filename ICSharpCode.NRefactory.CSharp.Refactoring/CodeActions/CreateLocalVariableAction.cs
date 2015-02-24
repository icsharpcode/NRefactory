// 
// CreateLocalVariable.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
	[NRefactoryCodeRefactoringProvider(Description = "Creates a local variable for a undefined variable")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Create local variable")]
	public class CreateLocalVariableAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);

			var node = root.FindNode(span);
			if (node.IsKind(SyntaxKind.Argument)) {
				var argumentSyntax = (ArgumentSyntax)node;
				if (!argumentSyntax.Expression.IsKind(SyntaxKind.IdentifierName))
					return;
				node = argumentSyntax.Expression;
			} else if (node == null || !node.IsKind(SyntaxKind.IdentifierName)) {
				return;
			}

			var symbol = model.GetSymbolInfo(node);
			if (symbol.Symbol != null)
				return;
			if (CreateFieldAction.IsInvocationTarget(node)) 
				return;

			var guessedType = TypeGuessing.GuessAstType(model, node);
			if (guessedType == null)
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					span, 
					DiagnosticSeverity.Error, 
					"Create local variable", 
					t2 => {
						var decl = SyntaxFactory.LocalDeclarationStatement(
							SyntaxFactory.VariableDeclaration(
								guessedType,
								SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>(new [] {
									SyntaxFactory.VariableDeclarator(node.ToString())
								})
							)
						);

						if (node.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression)) {
							decl = decl.WithDeclaration(decl.Declaration.WithVariables(
								SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>(new [] {
									SyntaxFactory.VariableDeclarator(node.ToString()).WithInitializer(
										SyntaxFactory.EqualsValueClause(((AssignmentExpressionSyntax)node.Parent).Right)
									)
								})
							));
//							if (!context.UseExplicitTypes)
							decl = decl.WithDeclaration(decl.Declaration.WithType(SyntaxFactory.ParseTypeName("var")));
							var root2 = root.ReplaceNode((SyntaxNode)node.Parent.Parent, decl.WithAdditionalAnnotations(Formatter.Annotation));
							return Task.FromResult(document.WithSyntaxRoot(root2));
						} 

						var statement = node.Ancestors().First(n => n is StatementSyntax);
						var newRoot = root.InsertNodesBefore(statement, new [] { decl.WithAdditionalAnnotations(Formatter.Annotation) });
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				) 
			);
		}
	}
}