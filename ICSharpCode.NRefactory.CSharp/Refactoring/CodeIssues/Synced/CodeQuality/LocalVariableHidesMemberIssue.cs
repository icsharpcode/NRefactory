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
using System.Linq;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;
using System.Collections.Generic;

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

            void CheckLocal(AstNode node, string name, AstNode token)
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
                    AddIssue(token, string.Format(msg, name, member.FullName));
                }
		    }

		    public override void VisitForeachStatement (ForeachStatement foreachStatement)
			{
				base.VisitForeachStatement (foreachStatement);

                CheckLocal(foreachStatement, foreachStatement.VariableName, foreachStatement.VariableNameToken);
			}
		}
	}

    public abstract class VariableHidesMemberIssue : ICodeIssueProvider
    {
        public abstract IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context);

        protected static bool HidesMember(BaseRefactoringContext ctx, AstNode node, string variableName)
        {
            IMember member;
            return HidesMember(ctx, node, variableName, out member);
        }

        protected static bool HidesMember(BaseRefactoringContext ctx, AstNode node, string variableName, out IMember member)
        {
            var typeDecl = node.GetParent<TypeDeclaration>();
            member = null;
            if (typeDecl == null)
                return false;
            var entityDecl = node.GetParent<EntityDeclaration>();
            var memberResolveResult = ctx.Resolve(entityDecl) as MemberResolveResult;
            if (memberResolveResult == null)
                return false;
            var typeResolveResult = ctx.Resolve(typeDecl) as TypeResolveResult;
            if (typeResolveResult == null)
                return false;

            var sourceMember = memberResolveResult.Member;

            member = typeResolveResult.Type.GetMembers(m => m.Name == variableName).FirstOrDefault(m2 => IsAccessible(sourceMember, m2));
            return member != null;
        }

        static bool IsAccessible(IMember sourceMember, IMember targetMember)
        {
            if (sourceMember.IsStatic != targetMember.IsStatic)
                return false;

            var sourceType = sourceMember.DeclaringType;
            var targetType = targetMember.DeclaringType;
            switch (targetMember.Accessibility)
            {
                case Accessibility.None:
                    return false;
                case Accessibility.Private:
                    // check for members of outer classes (private members of outer classes can be accessed)
                    var targetTypeDefinition = targetType.GetDefinition();
                    for (var t = sourceType.GetDefinition(); t != null; t = t.DeclaringTypeDefinition)
                    {
                        if (t.Equals(targetTypeDefinition))
                            return true;
                    }
                    return false;
                case Accessibility.Public:
                    return true;
                case Accessibility.Protected:
                    return IsProtectedAccessible(sourceType, targetType);
                case Accessibility.Internal:
                    return IsInternalAccessible(sourceMember.ParentAssembly, targetMember.ParentAssembly);
                case Accessibility.ProtectedOrInternal:
                    return IsInternalAccessible(sourceMember.ParentAssembly, targetMember.ParentAssembly) || IsProtectedAccessible(sourceType, targetType);
                case Accessibility.ProtectedAndInternal:
                    return IsInternalAccessible(sourceMember.ParentAssembly, targetMember.ParentAssembly) && IsProtectedAccessible(sourceType, targetType);
                default:
                    throw new Exception("Invalid value for Accessibility");
            }
        }

        static bool IsProtectedAccessible(IType sourceType, IType targetType)
        {
            return sourceType.GetAllBaseTypes().Any(type => targetType.Equals(type));
        }

        static bool IsInternalAccessible(IAssembly sourceAssembly, IAssembly targetAssembly)
        {
            return sourceAssembly.InternalsVisibleTo(targetAssembly);
        }

    }
}
