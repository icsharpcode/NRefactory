//
// SetterDoesNotUseValueParameterTests.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ValueParameterNotUsed")]
	public class ValueParameterNotUsedAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId = "ValueParameterNotUsedAnalyzer.;
		const string Description = "Warns about property or indexer setters and event adders or removers that do not use the value parameter.";
		const string MessageFormat = "Setter doesn't use the 'value' parameter.";
		const string Category = DiagnosticAnalyzerCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "'value' parameter not used");

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

		class GatherVisitor : GatherVisitorBase<ValueParameterNotUsedAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitAccessorDeclaration(AccessorDeclarationSyntax node)
			{
				base.VisitAccessorDeclaration(node);

				if (node.IsKind(SyntaxKind.SetAccessorDeclaration))
				{
					FindIssuesInAccessor(node, "The setter does not use the 'value' parameter");
				}
				else if (node.IsKind(SyntaxKind.AddAccessorDeclaration))
				{
					FindIssuesInAccessor(node, "The add accessor does not use the 'value' parameter");
				}
				else if (node.IsKind(SyntaxKind.RemoveAccessorDeclaration))
				{
					FindIssuesInAccessor(node, "The remove accessor does not use the 'value' parameter");
				}
			}

			public override void VisitEventDeclaration(EventDeclarationSyntax node)
			{
				if ((node.AccessorList == null) || !node.AccessorList.Accessors.Any())
					return;
				if (node.AccessorList.Accessors.Any(a => a.IsKind(SyntaxKind.AddAccessorDeclaration) && a.Body.Statements.Count == 0)
					&& (node.AccessorList.Accessors.Any(a => a.IsKind(SyntaxKind.RemoveAccessorDeclaration) && a.Body.Statements.Count == 0)))
					return;

				base.VisitEventDeclaration(node);
			}

			void FindIssuesInAccessor(AccessorDeclarationSyntax accessor, string issueText)
			{
				var body = accessor.Body;
				if (!IsEligible(body))
					return;

				bool referenceFound = false;
				if (body.Statements.Any())
				{
					var foundValueSymbol = semanticModel.LookupSymbols(body.Statements.First().SpanStart, null, "value").FirstOrDefault();
					if (foundValueSymbol == null)
						return;

					foreach (var valueRef in body.DescendantNodes().OfType<IdentifierNameSyntax>().Where(ins => ins.Identifier.ValueText == "value"))
					{
						var valueRefSymbol = semanticModel.GetSymbolInfo(valueRef).Symbol;
						if (foundValueSymbol.Equals(valueRefSymbol))
						{
							referenceFound = true;
							break;
						}
					}
				}

				if (!referenceFound)
					AddDiagnosticAnalyzer(Diagnostic.Create(Rule, accessor.Keyword.GetLocation(), issueText));
			}

			static bool IsEligible(BlockSyntax body)
			{
				if (body == null)
					return false;
				if (body.Statements.Any(s => s is ThrowStatementSyntax))
					return false;
				return true;
			}
		}
	}
}