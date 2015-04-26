//
// ParameterCanBeDeclaredWithBaseTypeCodeFixProvider.cs
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

//	public class IsTypeCriterion : ITypeCriterion
//	{
//		IType isType;
//
//		public IsTypeCriterion(IType isType)
//		{
//			this.isType = isType;
//		}
//
//		#region ITypeCriterion implementation
//		public bool SatisfiedBy (IType type)
//		{
//			return isType == type ||
//				type.GetAllBaseTypes().Any(t => t == isType);
//		}
//		#endregion
//	}
//
//	public class IsArrayTypeCriterion : ITypeCriterion
//	{
//		#region ITypeCriterion implementation
//
//		public bool SatisfiedBy(IType type)
//		{
//			return type is ArrayType;
//		}
//
//		#endregion
//	}
//
//	public class HasMemberCriterion : ITypeCriterion
//	{
////		IMember neededMember;
//		IList<IMember> acceptableMembers;
//
//		public HasMemberCriterion(IMember neededMember)
//		{
////			this.neededMember = neededMember;
//
//			if (neededMember.ImplementedInterfaceMembers.Any()) {
//				acceptableMembers = neededMember.ImplementedInterfaceMembers.ToList();
//			} else if (neededMember.IsOverride) {
//				acceptableMembers = new List<IMember>();
//				foreach (var member in InheritanceHelper.GetBaseMembers(neededMember, true)) {
//					acceptableMembers.Add(member);
//					if (member.IsShadowing)
//						break;
//				}
//				acceptableMembers.Add(neededMember);
//			} else {
//				acceptableMembers = new List<IMember> { neededMember };
//			}
//		}
//
//		#region ITypeCriterion implementation
//		public bool SatisfiedBy (IType type)
//		{
//			if (type == null)
//				throw new ArgumentNullException("type");
//
//			var typeMembers = type.GetMembers();
//			return typeMembers.Any(member => HasCommonMemberDeclaration(acceptableMembers, member));
//		}
//		#endregion
//
//		static bool HasCommonMemberDeclaration(IEnumerable<IMember> acceptableMembers, IMember member)
//		{
//			var implementedInterfaceMembers = member.MemberDefinition.ImplementedInterfaceMembers;
//			if (implementedInterfaceMembers.Any()) {
//				return ContainsAny(acceptableMembers, implementedInterfaceMembers);
//			} else {
//				return acceptableMembers.Contains(member/*				.MemberDefinition*/);
//			}
//		}
//
//		static bool ContainsAny<T>(IEnumerable<T> collection, IEnumerable<T> items)
//		{
//			foreach (var item in items) {
//				if (collection.Contains(item))
//					return true;
//			}
//			return false;
//		}
//	}

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class ParameterCanBeDeclaredWithBaseTypeCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds {
			get {
				return ImmutableArray.Create (NRefactoryDiagnosticIDs.ParameterCanBeDeclaredWithBaseTypeAnalyzerID);
			}
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
			var diagnostic = diagnostics.First ();
			var node = root.FindNode(context.Span);
			//if (!node.IsKind(SyntaxKind.BaseList))
			//	continue;
			var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
			context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, diagnostic.GetMessage(), document.WithSyntaxRoot(newRoot)), diagnostic);
		}
	}
}