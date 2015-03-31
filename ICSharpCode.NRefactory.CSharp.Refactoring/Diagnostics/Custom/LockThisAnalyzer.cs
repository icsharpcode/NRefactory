// LockThisAnalyzer.cs 
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
	public class LockThisAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "LockThisAnalyzer";
		const string Description            = "Warns about using lock (this) or MethodImplOptions.Synchronized.";
		const string MessageFormat          = "";
		const string Category               = DiagnosticAnalyzerCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Use of lock (this) or MethodImplOptions.Synchronized is discouraged");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<LockThisAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			public override void VisitAttribute(Attribute attribute)
//			{
//				base.VisitAttribute(attribute);
//
//				if (IsMethodSynchronizedAttribute(attribute)) {
//					var fixAction = new CodeAction(ctx.TranslateString("Create private locker field"), script => {
//						var containerEntity = GetParentMethodOrProperty(attribute);
//						var containerType = containerEntity.GetParent<TypeDeclaration>();
//
//						FixLockThisIssue(script, containerEntity, containerType);
//					}, attribute);
//
//					AddDiagnosticAnalyzer(new CodeIssue(attribute, ctx.TranslateString("Found [MethodImpl(MethodImplOptions.Synchronized)]"), fixAction));
//				}
//			}
//
//			static EntityDeclaration GetParentMethodOrProperty(AstNode node)
//			{
//				var containerEntity = node.GetParent<EntityDeclaration>();
//				if (containerEntity is Accessor) {
//					containerEntity = containerEntity.GetParent<EntityDeclaration>();
//				}
//
//				return containerEntity;
//			}
//
//			public override void VisitLockStatement(LockStatement lockStatement)
//			{
//				base.VisitLockStatement(lockStatement);
//
//				var expression = lockStatement.Expression;
//
//				if (IsThisReference(expression)) {
//					var fixAction = new CodeAction(ctx.TranslateString("Create private locker field"), script => {
//						var containerEntity = GetParentMethodOrProperty(lockStatement);
//
//						var containerType = containerEntity.GetParent<TypeDeclaration>();
//
//						FixLockThisIssue(script, containerEntity, containerType);
//
//					}, lockStatement);
//
//					AddDiagnosticAnalyzer(new CodeIssue(lockStatement.LockToken.StartLocation,
//						lockStatement.RParToken.EndLocation, ctx.TranslateString("Found lock (this)"), fixAction));
//				}
//			}
//
//			static bool IsEntityStatic(EntityDeclaration containerEntity)
//			{
//				return containerEntity.Modifiers.HasFlag(Modifiers.Static);
//			}
//
//			void FixLockThisIssue(Script script, EntityDeclaration containerEntity, TypeDeclaration containerType)
//			{
//				bool isStatic = IsEntityStatic(containerEntity);
//
//				List<BlockStatement> synchronizedStatements = FixMethodsWithMethodImplAttribute(script, containerType, isStatic).ToList();
//
//				List<AstNode> linkNodes = new List<AstNode>();
//
//				var locksToModify = LocksToModify(containerType, synchronizedStatements);
//				List<AstNode> nodeContexts = new List<AstNode>(locksToModify);
//
//				foreach (var synchronizedStatement in synchronizedStatements) {
//					if (synchronizedStatement.Statements.Count > 0) {
//						nodeContexts.Add(synchronizedStatement.Statements.First());
//
//						if (!isStatic) {
//							foreach (var childLock in synchronizedStatement.Descendants.OfType<LockStatement>()) {
//								if (IsThisReference(childLock.Expression)) {
//									nodeContexts.Add(childLock);
//								}
//							}
//						}
//					}
//				}
//
//				string proposedName = GetNameProposal(nodeContexts, "locker");
//
//				if (!isStatic) {
//					foreach (var lockToModify in locksToModify) {
//						var identifier = new IdentifierExpression (proposedName);
//						script.Replace(lockToModify.Expression, identifier);
//
//						linkNodes.Add(identifier);
//					}
//				}
//
//				foreach (var synchronizedStatement in synchronizedStatements) {
//					if (synchronizedStatement.Statements.Count > 0) {
//						var newBody = synchronizedStatement.Clone();
//
//						var outerLock = new LockStatement();
//						var outerLockIdentifier = new IdentifierExpression (proposedName);
//						outerLock.Expression = outerLockIdentifier;
//						outerLock.EmbeddedStatement = newBody;
//
//						linkNodes.Add(outerLockIdentifier);
//
//						if (!isStatic) {
//							foreach (var childLock in newBody.Descendants.OfType<LockStatement>()) {
//								if (IsThisReference(childLock.Expression)) {
//									var identifier = new IdentifierExpression (proposedName);
//									childLock.Expression.ReplaceWith(identifier);
//
//									linkNodes.Add(identifier);
//								}
//							}
//						}
//
//						script.InsertBefore(synchronizedStatement.Statements.First(), outerLock);
//
//						foreach (var stmt in synchronizedStatement.Statements) {
//							script.Remove(stmt);
//						}
//					}
//				}
//
//				if (linkNodes.Any()) {
//					var objectType = new PrimitiveType("object");
//
//					var lockerFieldDeclaration = new FieldDeclaration() {
//						Modifiers = isStatic ? Modifiers.Static : Modifiers.None,
//						ReturnType = objectType.Clone()
//					};
//
//					var lockerVariable = new VariableInitializer(proposedName, new ObjectCreateExpression(objectType.Clone()));
//					lockerFieldDeclaration.Variables.Add(lockerVariable);
//					script.InsertBefore(containerEntity, lockerFieldDeclaration);
//
//					linkNodes.Add(lockerVariable.NameToken);
//
//					script.Link(linkNodes.ToArray());
//				}
//			}
//
//			string GetNameProposal(List<AstNode> nodeContexts, string baseName)
//			{
//				var resolverStates = nodeContexts.Select(ctx.GetResolverStateBefore).ToList();
//				string nameProposal;
//				int n = 0;
//				do {
//					nameProposal = baseName + (n == 0 ? string.Empty : n.ToString());
//					n++;
//				} while (IdentifierNameExists(resolverStates, nameProposal));
//				return nameProposal;
//			}
//
//			static bool IdentifierNameExists(List<CSharpResolver> resolverStates, string nameProposal)
//			{
//				return resolverStates.Any(resolverState => {
//					ResolveResult result = resolverState.LookupSimpleNameOrTypeName(nameProposal, new List<IType>(), NameLookupMode.Expression);
//					return !result.IsError;
//				});
//			}
//
//			IEnumerable<LockStatement> LocksToModify(TypeDeclaration containerType, IEnumerable<BlockStatement> synchronizedStatements)
//			{
//				foreach (var lockToModify in LocksInType(containerType)) {
//					if (lockToModify.Ancestors.OfType<BlockStatement>()
//					    .Any(ancestor => synchronizedStatements.Contains(ancestor))) {
//
//						//These will be modified separately
//						continue;
//					}
//
//					if (!IsThisReference (lockToModify.Expression)) {
//						continue;
//					}
//
//					yield return lockToModify;
//				}
//			}
//
//			IEnumerable<BlockStatement> FixMethodsWithMethodImplAttribute(Script script, TypeDeclaration containerType, bool isStatic)
//			{
//				var bodies = new List<BlockStatement>();
//
//				foreach (var entityDeclarationToModify in EntitiesInType(containerType)) {
//					var methodDeclaration = entityDeclarationToModify as MethodDeclaration;
//					var accessor = entityDeclarationToModify as Accessor;
//					if (methodDeclaration == null && accessor == null) {
//						continue;
//					}
//
//					if ((methodDeclaration != null && IsEntityStatic(methodDeclaration) != isStatic) ||
//					    (accessor != null && IsEntityStatic(accessor.GetParent<EntityDeclaration>()) != isStatic)) {
//						//These will need a separate lock and therefore will not be changed.
//						continue;
//					}
//
//					var attributes = entityDeclarationToModify.Attributes.SelectMany(attributeSection => attributeSection.Attributes);
//					var methodSynchronizedAttribute = attributes.FirstOrDefault(IsMethodSynchronizedAttribute);
//					if (methodSynchronizedAttribute != null) {
//						short methodImplValue = GetMethodImplValue(methodSynchronizedAttribute);
//						short newValue = (short)(methodImplValue & ~((short)MethodImplOptions.Synchronized));
//						if (newValue != 0) {
//							InsertNewAttribute(script, methodSynchronizedAttribute, newValue);
//						} else {
//							var section = methodSynchronizedAttribute.GetParent<AttributeSection>();
//							if (section.Attributes.Count == 1) {
//								script.Remove(section);
//							} else {
//								script.Remove(methodSynchronizedAttribute);
//							}
//						}
//
//						bool isAbstract = entityDeclarationToModify.Modifiers.HasFlag(Modifiers.Abstract);
//						if (!isAbstract) {
//							var body = methodDeclaration == null ? accessor.Body : methodDeclaration.Body;
//							bodies.Add(body);
//						}
//					}
//				}
//
//				return bodies;
//			}
//
//			void InsertNewAttribute(Script script, Attribute attribute, short newValue) {
//				var availableValues = (MethodImplOptions[]) Enum.GetValues(typeof(MethodImplOptions));
//				var activeValues = availableValues.Where(value => (newValue & (short)value) != 0).ToList();
//
//				var astBuilder = ctx.CreateTypeSystemAstBuilder(attribute);
//				var methodImplOptionsType = astBuilder.ConvertType(new FullTypeName(typeof(MethodImplOptions).FullName));
//
//				Expression expression = CreateMethodImplReferenceNode(activeValues[0], methodImplOptionsType);
//				for (int optionIndex = 1; optionIndex < activeValues.Count; ++optionIndex) {
//					expression = new BinaryOperatorExpression(expression,
//					                                          BinaryOperatorType.BitwiseOr,
//					                                          CreateMethodImplReferenceNode(activeValues [optionIndex], methodImplOptionsType));
//				}
//
//				var newAttribute = new Attribute();
//				newAttribute.Type = attribute.Type.Clone();
//				newAttribute.Arguments.Add(expression);
//
//				script.Replace(attribute, newAttribute);
//			}
//
//			static MemberReferenceExpression CreateMethodImplReferenceNode(MethodImplOptions option, AstType methodImplOptionsType)
//			{
//				return methodImplOptionsType.Clone().Member(Enum.GetName(typeof(MethodImplOptions), option));
//			}
//
//			bool IsMethodSynchronizedAttribute(Attribute attribute)
//			{
//				var unresolvedType = attribute.Type;
//				var resolvedType = ctx.ResolveType(unresolvedType);
//
//				if (resolvedType.FullName != typeof(MethodImplAttribute).FullName) {
//					return false;
//				}
//
//				short methodImpl = GetMethodImplValue(attribute);
//
//				return (methodImpl & (short) MethodImplOptions.Synchronized) != 0;
//			}
//
//			short GetMethodImplValue(Attribute attribute)
//			{
//				short methodImpl = 0;
//				foreach (var argument in attribute.Arguments) {
//					var namedExpression = argument as NamedExpression;
//
//					if (namedExpression == null) {
//						short? implValue = GetMethodImplOptionsAsShort(argument);
//
//						if (implValue != null) {
//							methodImpl = (short)implValue;
//						}
//
//					} else if (namedExpression.Name == "Value") {
//						short? implValue = GetMethodImplOptionsAsShort(namedExpression.Expression);
//
//						if (implValue != null) {
//							methodImpl = (short)implValue;
//						}
//					}
//				}
//
//				return methodImpl;
//			}
//
//			short? GetMethodImplOptionsAsShort(AstNode argument)
//			{
//				//Returns null if the value could not be guessed
//
//				var result = ctx.Resolve(argument);
//				if (!result.IsCompileTimeConstant) {
//					return null;
//				}
//
//				if (result.Type.FullName == typeof(MethodImplOptions).FullName) {
//					return (short)(MethodImplOptions)result.ConstantValue;
//				}
//
//				return null;
//			}
//
//			static IEnumerable<EntityDeclaration> EntitiesInType(TypeDeclaration containerType)
//			{
//				return containerType.Descendants.OfType<EntityDeclaration>().Where(entityDeclaration => {
//					var childContainerType = entityDeclaration.GetParent<TypeDeclaration>();
//
//					return childContainerType == containerType;
//				});
//			}
//
//			static IEnumerable<LockStatement> LocksInType(TypeDeclaration containerType)
//			{
//				return containerType.Descendants.OfType<LockStatement>().Where(lockStatement => {
//					var childContainerType = lockStatement.GetParent<TypeDeclaration>();
//
//					return childContainerType == containerType;
//				});
//			}
//
//			static bool IsThisReference (Expression expression)
//			{
//				if (expression is ThisReferenceExpression) {
//					return true;
//				}
//
//				var parenthesizedExpression = expression as ParenthesizedExpression;
//				if (parenthesizedExpression != null) {
//					return IsThisReference(parenthesizedExpression.Expression);
//				}
//
//				return false;
//			}
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class LockThisFixProvider : NRefactoryCodeFixProvider
	{
		protected override IEnumerable<string> InternalGetFixableDiagnosticIds()
		{
			yield return LockThisAnalyzer.DiagnosticId;
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