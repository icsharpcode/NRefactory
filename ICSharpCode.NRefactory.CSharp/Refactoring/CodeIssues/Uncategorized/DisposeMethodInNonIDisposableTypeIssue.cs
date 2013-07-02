// DisposeMethodInNonIDisposableTypeIssue.cs
//
// Author:
//       Luís Reis <luiscubal@gmail.com>
//
// Copyright (c) 2013 Luís Reis
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
using System.Linq;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("Type does not implement IDisposable despite having a Dispose method",
	                  Description="This type declares a method named Dispose, but it does not implement the System.IDisposable interface",
	                  Category=IssueCategories.CodeQualityIssues,
	                  Severity=Severity.Warning,
	                  IssueMarker=IssueMarker.Underline)]
	public class DisposeMethodInNonIDisposableTypeIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			var visitor = new GatherVisitor(context);
			return visitor.GetIssues();
		}

		private class GatherVisitor : GatherVisitorBase<DisposeMethodInNonIDisposableTypeIssue>
		{
			public GatherVisitor(BaseRefactoringContext context)
				: base(context)
			{
			}

			static bool IsDisposeMethod(MethodDeclaration methodDeclaration)
			{
				if (!methodDeclaration.PrivateImplementationType.IsNull) {
					//Ignore explictly implemented methods
					return false;
				}
				if (methodDeclaration.Name != "Dispose") {
					return false;
				}
				if (methodDeclaration.Parameters.Count != 0) {
					return false;
				}

				if (methodDeclaration.HasModifier(Modifiers.Static)) {
					return false;
				}

				var primitiveType = methodDeclaration.ReturnType as PrimitiveType;
				if (primitiveType == null || primitiveType.KnownTypeCode != KnownTypeCode.Void) {
					return false;
				}

				return true;
			}

			public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
			{
				if (typeDeclaration.ClassType != ClassType.Class && typeDeclaration.ClassType != ClassType.Struct) {
					//Disabled for interfaces, because the method could be
					//explicitly implemented
					//Also, does not apply to enums because enums have no methods
					return;
				}

				var resolve = (TypeResolveResult)ctx.Resolve(typeDeclaration);
				if (Implements(resolve.Type, "System.IDisposable")) {
					return;
				}

				base.VisitTypeDeclaration(typeDeclaration);
			}

			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
			{
				if (!IsDisposeMethod(methodDeclaration)) {
					return;
				}

				var type = methodDeclaration.GetParent<TypeDeclaration>();
				if (type == null) {
					return;
				}

				AddIssue(methodDeclaration,
				         ctx.TranslateString("Type does not implement IDisposable despite having a Dispose method"),
				         script => Fix(script, methodDeclaration, type));
			}

			static IEnumerable<MethodDeclaration> DisposeMethods(TypeDeclaration newTypeDeclaration)
			{
				return newTypeDeclaration.Members
					.OfType<MethodDeclaration>()
						.Where(IsDisposeMethod);
			}

			void Fix(Script script, MethodDeclaration methodDeclaration, TypeDeclaration typeDeclaration)
			{
				var newTypeDeclaration = (TypeDeclaration) typeDeclaration.Clone();

				var resolver = ctx.GetResolverStateAfter(typeDeclaration.LBraceToken);

				var typeResolve = resolver.ResolveSimpleName("IDisposable", new List<IType>()) as TypeResolveResult;
				bool canShortenIDisposable = typeResolve != null && typeResolve.Type.FullName == "System.IDisposable";

				string interfaceName = (canShortenIDisposable ? string.Empty : "System.") + "IDisposable";

				newTypeDeclaration.BaseTypes.Add(new SimpleType(interfaceName));

				foreach (var method in DisposeMethods(newTypeDeclaration).ToList()) {
					method.Modifiers &= ~Modifiers.Private;
					method.Modifiers &= ~Modifiers.Protected;
					method.Modifiers &= ~Modifiers.Internal;
					method.Modifiers |= Modifiers.Public;
				}

				script.Replace(typeDeclaration, newTypeDeclaration);
			}

			static bool Implements(IType type, string fullName)
			{
				return type.GetAllBaseTypes ().Any (baseType => baseType.FullName == fullName);
			}

			//Ignore entities that are not methods -- don't visit children
			public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
			{
			}

			public override void VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration)
			{
			}

			public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
			{
			}

			public override void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
			{
			}

			public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
			{
			}

			public override void VisitEventDeclaration(EventDeclaration eventDeclaration)
			{
			}

			public override void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
			{
			}

			public override void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
			{
			}

			public override void VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration)
			{
			}
		}
	}
}

