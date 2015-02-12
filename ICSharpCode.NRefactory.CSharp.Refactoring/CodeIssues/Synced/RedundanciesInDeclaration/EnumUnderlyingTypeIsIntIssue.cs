//
// EnumUnderlyingTypeIsIntIssue.cs
//
// Author:
//       Luís Reis <luiscubal@gmail.com>
//
// Copyright (c) 2013 Luís Reis
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "EnumUnderlyingTypeIsInt")]
	public class EnumUnderlyingTypeIsIntIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "EnumUnderlyingTypeIsIntIssue";
		const string Description            = "The default underlying type of enums is int, so defining it explicitly is redundant.";
		const string MessageFormat          = "Default underlying type of enums is already int";
		const string Category               = IssueCategories.RedundanciesInDeclarations;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Underlying type of enum is int");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<EnumUnderlyingTypeIsIntIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
			{
				if (node.BaseList == null)
					return;
				var underlyingType = node.BaseList.Types.FirstOrDefault();
				if (underlyingType == null)
					return;
				var info = semanticModel.GetSymbolInfo(underlyingType.Type);
				var type = info.Symbol as ITypeSymbol;
				if (type != null && type.SpecialType == SpecialType.System_Int32) {
					VisitLeadingTrivia(node);
					AddIssue(Diagnostic.Create(Rule, node.BaseList.GetLocation()));
				}
			}

			public override void VisitBlock(BlockSyntax node)
			{
				//No need to visit statements
			}
		}
	}

	[ExportCodeFixProvider(EnumUnderlyingTypeIsIntIssue.DiagnosticId, LanguageNames.CSharp)]
	public class EnumUnderlyingTypeIsIntFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return EnumUnderlyingTypeIsIntIssue.DiagnosticId;
		}

		public override FixAllProvider GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override async Task ComputeFixesAsync(CodeFixContext context)
		{
			var document = context.Document;
			var cancellationToken = context.CancellationToken;
			var span = context.Span;
			var diagnostics = context.Diagnostics;
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagnostic in diagnostics) {
				var node = root.FindNode(diagnostic.Location.SourceSpan);
				if (!node.IsKind(SyntaxKind.BaseList))
					continue;
				var newRoot = root.ReplaceNode((SyntaxNode)
					node.Parent,
					node.Parent.RemoveNode(node, SyntaxRemoveOptions.KeepExteriorTrivia)
					.WithAdditionalAnnotations(Formatter.Annotation)
				);
				context.RegisterFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Remove redundant ': int'", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}