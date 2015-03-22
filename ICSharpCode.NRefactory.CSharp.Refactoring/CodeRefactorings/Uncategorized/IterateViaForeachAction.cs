//
// IterateViaForeachTests.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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
	[NRefactoryCodeRefactoringProvider(Description = "Iterates an IEnumerable with a foreach loop")]
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Iterate via 'foreach'")]
	public class IterateViaForeachAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
		}
//
//		public async Task ComputeRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
//		{
//			CodeAction action;
//			action = ActionFromUsingStatement(context);
//			if (action != null)
//				yield return action;
//			action = ActionFromVariableInitializer(context);
//			if (action != null)
//				yield return action;
//			action = ActionFromExpressionStatement(context);
//			if (action != null)
//				yield return action;
//		}
//
//		CodeAction ActionFromUsingStatement(SemanticModel context)
//		{
//			var initializer = context.GetNode<VariableInitializer>();
//			if (initializer == null || !initializer.NameToken.Contains(context.Location))
//				return null;
//			var initializerRR = context.Resolve(initializer) as LocalResolveResult;
//			if (initializerRR == null)
//				return null;
//			var elementType = GetElementType(initializerRR, context);
//			if (elementType == null)
//				return null;
//			var usingStatement = initializer.Parent.Parent as UsingStatement;
//			if (usingStatement == null)
//				return null;
//			return new CodeAction(context.TranslateString("Iterate via 'foreach'"), script => {
//				var iterator = MakeForeach(new IdentifierExpression(initializer.Name), elementType, context);
//				if (usingStatement.EmbeddedStatement is EmptyStatement) {
//					var blockStatement = new BlockStatement();
//					blockStatement.Statements.Add(iterator);
//					script.Replace(usingStatement.EmbeddedStatement, blockStatement);
//					script.FormatText((AstNode)blockStatement);
//				} else if (usingStatement.EmbeddedStatement is BlockStatement) {
//					var anchorNode = usingStatement.EmbeddedStatement.FirstChild;
//					script.InsertAfter(anchorNode, iterator);
//					script.FormatText(usingStatement.EmbeddedStatement);
//				}
//			}, initializer);
//		}
//
//		CodeAction ActionFromVariableInitializer(SemanticModel context)
//		{
//			var initializer = context.GetNode<VariableInitializer>();
//			if (initializer == null || initializer.Parent.Parent is ForStatement || !initializer.NameToken.Contains(context.Location))
//				return null;
//			var initializerRR = context.Resolve(initializer) as LocalResolveResult;
//			if (initializerRR == null)
//				return null;
//			var elementType = GetElementType(initializerRR, context);
//			if (elementType == null)
//				return null;
//
//			return new CodeAction(context.TranslateString("Iterate via foreach"), script => {
//				var iterator = MakeForeach(new IdentifierExpression(initializer.Name), elementType, context);
//				script.InsertAfter(context.GetNode<Statement>(), iterator);
//			}, initializer);
//		}
//
//		CodeAction ActionFromExpressionStatement(SemanticModel context)
//		{
//			var expressionStatement = context.GetNode<ExpressionStatement>();
//			if (expressionStatement == null)
//				return null;
//			var expression = expressionStatement.Expression;
//			var assignment = expression as AssignmentExpression;
//			if (assignment != null)
//				expression = assignment.Left;
//			if (!expression.Contains(context.Location))
//				return null;
//			var expressionRR = context.Resolve(expression);
//			if (expressionRR == null)
//				return null;
//			var elementType = GetElementType(expressionRR, context);
//			if (elementType == null)
//				return null;
//			return new CodeAction(context.TranslateString("Iterate via foreach"), script => {
//				var iterator = MakeForeach(expression, elementType, context);
//				if (expression == expressionStatement.Expression)
//					script.Replace(expressionStatement, iterator);
//				else
//					script.InsertAfter(expressionStatement, iterator);
//			}, expression);
//		}
//
//		static ForeachStatement MakeForeach(Expression node, IType type, SemanticModel context)
//		{
//			var namingHelper = new NamingHelper(context);
//			return new ForeachStatement {
//				VariableType = new SimpleType("var"),
//				VariableName = namingHelper.GenerateVariableName(type),
//				InExpression = node.Clone(),
//				EmbeddedStatement = new BlockStatement()
//			};
//		}
//
//		static IType GetElementType(ResolveResult rr, BaseSemanticModel context)
//		{
//			if (rr.IsError || rr.Type.Kind == TypeKind.Unknown)
//				return null;
//			var type = GetCollectionType(rr.Type);
//			if (type == null)
//				return null;
//
//			var parameterizedType = type as ParameterizedType;
//			if (parameterizedType != null)
//				return parameterizedType.TypeArguments.First();
//			return context.Compilation.FindType(KnownTypeCode.Object);
//		}
//
//		static IType GetCollectionType(IType type)
//		{
//			IType collectionType = null;
//			foreach (var baseType in type.GetAllBaseTypes()) {
//				if (baseType.IsKnownType(KnownTypeCode.IEnumerableOfT)) {
//					collectionType = baseType;
//					break;
//				} else if (baseType.IsKnownType(KnownTypeCode.IEnumerable)) {
//					collectionType = baseType;
//					// Don't break, continue in case type implements IEnumerable<T>
//				}
//			}
//			return collectionType;
//		}
	}
}
