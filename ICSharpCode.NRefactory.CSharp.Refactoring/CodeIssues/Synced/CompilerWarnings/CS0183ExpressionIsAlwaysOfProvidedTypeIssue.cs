// 
// ExpressionIsAlwaysOfProvidedTypeIssue.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[ExportDiagnosticAnalyzer("CS0183:Given expression is always of the provided type", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(Description = "CS0183:Given expression is always of the provided type", AnalysisDisableKeyword = "CSharpWarnings::CS0183", PragmaWarning = 183)]
	public class CS0183ExpressionIsAlwaysOfProvidedTypeIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "CS0183ExpressionIsAlwaysOfProvidedTypeIssue";
		const string Description            = "Given expression is always of the provided type. Consider comparing with 'null' instead";
		const string MessageFormat          = "Compare with 'null'";
		const string Category               = IssueCategories.CompilerWarnings;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<CS0183ExpressionIsAlwaysOfProvidedTypeIssue>
		{
			//			readonly CSharpConversions conversions;

			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
				// conversions = CSharpConversions.Get(ctx.Compilation);
			}
//
//			public override void VisitIsExpression(IsExpression isExpression)
//			{
//				base.VisitIsExpression(isExpression);
//
//				var type = ctx.Resolve(isExpression.Expression).Type;
//				var providedType = ctx.ResolveType(isExpression.Type);
//
//				if (type.Kind == TypeKind.Unknown || providedType.Kind == TypeKind.Unknown)
//					return;
////				var foundConversion = conversions.ImplicitConversion(type, providedType);
//				if (!IsValidReferenceOrBoxingConversion(type, providedType))
//					return;
//
//				var action = new CodeAction(
//					             ctx.TranslateString(""), 
//					             script => script.Replace(isExpression, new BinaryOperatorExpression(
//						             isExpression.Expression.Clone(), BinaryOperatorType.InEquality, new PrimitiveExpression(null))),
//					             isExpression
//				             );
//				AddIssue(new CodeIssue(isExpression, ctx.TranslateString(""), new [] { action }));
//			}
//
//			bool IsValidReferenceOrBoxingConversion(IType fromType, IType toType)
//			{
//				Conversion c = conversions.ImplicitConversion(fromType, toType);
//				return c.IsValid && (c.IsIdentityConversion || c.IsReferenceConversion || c.IsBoxingConversion);
//			}
		}
	}

	[ExportCodeFixProvider(CS0183ExpressionIsAlwaysOfProvidedTypeIssue.DiagnosticId, LanguageNames.CSharp)]
	public class CS0183ExpressionIsAlwaysOfProvidedTypeFixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return CS0183ExpressionIsAlwaysOfProvidedTypeIssue.DiagnosticId;
		}

		public async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagonstic in diagnostics) {
				var node = root.FindNode(diagonstic.Location.SourceSpan);
				//if (!node.IsKind(SyntaxKind.BaseList))
				//	continue;
				var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
				result.Add(CodeActionFactory.Create(node.Span, diagonstic.Severity, diagonstic.GetMessage(), document.WithSyntaxRoot(newRoot)));
			}
			return result;
		}
	}
}