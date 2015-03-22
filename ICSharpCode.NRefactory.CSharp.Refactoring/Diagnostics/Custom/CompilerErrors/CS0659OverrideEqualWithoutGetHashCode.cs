// 
// CS0659ClassOverrideEqualsWithoutGetHashCode.cs
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
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(PragmaWarning = 1717, AnalysisDisableKeyword = "CSharpWarnings::CS0659")]
	public class CS0659ClassOverrideEqualsWithoutGetHashCode : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId = "CS0659ClassOverrideEqualsWithoutGetHashCode";
		const string Description = "If two objects are equal then they must both have the same hash code";
		const string MessageFormat = "Add 'GetHashCode' method";
		const string Category = DiagnosticAnalyzerCategories.CompilerWarnings;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "CS0659: Class overrides Object.Equals but not Object.GetHashCode.");

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

		class GatherVisitor : GatherVisitorBase<CS0659ClassOverrideEqualsWithoutGetHashCode>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
			{
				base.VisitMethodDeclaration(node);

				var methodSymbol = semanticModel.GetDeclaredSymbol(node);
				if (methodSymbol == null || !methodSymbol.Name.Equals("Equals") || !methodSymbol.IsOverride ||
					methodSymbol.Parameters.Count() != 1 || (!methodSymbol.Parameters.Single().Type.GetFullName().Equals("object") &&
					!methodSymbol.Parameters.Single().Type.GetFullName().Equals("System.Object")))
					return;

				var classSymbol = methodSymbol.ContainingType;
				if (classSymbol == null)
					return;

				var hashCode = classSymbol.GetMembers().OfType<IMethodSymbol>().Where(m => m.Name.Equals("GetHashCode"));
				if (hashCode.Count() == 0 || !hashCode.Any(h => (h.IsOverride && (h.ReturnType.GetFullName().Equals("System.Int32") || h.ReturnType.GetFullName().Equals("int")))))
					AddDiagnosticAnalyzer(Diagnostic.Create(Rule, node.Identifier.GetLocation()));
			}
		}
	}

	[ExportCodeFixProvider(CS0659ClassOverrideEqualsWithoutGetHashCode.DiagnosticId, LanguageNames.CSharp)]
	public class CS0659ClassOverrideEqualsWithoutGetHashCodeFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return CS0659ClassOverrideEqualsWithoutGetHashCode.DiagnosticId;
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
				var hashCode = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)), "GetHashCode").WithModifiers(
					new SyntaxTokenList().Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword)).Add(SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
					.WithBody(SyntaxFactory.Block(
					SyntaxFactory.ReturnStatement().WithExpression(SyntaxFactory.ParseExpression("base.GetHashCode()")))).WithAdditionalAnnotations(Formatter.Annotation);
				var newRoot = root.InsertNodesAfter(node, new List<SyntaxNode>() { hashCode });
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, diagnostic.GetMessage(), document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}