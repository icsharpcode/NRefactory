// 
// LocalVariableNotUsedAnalyzer.cs
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
	public class LocalVariableNotUsedAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.LocalVariableNotUsedAnalyzerID, 
			GettextCatalog.GetString("Local variable is never used"),
			GettextCatalog.GetString("Local variable is never used"), 
			DiagnosticAnalyzerCategories.RedundanciesInDeclarations, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.LocalVariableNotUsedAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<LocalVariableNotUsedAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
			{
				base.VisitVariableDeclarator(node);
	
//				// check if variable is assigned
//				if (!variableInitializer.Initializer.IsNull)
//					return;
//				var decl = variableInitializer.Parent as VariableDeclarationStatement;
//				if (decl == null)
//					return;
//
//				var resolveResult = ctx.Resolve(variableInitializer) as LocalResolveResult;
//				if (resolveResult == null)
//					return;
//
//				if (IsUsed(decl.Parent, resolveResult.Variable, variableInitializer))
//					return;
//
//				AddDiagnosticAnalyzer(new CodeIssue(variableInitializer.NameToken, 
//					string.Format(ctx.TranslateString(""), resolveResult.Variable.Name), ctx.TranslateString(""),
//					script => {
//						if (decl.Variables.Count == 1) {
//							script.Remove(decl);
//						} else {
//							var newDeclaration = (VariableDeclarationStatement)decl.Clone();
//							newDeclaration.Variables.Remove(
//								newDeclaration.Variables.FirstOrNullObject(v => v.Name == variableInitializer.Name));
//							script.Replace(decl, newDeclaration);
//						}
//					}) { IssueMarker = IssueMarker.GrayOut });
			}


			public override void VisitForEachStatement(ForEachStatementSyntax node)
			{
				base.VisitForEachStatement(node);

//				var resolveResult = ctx.Resolve(foreachStatement.VariableNameToken) as LocalResolveResult;
//				if (resolveResult == null)
//					return;
//
//				if (IsUsed(foreachStatement, resolveResult.Variable, foreachStatement.VariableNameToken))
//					return;
//
//				AddDiagnosticAnalyzer(new CodeIssue(foreachStatement.VariableNameToken, ctx.TranslateString("Local variable is never used")));
			}

//			bool IsUsed(SyntaxNode rootNode, ILocalSymbol variable, SyntaxNode variableNode)
//			{
//				return ctx.FindReferences(rootNode, variable).Any(result => result.Node != variableNode);
//			}
		}
	}

	
}