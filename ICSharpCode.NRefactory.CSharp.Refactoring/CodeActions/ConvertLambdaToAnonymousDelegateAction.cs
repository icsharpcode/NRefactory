//
// ConvertLambdaToDelegateAction.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Converts a lambda to an anonymous delegate")]
	[ExportCodeRefactoringProvider("Convert lambda to anonymous delegate", LanguageNames.CSharp)]
	public class ConvertLambdaToAnonymousDelegateAction : SpecializedCodeAction<SimpleLambdaExpressionSyntax>
	{
		protected override IEnumerable<CodeAction> GetActions(Document document, SemanticModel semanticModel, SyntaxNode root, TextSpan span, SimpleLambdaExpressionSyntax node, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
//		protected override CodeAction GetAction(SemanticModel context, LambdaExpression node)
//		{
//			if (context.Location < node.StartLocation || context.Location >= node.Body.StartLocation)
//				return null;
//
//			var lambdaResolveResult = context.Resolve(node) as LambdaResolveResult;
//			if (lambdaResolveResult == null)
//				return null;
//
//			return new CodeAction(context.TranslateString("Convert to anonymous delegate"), script => {
//				BlockStatement newBody;
//				if (node.Body is BlockStatement) {
//					newBody = (BlockStatement)node.Body.Clone();
//				} else {
//					var expression = (Expression)node.Body.Clone();
//
//					Statement statement;
//					if (RequireReturnStatement(context, node)) {
//						statement = new ReturnStatement(expression);
//					}
//					else {
//						statement = expression;
//					}
//
//					newBody = new BlockStatement {
//						Statements = {
//							statement
//						}
//					};
//				}
//				var method = new AnonymousMethodExpression (newBody, GetParameters(lambdaResolveResult.Parameters, context));
//				script.Replace(node, method);
//			}, node);
//		}
//
//		IEnumerable<ParameterDeclaration> GetParameters(IList<IParameter> parameters, SemanticModel context)
//		{
//			if (parameters == null || parameters.Count == 0)
//				return null;
//			var result = new List<ParameterDeclaration> ();
//			foreach (var parameter in parameters) {
//				var type = context.CreateShortType(parameter.Type);
//				var name = parameter.Name;
//				ParameterModifier modifier = ParameterModifier.None;
//				if (parameter.IsRef) 
//					modifier |= ParameterModifier.Ref;
//				if (parameter.IsOut)
//					modifier |= ParameterModifier.Out;
//				result.Add (new ParameterDeclaration(type, name, modifier));
//			}
//			return result;
//		}
//
//		static bool RequireReturnStatement (SemanticModel context, LambdaExpression lambda)
//		{
//			var type = LambdaHelper.GetLambdaReturnType (context, lambda);
//			return type != null && type.ReflectionName != "System.Void";
//		}
	}
}

