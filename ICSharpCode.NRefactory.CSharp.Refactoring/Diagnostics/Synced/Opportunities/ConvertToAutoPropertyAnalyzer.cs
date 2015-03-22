//
// ConvertToAutoPropertyAnalyzer.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "ConvertToAutoProperty")]
	public class ConvertToAutoPropertyAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "ConvertToAutoPropertyAnalyzer";
		const string Description            = "Convert property to auto property";
		const string MessageFormat          = "Convert to auto property";
		const string Category               = DiagnosticAnalyzerCategories.Opportunities;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "Convert property to auto property");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<ConvertToAutoPropertyAnalyzer>
		{
			//readonly Stack<TypeDeclaration> typeStack = new Stack<TypeDeclaration>();

			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitBlockStatement(BlockStatement blockStatement)
//			{
//				// SKIP
//			}
//
//			bool IsValidField(IField field)
//			{
//				if (field == null || field.Attributes.Count > 0 || field.IsVolatile)
//					return false;
//				foreach (var m in typeStack.Peek().Members.OfType<FieldDeclaration>()) {
//					foreach (var i in m.Variables) {
//						if (i.StartLocation == field.BodyRegion.Begin) {
//							if (!i.Initializer.IsNull)
//								return false;
//							break;
//						}
//					}
//				}
//				return true;
//			}
//
//			public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
//			{
//				var field = RemoveBackingStoreAction.GetBackingField(ctx, propertyDeclaration);
//				if (!IsValidField(field))
//					return;
//				AddDiagnosticAnalyzer(new CodeIssue(
//					propertyDeclaration.NameToken,
//					ctx.TranslateString("Convert to auto property")
//				) {
//					ActionProvider = { typeof (RemoveBackingStoreAction) }
//				}
//				);
//			}
//
//			public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
//			{
//				typeStack.Push(typeDeclaration); 
//				base.VisitTypeDeclaration(typeDeclaration);
//				typeStack.Pop();
//			}
		}
	}

	[ExportCodeFixProvider(ConvertToAutoPropertyAnalyzer.DiagnosticId, LanguageNames.CSharp)]
	public class ConvertToAutoPropertyFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return ConvertToAutoPropertyAnalyzer.DiagnosticId;
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
				context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Convert to auto property", document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}