//
// StringCompareIsCultureSpecificAnalyzer.cs
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
	public class StringCompareIsCultureSpecificAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.StringCompareIsCultureSpecificAnalyzerID, 
			GettextCatalog.GetString("Warns when a culture-aware 'Compare' call is used by default"),
			GettextCatalog.GetString("'string.Compare' is culture-aware"), 
			DiagnosticAnalyzerCategories.PracticesAndImprovements, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.StringCompareIsCultureSpecificAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		//static readonly DiagnosticDescriptor Rule1 = new DiagnosticDescriptor (DiagnosticId, Description, "Use ordinal comparison", Category, DiagnosticSeverity.Warning, true, "'string.Compare' is culture-aware");
		//static readonly DiagnosticDescriptor Rule2 = new DiagnosticDescriptor (DiagnosticId, Description, "Use culture-aware comparison", Category, DiagnosticSeverity.Warning, true, "'string.Compare' is culture-aware");


		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<StringCompareIsCultureSpecificAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitInvocationExpression(InvocationExpression invocationExpression)
//			{
//				base.VisitInvocationExpression(invocationExpression);
//
//				var rr = ctx.Resolve(invocationExpression) as CSharpInvocationResolveResult;
//				if (rr == null || rr.IsError)
//					return;
//
//				if (!rr.Member.IsStatic ||
//					rr.Member.Name != "Compare" || 
//				    !rr.Member.DeclaringType.IsKnownType (KnownTypeCode.String) ||
//				    !rr.Member.Parameters[0].Type.IsKnownType(KnownTypeCode.String)) {
//					return;
//				}
//				if (rr.Member.Parameters.Count != 2 &&
//				    rr.Member.Parameters.Count != 3 &&
//				    rr.Member.Parameters.Count != 5 &&
//				    rr.Member.Parameters.Count != 6)
//					return;
//
//				bool? ignoreCase = null;
//				Expression caseArg = null;
//				IParameter lastParameter = rr.Member.Parameters.Last();
//				if (lastParameter.Type.Name == "StringComparison")
//					return; // already specifying a string comparison
//
//				if (rr.Member.Parameters.Count == 3) {
//					if (!rr.Member.Parameters[2].Type.IsKnownType(KnownTypeCode.Boolean))
//						return;
//					if (rr.Arguments[2].IsCompileTimeConstant) {
//						ignoreCase = (bool)rr.Arguments[2].ConstantValue;
//					} else {
//						caseArg = invocationExpression.Arguments.ElementAt(2);
//					}
//				}
//
//				if (rr.Member.Parameters.Count == 6) {
//					if (!rr.Member.Parameters[5].Type.IsKnownType(KnownTypeCode.Boolean))
//						return;
//					if (rr.Arguments[5].IsCompileTimeConstant) {
//						ignoreCase = (bool)rr.Arguments[5].ConstantValue;
//					} else {
//						caseArg = invocationExpression.Arguments.ElementAt(5);
//					}
//				}
//
//
//				AddDiagnosticAnalyzer(new CodeIssue(
//					invocationExpression,
//					ctx.TranslateString(), 
//					new CodeAction(
//						ctx.TranslateString("), 
//						script => AddArgument(script, invocationExpression, CreateCompareArgument (invocationExpression, ignoreCase, caseArg, "Ordinal")), 
//						invocationExpression
//					),
//					new CodeAction(
//						ctx.TranslateString(), 
//						script => AddArgument(script, invocationExpression, CreateCompareArgument (invocationExpression, ignoreCase, caseArg, "Ordinal")), 
//						invocationExpression
//					)
//				));
//			}
//
//			Expression CreateCompareArgument (InvocationExpression invocationExpression, bool? ignoreCase, Expression caseArg, string stringComparison)
//			{
//				var astBuilder = ctx.CreateTypeSystemAstBuilder(invocationExpression);
//				if (caseArg == null)
//					return astBuilder.ConvertType(new TopLevelTypeName("System", "StringComparison")).Member(ignoreCase == true ? stringComparison + "IgnoreCase" : stringComparison);
//
//				return new ConditionalExpression(
//					caseArg.Clone(),
//					astBuilder.ConvertType(new TopLevelTypeName("System", "StringComparison")).Member(stringComparison + "IgnoreCase"),
//					astBuilder.ConvertType(new TopLevelTypeName("System", "StringComparison")).Member(stringComparison)
//				);
//			}
//
//			void AddArgument(Script script, InvocationExpression invocationExpression, Expression compareArgument)
//			{
//				var copy = (InvocationExpression)invocationExpression.Clone();
//				copy.Arguments.Clear();
//				if (invocationExpression.Arguments.Count() <= 3) {
//					copy.Arguments.AddRange(invocationExpression.Arguments.Take(2).Select(a => a.Clone())); 
//				} else {
//					copy.Arguments.AddRange(invocationExpression.Arguments.Take(5).Select(a => a.Clone())); 
//				}
//				copy.Arguments.Add(compareArgument);
//				script.Replace(invocationExpression, copy);
//			}
		}
	}
}