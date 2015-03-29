//
// LowercaseLongLiteralAnalyzer.cs
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
using Microsoft.CodeAnalysis.FindSymbols;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "LongLiteralEndingLowerL")]
	public class LongLiteralEndingLowerLAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId = "LongLiteralEndingLowerLAnalyzer";
		const string Description = "Lowercase 'l' is often confused with '1'";
		const string MessageFormat = "Long literal ends with 'l' instead of 'L'";
		const string Category = DiagnosticAnalyzerCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Long literal ends with 'l' instead of 'L'");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
		{
			get
			{
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<LongLiteralEndingLowerLAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitLiteralExpression(LiteralExpressionSyntax node)
			{
				if (!(node.Token.Value is long || node.Token.Value is ulong))
					return;

				String literal = node.Token.Text;
				if (literal.Length < 2)
					return;

				char prevChar = literal[literal.Length - 2];
				char lastChar = literal[literal.Length - 1];

				if (prevChar == 'u' || prevChar == 'U') //ul/Ul is not confusing
					return;

				if (lastChar == 'l' || prevChar == 'l')
					AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.GetLocation()));
			}
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class LongLiteralEndingLowerLFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return LongLiteralEndingLowerLAnalyzer.DiagnosticId;
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
			var result = new List<CodeAction>();
			foreach (var diagnostic in diagnostics) {
				var node = root.FindNode(diagnostic.Location.SourceSpan);
				String newLiteral = ((LiteralExpressionSyntax)node).Token.Text.ToUpperInvariant();
				char prevChar = newLiteral[newLiteral.Length - 2];
				char lastChar = newLiteral[newLiteral.Length - 1];
				double newLong = 0;
				if (prevChar == 'U' || prevChar == 'L') //match ul, lu, or l. no need to match just u.
					newLong = long.Parse(newLiteral.Remove(newLiteral.Length - 2));
				else if (lastChar == 'L')
					newLong = long.Parse(newLiteral.Remove(newLiteral.Length - 1));
				else
					newLong = long.Parse(newLiteral); //just in case

				var newRoot = root.ReplaceNode((SyntaxNode)node, ((LiteralExpressionSyntax)node).WithToken(SyntaxFactory.Literal(newLiteral, newLong)));
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Make suffix upper case", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}