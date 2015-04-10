//
// RedundantLambdaSignatureParenthesesAnalyzer.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
	public class RedundantLambdaSignatureParenthesesAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.RedundantLambdaSignatureParenthesesAnalyzerID, 
			GettextCatalog.GetString("Redundant lambda signature parentheses"),
			GettextCatalog.GetString("Redundant lambda signature parentheses"), 
			DiagnosticAnalyzerCategories.RedundanciesInCode, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.RedundantLambdaSignatureParenthesesAnalyzerID),
			customTags: DiagnosticCustomTags.Unnecessary
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<RedundantLambdaSignatureParenthesesAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitLambdaExpression(LambdaExpression lambdaExpression)
//			{
//				base.VisitLambdaExpression(lambdaExpression);
//				var p = lambdaExpression.Parameters.FirstOrDefault();
//				if (p == null || !p.Type.IsNull)
//					return;
//				var lParToken = lambdaExpression.LParToken;
//				if (lParToken.IsNull)
//					return;
//				if (p.GetNextSibling(n => n.Role == Roles.Parameter) != null)
//					return;
//				Action<Script> action = script =>  {
//					script.Remove(lParToken);
//					script.Remove(lambdaExpression.RParToken);
//					script.FormatText(lambdaExpression);
//				};
//
//				var issueText = ctx.TranslateString("");
//				var fixText = ctx.TranslateString("");
//				AddDiagnosticAnalyzer(new CodeIssue(lambdaExpression.LParToken, issueText, fixText, action) { IssueMarker = IssueMarker.GrayOut });
//				AddDiagnosticAnalyzer(new CodeIssue(lambdaExpression.RParToken, issueText, fixText, action) { IssueMarker = IssueMarker.GrayOut });
//			}
		}
	}
}