//
// ThreadStaticAtInstanceFieldAnalyzer.cs
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ThreadStaticAtInstanceField")]
	public class ThreadStaticAtInstanceFieldAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "ThreadStaticAtInstanceFieldAnalyzer";
		const string Description            = "[ThreadStatic] doesn't work with instance fields";
		const string MessageFormat          = "";
		const string Category               = DiagnosticAnalyzerCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "[ThreadStatic] doesn't work with instance fields");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<ThreadStaticAtInstanceFieldAnalyzer>
		{
			//IType threadStaticAttribute;

			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
				//threadStaticAttribute = ctx.Compilation.FindType(typeof(ThreadStaticAttribute));
			}

//			public override void VisitBlockStatement(BlockStatement blockStatement)
//			{
//			}
//			
//			public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
//			{
//                if (fieldDeclaration.HasModifier(Modifiers.Static))
//                    return;
//
//                foreach (var attributeSection in fieldDeclaration.Attributes) {
//					int attributeCount = attributeSection.Attributes.Count;
//					foreach (var attribute in attributeSection.Attributes) {
//						var resolvedAttribute = ctx.Resolve(attribute.Type) as TypeResolveResult;
//						if (resolvedAttribute == null)
//							continue;
//						if (threadStaticAttribute.Equals(resolvedAttribute.Type)) {
//							string title = ctx.TranslateString("ThreadStatic does nothing on instance fields");
//							if (attributeCount == 1)
//								AddDiagnosticAnalyzer(new CodeIssue(attributeSection, title, GetActions(attribute, attributeSection, fieldDeclaration)));
//							else
//								AddDiagnosticAnalyzer(new CodeIssue(attribute, title, GetActions(attribute, attributeSection, fieldDeclaration)));
//						}
//					}
//				}
//			}
//
//			IEnumerable<CodeAction> GetActions(Attribute attribute, AttributeSection attributeSection, FieldDeclaration fieldDeclaration)
//			{
//				string removeAttributeMessage = ctx.TranslateString("Remove attribute");
//				yield return new CodeAction(removeAttributeMessage, script => {
//					if (attributeSection.Attributes.Count > 1) {
//						var newSection = new AttributeSection();
//						newSection.AttributeTarget = attributeSection.AttributeTarget;
//						foreach (var attr in attributeSection.Attributes) {
//							if (attr != attribute)
//								newSection.Attributes.Add((Attribute)attr.Clone());
//						}
//						script.Replace(attributeSection, newSection);
//					} else {
//						script.Remove(attributeSection);
//					}
//				}, attribute);
//
//				var makeStaticMessage = ctx.TranslateString("Make the field static");
//				yield return new CodeAction(makeStaticMessage, script => {
//					var newDeclaration = (FieldDeclaration)fieldDeclaration.Clone();
//					newDeclaration.Modifiers |= Modifiers.Static;
//					script.Replace(fieldDeclaration, newDeclaration);
//				}, fieldDeclaration.NameToken);
//			}
		}
	}

	[ExportCodeFixProvider(ThreadStaticAtInstanceFieldAnalyzer.DiagnosticId, LanguageNames.CSharp)]
	public class ThreadStaticAtInstanceFieldFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ThreadStaticAtInstanceFieldAnalyzer.DiagnosticId;
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
				//if (!node.IsKind(SyntaxKind.BaseList))
				//	continue;
				var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, diagnostic.GetMessage(), document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}