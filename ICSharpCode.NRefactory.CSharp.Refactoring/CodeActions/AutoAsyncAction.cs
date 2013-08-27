//
// AutoAsyncAction.cs
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
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Analysis;
using ICSharpCode.NRefactory.PatternMatching;

using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Semantics;


namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Convert to async function",
	               Description = "Converts classic code to ")]
	public class AutoAsyncAction : CodeActionProvider
	{
		static readonly ReturnStatement ReturnTaskCompletionSourcePattern = new ReturnStatement {
			Expression = new MemberReferenceExpression {
				Target = new IdentifierExpression(Pattern.AnyString).WithName("target"),
				MemberName = "Task"
			}
		};

		class OriginalNodeAnnotation
		{
			internal readonly AstNode sourceNode;

			internal OriginalNodeAnnotation(AstNode sourceNode) {
				this.sourceNode = sourceNode;
			}
		}

		void AddOriginalNodeAnnotations(AstNode currentFunction)
		{
			foreach (var nodeToAnnotate in currentFunction
			         .DescendantNodesAndSelf(MayHaveChildrenToAnnotate)
			         .Where(ShouldAnnotate)) {
				nodeToAnnotate.AddAnnotation(new OriginalNodeAnnotation(nodeToAnnotate));
			}
		}

		void RemoveOriginalNodeAnnotations(AstNode currentFunction)
		{
			foreach (var nodeToAnnotate in currentFunction
			         .DescendantNodesAndSelf(MayHaveChildrenToAnnotate)
			         .Where(ShouldAnnotate)) {
				nodeToAnnotate.RemoveAnnotations<OriginalNodeAnnotation>();
			}
		}

		bool MayHaveChildrenToAnnotate(AstNode node)
		{
			return node is Statement ||
				node is Expression ||
				node is MethodDeclaration;
		}

		bool ShouldAnnotate(AstNode node)
		{
			return node is InvocationExpression;
		}

		public override IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			if (!context.Supports(new Version(5, 0))) {
				//Old C# version -- async/await are not available
				yield break;
			}

			var currentFunction = GetCurrentFunctionNode(context);
			if (currentFunction == null) {
				yield break;
			}

			if (IsAsync(currentFunction))
				yield break;

			//Only suggest modifying functions that return void, Task or Task<T>.
			IType returnType = GetReturnType(context, currentFunction);
			if (returnType == null) {
				yield break;
			}

			bool isVoid = false;
			IType resultType = null;
			switch (returnType.FullName) {
				case "System.Void":
					isVoid = true;
					break;
				case "System.Threading.Tasks.Task":
					resultType = returnType.IsParameterized ? returnType.TypeArguments.FirstOrDefault() : null;
					break;
				default:
					yield break;
			}

			var functionBody = currentFunction.GetChildByRole(Roles.Body);
			var statements = GetStatements(functionBody).ToList();
			var returnStatements = statements.OfType<ReturnStatement>().ToList();

			string taskCompletionSourceIdentifier = null;

			if (isVoid) {
				if (returnStatements.Any())
					yield break;
			} else if (!isVoid) {
				if (returnStatements.Count() != 1)
					yield break;

				var returnStatement = returnStatements.Single();
				if (functionBody.Statements.Last() != returnStatement)
					yield break;

				var match = ReturnTaskCompletionSourcePattern.Match(returnStatement);
				if (!match.Success) {
					yield break;
				}
				var taskCompletionSource = match.Get<IdentifierExpression>("target").Single();
				var taskCompletionSourceResolveResult = context.Resolve(taskCompletionSource);

				//Make sure the TaskCompletionSource is a local variable
				if (!(taskCompletionSourceResolveResult is LocalResolveResult) ||
				    taskCompletionSourceResolveResult.Type.FullName != "System.Threading.Tasks.TaskCompletionSource") {

					yield break;
				}
				taskCompletionSourceIdentifier = taskCompletionSource.Identifier;

				//Make sure there are no unsupported uses of the task completion source
				foreach (var identifier in functionBody.Descendants.OfType<Identifier>()) {
					if (identifier.Name != taskCompletionSourceIdentifier)
						continue;

					var statement = identifier.GetParent<Statement>();
					var variableStatement = statement as VariableDeclarationStatement;
					if (variableStatement != null) {
						if (functionBody.Statements.First() != variableStatement || variableStatement.Variables.Count != 1) {
							//This may actually be valid, but it would add even more complexity to this action
							yield break;
						}
						continue;
					}

					if (statement == returnStatement)
						continue;

					if (identifier.Parent is MemberReferenceExpression) {
						//Right side of the member.
						//We don't care about this case since it's not a reference to the variable.
						continue;
					}

					var identifierExpressionParent = identifier.Parent as IdentifierExpression;
					if (identifierExpressionParent == null) {
						//Identifier is in an unsupported context
						yield break;
					}
					var memberReferenceExpression = identifierExpressionParent.Parent as MemberReferenceExpression;
					if (memberReferenceExpression == null) {
						//Identifier is in an unsupported context
						yield break;
					}

					if (memberReferenceExpression.MemberName != "SetResult") {
						//Aside from the final return statement, the only member of task completion source
						//that can be used is SetResult.
						//Perhaps future versions could also include SetException and SetCancelled.
						yield break;
					}
				}
			}

			yield return new CodeAction(context.TranslateString("Convert to async method"),
			                            script => {
				AddOriginalNodeAnnotations(currentFunction);
				var newFunction = currentFunction.Clone();
				RemoveOriginalNodeAnnotations(currentFunction);

				//Set async
				var lambda = newFunction as LambdaExpression;
				if (lambda != null)
					lambda.IsAsync = true;
				var anonymousMethod = newFunction as AnonymousMethodExpression;
				if (anonymousMethod != null)
					anonymousMethod.IsAsync = true;
				var methodDeclaration = newFunction as MethodDeclaration;
				if (methodDeclaration != null)
					methodDeclaration.Modifiers |= Modifiers.Async;

				TransformBody(context, isVoid, resultType, taskCompletionSourceIdentifier, newFunction.GetChildByRole(Roles.Body));

				script.Replace(currentFunction, newFunction);
			},
			                            GetFunctionToken(currentFunction));
		}

		void TransformBody(RefactoringContext context, bool isVoid, IType resultType, string taskCompletionSourceIdentifier, BlockStatement blockStatement)
		{
			if (!isVoid) {
				blockStatement.Statements.First().Remove(); //Remove task completion source declaration
				blockStatement.Statements.Last().Remove(); //Remove final return

				//We use ToList() because we will be modifying the original collection
				foreach (var expressionStatement in blockStatement.Descendants.OfType<ExpressionStatement>().ToList()) {
					var invocationExpression = expressionStatement.Expression as InvocationExpression;
					if (invocationExpression == null || invocationExpression.Arguments.Count != 1)
						continue;

					var target = invocationExpression.Target as MemberReferenceExpression;
					if (target == null || target.MemberName != "SetResult") {
						continue;
					}
					var targetExpression = target.Target as IdentifierExpression;
					if (targetExpression == null || targetExpression.Identifier != taskCompletionSourceIdentifier) {
						continue;
					}

					var returnedExpression = invocationExpression.Arguments.Single();
					returnedExpression.Remove();

					var originalInvocation = (InvocationExpression) invocationExpression.Annotation<OriginalNodeAnnotation>().sourceNode;
					var originalReturnedExpression = originalInvocation.Arguments.Single();
					var argumentType = context.Resolve(originalReturnedExpression).Type;

					if (resultType == null) {
						var parent = expressionStatement.Parent;
						var resultIdentifier = CreateVariableName("result");

						var blockParent = parent as BlockStatement;
						var resultDeclarationType = argumentType == SpecialType.NullType ? new PrimitiveType("object") : context.CreateShortType(argumentType);
						var declaration = new VariableDeclarationStatement(resultDeclarationType, resultIdentifier, returnedExpression);
						if (blockParent == null) {
							var newStatement = new BlockStatement();
							newStatement.Add(declaration);
							newStatement.Add(new ReturnStatement());
							expressionStatement.ReplaceWith(newStatement);
						} else {
							blockParent.Statements.InsertAfter(expressionStatement, new ReturnStatement());
							expressionStatement.ReplaceWith(declaration);
						}
					} else {
						var newStatement = new ReturnStatement(returnedExpression);
						expressionStatement.ReplaceWith(newStatement);
					}
				}
			}

			//Find all instances of ContinueWith to replace and associated 
			var continuations = new List<Tuple<InvocationExpression, List<string>>>();
			foreach (var invocation in blockStatement.DescendantNodes(node => true /* STUB: Recognize async lambdas */).OfType<InvocationExpression>()) {
				if (invocation.Arguments.Count != 1)
					continue;

				var originalInvocation = (InvocationExpression) invocation.Annotation<OriginalNodeAnnotation>().sourceNode;
				var resolveResult = context.Resolve(originalInvocation) as MemberResolveResult;
				if (resolveResult.Member.FullName != "System.Threading.Tasks.Task.ContinueWith")
					continue;

				var lambda = invocation.Arguments.Single();

				List<string> associatedTaskNames = new List<string>();

				var lambdaParameters = lambda.GetChildrenByRole(Roles.Parameter).Select(p => p.Name).ToList();
				var lambdaTaskParameterName = lambdaParameters.FirstOrDefault();
				if (lambdaTaskParameterName != null) {
					associatedTaskNames.Add(lambdaTaskParameterName);
				}

				var parentInitializer = originalInvocation.Parent as VariableInitializer;
				if (parentInitializer != null) {
					var parentDeclaration = parentInitializer.Parent as VariableDeclarationStatement;

					if (parentDeclaration != null &&
					    context.ResolveType(parentDeclaration.Type).FullName == "System.Threading.Tasks.Task") {
						associatedTaskNames.Add(parentInitializer.Name);
					}
				}

				continuations.Add(Tuple.Create(invocation, associatedTaskNames));
			}

			foreach (var continuationTuple in continuations) {
				string taskName = continuationTuple.Item2.FirstOrDefault() ?? "task";
				string resultName = taskName + "Result";
				var continuation = continuationTuple.Item1;

				var target = continuation.Target.GetChildByRole(Roles.TargetExpression).Detach();
				Expression awaitedExpression = new UnaryOperatorExpression(UnaryOperatorType.Await, target);
				Statement replacement;
				if (resultType == null) {
					replacement = new ExpressionStatement(awaitedExpression);
				} else {
					replacement = new VariableDeclarationStatement(context.CreateShortType(resultType), resultName, awaitedExpression);
				}

				var parentStatement = continuation.GetParent<Statement>();
				var grandParentStatement = parentStatement.Parent;
				BlockStatement block = grandParentStatement as BlockStatement;
				if (block == null) {
					block = new BlockStatement();
					block.Add(replacement);
					parentStatement.ReplaceWith(block);
				} else {
					parentStatement.ReplaceWith(replacement);
				}

				var lambdaOrDelegate = continuation.Arguments.Single();
				Statement lambdaContent;
				if (lambdaOrDelegate is LambdaExpression) {
					lambdaContent = (Statement)lambdaOrDelegate.GetChildByRole(LambdaExpression.BodyRole);
				} else {
					lambdaContent = lambdaOrDelegate.GetChildByRole(Roles.Body);
				}

				foreach (var memberReference in lambdaContent.Descendants.OfType<MemberReferenceExpression>()) {
					if (memberReference.MemberName != "Result") {
						continue;
					}

					var memberReferenceTarget = memberReference.Target as IdentifierExpression;
					if (memberReferenceTarget == null) {
						continue;
					}

					if (continuationTuple.Item2.Contains(memberReferenceTarget.Identifier)) {
						memberReference.ReplaceWith(new IdentifierExpression(resultName));
					}
				}

				if (lambdaContent is BlockStatement) {
					Statement previousStatement = replacement;
					foreach (var statementInContinuation in lambdaContent.GetChildrenByRole(BlockStatement.StatementRole)) {
						statementInContinuation.Detach();
						block.Statements.InsertAfter(previousStatement, statementInContinuation);
						previousStatement = statementInContinuation;
					}
				} else {
					lambdaContent.Detach();
					block.Statements.InsertAfter(replacement, lambdaContent);
				}
			}
		}
		
		string CreateVariableName(string proposedName) {
			//STUB
			return proposedName;
		}

		static IType GetReturnType(BaseRefactoringContext context, AstNode currentFunction)
		{
			var resolveResult = context.Resolve(currentFunction);
			return resolveResult.IsError ? null : resolveResult.Type;
		}

		static IEnumerable<Statement> GetStatements(Statement statement)
		{
			return statement.DescendantNodesAndSelf(stmt => stmt is Statement).OfType<Statement>();
		}

		static AstNode GetCurrentFunctionNode(RefactoringContext context)
		{
			//Note: Accessors, constructors and destructors are never asynchronous
			return context.GetNode(candidate => candidate is MethodDeclaration ||
			candidate is LambdaExpression ||
			candidate is AnonymousMethodExpression);
		}

		static AstNode GetFunctionToken(AstNode currentFunction)
		{
			return (AstNode)currentFunction.GetChildByRole(Roles.Identifier) ??
			currentFunction.GetChildByRole(LambdaExpression.ArrowRole) ??
			currentFunction.GetChildByRole(AnonymousMethodExpression.DelegateKeywordRole);
		}

		static bool IsAsync(AstNode currentFunction)
		{
			var method = currentFunction as MethodDeclaration;
			if (method != null) {
				return method.HasModifier(Modifiers.Async);
			}
			return !currentFunction.GetChildByRole(LambdaExpression.AsyncModifierRole).IsNull;
		}
	}
}

