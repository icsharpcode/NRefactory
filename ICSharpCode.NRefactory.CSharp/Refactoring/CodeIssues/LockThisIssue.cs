// LockThisIssue.cs 
// 
// Author:
//      Luís Reis <luiscubal@gmail.com>
// 
// Copyright (c) 2013 Luís Reis <luiscubal@gmail.com>
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
using ICSharpCode.NRefactory.CSharp.Analysis;
using ICSharpCode.NRefactory.Refactoring;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("Use of lock (this) is discouraged",
	                  Description = "Warns about using lock (this).",
	                  Category = IssueCategories.CodeQualityIssues,
	                  Severity = Severity.Warning)]
	public class LockThisIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues (BaseRefactoringContext context)
		{
			return new GatherVisitor(context).GetIssues();
		}

		class GatherVisitor : GatherVisitorBase<LockThisIssue>
		{
			public GatherVisitor (BaseRefactoringContext context) : base (context)
			{
			}

			public override void VisitAttribute(Attribute attribute)
			{
				base.VisitAttribute(attribute);

				if (IsMethodSynchronizedAttribute(attribute)) {
					var fixAction = new CodeAction(ctx.TranslateString("Create private locker field"), script => {
						var containerEntity = attribute.GetParent<EntityDeclaration>();
						if (containerEntity is Accessor) {
							containerEntity = attribute.GetParent<EntityDeclaration>();
						}
						var containerType = containerEntity.GetParent<TypeDeclaration>();

						FixLockThisIssue(script, containerEntity, containerType);
					}, attribute);

					AddIssue(attribute, ctx.TranslateString("Found [MethodImpl(MethodImplOptions.Synchronized)]"), fixAction);
				}
			}

			public override void VisitLockStatement(LockStatement lockStatement)
			{
				base.VisitLockStatement(lockStatement);

				var expression = lockStatement.Expression;

				if (IsThisReference(expression)) {
					var fixAction = new CodeAction(ctx.TranslateString("Create private locker field"), script => {
						var containerEntity = lockStatement.GetParent<EntityDeclaration>();
						if (containerEntity is Accessor) {
							containerEntity = containerEntity.GetParent<EntityDeclaration>();
						}
						var containerType = containerEntity.GetParent<TypeDeclaration>();

						FixLockThisIssue(script, containerEntity, containerType);

					}, lockStatement);

					AddIssue(lockStatement, ctx.TranslateString("Found lock (this)"), fixAction);
				}
			}

			void FixLockThisIssue(Script script, AstNode containerEntity, TypeDeclaration containerType)
			{
				var objectType = new PrimitiveType("object");

				var lockerFieldDeclaration = new FieldDeclaration() {
					ReturnType = objectType.Clone()
				};

				var lockerVariable = new VariableInitializer("locker", new ObjectCreateExpression(objectType.Clone()));
				lockerFieldDeclaration.Variables.Add(lockerVariable);
				script.InsertBefore(containerEntity, lockerFieldDeclaration);

				var synchronizedMethods = FixMethodsWithMethodImplAttribute(script, containerType);
				FixLocks(script, containerType, lockerVariable, synchronizedMethods);
			}

			IEnumerable<Statement> FixMethodsWithMethodImplAttribute(Script script, TypeDeclaration containerType)
			{
				var bodies = new List<Statement>();

				foreach (var entityDeclarationToModify in EntitiesInType(containerType)) {
					var methodDeclaration = entityDeclarationToModify as MethodDeclaration;
					var accessor = entityDeclarationToModify as Accessor;
					if (methodDeclaration == null && accessor == null) {
						continue;
					}

					if (entityDeclarationToModify.Modifiers.HasFlag(Modifiers.Abstract)) {
						continue;
					}

					var attributes = entityDeclarationToModify.Attributes.SelectMany(attributeSection => attributeSection.Attributes);
					var methodSynchronizedAttribute = attributes.FirstOrDefault(IsMethodSynchronizedAttribute);
					if (methodSynchronizedAttribute != null) {
						short methodImplValue = GetMethodImplValue(methodSynchronizedAttribute);
						if (methodImplValue != 0) {
							short newValue = (short)(methodImplValue & ~((short)MethodImplOptions.Synchronized));
							InsertNewAttribute(script, methodSynchronizedAttribute, newValue);
						} else {
							var section = methodSynchronizedAttribute.GetParent<AttributeSection>();
							if (section.Attributes.Count == 1) {
								script.Remove(section);
							} else {
								script.Remove(methodSynchronizedAttribute);
							}
						}
					
						var body = methodDeclaration == null ? accessor.Body : methodDeclaration.Body;

						bodies.Add(body);
					}
				}

				return bodies;
			}

			void InsertNewAttribute(Script script, Attribute attribute, short newValue) {
				var options = (MethodImplOptions[]) Enum.GetValues(typeof(MethodImplOptions));

				var astBuilder = ctx.CreateTypeSytemAstBuilder(attribute);
				var methodImplOptionsType = astBuilder.ConvertType(new FullTypeName(typeof(MethodImplOptions).FullName));

				Expression expression = CreateMethodImplReferenceNode(options[0], methodImplOptionsType);
				for (int optionIndex = 1; optionIndex < options.Length; ++optionIndex) {
					expression = new BinaryOperatorExpression(expression,
					                                          BinaryOperatorType.BitwiseOr,
					                                          CreateMethodImplReferenceNode(options [optionIndex], methodImplOptionsType));
				}

				var newAttribute = new Attribute();
				newAttribute.Type = attribute.Type.Clone();
				newAttribute.Arguments.Add(expression);

				script.Replace(attribute, newAttribute);
			}

			static MemberReferenceExpression CreateMethodImplReferenceNode(MethodImplOptions option, AstType methodImplOptionsType)
			{
				return new MemberReferenceExpression(new TypeReferenceExpression(methodImplOptionsType.Clone()), Enum.GetName(typeof(MethodImplOptions), option));
			}

			bool IsMethodSynchronizedAttribute(Attribute attribute)
			{
				var unresolvedType = attribute.Type;
				var resolvedType = ctx.ResolveType(unresolvedType);

				if (resolvedType.FullName != typeof(MethodImplAttribute).FullName) {
					return false;
				}

				short methodImpl = GetMethodImplValue(attribute);

				return (methodImpl & (short) MethodImplOptions.Synchronized) != 0;
			}

			short GetMethodImplValue(Attribute attribute)
			{
				short methodImpl = 0;
				foreach (var argument in attribute.Arguments) {
					var namedExpression = argument as NamedExpression;

					if (namedExpression == null) {
						short? implValue = GetMethodImplOptionsAsShort(argument);

						if (implValue != null) {
							methodImpl = (short)implValue;
						}

					} else if (namedExpression.Name == "Value") {
						short? implValue = GetMethodImplOptionsAsShort(namedExpression.Expression);

						if (implValue != null) {
							methodImpl = (short)implValue;
						}
					}
				}

				return methodImpl;
			}

			short? GetMethodImplOptionsAsShort(Expression argument)
			{
				//Returns null if the value could not be guessed

				var result = ctx.Resolve(argument);
				if (!result.IsCompileTimeConstant) {
					return null;
				}

				if (result.Type.FullName == typeof(MethodImplOptions).FullName) {
					return (short)(MethodImplOptions)result.ConstantValue;
				}

				return null;
			}

			Task FixLocks(Script script, TypeDeclaration containerType, VariableInitializer lockerVariable, IEnumerable<Statement> synchronizedStatements)
			{
				List<AstNode> linkNodes = new List<AstNode>();
				linkNodes.Add(lockerVariable.NameToken);

				foreach (var lockToModify in LocksInType(containerType)) {
					if (IsThisReference (lockToModify.Expression)) {
						var identifier = new IdentifierExpression ("locker");
						script.Replace(lockToModify.Expression, identifier);

						linkNodes.Add(identifier);
					}
				}

				foreach (var synchronizedStatement in synchronizedStatements) {
					var lockStatement = new LockStatement();
					var identifier = new IdentifierExpression ("locker");
					lockStatement.Expression = identifier;
					lockStatement.EmbeddedStatement = synchronizedStatement.Clone();

					script.Replace(synchronizedStatement, lockStatement);

					linkNodes.Add(identifier);
				}

				return script.Link(linkNodes.ToArray());
			}

			static IEnumerable<EntityDeclaration> EntitiesInType(TypeDeclaration containerType)
			{
				return containerType.Descendants.OfType<EntityDeclaration>().Where(entityDeclaration => {
					var childContainerType = entityDeclaration.GetParent<TypeDeclaration>();

					return childContainerType == containerType;
				});
			}

			static IEnumerable<LockStatement> LocksInType(TypeDeclaration containerType)
			{
				return containerType.Descendants.OfType<LockStatement>().Where(lockStatement => {
					var childContainerType = lockStatement.GetParent<TypeDeclaration>();

					return childContainerType == containerType;
				});
			}

			static bool IsThisReference (Expression expression)
			{
				if (expression is ThisReferenceExpression) {
					return true;
				}

				var parenthesizedExpression = expression as ParenthesizedExpression;
				if (parenthesizedExpression != null) {
					return IsThisReference(parenthesizedExpression.Expression);
				}

				return false;
			}
		}
	}
}

