//
// EmptyStatementIssue.cs
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
 
namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[ExportDiagnosticAnalyzer(DiagnosticId, LanguageNames.CSharp)]
	public class EmptyStatementIssue : ISyntaxNodeAnalyzer<SyntaxKind>
	{
		internal const string DiagnosticId  = "EmptyStatementIssue";
		const string Description   = "Empty statement is redundant";
		internal const string MessageFormat = "Remove ';'";
		const string Category      = IssueCategories.RedundanciesInCode;

		static readonly DiagnosticDescriptor Rule = new NRefactoryDiagnosticDescriptor (DiagnosticId, "?", Description, MessageFormat, Category, DiagnosticSeverity.Warning) {
			AnalysisDisableKeyword = "EmptyStatement"
		};

		public IEnumerable<DiagnosticDescriptor> GetSupportedDiagnostics()
		{
			return ImmutableArray.Create(Rule);
		}

		public IEnumerable<SyntaxKind> SyntaxKindsOfInterest {
			get {
				return ImmutableArray.Create(SyntaxKind.EmptyStatement);
			}
		}

		public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, System.Threading.CancellationToken cancellationToken)
		{
			if (IsEmbeddedStatement(node))
				return;
			addDiagnostic (Diagnostic.Create(Rule, node.GetLocation())); 
		}

		internal static bool IsEmbeddedStatement(SyntaxNode stmt)
		{
			return !stmt.Parent.IsKind(SyntaxKind.Block);
		}
	}

	[ExportCodeFixProvider(EmptyStatementIssue.DiagnosticId, LanguageNames.CSharp)]
	public class EmptyStatementCodeFixProvider : ICodeFixProvider
	{
		#region ICodeFixProvider implementation

		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return EmptyStatementIssue.DiagnosticId;
		}

		public async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagonstic in diagnostics) {
				var token = root.FindNode(diagonstic.Location.SourceSpan);
				if (token.IsKind(SyntaxKind.EmptyStatement)) {
					var newRoot = root.RemoveNode(token, SyntaxRemoveOptions.KeepDirectives);
					result.Add(CodeActionFactory.Create(token.Span, DiagnosticSeverity.Info, EmptyStatementIssue.MessageFormat, document.WithSyntaxRoot(newRoot)));
				}
			}
			return result;
		}
		#endregion
	}
}
