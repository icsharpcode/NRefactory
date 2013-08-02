//
// CS1520MethodMustHaveAReturnType.cs
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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescriptionAttribute("Class, struct or interface method must have a return type",
	                           Description = "Found method without return type",
	                           Category = IssueCategories.CompilerErrors,
	                           Severity = Severity.Error,
	                           AcceptInvalidContexts = true)]
	public class CS1520MethodMustHaveAReturnType : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			foreach (var error in context.ErrorsAndWarnings) {
				if (error.ErrorType != ErrorType.Error)
					continue;
				if (error.ErrorCode != "CS1520")
					continue;

				var entity = context.RootNode.GetNodeAt<ConstructorDeclaration>(error.Region.Begin);
				var typeDeclaration = entity.GetParent<TypeDeclaration>();

				var fixActions = new List<CodeAction>();

				fixActions.Add(new CodeAction(context.TranslateString("Make this a void method"), script =>  {
					var generatedMethod = new MethodDeclaration();
					generatedMethod.Modifiers = entity.Modifiers;
					generatedMethod.ReturnType = new PrimitiveType("void");
					generatedMethod.Name = entity.Name;
					generatedMethod.Parameters.AddRange(entity.Parameters.Select(parameter => (ParameterDeclaration)parameter.Clone()));
					generatedMethod.Body = (BlockStatement) entity.Body.Clone();
					generatedMethod.Attributes.AddRange(entity.Attributes.Select(attribute => (AttributeSection) attribute.Clone()));

					script.Replace(entity, generatedMethod);
				}, entity));

				fixActions.Add(new CodeAction(context.TranslateString("Make this a constructor"), script =>  {
					var generatedConstructor = new ConstructorDeclaration();
					generatedConstructor.Modifiers = entity.Modifiers;
					generatedConstructor.Name = typeDeclaration.Name;
					generatedConstructor.Parameters.AddRange(entity.Parameters.Select(parameter => (ParameterDeclaration)parameter.Clone()));
					generatedConstructor.Body = (BlockStatement) entity.Body.Clone();
					generatedConstructor.Attributes.AddRange(entity.Attributes.Select(attribute => (AttributeSection) attribute.Clone()));

					script.Replace(entity, generatedConstructor);
				}, entity));

				yield return new CodeIssue(context.TranslateString("Class, struct or interface method must have a return type"), entity.NameToken.StartLocation, entity.NameToken.EndLocation, fixActions);
			}
		}
	}
}