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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[ExportDiagnosticAnalyzer("", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(Description = "", AnalysisDisableKeyword = "")]
	[IssueDescription("CS0659: Class overrides Object.Equals but not Object.GetHashCode.",
		Description = "If two objects are equal then they must both have the same hash code",
		Category = IssueCategories.CompilerWarnings,
		Severity = Severity.Warning,
		PragmaWarning = 1717,
		AnalysisDisableKeyword = "CSharpWarnings::CS0659")]
	public class CS0659ClassOverrideEqualsWithoutGetHashCode : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "";
		const string Description            = "";
		const string MessageFormat          = "";
		const string Category               = IssueCategories.CodeQualityIssues;

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

		class GatherVisitor : GatherVisitorBase<CS0659ClassOverrideEqualsWithoutGetHashCode>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
			{
				base.VisitMethodDeclaration(methodDeclaration);

				var resolvedResult = ctx.Resolve(methodDeclaration) as MemberResolveResult;
				if (resolvedResult == null)
					return;
				var method = resolvedResult.Member as IMethod;

				if (method == null || !method.Name.Equals("Equals") || ! method.IsOverride)
					return;

				if (methodDeclaration.Parameters.Count != 1)
					return;
	
				if (!method.Parameters.Single().Type.FullName.Equals("System.Object"))
					return;

				var classDeclration = method.DeclaringTypeDefinition;
				if (classDeclration == null)
					return;

				List<IMethod> getHashCode = new List<IMethod>();
				var methods = classDeclration.GetMethods();

				foreach (var m in methods) {
					if (m.Name.Equals("GetHashCode")) {
						getHashCode.Add(m);
					}
				}

				if (!getHashCode.Any()) {
					AddIssue(ctx, methodDeclaration);
					return;
				} else if (getHashCode.Any(f => (f.IsOverride && f.ReturnType.IsKnownType(KnownTypeCode.Int32)))) {
					return;
				}
				AddIssue(ctx, methodDeclaration);
			}

			private void AddIssue(BaseSemanticModel ctx, AstNode node)
			{
				var getHashCode = new MethodDeclaration();
				getHashCode.Name = "GetHashCode";
				getHashCode.Modifiers = Modifiers.Public;
				getHashCode.Modifiers |= Modifiers.Override;
				getHashCode.ReturnType = new PrimitiveType("int");

				var blockStatement = new BlockStatement();
				var invocationExpression = new InvocationExpression(new MemberReferenceExpression(new BaseReferenceExpression(),"GetHashCode"));
				var returnStatement = new ReturnStatement(invocationExpression);
				blockStatement.Add(returnStatement);
				getHashCode.Body = blockStatement;

				AddIssue(new CodeIssue(
					(node as MethodDeclaration).NameToken, 
					ctx.TranslateString("If two objects are equal then they must both have the same hash code"),
					new CodeAction(
					ctx.TranslateString("Override GetHashCode"),
					script => {
					script.InsertAfter(node, getHashCode); 
				},
				node
					)));
			}
		}
	}

	[ExportCodeFixProvider(.DiagnosticId, LanguageNames.CSharp)]
	public class FixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return .DiagnosticId;
		}

		public async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagonstic in diagnostics) {
				var node = root.FindNode(diagonstic.Location.SourceSpan);
				//if (!node.IsKind(SyntaxKind.BaseList))
				//	continue;
				var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
				result.Add(CodeActionFactory.Create(node.Span, diagonstic.Severity, diagonstic.GetMessage(), document.WithSyntaxRoot(newRoot)));
			}
			return result;
		}
	}
}