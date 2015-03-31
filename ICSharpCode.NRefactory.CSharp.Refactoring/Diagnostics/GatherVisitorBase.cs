//
// GatherVisitorBase.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using ICSharpCode.NRefactory6.CSharp.Refactoring;

namespace ICSharpCode.NRefactory6.CSharp
{

	/// <summary>
	/// A base class for writing issue provider visitor implementations.
	/// </summary>
	public class GatherVisitorBase<T> : CSharpSyntaxWalker where T : GatherVisitorDiagnosticAnalyzer
	{
		/// <summary>
		/// The issue provider. May be <c>null</c> if none was specified.
		/// </summary>
		protected readonly T issueProvider;
		protected readonly SemanticModel semanticModel;
		readonly Action<Diagnostic> addDiagnostic;
		protected readonly CancellationToken cancellationToken;

		public SemanticModel Ctx {
			get {
				return semanticModel;
			}
		}

		bool isAllDisabled;
		bool isDisabled;
		bool isDisabledOnce;
		bool isGloballySuppressed;
		//bool isPragmaDisabled;
		bool isAttributeSuppressed;
		[SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
		static string disableString;
		[SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
		static string disableOnceString;
		[SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
		static string restoreString;
		[SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
		static string suppressMessageCategory;
		[SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
		static string suppressMessageCheckId;
		//[SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
		//static int pragmaWarning;

		static void SetDisableKeyword(string disableKeyword)
		{
			disableString = "disable " + disableKeyword;
			disableOnceString = "disable once " + disableKeyword;
			restoreString = "restore " + disableKeyword;
		}

		static GatherVisitorBase()
		{
			var attr = (NRefactoryCodeDiagnosticAnalyzerAttribute)typeof(T).GetCustomAttributes(false).FirstOrDefault(a => a is NRefactoryCodeDiagnosticAnalyzerAttribute);
			if (attr == null)
				return;
			if (attr.AnalysisDisableKeyword != null) 
				SetDisableKeyword(attr.AnalysisDisableKeyword);
			suppressMessageCheckId = attr.SuppressMessageCheckId;
			suppressMessageCategory = attr.SuppressMessageCategory;
			//pragmaWarning = attr.PragmaWarning;
		}

		protected void VisitLeadingTrivia (SyntaxNode node)
		{
			var token = node.ChildTokens().First();
			VisitLeadingTrivia(token); 
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GatherVisitorBase{T}"/> class.
		/// </summary>
		public GatherVisitorBase(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken, T qualifierDirectiveEvidentIssueProvider = default(T)) : base (SyntaxWalkerDepth.StructuredTrivia)
		{
			this.semanticModel = semanticModel;
			this.addDiagnostic = addDiagnostic;
			this.cancellationToken = cancellationToken;
			this.issueProvider = qualifierDirectiveEvidentIssueProvider;
			if (suppressMessageCheckId != null) {
				foreach (var attr in this.semanticModel.Compilation.Assembly.GetAttributes()) {
					if (attr.AttributeClass.Name == "SuppressMessageAttribute" && attr.AttributeClass.ContainingNamespace.GetFullName() == "System.Diagnostics.CodeAnalysis") {
						if (attr.ConstructorArguments.Length < 2)
							return;
						var category = attr.ConstructorArguments [0].Value;
						if (category == null || category.ToString() != suppressMessageCategory)
							continue;
						var checkId = attr.ConstructorArguments [1].Value;
						if (checkId == null || checkId.ToString() != suppressMessageCheckId) 
							continue;
						isGloballySuppressed = true;
					}
				}
			}
		}

		public override void VisitClassDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax node)
		{
			var oldAttrSuppressed = isAttributeSuppressed;
			base.VisitClassDeclaration(node);
			isAttributeSuppressed = oldAttrSuppressed;
		}

		public override void VisitStructDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.StructDeclarationSyntax node)
		{
			var oldAttrSuppressed = isAttributeSuppressed;
			base.VisitStructDeclaration(node);
			isAttributeSuppressed = oldAttrSuppressed;
		}

		public override void VisitInterfaceDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.InterfaceDeclarationSyntax node)
		{
			var oldAttrSuppressed = isAttributeSuppressed;
			base.VisitInterfaceDeclaration(node);
			isAttributeSuppressed = oldAttrSuppressed;
		}

		public override void VisitEnumDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.EnumDeclarationSyntax node)
		{
			var oldAttrSuppressed = isAttributeSuppressed;
			base.VisitEnumDeclaration(node);
			isAttributeSuppressed = oldAttrSuppressed;
		}

		public override void VisitDelegateDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.DelegateDeclarationSyntax node)
		{
			var oldAttrSuppressed = isAttributeSuppressed;
			base.VisitDelegateDeclaration(node);
			isAttributeSuppressed = oldAttrSuppressed;
		}

		public override void VisitConstructorDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.ConstructorDeclarationSyntax node)
		{
			var oldAttrSuppressed = isAttributeSuppressed;
			base.VisitConstructorDeclaration(node);
			isAttributeSuppressed = oldAttrSuppressed;
		}

		public override void VisitDestructorDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.DestructorDeclarationSyntax node)
		{
			var oldAttrSuppressed = isAttributeSuppressed;
			base.VisitDestructorDeclaration(node);
			isAttributeSuppressed = oldAttrSuppressed;
		}

		public override void VisitFieldDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax node)
		{
			var oldAttrSuppressed = isAttributeSuppressed;
			base.VisitFieldDeclaration(node);
			isAttributeSuppressed = oldAttrSuppressed;
		}

		public override void VisitIndexerDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.IndexerDeclarationSyntax node)
		{
			var oldAttrSuppressed = isAttributeSuppressed;
			base.VisitIndexerDeclaration(node);
			isAttributeSuppressed = oldAttrSuppressed;
		}

		public override void VisitMethodDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax node)
		{
			var oldAttrSuppressed = isAttributeSuppressed;
			base.VisitMethodDeclaration(node);
			isAttributeSuppressed = oldAttrSuppressed;
		}

		public override void VisitOperatorDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.OperatorDeclarationSyntax node)
		{
			var oldAttrSuppressed = isAttributeSuppressed;
			base.VisitOperatorDeclaration(node);
			isAttributeSuppressed = oldAttrSuppressed;
		}

		public override void VisitPropertyDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax node)
		{
			var oldAttrSuppressed = isAttributeSuppressed;
			base.VisitPropertyDeclaration(node);
			isAttributeSuppressed = oldAttrSuppressed;
		}

		public override void VisitEnumMemberDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.EnumMemberDeclarationSyntax node)
		{
			var oldAttrSuppressed = isAttributeSuppressed;
			base.VisitEnumMemberDeclaration(node);
			isAttributeSuppressed = oldAttrSuppressed;
		}

		public override void VisitEventDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.EventDeclarationSyntax node)
		{
			var oldAttrSuppressed = isAttributeSuppressed;
			base.VisitEventDeclaration(node);
			isAttributeSuppressed = oldAttrSuppressed;
		}

		public override void VisitEventFieldDeclaration(Microsoft.CodeAnalysis.CSharp.Syntax.EventFieldDeclarationSyntax node)
		{
			var oldAttrSuppressed = isAttributeSuppressed;
			base.VisitEventFieldDeclaration(node);
			isAttributeSuppressed = oldAttrSuppressed;
		}

		public override void VisitBlock(Microsoft.CodeAnalysis.CSharp.Syntax.BlockSyntax node)
		{
			cancellationToken.ThrowIfCancellationRequested();
			base.VisitBlock(node);
		}

		public override void VisitTrivia(SyntaxTrivia trivia)
		{
			switch (trivia.Kind()) {
				case SyntaxKind.SingleLineCommentTrivia:
					var txt = trivia.ToString();
					if (string.IsNullOrEmpty(txt))
						return;
					if (isAllDisabled) {
						isAllDisabled &= txt.IndexOf(GatherVisitorConstants.RestoreAllString, StringComparison.OrdinalIgnoreCase) < 0;
					} else {
						isAllDisabled |= txt.IndexOf(GatherVisitorConstants.DisableAllString, StringComparison.OrdinalIgnoreCase) > 0;
					}
	
					if (restoreString != null) {
						if (isDisabled) {
							isDisabled &= txt.IndexOf(restoreString, StringComparison.Ordinal) < 0;
						} else {
							isDisabled |= txt.IndexOf(disableString, StringComparison.Ordinal) > 0;
							isDisabledOnce |= txt.IndexOf(disableOnceString, StringComparison.Ordinal) > 0;
						}
					}
					break;
				case SyntaxKind.PragmaWarningDirectiveTrivia:
					//			if (pragmaWarning == 0)
					//				return;
					//
					//			var warning = preProcessorDirective as PragmaWarningPreprocessorDirective;
					//			if (warning == null)
					//				return;
					//			if (warning.IsDefined(pragmaWarning))
					//				isPragmaDisabled = warning.Disable;
					break;
			}
		}

		public override void VisitAttribute(Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax node)
		{
			base.VisitAttribute(node);

			if (suppressMessageCheckId == null)
				return;
			var symbol = semanticModel.GetSymbolInfo(node).Symbol;
			if (symbol != null && symbol.Name == "SuppressMessageAttribute" && symbol.ContainingNamespace.GetFullName() == "System.Diagnostics.CodeAnalysis") {
				if (node.ArgumentList.Arguments.Count < 2)
					return;
				var category = node.ArgumentList.Arguments.First();
				if (category == null || category.ToString() != suppressMessageCategory)
					return;
				var checkId = node.ArgumentList.Arguments.Skip(1);
				if (checkId == null || checkId.ToString() != suppressMessageCheckId) 
					return;
				isAttributeSuppressed = true;
			}
		}

		protected bool IsSuppressed()
		{
			if (isAllDisabled)
				return true;
			if (isDisabledOnce) {
				isDisabledOnce = false;
				return true;
			}
			return isDisabled || isGloballySuppressed || isAttributeSuppressed;
		}

		protected void AddDiagnosticAnalyzer(Diagnostic issue)
		{
			if (IsSuppressed())
				return;
			addDiagnostic(issue);
		}
	}
}
