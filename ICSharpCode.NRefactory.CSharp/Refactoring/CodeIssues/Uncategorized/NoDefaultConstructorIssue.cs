using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("CS1729: Base class does not contain a 0 argument constructor",
	                   Description = "CS1729: Base class does not contain a 0 argument constructor",
	                   Category = IssueCategories.CompilerErrors,
	                   Severity = Severity.Error,
	                   IssueMarker = IssueMarker.Underline)]
	public class NoDefaultConstructorIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context).GetIssues();
		}

		private class GatherVisitor : GatherVisitorBase<NoDefaultConstructorIssue>
		{
			IType currentType;
			IType baseType;
			
			public GatherVisitor(BaseRefactoringContext context)
				: base(context)
			{
			}

			public override void VisitTypeDeclaration(TypeDeclaration declaration)
			{
				IType outerType = currentType;
				IType outerBaseType = baseType;
				
				var result = ctx.Resolve(declaration) as TypeResolveResult;
				currentType = result != null ? result.Type : SpecialType.UnknownType;
				baseType = currentType.DirectBaseTypes.FirstOrDefault(t => t.Kind != TypeKind.Interface) ?? SpecialType.UnknownType;
				
				base.VisitTypeDeclaration(declaration);
				
				if (currentType.Kind == TypeKind.Class && currentType.GetConstructors().All(ctor => ctor.IsSynthetic)) {
					// current type only has the compiler-provided default ctor
					if (!BaseTypeHasUsableParameterlessConstructor()) {
						AddIssue(declaration.NameToken, GetIssueText(baseType));
					}
				}
				
				currentType = outerType;
				baseType = outerBaseType;
			}

			public override void VisitConstructorDeclaration(ConstructorDeclaration declaration)
			{
				if (declaration.HasModifier(Modifiers.Static))
					return;
				
				var initializer = declaration.Initializer;
				if (initializer.IsNull) {
					// Check if parameterless ctor is available:
					if (!BaseTypeHasUsableParameterlessConstructor()) {
						AddIssue(declaration.NameToken, GetIssueText(baseType));
					}
				} else {
					const OverloadResolutionErrors errorsIndicatingWrongNumberOfArguments =
						OverloadResolutionErrors.MissingArgumentForRequiredParameter
						| OverloadResolutionErrors.TooManyPositionalArguments
						| OverloadResolutionErrors.Inaccessible;
					
					// Check if existing initializer is valid:
					var rr = ctx.Resolve(initializer) as CSharpInvocationResolveResult;
					if (rr != null && (rr.OverloadResolutionErrors & errorsIndicatingWrongNumberOfArguments) != 0) {
						IType targetType = initializer.ConstructorInitializerType == ConstructorInitializerType.Base ? baseType : currentType;
						AddIssue(declaration.NameToken, GetIssueText(targetType, initializer.Arguments.Count));
					}
				}
			}

			bool BaseTypeHasUsableParameterlessConstructor()
			{
				var memberLookup = new MemberLookup(currentType.GetDefinition(), ctx.Compilation.MainAssembly);
				OverloadResolution or = new OverloadResolution(ctx.Compilation, new ResolveResult[0]);
				foreach (var ctor in baseType.GetConstructors()) {
					if (memberLookup.IsAccessible(ctor, allowProtectedAccess: true)) {
						if (or.AddCandidate(ctor) == OverloadResolutionErrors.None)
							return true;
					}
				}
				return false;
			}
			
			string GetIssueText(IType targetType, int argumentCount = 0)
			{
				return string.Format(ctx.TranslateString("CS1729: The type '{0}' does not contain a constructor that takes '{1}' arguments"), targetType.Name, argumentCount);
			}
		}
	}
}

