//
// CS1729TypeHasNoConstructorWithNArgumentsIssue.cs
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
	public class CS1729TypeHasNoConstructorWithNArgumentsIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "CS1729TypeHasNoConstructorWithNArgumentsIssue";
		const string Description            = "CS1729: Class does not contain a 0 argument constructor";
		const string MessageFormat          = "";
		const string Category               = IssueCategories.CompilerErrors;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Error, true, "CS1729: Class does not contain a 0 argument constructor");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		private class GatherVisitor : GatherVisitorBase<CS1729TypeHasNoConstructorWithNArgumentsIssue>
		{
//			IType currentType;
//			IType baseType;
//			
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}
//
//			public override void VisitTypeDeclaration(TypeDeclaration declaration)
//			{
//				IType outerType = currentType;
//				IType outerBaseType = baseType;
//				
//				var result = ctx.Resolve(declaration) as TypeResolveResult;
//				currentType = result != null ? result.Type : SpecialType.UnknownType;
//				baseType = currentType.DirectBaseTypes.FirstOrDefault(t => t.Kind != TypeKind.Interface) ?? SpecialType.UnknownType;
//				
//				base.VisitTypeDeclaration(declaration);
//				
//				if (currentType.Kind == TypeKind.Class && currentType.GetConstructors().All(ctor => ctor.IsSynthetic)) {
//					// current type only has the compiler-provided default ctor
//					if (!BaseTypeHasUsableParameterlessConstructor()) {
//						AddIssue(new CodeIssue(declaration.NameToken, GetIssueText(baseType)));
//					}
//				}
//				
//				currentType = outerType;
//				baseType = outerBaseType;
//			}
//
//			public override void VisitConstructorDeclaration(ConstructorDeclaration declaration)
//			{
//				base.VisitConstructorDeclaration(declaration);
//				
//				if (declaration.Initializer.IsNull && !declaration.HasModifier(Modifiers.Static)) {
//					// Check if parameterless ctor is available:
//					if (!BaseTypeHasUsableParameterlessConstructor()) {
//						AddIssue(new CodeIssue(declaration.NameToken, GetIssueText(baseType)));
//					}
//				}
//			}
//			
//			const OverloadResolutionErrors errorsIndicatingWrongNumberOfArguments =
//					OverloadResolutionErrors.MissingArgumentForRequiredParameter
//					| OverloadResolutionErrors.TooManyPositionalArguments
//					| OverloadResolutionErrors.Inaccessible;
//			
//			public override void VisitConstructorInitializer(ConstructorInitializer constructorInitializer)
//			{
//				base.VisitConstructorInitializer(constructorInitializer);
//				
//				// Check if existing initializer is valid:
//				var rr = ctx.Resolve(constructorInitializer) as CSharpInvocationResolveResult;
//				if (rr != null && (rr.OverloadResolutionErrors & errorsIndicatingWrongNumberOfArguments) != 0) {
//					IType targetType = constructorInitializer.ConstructorInitializerType == ConstructorInitializerType.Base ? baseType : currentType;
//					AddIssue(new CodeIssue(constructorInitializer.Keyword, GetIssueText(targetType, constructorInitializer.Arguments.Count)));
//				}
//			}
//			
//			public override void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
//			{
//				base.VisitObjectCreateExpression(objectCreateExpression);
//				
//				var rr = ctx.Resolve(objectCreateExpression) as CSharpInvocationResolveResult;
//				if (rr != null && (rr.OverloadResolutionErrors & errorsIndicatingWrongNumberOfArguments) != 0) {
//					AddIssue(new CodeIssue(objectCreateExpression.Type, GetIssueText(rr.Type, objectCreateExpression.Arguments.Count)));
//				}
//			}
//			
//			bool BaseTypeHasUsableParameterlessConstructor()
//			{
//				if (baseType.Kind == TypeKind.Unknown)
//					return true; // don't show CS1729 error message if base type is unknown 
//				var memberLookup = new MemberLookup(currentType.GetDefinition(), ctx.Compilation.MainAssembly);
//				OverloadResolution or = new OverloadResolution(ctx.Compilation, new ResolveResult[0]);
//				foreach (var ctor in baseType.GetConstructors()) {
//					if (memberLookup.IsAccessible(ctor, allowProtectedAccess: true)) {
//						if (or.AddCandidate(ctor) == OverloadResolutionErrors.None)
//							return true;
//					}
//				}
//				return false;
//			}
//			
//			string GetIssueText(IType targetType, int argumentCount = 0)
//			{
//				return string.Format(ctx.TranslateString("CS1729: The type '{0}' does not contain a constructor that takes '{1}' arguments"), targetType.Name, argumentCount);
//			}
		}
	}

	[ExportCodeFixProvider(CS1729TypeHasNoConstructorWithNArgumentsIssue.DiagnosticId, LanguageNames.CSharp)]
	public class CS1729TypeHasNoConstructorWithNArgumentsFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return CS1729TypeHasNoConstructorWithNArgumentsIssue.DiagnosticId;
		}

		public override async Task ComputeFixesAsync(CodeFixContext context)
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
				context.RegisterFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, diagnostic.GetMessage(), document.WithSyntaxRoot(newRoot)), diagnostic);
			}
		}
	}
}