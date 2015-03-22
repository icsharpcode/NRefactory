//
// DelegateSubtractionAnalyzer.cs
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Threading;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "DelegateSubtraction")]
	public class DelegateSubtractionAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "DelegateSubtractionAnalyzer";
		const string Description            = "Delegate subtraction has unpredictable result";
		const string MessageFormat          = "Delegate subtraction has unpredictable result";
		const string Category               = DiagnosticAnalyzerCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Delegate subtractions");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<DelegateSubtractionAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			bool IsEvent(SyntaxNode node)
			{
				var rr = semanticModel.GetSymbolInfo(node);
				return rr.Symbol != null && rr.Symbol.Kind == SymbolKind.Event;
			}

			bool IsDelegate(SyntaxNode node)
			{
				var rr = semanticModel.GetTypeInfo(node);
				return rr.Type != null && rr.Type.TypeKind == TypeKind.Delegate;
			}

			public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
			{
				base.VisitAssignmentExpression(node);
				switch (node.Kind())
				{
					case SyntaxKind.SubtractAssignmentExpression:
						if (!IsEvent(node.Left) && IsDelegate(node.Right))
							AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.GetLocation()));
						break;
				}
			}

			public override void VisitBinaryExpression(BinaryExpressionSyntax node)
			{
				base.VisitBinaryExpression(node);
				switch (node.Kind()) {
					case SyntaxKind.SubtractExpression:
						if (!IsEvent(node.Left) && IsDelegate(node.Right))
							AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.GetLocation()));
						break;
				}
			}
		}
	}
}