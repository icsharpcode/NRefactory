//
// BaseMethodCallWithDefaultParameterIssue.cs
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
using System.Linq;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "BaseMethodCallWithDefaultParameter")]
	public class BaseMethodCallWithDefaultParameterIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "BaseMethodCallWithDefaultParameterIssue";
		const string Description            = "Call to base member with implicit default parameters";
		const string MessageFormat          = "Call to base member with implicit default parameters";
		const string Category               = IssueCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Call to base member with implicit default parameters");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<BaseMethodCallWithDefaultParameterIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
			{
				// skip
			}

			public override void VisitDestructorDeclaration(DestructorDeclarationSyntax node)
			{
				// skip
			}

			public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
			{
				// skip
			}

			public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
			{
				// skip
			}

			public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
			{
				// skip
			}

			public override void VisitEventDeclaration(EventDeclarationSyntax node)
			{
				// skip
			}

			public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
			{
				// skip
			}

			public override void VisitInvocationExpression(InvocationExpressionSyntax node)
			{
				base.VisitInvocationExpression(node);
				var mr = node.Expression as MemberAccessExpressionSyntax;
				if (mr == null || !mr.Expression.IsKind(SyntaxKind.BaseExpression))
					return;

				var invocationRR = semanticModel.GetSymbolInfo(node);
				if (invocationRR.Symbol == null)
					return;

				var parentEntity = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
				if (parentEntity == null)
					return;
				var rr = semanticModel.GetDeclaredSymbol(parentEntity);
				if (rr == null || rr.OverriddenMethod != invocationRR.Symbol)
					return;

				var parameters = invocationRR.Symbol.GetParameters();
				if (node.ArgumentList.Arguments.Count >= parameters.Length ||
					parameters.Length == 0 || 
					!parameters.Last().IsOptional)
					return;
				AddIssue (Diagnostic.Create(Rule, Location.Create(semanticModel.SyntaxTree, node.Span)));
			}

			public override void VisitElementAccessExpression(ElementAccessExpressionSyntax node)
			{
				base.VisitElementAccessExpression(node);

				var mr = node.Expression;
				if (mr == null || !mr.IsKind(SyntaxKind.BaseExpression))
					return;

				var invocationRR = semanticModel.GetSymbolInfo(node);
				if (invocationRR.Symbol == null)
					return;

				var parentEntity = node.FirstAncestorOrSelf<IndexerDeclarationSyntax>();
				if (parentEntity == null)
					return;
				var rr = semanticModel.GetDeclaredSymbol(parentEntity);
				if (rr == null || rr.OverriddenProperty != invocationRR.Symbol)
					return;

				var parameters = invocationRR.Symbol.GetParameters();
				if (node.ArgumentList.Arguments.Count >= parameters.Length ||
					parameters.Length == 0 || 
					!parameters.Last().IsOptional)
					return;
				AddIssue (Diagnostic.Create(Rule, Location.Create(semanticModel.SyntaxTree, node.Span)));
			}
		}
	}
}