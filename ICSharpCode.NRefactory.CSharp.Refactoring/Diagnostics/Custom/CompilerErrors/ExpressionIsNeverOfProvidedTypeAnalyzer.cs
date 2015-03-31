// 
// ExpressionIsNeverOfProvidedTypeAnalyzer.cs
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
	public class ExpressionIsNeverOfProvidedTypeAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "ExpressionIsNeverOfProvidedTypeAnalyzer";
		const string Description            = "CS0184:Given expression is never of the provided type";
		const string MessageFormat          = "";
		const string Category               = DiagnosticAnalyzerCategories.CompilerWarnings;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "CS0184:Given expression is never of the provided type");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<ExpressionIsNeverOfProvidedTypeAnalyzer>
		{
			//readonly CSharpConversions conversions;

			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
				//conversions = CSharpConversions.Get(ctx.Compilation);
			}

//			public override void VisitIsExpression(IsExpression isExpression)
//			{
//				base.VisitIsExpression(isExpression);
//
////				var conversions = CSharpConversions.Get(ctx.Compilation);
//				var exprType = ctx.Resolve(isExpression.Expression).Type;
//				var providedType = ctx.ResolveType(isExpression.Type);
//
//				if (exprType.Kind == TypeKind.Unknown || providedType.Kind == TypeKind.Unknown)
//					return;
//				if (IsValidReferenceOrBoxingConversion(exprType, providedType))
//					return;
//				
//				var exprTP = exprType as ITypeParameter;
//				var providedTP = providedType as ITypeParameter;
//				if (exprTP != null) {
//					if (IsValidReferenceOrBoxingConversion(exprTP.EffectiveBaseClass, providedType)
//					    && exprTP.EffectiveInterfaceSet.All(i => IsValidReferenceOrBoxingConversion(i, providedType)))
//						return;
//				}
//				if (providedTP != null) {
//					if (IsValidReferenceOrBoxingConversion(exprType, providedTP.EffectiveBaseClass))
//						return;
//				}
//				
//				AddDiagnosticAnalyzer(new CodeIssue(isExpression, ctx.TranslateString("Given expression is never of the provided type")));
//			}
//
//			bool IsValidReferenceOrBoxingConversion(IType fromType, IType toType)
//			{
//				Conversion c = conversions.ExplicitConversion(fromType, toType);
//				return c.IsValid && (c.IsIdentityConversion || c.IsReferenceConversion || c.IsBoxingConversion || c.IsUnboxingConversion);
//			}
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class ExpressionIsNeverOfProvidedTypeFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ExpressionIsNeverOfProvidedTypeAnalyzer.DiagnosticId;
		}

		public override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public async override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			var cancellationToken = context.CancellationToken;
			var span = context.Span;
			var diagnostics = context.Diagnostics;
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var diagnostic = diagnostics.First ();
			var node = root.FindNode(context.Span);
			//if (!node.IsKind(SyntaxKind.BaseList))
			//	continue;
			var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
			context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, diagnostic.GetMessage(), document.WithSyntaxRoot(newRoot)), diagnostic);
		}
	}
}