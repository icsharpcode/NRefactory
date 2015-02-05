// 
// CreateField.cs
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
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Creates a field for a undefined variable.")]
	[ExportCodeRefactoringProvider("Create field", LanguageNames.CSharp)]
	public class CreateFieldAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);

			var node = root.FindNode(span);
			INamedTypeSymbol targetType;

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
			

			var enclosingType = model.GetEnclosingNamedType(span.Start, cancellationToken);
			targetType = enclosingType;
			var mref = node.Parent as MemberAccessExpressionSyntax;
			if (mref != null && mref.Name == node) {
				var target = model.GetTypeInfo(mref.Expression);
				if (target.Type == null || !target.Type.Locations.First().IsInSource)
					return;
				
				targetType = target.Type as INamedTypeSymbol;
				if (targetType == null || targetType.TypeKind == TypeKind.Enum || targetType.TypeKind == TypeKind.Interface)
					return;
			}

			var guessedType = TypeGuessing.GuessAstType(model, node);
			if (guessedType == null)
				return;
			
			context.RegisterRefactoring(
				CodeActionFactory.CreateInsertion(
					span, 
					DiagnosticSeverity.Error, 
					"Create field", 
					t2 => {
						bool isStatic = targetType.IsStatic;
						if (enclosingType == targetType && ((mref != null && mref.Expression is ThisExpressionSyntax) || mref == null && !(node.Parent.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.InitializerExpressionSyntax))) {
							var enclosingSymbol = model.GetEnclosingSymbol(span.Start, cancellationToken);
							if (enclosingSymbol != null && enclosingSymbol.IsStatic)
								isStatic = true;
						}

						var decl = SyntaxFactory.FieldDeclaration(
							SyntaxFactory.VariableDeclaration(
								guessedType,
								SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>(new [] {
									SyntaxFactory.VariableDeclarator(node.ToString())
								})
							)
						);

						if (isStatic)
							decl = decl.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)));
						return Task.FromResult(new InsertionResult (context, decl, targetType, targetType.Locations.First ()));
					}
				) 
			);
		}

		internal static bool IsInvocationTarget(SyntaxNode node)
		{
			var invoke = node.Parent as InvocationExpressionSyntax;
			return invoke != null && invoke.Expression == node;
		}
	}
}