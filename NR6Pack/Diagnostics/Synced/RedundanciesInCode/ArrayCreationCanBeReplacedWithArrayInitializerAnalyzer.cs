// RedundantArrayCreationExpressionAnalyzer.cs
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

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ArrayCreationCanBeReplacedWithArrayInitializerAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ArrayCreationCanBeReplacedWithArrayInitializerAnalyzerID, 
			GettextCatalog.GetString("When initializing explicitly typed local variable or array type, array creation expression can be replaced with array initializer."),
			GettextCatalog.GetString("Redundant array creation expression"), 
			DiagnosticAnalyzerCategories.RedundanciesInCode, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.ArrayCreationCanBeReplacedWithArrayInitializerAnalyzerID),
			customTags: DiagnosticCustomTags.Unnecessary
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				nodeContext => {
					Diagnostic diagnostic;
					if (TryGetDiagnostic (nodeContext, out diagnostic)) {
						nodeContext.ReportDiagnostic(diagnostic);
					}
				}, 
				new SyntaxKind[] {  SyntaxKind.ArrayCreationExpression, SyntaxKind.ImplicitArrayCreationExpression }
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;

			InitializerExpressionSyntax initializer = null;
			var node = nodeContext.Node as ArrayCreationExpressionSyntax;
			if (node != null) initializer = node.Initializer;
			var inode = nodeContext.Node as ImplicitArrayCreationExpressionSyntax;
			if (inode != null) initializer = inode.Initializer;

			if (initializer == null)
				return false;
			var varInitializer = nodeContext.Node.Parent.Parent;
			if (varInitializer == null)
				return false;
			var variableDeclaration = varInitializer.Parent as VariableDeclarationSyntax;
			if (variableDeclaration != null) {
				if (!variableDeclaration.Type.IsKind(SyntaxKind.ArrayType))
					return false;
				diagnostic = Diagnostic.Create (
					descriptor,
					Location.Create(nodeContext.SemanticModel.SyntaxTree, TextSpan.FromBounds((node != null ? node.NewKeyword : inode.NewKeyword).Span.Start, initializer.Span.Start))
				);
				return true;
			}
			return false;
		}
	}
}