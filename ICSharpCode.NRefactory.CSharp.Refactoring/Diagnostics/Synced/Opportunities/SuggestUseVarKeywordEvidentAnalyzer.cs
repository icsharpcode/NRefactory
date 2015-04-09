// 
// UseVarKeywordInspector.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
	/// <summary>
	/// Checks for places where the 'var' keyword can be used. Note that the action is actually done with a context
	/// action.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class SuggestUseVarKeywordEvidentAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "SuggestUseVarKeywordEvidentAnalyzer";
		const string Description            = "Use 'var' keyword when possible";
		const string MessageFormat          = "Use 'var' keyword";
		const string Category               = DiagnosticAnalyzerCategories.Opportunities;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "Use 'var'");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<SuggestUseVarKeywordEvidentAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}
//
//			public override void VisitSyntaxTree(SyntaxTree syntaxTree)
//			{
//				if (!ctx.Supports(UseVarKeywordAction.minimumVersion))
//					return;
//				base.VisitSyntaxTree(syntaxTree);
//			}
//
//			public override void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
//			{
//				base.VisitVariableDeclarationStatement(variableDeclarationStatement);
//				if (variableDeclarationStatement.Type is PrimitiveType) {
//					return;
//				}
//				if (variableDeclarationStatement.Type.IsVar()) {
//					return;
//				}
//				if (variableDeclarationStatement.Variables.Count != 1) {
//					return;
//				}
//				
//				//only checks for cases where the type would be obvious - assignment of new, cast, etc.
//				//also check the type actually matches else the user might want to assign different subclasses later
//				var v = variableDeclarationStatement.Variables.Single();
//				
//				var arrCreate = v.Initializer as ArrayCreateExpression;
//				if (arrCreate != null) {
//					var n = variableDeclarationStatement.Type as ComposedType;
//					//FIXME: check the specifier compatibility
//					if (n != null && n.ArraySpecifiers.Any() && n.BaseType.IsMatch(arrCreate.Type)) {
//						AddDiagnosticAnalyzer(variableDeclarationStatement);
//					}
//				}
//				var objCreate = v.Initializer as ObjectCreateExpression;
//				if (objCreate != null && objCreate.Type.IsMatch(variableDeclarationStatement.Type)) {
//					AddDiagnosticAnalyzer(variableDeclarationStatement);
//				}
//				var asCast = v.Initializer as AsExpression;
//				if (asCast != null && asCast.Type.IsMatch(variableDeclarationStatement.Type)) {
//					AddDiagnosticAnalyzer(variableDeclarationStatement);
//				}
//				var cast = v.Initializer as CastExpression;
//				if (cast != null && cast.Type.IsMatch(variableDeclarationStatement.Type)) {
//					AddDiagnosticAnalyzer(variableDeclarationStatement);
//				}
//			}
//			
//			void AddDiagnosticAnalyzer(VariableDeclarationStatement variableDeclarationStatement)
//			{
//				AddDiagnosticAnalyzer(new CodeIssue(variableDeclarationStatement.Type, ctx.TranslateString("")) { IssueMarker = IssueMarker.DottedLine, ActionProvider = { typeof(UseVarKeywordAction) } });
//			}
		}
	}
}