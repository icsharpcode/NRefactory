// RedundantArrayCreationExpressionIssue.cs
//
// Author:
//      Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013 Ji Kun <jikun.nus@gmail.com>
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

//
// RedundantArrayCreationExpression.cs
//
// Author:
//       Ji Kun <jikun.nus@gmail.com>
//
// Copyright (c) 2013 Ji Kun
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
using System.IO;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.PatternMatching;
using System.Linq;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.CSharp.Resolver;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("RedundantArrayCreationExpression",
						Description = "When initializing explicitly typed local variable or array type, array creation expression can be replaced with array initializer.",
						Category = IssueCategories.RedundanciesInCode,
						Severity = Severity.Warning,
						ResharperDisableKeyword = "RedundantArrayCreationExpression",
						IssueMarker = IssueMarker.GrayOut)]
	public class RedundantArrayCreationExpressionIssue : GatherVisitorCodeIssueProvider
	{

		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context, this);
		}


		class GatherVisitor : GatherVisitorBase<RedundantArrayCreationExpressionIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx, RedundantArrayCreationExpressionIssue issueProvider) : base (ctx, issueProvider)
			{
			}

			private void AddIssue(AstNode node, AstNode initializer)
			{
				AddIssue(node.StartLocation,initializer.StartLocation, ctx.TranslateString("Array creation expression can be replaced with initializer"), ctx.TranslateString("Use Array Initializer"),
				script =>
				{
					var startOffset = script.GetCurrentOffset(node.StartLocation);
					var endOffset = script.GetCurrentOffset(initializer.StartLocation);
					if (startOffset < endOffset)
						script.RemoveText(startOffset, endOffset - startOffset);
				});
			}

			public override void VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression)
			{
				base.VisitArrayCreateExpression(arrayCreateExpression);

				if (arrayCreateExpression.Initializer.IsNull)
					return;

				var variableInilizer = arrayCreateExpression.GetParent<VariableInitializer>();
				if (variableInilizer == null)
					return;

				if (variableInilizer.Parent is VariableDeclarationStatement)		
				{
					var variableDeclaration = variableInilizer.Parent;

					if (variableDeclaration.GetChildByRole(Roles.Type) is ComposedType)
					{
						AddIssue(arrayCreateExpression, arrayCreateExpression.Initializer);
					}
				}

				else if (variableInilizer.Parent is FieldDeclaration)
				{
					var filedDeclaration = variableInilizer.Parent;

					if (filedDeclaration.GetChildByRole(Roles.Type) is ComposedType)
					{
						AddIssue(arrayCreateExpression, arrayCreateExpression.Initializer);
					}
				}
			}
		}
	}
}