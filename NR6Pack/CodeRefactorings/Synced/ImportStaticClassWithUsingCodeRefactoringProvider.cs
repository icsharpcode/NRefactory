//
// ImportStaticClassWithUsingCodeRefactoringProvider.cs
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
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using System.Diagnostics;
using System;

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name="Import static class with using directive in file")]
	public class ImportStaticClassWithUsingCodeRefactoringProvider : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
				return;
			var span = context.Span;
			if (!span.IsEmpty)
				return;
			var cancellationToken = context.CancellationToken;
			if (cancellationToken.IsCancellationRequested)
				return;

			var options = document.Project.ParseOptions as CSharpParseOptions;
			if (options != null && options.LanguageVersion < LanguageVersion.CSharp6)
				return;
			
			var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			if (model.IsFromGeneratedCode(cancellationToken))
				return;
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
			var node = root.FindToken(span.Start).Parent;
			if (!(node.Parent is MemberAccessExpressionSyntax))
				return;
			var info = model.GetSymbolInfo (node, cancellationToken);
			if (info.Symbol == null || info.Symbol.Kind != SymbolKind.NamedType || !info.Symbol.IsStatic)
				return;

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					node.Span, 
					DiagnosticSeverity.Info, 
					GettextCatalog.GetString ("Import static class with using"), 
					t2 => {
						return ImportStaticClassWithUsing (document, model, root, node, info, t2);
					}
				) 
			);
		}

		async Task<Document> ImportStaticClassWithUsing (Document document, SemanticModel model, SyntaxNode root, SyntaxNode node, SymbolInfo info, CancellationToken cancellationToken)
		{
			var cu = root as CompilationUnitSyntax;
			var staticUsing = SyntaxFactory
				.UsingDirective (SyntaxFactory.ParseName (info.Symbol.ToDisplayString (SymbolDisplayFormat.CSharpErrorMessageFormat)))
				.WithStaticKeyword (SyntaxFactory.Token (SyntaxKind.StaticKeyword))
				.WithAdditionalAnnotations (Formatter.Annotation);
			cu = cu.AddUsingDirective (staticUsing, node, true);
			var newDoc = document.WithSyntaxRoot (cu);

			var newModel = await newDoc.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
			var visitor = new SearchImportReplacementsVisitor (newModel, info, cancellationToken);
			var newRoot = await newModel.SyntaxTree.GetRootAsync (cancellationToken).ConfigureAwait (false);
			visitor.Visit (newRoot);
			cu = (CompilationUnitSyntax)newRoot.TrackNodes (visitor.ReplaceNodes);

			foreach (var ma in visitor.ReplaceNodes) {
				var current = cu.GetCurrentNode<MemberAccessExpressionSyntax> (ma);
				cu = cu.ReplaceNode (current, current.Name.WithAdditionalAnnotations (Formatter.Annotation));
			}

			return document.WithSyntaxRoot (cu);
		}

		class SearchImportReplacementsVisitor : CSharpSyntaxWalker
		{
			readonly SemanticModel model;
			readonly SymbolInfo info;
			readonly CancellationToken cancellationToken;

			public List<MemberAccessExpressionSyntax> ReplaceNodes = new List<MemberAccessExpressionSyntax> ();

			public SearchImportReplacementsVisitor (Microsoft.CodeAnalysis.SemanticModel model, Microsoft.CodeAnalysis.SymbolInfo info, CancellationToken cancellationToken)
			{
				this.cancellationToken = cancellationToken;
				this.model = model;
				this.info = info;
			}

			public override void VisitBlock(BlockSyntax node)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				var tokens = new List<SyntaxToken>();
				foreach (var statement in node.Statements) {
					if (statement.Kind() == SyntaxKind.LocalDeclarationStatement) {
						var declarationStatement = (LocalDeclarationStatementSyntax)statement;
						tokens.AddRange (declarationStatement.Declaration.Variables.Select (d => d.Identifier));
					}
				}

				foreach (var token in tokens)
					AddName (token.ValueText);
				DefaultVisit(node);
				foreach (var token in tokens)
					RemoveName (token.ValueText);
			}

			List<string> GetMemberNames(TypeDeclarationSyntax node)
			{
				var result = new List<string> ();

				var sym = model.GetSymbolInfo (node).Symbol as INamedTypeSymbol;
				if (sym != null) {
					result.AddRange (sym.MemberNames);
				} else {
					foreach (var member in node.Members) {
						result.Add (GetNameToken (member).ValueText);
					}
				}
				return result;
			}

			public static SyntaxToken GetNameToken( MemberDeclarationSyntax member)
			{
				if (member != null)
				{
					switch (member.Kind())
					{
						case SyntaxKind.EnumDeclaration:
						return ((EnumDeclarationSyntax)member).Identifier;
						case SyntaxKind.ClassDeclaration:
						case SyntaxKind.InterfaceDeclaration:
						case SyntaxKind.StructDeclaration:
						return ((TypeDeclarationSyntax)member).Identifier;
						case SyntaxKind.DelegateDeclaration:
						return ((DelegateDeclarationSyntax)member).Identifier;
						case SyntaxKind.FieldDeclaration:
						return ((FieldDeclarationSyntax)member).Declaration.Variables.First().Identifier;
						case SyntaxKind.EventFieldDeclaration:
						return ((EventFieldDeclarationSyntax)member).Declaration.Variables.First().Identifier;
						case SyntaxKind.PropertyDeclaration:
						return ((PropertyDeclarationSyntax)member).Identifier;
						case SyntaxKind.EventDeclaration:
						return ((EventDeclarationSyntax)member).Identifier;
						case SyntaxKind.MethodDeclaration:
						return ((MethodDeclarationSyntax)member).Identifier;
					}
				}

				// Constructors, destructors, indexers and operators don't have names.
				return default(SyntaxToken);
			}

			public override void VisitClassDeclaration (ClassDeclarationSyntax node)
			{
				var memberNames = GetMemberNames (node);
				foreach (var token in memberNames)
					AddName (token);

				base.VisitClassDeclaration (node);

				foreach (var token in memberNames)
					RemoveName (token);
			}

			Dictionary<string, int> conflictNames = new Dictionary<string, int> ();

			void RemoveName (string name)
			{
				if (conflictNames.ContainsKey(name)) {
					conflictNames [name]--;
				}
			}

			void AddName (string name)
			{
				if (!conflictNames.ContainsKey(name)) {
					conflictNames [name] = 1;
					return;
				}
				conflictNames [name]++;
			}

			public override void VisitStructDeclaration (StructDeclarationSyntax node)
			{
				var memberNames = GetMemberNames (node);

				foreach (var token in memberNames)
					AddName (token);

				base.VisitStructDeclaration (node);

				foreach (var token in memberNames)
					RemoveName (token);
			}

			public override void VisitMemberAccessExpression (MemberAccessExpressionSyntax node)
			{
				if (node.IsKind (SyntaxKind.SimpleMemberAccessExpression)) {
					var baseInfo = model.GetSymbolInfo (node.Expression);
					if (baseInfo.Symbol == info.Symbol) {
						var name = node.Name.ToString ();
						if (!conflictNames.ContainsKey(name) || conflictNames[name] <= 0)
							ReplaceNodes.Add (node);
					}
				}
				base.VisitMemberAccessExpression (node);
			}
		}
	}
}

