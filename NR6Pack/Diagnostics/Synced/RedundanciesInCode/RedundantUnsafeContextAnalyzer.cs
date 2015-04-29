//
// RedundantUnsafeContextAnalyzer.cs
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
    [NotPortedYet]
    public class RedundantUnsafeContextAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.RedundantUnsafeContextAnalyzerID, 
			GettextCatalog.GetString("Unsafe modifier in redundant in unsafe context or when no unsafe constructs are used"),
			GettextCatalog.GetString("'unsafe' modifier is redundant"), 
			DiagnosticAnalyzerCategories.RedundanciesInCode, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.RedundantUnsafeContextAnalyzerID),
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
			if (nodeContext.IsFromGeneratedCode())
				return false;
			//var node = nodeContext.Node as ;
			//diagnostic = Diagnostic.Create (descriptor, node.GetLocation ());
			//return true;
			return false;
		}

//		class GatherVisitor : GatherVisitorBase<RedundantUnsafeContextAnalyzer>
//		{
//			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
//				: base (semanticModel, addDiagnostic, cancellationToken)
//			{
//			}

////			class UnsafeState 
////			{
////				public bool InUnsafeContext;
////				public bool UseUnsafeConstructs;
////
////				public UnsafeState(bool inUnsafeContext)
////				{
////					this.InUnsafeContext = inUnsafeContext;
////					this.UseUnsafeConstructs = false;
////				}
////
////				public override string ToString()
////				{
////					return string.Format("[UnsafeState: InUnsafeContext={0}, UseUnsafeConstructs={1}]", InUnsafeContext, UseUnsafeConstructs);
////				}
////			}
////
////			readonly Stack<UnsafeState> unsafeStateStack = new Stack<UnsafeState> ();
////
////			public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
////			{
////				bool unsafeIsRedundant = false;
////				if (unsafeStateStack.Count > 0) {
////					var curState = unsafeStateStack.Peek();
////
////					unsafeIsRedundant |= typeDeclaration.HasModifier(Modifiers.Unsafe);
////
////					unsafeStateStack.Push(new UnsafeState (curState.InUnsafeContext)); 
////				} else {
////					unsafeStateStack.Push(new UnsafeState (typeDeclaration.HasModifier(Modifiers.Unsafe))); 
////				}
////
////				base.VisitTypeDeclaration(typeDeclaration);
////
////				var state = unsafeStateStack.Pop();
////				unsafeIsRedundant = typeDeclaration.HasModifier(Modifiers.Unsafe) && !state.UseUnsafeConstructs;
////				if (unsafeIsRedundant) {
////					AddDiagnosticAnalyzer(new CodeIssue(
////						typeDeclaration.ModifierTokens.First (t => t.Modifier == Modifiers.Unsafe),
////						ctx.TranslateString(""), 
////						ctx.TranslateString(""), 
////						script => script.ChangeModifier(typeDeclaration, typeDeclaration.Modifiers & ~Modifiers.Unsafe)
////					) { IssueMarker = IssueMarker.GrayOut });
////				}
////			}
////
////			public override void VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration)
////			{
////				base.VisitFixedFieldDeclaration(fixedFieldDeclaration);
////				unsafeStateStack.Peek().UseUnsafeConstructs = true;
////			}
////
////			public override void VisitComposedType(ComposedType composedType)
////			{
////				base.VisitComposedType(composedType);
////				if (composedType.PointerRank > 0)
////					unsafeStateStack.Peek().UseUnsafeConstructs = true;
////			}
////
////			public override void VisitFixedStatement(FixedStatement fixedStatement)
////			{
////				base.VisitFixedStatement(fixedStatement);
////
////				unsafeStateStack.Peek().UseUnsafeConstructs = true;
////			}
////
////			public override void VisitSizeOfExpression(SizeOfExpression sizeOfExpression)
////			{
////				base.VisitSizeOfExpression(sizeOfExpression);
////				unsafeStateStack.Peek().UseUnsafeConstructs = true;
////			}
////
////			public override void VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression)
////			{
////				base.VisitUnaryOperatorExpression(unaryOperatorExpression);
////				if (unaryOperatorExpression.Operator == UnaryOperatorType.AddressOf ||
////				    unaryOperatorExpression.Operator == UnaryOperatorType.Dereference)
////					unsafeStateStack.Peek().UseUnsafeConstructs = true;
////			}
////		
////			public override void VisitUnsafeStatement(UnsafeStatement unsafeStatement)
////			{
////				unsafeStateStack.Peek().UseUnsafeConstructs = true;
////				bool isRedundant = unsafeStateStack.Peek().InUnsafeContext;
////				unsafeStateStack.Push(new UnsafeState (true)); 
////				base.VisitUnsafeStatement(unsafeStatement);
////				isRedundant |= !unsafeStateStack.Pop().UseUnsafeConstructs;
////
////				if (isRedundant) {
////					AddDiagnosticAnalyzer(new CodeIssue(
////						unsafeStatement.UnsafeToken,
////						ctx.TranslateString("'unsafe' statement is redundant."), 
////						ctx.TranslateString("Replace 'unsafe' statement with it's body"), 
////						s => {
////							s.Remove(unsafeStatement.UnsafeToken);
////							s.Remove(unsafeStatement.Body.LBraceToken);
////							s.Remove(unsafeStatement.Body.RBraceToken);
////							s.FormatText(unsafeStatement.Parent);
////						}
////					));
////				}
////			}
//		}
	}
}