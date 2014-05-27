//
// BaseMethodParameterNameMismatchIssue.cs
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
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[ExportDiagnosticAnalyzer("Parameter name differs in base declaration", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(Description = "Parameter name differs in base declaration")]
	public class BaseMethodParameterNameMismatchIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "BaseMethodParameterNameMismatchIssue";
		const string Description            = "";
		const string MessageFormat          = "";
		const string Category               = IssueCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<BaseMethodParameterNameMismatchIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
//			{
//				// skip
//			}
//
//			public override void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
//			{
//				// skip
//			}
//
//			public override void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
//			{
//				// skip
//			}
//
//			public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
//			{
//				// skip
//			}
//
//			public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
//			{
//				// skip
//			}
//
//			public override void VisitBlockStatement(BlockStatement blockStatement)
//			{
//				// SKIP
//			}
//
//			public override void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
//			{
//				Check (indexerDeclaration);
//			}
//
//			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
//			{
//				Check (methodDeclaration);
//			}
//
//			void Check (EntityDeclaration entity)
//			{
//				if (!entity.HasModifier(Modifiers.Override))
//					return;
//				var rr = ctx.Resolve(entity) as MemberResolveResult;
//				if (rr == null || rr.IsError)
//					return;
//				var method = rr.Member as IParameterizedMember;
//				if (method == null)
//					return;
//				var baseMethod = InheritanceHelper.GetBaseMember(method) as IParameterizedMember;
//				if (baseMethod == null)
//					return;
//
//				for (int i = 0; i < Math.Min (method.Parameters.Count, baseMethod.Parameters.Count); i++) {
//					var arg     = method.Parameters[i];
//					var baseArg = baseMethod.Parameters[i];
//
//					if (arg.Name != baseArg.Name) {
//						int _i = i;
//						var parameters = entity.GetChildrenByRole (Roles.Parameter);
//						AddIssue(new CodeIssue(
//							parameters.ElementAt(_i).NameToken,
//							ctx.TranslateString("Parameter name differs in base method declaration"),
//							string.Format(ctx.TranslateString("Rename to '{0}'"), baseArg.Name),
//							s => {
//								s.Rename(arg, baseArg.Name);
//							}
//						));
//					}
//				}
//			}
		}
	}

	[ExportCodeFixProvider(BaseMethodParameterNameMismatchIssue.DiagnosticId, LanguageNames.CSharp)]
	public class BaseMethodParameterNameMismatchFixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return BaseMethodParameterNameMismatchIssue.DiagnosticId;
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