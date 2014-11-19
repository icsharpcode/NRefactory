// 
// InsertAnonymousMethodSignature.cs
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
using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "Inserts a signature to parameterless anonymous methods")]
	[ExportCodeRefactoringProvider("Insert anonymous method signature", LanguageNames.CSharp)]
	public class InsertAnonymousMethodSignatureAction : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			var anonymousMethodExpression = root.FindNode(span) as AnonymousMethodExpressionSyntax;
			if (anonymousMethodExpression == null || !anonymousMethodExpression.DelegateKeyword.Span.Contains(span) || anonymousMethodExpression.ParameterList != null)
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					anonymousMethodExpression.Span,
					DiagnosticSeverity.Info,
					"Insert anonymous method signature",
					t2 => {
						var typeInfo = model.GetTypeInfo(anonymousMethodExpression);
						var type = typeInfo.ConvertedType ?? typeInfo.Type;
						if (type == null)
							return Task.FromResult(document);
						var method = type.GetDelegateInvokeMethod();

						if (method == null)
							return Task.FromResult(document);
						var parameters = new List<ParameterSyntax> ();

						foreach (var param in method.Parameters) {
							var t = SyntaxFactory.ParseTypeName(param.Type.ToMinimalDisplayString(model, anonymousMethodExpression.SpanStart));
							parameters.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier(param.Name)).WithType(t));
						}

						var newRoot = root.ReplaceNode((SyntaxNode)anonymousMethodExpression, anonymousMethodExpression.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters))).WithAdditionalAnnotations(Formatter.Annotation));
						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
			);
		}
	}
}