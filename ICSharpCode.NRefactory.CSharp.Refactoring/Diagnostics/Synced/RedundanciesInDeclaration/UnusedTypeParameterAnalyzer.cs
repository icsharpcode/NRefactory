// 
// UnusedTypeParameterAnalyzer.cs
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "UnusedTypeParameter")]
	public class UnusedTypeParameterAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		//static FindReferences refFinder = new FindReferences();

		internal const string DiagnosticId  = "UnusedTypeParameterAnalyzer.;
		const string Description            = "Type parameter is never used";
		const string MessageFormat          = "Type parameter '{0}' is never used";
		const string Category               = DiagnosticAnalyzerCategories.RedundanciesInDeclarations;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Unused type parameter");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

//		protected static bool FindUsage(BaseSemanticModel context, SyntaxTree unit,
//		                                 ITypeParameter typeParameter, AstNode declaration)
//		{
//			var found = false;
//			var searchScopes = refFinder.GetSearchScopes(typeParameter);
//			refFinder.FindReferencesInFile(searchScopes, context.Resolver,
//				(node, resolveResult) => {
//					if (node != declaration)
//						found = true;
//				}, context.CancellationToken);
//			return found;
//		}
//
		class GatherVisitor : GatherVisitorBase<UnusedTypeParameterAnalyzer>
		{
			SyntaxTree unit;

			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitTypeParameterDeclaration(TypeParameterDeclaration decl)
//			{
//				base.VisitTypeParameterDeclaration(decl);
//
//				var resolveResult = ctx.Resolve(decl) as TypeResolveResult;
//				if (resolveResult == null)
//					return;
//				var typeParameter = resolveResult.Type as ITypeParameter;
//				if (typeParameter == null)
//					return;
//				var methodDecl = decl.Parent as MethodDeclaration;
//				if (methodDecl == null)
//					return;
//
//				if (FindUsage(ctx, unit, typeParameter, decl))
//					return;
//
			//				AddDiagnosticAnalyzer(new CodeIssue(decl.NameToken, ctx.TranslateString("Type parameter is never used")) { IssueMarker = IssueMarker.GrayOut });
//			}
		}
	}

}
