// RedundantArrayCreationExpressionIssue.cs
//
// Author:
//      Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013 Ji Kun <jikun.nus@gmail.com>
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ArrayCreationCanBeReplacedWithArrayInitializer")]
	public class ArrayCreationCanBeReplacedWithArrayInitializerIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "ArrayCreationCanBeReplacedWithArrayInitializerIssue";
		const string Description            = "When initializing explicitly typed local variable or array type, array creation expression can be replaced with array initializer.";
		const string MessageFormat          = "Redundant array creation expression";
		const string Category               = IssueCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Array creation can be replaced with array initializer");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<ArrayCreationCanBeReplacedWithArrayInitializerIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node)
			{
				base.VisitImplicitArrayCreationExpression(node);
				if (node.Initializer == null)
					return;
				var varInitializer = node.Parent.Parent;
				if (varInitializer == null)
					return;
				var variableDeclaration = varInitializer.Parent as VariableDeclarationSyntax;
				if (variableDeclaration != null) {
					if (!variableDeclaration.Type.IsKind(SyntaxKind.ArrayType))
						return;
					AddIssue (Diagnostic.Create(Rule, Location.Create(semanticModel.SyntaxTree, TextSpan.FromBounds(node.NewKeyword.Span.Start, node.Initializer.Span.Start))));
				}
			}

			public override void VisitArrayCreationExpression(ArrayCreationExpressionSyntax node)
			{
				base.VisitArrayCreationExpression(node);
				if (node.Initializer == null)
					return;
				var varInitializer = node.Parent.Parent;
				if (varInitializer == null)
					return;
				var variableDeclaration = varInitializer.Parent as VariableDeclarationSyntax;
				if (variableDeclaration != null) {
					if (!variableDeclaration.Type.IsKind(SyntaxKind.ArrayType))
						return;
					AddIssue (Diagnostic.Create(Rule, Location.Create(semanticModel.SyntaxTree, TextSpan.FromBounds(node.NewKeyword.Span.Start, node.Initializer.Span.Start))));
				}
			}
		}
	}

	[ExportCodeFixProvider(ArrayCreationCanBeReplacedWithArrayInitializerIssue.DiagnosticId, LanguageNames.CSharp)]
	public class ArrayCreationCanBeReplacedWithArrayInitializerFixProvider : NRefactoryCodeFixProvider
	{
		#region ICodeFixProvider implementation

		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ArrayCreationCanBeReplacedWithArrayInitializerIssue.DiagnosticId;
		}

		public async override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			var cancellationToken = context.CancellationToken;
			var span = context.Span;
			var diagnostics = context.Diagnostics;
			var text = await document.GetTextAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagnostic in diagnostics) {
				var sourceSpan = diagnostic.Location.SourceSpan;
				context.RegisterCodeFix(CodeActionFactory.Create(sourceSpan, diagnostic.Severity, "Use array initializer", document.WithText(text.Replace(sourceSpan, ""))), diagnostic);
			}
		}
		#endregion
	}
}