//
// RedundantEmptyFinallyBlockAnalyzer.cs
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
	public class RedundantEmptyFinallyBlockAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.RedundantEmptyFinallyBlockAnalyzerID, 
			GettextCatalog.GetString("Redundant empty finally block"),
			GettextCatalog.GetString("Redundant empty finally block"), 
			DiagnosticAnalyzerCategories.RedundanciesInCode, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.RedundantEmptyFinallyBlockAnalyzerID),
			customTags: DiagnosticCustomTags.Unnecessary
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
			//var node = nodeContext.Node as ;
			//diagnostic = Diagnostic.Create (descriptor, node.GetLocation ());
			//return true;
			return false;
		}

//		class GatherVisitor : GatherVisitorBase<RedundantEmptyFinallyBlockAnalyzer>
//		{
//			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
//				: base (semanticModel, addDiagnostic, cancellationToken)
//			{
//			}

////			static bool IsEmpty (BlockStatement blockStatement)
////			{
////				return !blockStatement.Descendants.Any(s => s is Statement && !(s is EmptyStatement || s is BlockStatement));
////			}
////
////			public override void VisitBlockStatement(BlockStatement blockStatement)
////			{
////				base.VisitBlockStatement(blockStatement);
////				if (blockStatement.Role != TryCatchStatement.FinallyBlockRole || !IsEmpty (blockStatement))
////					return;
////				var tryCatch = blockStatement.Parent as TryCatchStatement;
////				if (tryCatch == null)
////					return;
////				AddDiagnosticAnalyzer(new CodeIssue(
////					tryCatch.FinallyToken.StartLocation,
////					blockStatement.EndLocation,
////					ctx.TranslateString(""),
////					ctx.TranslateString(""),
////					s => {
////						if (tryCatch.CatchClauses.Any()) {
////							s.Remove(tryCatch.FinallyToken);
////							s.Remove(blockStatement); 
////							s.FormatText(tryCatch);
////							return;
////						}
////						s.Remove(tryCatch.TryToken);
////						s.Remove(tryCatch.TryBlock.LBraceToken);
////						s.Remove(tryCatch.TryBlock.RBraceToken);
////						s.Remove(tryCatch.FinallyToken);
////						s.Remove(tryCatch.FinallyBlock); 
////						s.FormatText(tryCatch.Parent);
////					}
////				) { IssueMarker = IssueMarker.GrayOut });
////			}
//		}
	}
}