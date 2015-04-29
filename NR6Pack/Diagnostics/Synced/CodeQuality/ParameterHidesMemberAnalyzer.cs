// 
// ParameterHidesMemberAnalyzer.cs
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
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using System.Threading;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
    [NotPortedYet]
    public class ParameterHidesMemberAnalyzer : VariableHidesMemberAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ParameterHidesMemberAnalyzerID, 
			GettextCatalog.GetString("Parameter has the same name as a member and hides it"),
			"{0}", 
			DiagnosticAnalyzerCategories.CodeQualityIssues, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.ParameterHidesMemberAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			//context.RegisterSyntaxNodeAction(
			//	(nodeContext) => {
			//		Diagnostic diagnostic;
			//		if (TryGetDiagnostic (nodeContext, out diagnostic)) {
			//			nodeContext.ReportDiagnostic(diagnostic);
			//		}
			//	}, 
			//	new SyntaxKind[] { SyntaxKind.None }
			//);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;
			//var node = nodeContext.Node as ;
			//diagnostic = Diagnostic.Create (descriptor, node.GetLocation ());
			//return true;
			return false;
		}

//		class GatherVisitor : GatherVisitorBase<ParameterHidesMemberAnalyzer>
//		{
//			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
//				: base(semanticModel, addDiagnostic, cancellationToken)
//			{
//			}

////			public override void VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
////			{
////				base.VisitParameterDeclaration(parameterDeclaration);
////
////				var rr = ctx.Resolve(parameterDeclaration.Parent) as MemberResolveResult;
////				if (rr == null || rr.IsError)
////					return;
////				var parent = rr.Member;
////				if (parent.SymbolKind == SymbolKind.Constructor || parent.ImplementedInterfaceMembers.Any())
////					return;
////				if (parent.IsOverride || parent.IsAbstract || parent.IsPublic || parent.IsProtected)
////					return;
////					
////				IMember member;
////				if (HidesMember(ctx, parameterDeclaration, parameterDeclaration.Name, out member)) {
////					string msg;
////					switch (member.SymbolKind) {
////						case SymbolKind.Field:
////							msg = ctx.TranslateString("Parameter '{0}' hides field '{1}'");
////							break;
////						case SymbolKind.Method:
////							msg = ctx.TranslateString("Parameter '{0}' hides method '{1}'");
////							break;
////						case SymbolKind.Property:
////							msg = ctx.TranslateString("Parameter '{0}' hides property '{1}'");
////							break;
////						case SymbolKind.Event:
////							msg = ctx.TranslateString("Parameter '{0}' hides event '{1}'");
////							break;
////						default:
////							msg = ctx.TranslateString("Parameter '{0}' hides member '{1}'");
////							break;
////					}
////					AddDiagnosticAnalyzer(new CodeIssue(parameterDeclaration.NameToken,
////						string.Format(msg, parameterDeclaration.Name, member.FullName)));
////				}
////			}
//		}
	}
}