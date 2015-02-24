//
// ReplaceWithIsOperatorIssue.cs
//
// Author:
//	   Ji Kun <jikun.nus@gmail.com>
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
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.using System;

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
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "OperatorIsCanBeUsed")]
	public class OperatorIsCanBeUsedIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "OperatorIsCanBeUsedIssue";
		const string Description            = "Operator Is can be used instead of comparing object GetType() and instances of System.Type object";
		const string MessageFormat          = "Operator 'is' can be used";
		const string Category               = IssueCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Operator 'is' can be used");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		sealed class GatherVisitor : GatherVisitorBase<OperatorIsCanBeUsedIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitBinaryExpression(BinaryExpressionSyntax node)
			{
				base.VisitBinaryExpression(node);
				//a.gettype == typeof(b) or //typeof(b) == a.gettype
				if (!node.IsKind(SyntaxKind.EqualsExpression))
					return;

				InvocationExpressionSyntax invocation;
				TypeOfExpressionSyntax typeOf;
				if(!Matches(node.Left, node.Right, out invocation, out typeOf) && !Matches(node.Right, node.Left, out invocation, out typeOf))
					return;

				ITypeSymbol type = semanticModel.GetTypeInfo(typeOf.Type).Type;
				if (type == null || !type.IsSealed)
					return;

				AddIssue(Diagnostic.Create(Rule, node.GetLocation()));
			}

			private bool Matches(ExpressionSyntax member, ExpressionSyntax typeofExpr, out InvocationExpressionSyntax invoc, out TypeOfExpressionSyntax typeOf)
			{
				invoc = member as InvocationExpressionSyntax;
				typeOf = typeofExpr as TypeOfExpressionSyntax;
				if (invoc == null || typeOf == null)
					return false;

				var memberAccess = invoc.Expression as MemberAccessExpressionSyntax;
				return memberAccess != null && memberAccess.Name.Identifier.ValueText == "GetType"; 
			}
		}
	}

	[ExportCodeFixProvider(OperatorIsCanBeUsedIssue.DiagnosticId, LanguageNames.CSharp)]
	public class OperatorIsCanBeUsedIssueFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return OperatorIsCanBeUsedIssue.DiagnosticId;
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
				var node = root.FindNode(diagnostic.Location.SourceSpan) as BinaryExpressionSyntax;

				ExpressionSyntax a;
				TypeSyntax b;
				InvocationExpressionSyntax left = node.Left as InvocationExpressionSyntax;

				//we know it's one or the other
				if (left != null) {
					a = left.Expression;
					b = ((TypeOfExpressionSyntax)node.Right).Type;
				} else {
					a = ((InvocationExpressionSyntax)node.Right).Expression;
					b = ((TypeOfExpressionSyntax)node.Left).Type;
				}
				var isExpr = SyntaxFactory.BinaryExpression(SyntaxKind.IsExpression, ((MemberAccessExpressionSyntax)a).Expression, b);
				var newRoot = root.ReplaceNode((SyntaxNode)node, isExpr.WithAdditionalAnnotations(Formatter.Annotation));
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Replace with 'is' operator", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}