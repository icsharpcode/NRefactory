// 
// LocalVariableHideFieldIssue.cs
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
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Local variable hides member",
					   Description = "Local variable has the same name as a member and hides it.",
					   Category = IssueCategories.CodeQualityIssues,
					   Severity = Severity.Warning,
					   IssueMarker = IssueMarker.Underline,
                       ResharperDisableKeyword = "LocalVariableHidesMember")]
	public class LocalVariableHidesMemberIssue : VariableHidesMemberIssue
	{
	    private string ctx = "evlelv";
	    public override System.Collections.Generic.IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
	    {
	        var ctx = context;
	        Console.WriteLine(  ctx);
			return new GatherVisitor (context).GetIssues ();
		}

		class GatherVisitor : GatherVisitorBase<LocalVariableHidesMemberIssue>
		{
			public GatherVisitor (BaseRefactoringContext ctx)
				: base (ctx)
			{
			}

			public override void VisitVariableInitializer (VariableInitializer variableInitializer)
			{
                base.VisitVariableInitializer(variableInitializer);

				if (!(ctx.Resolve (variableInitializer) is LocalResolveResult))
					return;
                CheckLocal(variableInitializer, variableInitializer.Name, variableInitializer.NameToken);
			}

            private void CheckLocal(AstNode node, string name, AstNode token)
		    {
		        IMember member;
                if (HidesMember(ctx, node, name, out member)) {
                    string msg;
                    switch (member.SymbolKind) {
                        case SymbolKind.Field:
                            msg = ctx.TranslateString("Local variable '{0}' hides field '{1}'");
                            break;
                        case SymbolKind.Method:
                            msg = ctx.TranslateString("Local variable '{0}' hides method '{1}'");
                            break;
                        case SymbolKind.Property:
                            msg = ctx.TranslateString("Local variable '{0}' hides property '{1}'");
                            break;
                        case SymbolKind.Event:
                            msg = ctx.TranslateString("Local variable '{0}' hides event '{1}'");
                            break;
                        default:
                            msg = ctx.TranslateString("Local variable '{0}' hides member '{1}'");
                            break;
                    }
                    var resolveResult = ctx.Resolve(node);
                    if (resolveResult is LocalResolveResult)
                    {
                        var lr = resolveResult as LocalResolveResult;
                        AddIssue(token, string.Format(msg, name, member.FullName),
                            new CodeAction(ctx.TranslateString("Rename local"), script => script.Rename(lr.Variable), token));
                    } else if (resolveResult is ForEachResolveResult) {
                        var lr = resolveResult as ForEachResolveResult;
                        AddIssue(token, string.Format(msg, name, member.FullName),
                            new CodeAction(ctx.TranslateString("Rename loop variable"), script => script.Rename(lr.ElementVariable), token));
                    } else  {
                        AddIssue(token, string.Format(msg, name, member.FullName));
                    }
                }
		    }

		    public override void VisitForeachStatement (ForeachStatement foreachStatement)
			{
				base.VisitForeachStatement (foreachStatement);

                CheckLocal(foreachStatement, foreachStatement.VariableName, foreachStatement.VariableNameToken);
			}
		}
	}
}
