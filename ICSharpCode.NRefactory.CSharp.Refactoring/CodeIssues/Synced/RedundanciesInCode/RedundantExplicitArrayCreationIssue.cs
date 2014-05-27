//
// RedundantExplicitArrayCreationIssue.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[ExportDiagnosticAnalyzer("Redundant explicit type in array creation", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(Description = "Redundant explicit type in array creation", AnalysisDisableKeyword = "RedundantExplicitArrayCreation")]
	public class RedundantExplicitArrayCreationIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "RedundantExplicitArrayCreationIssue";
		const string Description            = "Redundant explicit array type specification";
		const string MessageFormat          = "Remove redundant type specification";
		const string Category               = IssueCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<RedundantExplicitArrayCreationIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitSyntaxTree(SyntaxTree syntaxTree)
//			{
//				if (!ctx.Supports(new Version(3, 0)))
//					return;
//				base.VisitSyntaxTree(syntaxTree);
//			}
//
//			public override void VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression)
//			{
//				base.VisitArrayCreateExpression(arrayCreateExpression);
//				if (arrayCreateExpression.Arguments.Count != 0)
//					return;
//				var arrayType = arrayCreateExpression.Type;
//				if (arrayType.IsNull)
//					return;
//				var arrayTypeRR = ctx.Resolve(arrayType);
//				if (arrayTypeRR.IsError)
//					return;
//
//				IType elementType = null;
//				foreach (var element in arrayCreateExpression.Initializer.Elements) {
//					var elementTypeRR = ctx.Resolve(element);
//					if (elementTypeRR.IsError)
//						return;
//					if (elementType == null) {
//						elementType = elementTypeRR.Type;
//						continue;
//					}
//					if (elementType != elementTypeRR.Type)
//						return;
//				}
//				if (elementType != arrayTypeRR.Type)
//					return;
//
//				AddIssue(
//					new CodeIssue (
//						arrayType,
//						s => s.Remove(arrayType) 
//					) { IssueMarker = IssueMarker.GrayOut }
//				);
//			}
		}
	}

	[ExportCodeFixProvider(RedundantExplicitArrayCreationIssue.DiagnosticId, LanguageNames.CSharp)]
	public class RedundantExplicitArrayCreationFixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return RedundantExplicitArrayCreationIssue.DiagnosticId;
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