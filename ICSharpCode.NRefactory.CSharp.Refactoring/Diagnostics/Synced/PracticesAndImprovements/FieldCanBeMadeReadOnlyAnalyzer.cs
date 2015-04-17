//
// FieldCanBeMadeReadOnlyAnalyzer.cs
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

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class FieldCanBeMadeReadOnlyAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.FieldCanBeMadeReadOnlyAnalyzerID,
			GettextCatalog.GetString ("Convert field to readonly"),
			GettextCatalog.GetString ("Convert field to readonly"),
			DiagnosticAnalyzerCategories.PracticesAndImprovements,
			DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor (NRefactoryDiagnosticIDs.FieldCanBeMadeReadOnlyAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize (AnalysisContext context)
		{
			context.RegisterCompilationStartAction (nodeContext => Analyze (nodeContext));
		}

		void Analyze (CompilationStartAnalysisContext compilationContext)
		{
			var compilation = compilationContext.Compilation;
			compilationContext.RegisterSyntaxTreeAction (async delegate (SyntaxTreeAnalysisContext context) {
				if (!compilation.SyntaxTrees.Contains (context.Tree))
					return;
				var semanticModel = compilation.GetSemanticModel (context.Tree);
				var root = await context.Tree.GetRootAsync (context.CancellationToken).ConfigureAwait (false);
				var model = compilationContext.Compilation.GetSemanticModel (context.Tree);
				if (model.IsFromGeneratedCode (compilationContext.CancellationToken))
					return;
				foreach (var type in root.DescendantNodesAndSelf (SkipMembers).OfType<ClassDeclarationSyntax> ()) {
					var fieldDeclarations = type
						.ChildNodes ()
						.OfType<FieldDeclarationSyntax> ()
						.Where (f => FieldFilter (model, f))
						.SelectMany (fd => fd.Declaration.Variables.Select (v => new { Field = fd, Variable = v, Symbol = semanticModel.GetDeclaredSymbol (v, context.CancellationToken) }));
					foreach (var candidateField in fieldDeclarations) {
						context.CancellationToken.ThrowIfCancellationRequested ();
						// handled by ConvertToConstantIssue
						if (candidateField?.Variable?.Initializer != null && semanticModel.GetConstantValue (candidateField.Variable.Initializer.Value, context.CancellationToken).HasValue)
							continue;
						
						// user-defined value type -- might be mutable
						var field = candidateField.Symbol;
						if (field != null && !field.GetReturnType ().IsReferenceType) {
							if (field.GetReturnType ().IsDefinedInSource ()) {
								continue;
							}
						}
						bool wasAltered = false;
						bool wasUsed = false;
						foreach (var member in type.Members) {
							if (member == candidateField.Field)
								continue;
							if (IsAltered (model, member, candidateField.Symbol, context.CancellationToken, out wasUsed)) {
								wasAltered = true;
                                break;
							}
						}
						if (!wasAltered && wasUsed) {
							context.CancellationToken.ThrowIfCancellationRequested ();
							try {
								context.ReportDiagnostic (Diagnostic.Create (descriptor, candidateField.Variable.Identifier.GetLocation ()));
							} catch (InvalidOperationException) {}
						}
					}
				}
			});
		}

		bool IsAltered (SemanticModel model, MemberDeclarationSyntax member, ISymbol symbol, CancellationToken token, out bool wasUsed)
		{
			wasUsed = false;
			foreach (var usage in member.DescendantNodesAndSelf ().Where (n => n.IsKind (SyntaxKind.IdentifierName)).OfType<ExpressionSyntax> ()) {
				var info = model.GetSymbolInfo (usage).Symbol;
				if (info == symbol) 
					wasUsed = true;
				if (!usage.IsWrittenTo ())
					continue;
				if (member.IsKind (SyntaxKind.ConstructorDeclaration) && !usage.Ancestors ().Any (a => a.IsKind (SyntaxKind.AnonymousMethodExpression) || a.IsKind (SyntaxKind.SimpleLambdaExpression) || a.IsKind (SyntaxKind.ParenthesizedLambdaExpression)))
					continue;
				if (info == symbol)
					return true;
			}
			return false;
		}

		bool FieldFilter (SemanticModel model, FieldDeclarationSyntax fieldDeclaration)
		{
			if (fieldDeclaration.Modifiers.Any (
				p => p.IsKind (SyntaxKind.ConstKeyword) || p.IsKind (SyntaxKind.ReadOnlyKeyword) ||
					 p.IsKind (SyntaxKind.PublicKeyword) || p.IsKind (SyntaxKind.ProtectedKeyword) || p.IsKind (SyntaxKind.InternalKeyword)))
				return false;

			return true;
		}

		bool SkipMembers (SyntaxNode arg)
		{
			return !arg.IsKind (SyntaxKind.Block);
		}
	}
}