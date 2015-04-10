//
// BaseMethodParameterNameMismatchAnalyzer.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
	public class BaseMethodParameterNameMismatchAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.BaseMethodParameterNameMismatchAnalyzerID, 
			GettextCatalog.GetString("Parameter name differs in base declaration"),
			GettextCatalog.GetString("Parameter name differs in base declaration"), 
			DiagnosticAnalyzerCategories.CodeQualityIssues, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.BaseMethodParameterNameMismatchAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<BaseMethodParameterNameMismatchAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax constructorDeclaration)
			{
				// skip
			}

			public override void VisitDestructorDeclaration(DestructorDeclarationSyntax destructorDeclaration)
			{
				// skip
			}

			public override void VisitOperatorDeclaration(OperatorDeclarationSyntax operatorDeclaration)
			{
				// skip
			}

			public override void VisitPropertyDeclaration(PropertyDeclarationSyntax propertyDeclaration)
			{
				// skip
			}

			public override void VisitFieldDeclaration(FieldDeclarationSyntax fieldDeclaration)
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

			public override void VisitBlock(BlockSyntax node)
			{
				// SKIP
			}

			public override void VisitIndexerDeclaration(IndexerDeclarationSyntax indexerDeclaration)
			{
				var rr = semanticModel.GetDeclaredSymbol(indexerDeclaration);
				if (rr == null || !rr.IsOverride)
					return;
				var baseProperty = rr.OverriddenProperty;
				if (baseProperty == null)
					return;
				Check(indexerDeclaration.ParameterList.Parameters, rr.Parameters, baseProperty.Parameters); 
			}
		
			public override void VisitMethodDeclaration(MethodDeclarationSyntax methodDeclaration)
			{
				var rr = semanticModel.GetDeclaredSymbol(methodDeclaration);
				if (rr == null || !rr.IsOverride)
					return;
				var baseMethod = rr.OverriddenMethod;
				if (baseMethod == null)
					return;
				Check(methodDeclaration.ParameterList.Parameters, rr.Parameters, baseMethod.Parameters); 
			}

			void Check(SeparatedSyntaxList<ParameterSyntax> syntaxParams, ImmutableArray<IParameterSymbol> list1, ImmutableArray<IParameterSymbol> list2)
			{
				var upper = Math.Min(list1.Length, list2.Length);
				for (int i = 0; i < upper; i++) {
					var arg     = list1[i];
					var baseArg = list2[i];

					if (arg.Name != baseArg.Name) {
						AddDiagnosticAnalyzer (Diagnostic.Create(
							descriptor.Id,
							descriptor.Category,
							descriptor.MessageFormat,
							descriptor.DefaultSeverity,
							descriptor.DefaultSeverity,
							descriptor.IsEnabledByDefault,
							4,
							descriptor.Title,
							descriptor.Description,
							descriptor.HelpLinkUri,
							Location.Create(semanticModel.SyntaxTree, syntaxParams[i].Identifier.Span),
							null,
							new [] { baseArg.Name }
						));
					}
				}
			}
		}
	}
}