// 
// EmptyConstructorIssue.cs
// 
// Author:
//      Ji Kun <jikun.nus@gmail.com>
// 
// Copyright (c) 2013 Ji Kun
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
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SaHALL THE
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[ExportDiagnosticAnalyzer("Empty constructor", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(Description = "An empty public constructor without paramaters is redundant.", AnalysisDisableKeyword = "EmptyConstructor")]
	public class EmptyConstructorIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "EmptyConstructorIssue";
		const string Description            = "Empty constructor is redundant.";
		const string MessageFormat          = "Remove redundant constructor";
		const string Category               = IssueCategories.RedundanciesInDeclarations;

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

		class GatherVisitor : GatherVisitorBase<EmptyConstructorIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}
			bool hasEmptyConstructor;
			bool hasUnemptyConstructor;
			ConstructorDeclarationSyntax emptyContructorNode;

			public override void VisitClassDeclaration(ClassDeclarationSyntax node)
			{
				hasEmptyConstructor = false;
				hasUnemptyConstructor = false;
				emptyContructorNode = null;

				foreach (var child in node.Members.OfType<ConstructorDeclarationSyntax>()) {
					if (child.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword))) 
						continue;
					if (child.ParameterList.Parameters.Count > 0 || !EmptyDestructorIssue.IsEmpty(child.Body)) {
						hasUnemptyConstructor = true;
					} else if (child.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword))) {
						if (child.Initializer != null && child.Initializer.ArgumentList.Arguments.Count > 0)
							continue;
						hasEmptyConstructor = true;
						emptyContructorNode = child;
					}
				}
				if (!hasUnemptyConstructor && hasEmptyConstructor)
					base.VisitClassDeclaration(node);
			}

			public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
			{
				if (!hasUnemptyConstructor && hasEmptyConstructor && emptyContructorNode == node) {
					AddIssue(Diagnostic.Create(Rule, node.GetLocation()));
				}
			}

			public override void VisitBlock(BlockSyntax node)
			{
				// skip
			}
		}
	}

	[ExportCodeFixProvider(EmptyConstructorIssue.DiagnosticId, LanguageNames.CSharp)]
	public class EmptyConstructorFixProvider : ICodeFixProvider
	{
		#region ICodeFixProvider implementation

		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return EmptyConstructorIssue.DiagnosticId;
		}

		public async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagonstic in diagnostics) {
				var node = root.FindNode(diagonstic.Location.SourceSpan);
				if (!node.IsKind(SyntaxKind.ConstructorDeclaration))
					continue;
				var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
				result.Add(CodeActionFactory.Create(node.Span, diagonstic.Severity, diagonstic.GetMessage(), document.WithSyntaxRoot(newRoot)));
			}
			return result;
		}
		#endregion
	}
}