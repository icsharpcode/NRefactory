// 
// NullValueAnalysis.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Text;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.NRefactory.CSharp.Analysis
{
	/// <summary>
	/// Represents the null value status of a variable at a specific location.
	/// </summary>
	public enum NullValueStatus
	{
		/// <summary>
		/// The value of the variable is unknown, possibly due to limitations
		/// of the null value analysis.
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// The value of the variable is unknown and even assigning it to a
		/// value won't change its state, since it has been captured by a lambda
		/// that may change it at any time (potentially even from a different thread).
		/// Only going out of scope and creating a new variable may change the value
		/// of this variable.
		/// </summary>
		CapturedUnknown,
		/// <summary>
		/// This variable is potentially unassigned.
		/// </summary>
		Unassigned,
		/// <summary>
		/// The value of the variable is provably null.
		/// </summary>
		DefinitelyNull,
		/// <summary>
		/// The value of the variable might or might not be null
		/// </summary>
		PotentiallyNull,
		/// <summary>
		/// The value of the variable is provably not null
		/// </summary>
		DefinitelyNotNull,
		/// <summary>
		/// The position of this node is unreachable, therefore the value
		/// of the variable is not meaningful.
		/// </summary>
		UnreachablePosition,
		/// <summary>
		/// The analyser has encountered an error when attempting to find the value
		/// of this variable.
		/// </summary>
		Error
	}

	public static class NullValueStatusExtensions
	{
		public static bool IsDefiniteValue (this NullValueStatus self) {
			return self == NullValueStatus.DefinitelyNull || self == NullValueStatus.DefinitelyNotNull;
		}
	}

	public class NullValueAnalysis
	{
		sealed class VariableStatusInfo : IEquatable<VariableStatusInfo>, IEnumerable<KeyValuePair<string, NullValueStatus>>
		{
			Dictionary<string, NullValueStatus> VariableStatus = new Dictionary<string, NullValueStatus>();

			public NullValueStatus this[string name]
			{
				get {
					NullValueStatus status;
					if (VariableStatus.TryGetValue(name, out status)) {
						return status;
					}
					return NullValueStatus.Unassigned;
				}
				set {
					if (value == NullValueStatus.Unassigned) {
						VariableStatus.Remove(name);
					} else {
						VariableStatus [name] = value;
					}
				}
			}

			/// <summary>
			/// Modifies the variable state to consider a new incoming path
			/// </summary>
			/// <returns><c>true</c>, if the state has changed, <c>false</c> otherwise.</returns>
			/// <param name="edgeInfo">The variable state of the incoming path</param>
			public bool ReceiveIncoming(VariableStatusInfo incomingState)
			{
				bool changed = false;
				var listOfVariables = VariableStatus.Keys.Concat(incomingState.VariableStatus.Keys).ToList();
				foreach (string variable in listOfVariables)
				{
					var newValue = CombineStatus(this [variable], incomingState [variable]);
					if (this [variable] != newValue) {
						this [variable] = newValue;
						changed = true;
					}
				}

				return changed;
			}

			public static NullValueStatus CombineStatus(NullValueStatus oldValue, NullValueStatus incomingValue)
			{
				Debug.Assert(incomingValue != NullValueStatus.UnreachablePosition);

				if (oldValue == NullValueStatus.Error || incomingValue == NullValueStatus.Error)
					return NullValueStatus.Error;

				if (oldValue == NullValueStatus.UnreachablePosition ||
				    oldValue == NullValueStatus.Unassigned)
					return incomingValue;

				if (incomingValue == NullValueStatus.Unassigned) {
					return NullValueStatus.Unassigned;
				}

				if (oldValue == NullValueStatus.CapturedUnknown || incomingValue == NullValueStatus.CapturedUnknown) {
					//TODO: Check if this is right
					return NullValueStatus.CapturedUnknown;
				}

				if (oldValue == NullValueStatus.Unknown) {
					return NullValueStatus.Unknown;
				}

				if (oldValue == NullValueStatus.DefinitelyNull) {
					return incomingValue == NullValueStatus.DefinitelyNull ?
						NullValueStatus.DefinitelyNull : NullValueStatus.PotentiallyNull;
				}

				if (oldValue == NullValueStatus.DefinitelyNotNull) {
					if (incomingValue == NullValueStatus.Unknown)
						return NullValueStatus.Unknown;
					if (incomingValue == NullValueStatus.DefinitelyNotNull)
						return NullValueStatus.DefinitelyNotNull;
					return NullValueStatus.PotentiallyNull;
				}

				Debug.Assert(oldValue == NullValueStatus.PotentiallyNull);
				return NullValueStatus.PotentiallyNull;
			}

			public VariableStatusInfo Clone() {
				var clone = new VariableStatusInfo();
				foreach (var item in VariableStatus) {
					clone.VariableStatus.Add(item.Key, item.Value);
				}
				return clone;
			}

			public override bool Equals(object obj)
			{
				return Equals(obj as VariableStatusInfo);
			}

			public bool Equals(VariableStatusInfo obj)
			{
				if (obj == null) {
					return false;
				}

				if (VariableStatus.Count != obj.VariableStatus.Count)
					return false;

				return VariableStatus.All(item => item.Value == obj[item.Key]);
			}

			public override int GetHashCode()
			{
				//STUB
				return VariableStatus.Count.GetHashCode();
			}

			public static bool operator ==(VariableStatusInfo obj1, VariableStatusInfo obj2) {
				return object.ReferenceEquals(obj1, null) ?
					object.ReferenceEquals(obj2, null) : obj1.Equals(obj2);
			}

			public static bool operator !=(VariableStatusInfo obj1, VariableStatusInfo obj2) {
				return !(obj1 == obj2);
			}

			public IEnumerator<KeyValuePair<string, NullValueStatus>> GetEnumerator()
			{
				return VariableStatus.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			public override string ToString()
			{
				var builder = new StringBuilder("[");
				foreach (var item in this) {
					builder.Append(item.Key);
					builder.Append("=");
					builder.Append(item.Value);
				}
				builder.Append("]");
				return builder.ToString();
			}
		}

		sealed class NullAnalysisNode : ControlFlowNode
		{
			public readonly VariableStatusInfo VariableState = new VariableStatusInfo();
			public bool Visited { get; private set; }

			public NullAnalysisNode(Statement previousStatement, Statement nextStatement, ControlFlowNodeType type)
				: base(previousStatement, nextStatement, type)
			{
			}

			public bool ReceiveIncoming(VariableStatusInfo incomingState)
			{
				bool changed = VariableState.ReceiveIncoming(incomingState);
				if (!Visited) {
					Visited = true;
					return true;
				}
				return changed;
			}
		}

		sealed class NullAnalysisGraphBuilder : ControlFlowGraphBuilder
		{
			protected override ControlFlowNode CreateNode(Statement previousStatement, Statement nextStatement, ControlFlowNodeType type)
			{
				return new NullAnalysisNode(previousStatement, nextStatement, type);
			}
		}

		BaseRefactoringContext context;
		readonly NullAnalysisVisitor visitor;
		List<NullAnalysisNode> allNodes;
		HashSet<Tuple<NullAnalysisNode, VariableStatusInfo>> nodesToVisit = new HashSet<Tuple<NullAnalysisNode, VariableStatusInfo>>();
		Dictionary<Statement, NullAnalysisNode> nodeBeforeStatementDict;
		Dictionary<Statement, NullAnalysisNode> nodeAfterStatementDict;
		Dictionary<Expression, NullValueStatus> expressionResult = new Dictionary<Expression, NullValueStatus>();

		public NullValueAnalysis(BaseRefactoringContext context, MethodDeclaration methodDeclaration, CancellationToken cancellationToken)
			: this(context, methodDeclaration.Body, methodDeclaration.Parameters, cancellationToken)
		{
		}

		public NullValueAnalysis(BaseRefactoringContext context, Statement rootStatement, IEnumerable<ParameterDeclaration> parameters, CancellationToken cancellationToken)
		{
			if (rootStatement == null)
				throw new ArgumentNullException("rootStatement");
			if (context == null)
				throw new ArgumentNullException("resolver");

			this.context = context;
			this.visitor = new NullAnalysisVisitor(this);

			var cfgBuilder = new NullAnalysisGraphBuilder();
			allNodes = cfgBuilder.BuildControlFlowGraph(rootStatement, cancellationToken).Cast<NullAnalysisNode>().ToList();
			foreach (var node in allNodes) {
				if (node.Type == ControlFlowNodeType.StartNode && node.NextStatement == rootStatement) {
					Debug.Assert(!nodesToVisit.Any());
					
					SetupNode(node, parameters);
				}
			}

			VisitNodes();
			nodeBeforeStatementDict = allNodes.Where(node => node.Type == ControlFlowNodeType.StartNode || node.Type == ControlFlowNodeType.BetweenStatements)
				.ToDictionary(node => node.NextStatement);
			nodeAfterStatementDict = allNodes.Where(node => node.Type == ControlFlowNodeType.BetweenStatements || node.Type == ControlFlowNodeType.EndNode)
				.ToDictionary(node => node.PreviousStatement);
		}

		void SetupNode(NullAnalysisNode node, IEnumerable<ParameterDeclaration> parameters)
		{
			foreach (var parameter in parameters) {
				string name = parameter.Name;
				var resolveResult = context.Resolve(parameter.Type);
				node.VariableState [name] = GetInitialVariableStatus(resolveResult);
			}

			nodesToVisit.Add(Tuple.Create(node, node.VariableState));
		}

		static bool IsTypeNullable(IType type)
		{
			return type.IsReferenceType == true || type.FullName == "System.Nullable`1";
		}

		static NullValueStatus GetInitialVariableStatus(ResolveResult resolveResult)
		{
			var typeResolveResult = resolveResult as TypeResolveResult;
			if (typeResolveResult == null) {
				return NullValueStatus.Error;
			}
			var type = typeResolveResult.Type;
			if (type.IsReferenceType == null) {
				return NullValueStatus.Error;
			}
			return IsTypeNullable(type) ? NullValueStatus.PotentiallyNull : NullValueStatus.DefinitelyNotNull;
		}

		void VisitNodes()
		{
			while (nodesToVisit.Any()) {
				var tuple = nodesToVisit.First();
				nodesToVisit.Remove(tuple);

				Visit(tuple.Item1, tuple.Item2);
			}
		}

		void Visit(NullAnalysisNode node, VariableStatusInfo statusInfo)
		{
			var nextStatement = node.NextStatement;
			VariableStatusInfo outgoingStatusInfo = statusInfo;
			VisitorResult result = null;

			if (nextStatement != null) {
				result = nextStatement.AcceptVisitor(visitor, statusInfo);
				Debug.Assert(result != null);

				outgoingStatusInfo = result.Variables;
			}

			foreach (var outgoingEdge in node.Outgoing) {
				var edgeInfo = outgoingStatusInfo.Clone();
				if (result != null) {
					switch (outgoingEdge.Type) {
						case ControlFlowEdgeType.ConditionTrue:
							if (result.KnownBoolResult == false) {
								//No need to explore this path -- expression is known to be false
								continue;
							}
							edgeInfo = result.TruePathVariables;
							break;
						case ControlFlowEdgeType.ConditionFalse:
							if (result.KnownBoolResult == true) {
								//No need to explore this path -- expression is known to be true
								continue;
							}
							edgeInfo = result.FalsePathVariables;
							break;
					}
				}

				var outgoingNode = (NullAnalysisNode) outgoingEdge.To;
				if (outgoingNode.ReceiveIncoming(edgeInfo)) {
					nodesToVisit.Add(Tuple.Create(outgoingNode, edgeInfo));
				}
			}
		}

		public NullValueStatus GetExpressionResult(Expression expr)
		{
			if (expr == null)
				throw new ArgumentNullException("expr");

			NullValueStatus info;
			if (expressionResult.TryGetValue(expr, out info)) {
				return info;
			}

			return NullValueStatus.UnreachablePosition;
		}

		public NullValueStatus GetVariableStatusBeforeStatement(Statement stmt, string variableName)
		{
			if (stmt == null)
				throw new ArgumentNullException("stmt");
			if (variableName == null)
				throw new ArgumentNullException("variableName");

			NullAnalysisNode node;
			if (nodeBeforeStatementDict.TryGetValue(stmt, out node)) {
				return node.VariableState [variableName];
			}

			return NullValueStatus.UnreachablePosition;
		}

		public NullValueStatus GetVariableStatusAfterStatement(Statement stmt, string variableName)
		{
			if (stmt == null)
				throw new ArgumentNullException("stmt");
			if (variableName == null)
				throw new ArgumentNullException("variableName");

			NullAnalysisNode node;
			if (nodeAfterStatementDict.TryGetValue(stmt, out node)) {
				return node.VariableState [variableName];
			}

			return NullValueStatus.UnreachablePosition;
		}

		class ConditionalBranchInfo
		{
			/// <summary>
			/// True if the variable is null for the true path, false if it is false for the true path.
			/// </summary>
			public Dictionary<string, bool> TrueResultVariableNullStates = new Dictionary<string, bool>();
			/// <summary>
			/// True if the variable is null for the false path, false if it is false for the false path.
			/// </summary>
			public Dictionary<string, bool> FalseResultVariableNullStates = new Dictionary<string, bool>();
		}

		class VisitorResult
		{
			/// <summary>
			/// Indicates the return value of the expression.
			/// </summary>
			/// <remarks>
			/// Only applicable for expressions.
			/// </remarks>
			public NullValueStatus NullableReturnResult;

			/// <summary>
			/// Information that indicates the restrictions to add
			/// when branching.
			/// </summary>
			/// <remarks>
			/// Used in if/else statements, conditional expressions and
			/// while statements.
			/// </remarks>
			public ConditionalBranchInfo ConditionalBranchInfo;

			/// <summary>
			/// The state of the variables after the expression is executed.
			/// </summary>
			public VariableStatusInfo Variables;

			/// <summary>
			/// The known bool result of an expression.
			/// </summary>
			public bool? KnownBoolResult;

			public static VisitorResult ForValue(VariableStatusInfo oldResult, NullValueStatus newValue)
			{
				var clone = new VisitorResult();
				clone.NullableReturnResult = newValue;
				clone.Variables = oldResult.Clone();
				return clone;
			}

			public static VisitorResult ForBoolValue(VariableStatusInfo oldResult, bool newValue)
			{
				var clone = new VisitorResult();
				clone.NullableReturnResult = NullValueStatus.DefinitelyNotNull; //Bool expressions are never null
				clone.KnownBoolResult = newValue;
				clone.Variables = oldResult.Clone();
				return clone;
			}

			public VisitorResult Negated {
				get {
					var clone = new VisitorResult();
					if (NullableReturnResult.IsDefiniteValue()) {
						clone.NullableReturnResult = NullableReturnResult == NullValueStatus.DefinitelyNull
							? NullValueStatus.DefinitelyNotNull : NullValueStatus.DefinitelyNull;
					} else {
						clone.NullableReturnResult = NullableReturnResult;
					}
					clone.Variables = Variables.Clone();
					clone.KnownBoolResult = !KnownBoolResult;
					if (ConditionalBranchInfo != null) {
						clone.ConditionalBranchInfo = new ConditionalBranchInfo();
						foreach (var item in ConditionalBranchInfo.TrueResultVariableNullStates) {
							clone.ConditionalBranchInfo.FalseResultVariableNullStates [item.Key] = item.Value;
						}
						foreach (var item in ConditionalBranchInfo.FalseResultVariableNullStates) {
							clone.ConditionalBranchInfo.TrueResultVariableNullStates [item.Key] = item.Value;
						}
					}
					return clone;
				}
			}

			public VariableStatusInfo TruePathVariables {
				get {
					var variables = Variables.Clone();
					if (ConditionalBranchInfo != null) {
						foreach (var item in ConditionalBranchInfo.TrueResultVariableNullStates) {
							variables [item.Key] = item.Value ? NullValueStatus.DefinitelyNull : NullValueStatus.DefinitelyNotNull;
						}
					}
					return variables;
				}
			}

			public VariableStatusInfo FalsePathVariables {
				get {
					var variables = Variables.Clone();
					if (ConditionalBranchInfo != null) {
						foreach (var item in ConditionalBranchInfo.FalseResultVariableNullStates) {
							variables [item.Key] = item.Value ? NullValueStatus.DefinitelyNull : NullValueStatus.DefinitelyNotNull;
						}
					}
					return variables;
				}
			}

			public static VisitorResult AndOperation(VisitorResult tentativeLeftResult, VisitorResult tentativeRightResult)
			{
				var result = new VisitorResult();
				result.KnownBoolResult = tentativeLeftResult.KnownBoolResult & tentativeRightResult.KnownBoolResult;

				var trueTruePath = tentativeRightResult.TruePathVariables;
				var trueFalsePath = tentativeRightResult.FalsePathVariables;
				var falsePath = tentativeLeftResult.FalsePathVariables;

				var trueVariables = trueTruePath;

				var falseVariables = trueFalsePath.Clone();
				falseVariables.ReceiveIncoming(falsePath);
				result.Variables = trueVariables.Clone();
				result.Variables.ReceiveIncoming(falseVariables);

				result.ConditionalBranchInfo = new ConditionalBranchInfo();

				foreach (var variable in trueVariables) {
					if (!variable.Value.IsDefiniteValue())
						continue;

					string variableName = variable.Key;

					if (variable.Value != result.Variables[variableName]) {
						bool isNull = variable.Value == NullValueStatus.DefinitelyNull;
						result.ConditionalBranchInfo.TrueResultVariableNullStates.Add(variableName, isNull);
					}
				}

				foreach (var variable in falseVariables) {
					if (!variable.Value.IsDefiniteValue())
						continue;

					string variableName = variable.Key;

					if (variable.Value != result.Variables [variableName]) {
						bool isNull = variable.Value == NullValueStatus.DefinitelyNull;
						result.ConditionalBranchInfo.FalseResultVariableNullStates.Add(variableName, isNull);
					}
				}

				return result;
			}

			public static VisitorResult OrOperation(VisitorResult tentativeLeftResult, VisitorResult tentativeRightResult)
			{
				return VisitorResult.AndOperation(tentativeLeftResult.Negated, tentativeRightResult.Negated).Negated;
			}
		}

		class NullAnalysisVisitor : DepthFirstAstVisitor<VariableStatusInfo, VisitorResult>
		{
			static readonly Expression IdentifierPattern = PatternHelper.OptionalParentheses(
				new IdentifierExpression(Pattern.AnyString).WithName("identifier"));

			NullValueAnalysis analysis;

			public NullAnalysisVisitor(NullValueAnalysis analysis) {
				this.analysis = analysis;
			}

			public override VisitorResult VisitEmptyStatement(EmptyStatement emptyStatement, VariableStatusInfo data)
			{
				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitBlockStatement(BlockStatement blockStatement, VariableStatusInfo data)
			{
				//We'll visit the child statements later (we'll visit each one directly from the CFG)
				//As such this is mostly a dummy node.
				return new VisitorResult { Variables = data };
			}

			public override VisitorResult VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement, VariableStatusInfo data)
			{
				foreach (var variable in variableDeclarationStatement.Variables) {
					if (variable.Initializer.IsNull) {
						data = data.Clone();
						data [variable.Name] = NullValueStatus.Unassigned;
					} else {
						var result = variable.Initializer.AcceptVisitor(this, data);
						data = result.Variables.Clone();
						data [variable.Name] = result.NullableReturnResult;
					}
				}

				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitIfElseStatement(IfElseStatement ifElseStatement, VariableStatusInfo data)
			{
				//We'll visit the true/false statements later (directly from the CFG)
				return ifElseStatement.Condition.AcceptVisitor(this, data);
			}

			public override VisitorResult VisitWhileStatement(WhileStatement whileStatement, VariableStatusInfo data)
			{
				return whileStatement.Condition.AcceptVisitor(this, data);
			}

			public override VisitorResult VisitForStatement(ForStatement forStatement, VariableStatusInfo data)
			{
				//The initializers, the embedded statement and the iterators aren't visited here
				//because they have their own CFG nodes.
				return forStatement.Condition.AcceptVisitor(this, data);
			}

			public override VisitorResult VisitExpressionStatement(ExpressionStatement expressionStatement, VariableStatusInfo data)
			{
				return expressionStatement.Expression.AcceptVisitor(this, data);
			}

			public override VisitorResult VisitReturnStatement(ReturnStatement returnStatement, VariableStatusInfo data)
			{
				if (returnStatement.Expression.IsNull)
					return VisitorResult.ForValue(data, NullValueStatus.Unknown);
				return returnStatement.Expression.AcceptVisitor(this, data);
			}

			void RegisterExpressionResult(Expression expression, NullValueStatus expressionResult)
			{
				NullValueStatus oldStatus;
				if (analysis.expressionResult.TryGetValue(expression, out oldStatus)) {
					analysis.expressionResult[expression] = VariableStatusInfo.CombineStatus(analysis.expressionResult[expression], expressionResult);
				}
				else {
					analysis.expressionResult[expression] = expressionResult;
				}
			}

			VisitorResult HandleExpressionResult(Expression expression, VariableStatusInfo dataAfterExpression, NullValueStatus expressionResult) {
				RegisterExpressionResult(expression, expressionResult);

				return VisitorResult.ForValue(dataAfterExpression, expressionResult);
			}

			VisitorResult HandleExpressionResult(Expression expression, VariableStatusInfo dataAfterExpression, bool expressionResult) {
				RegisterExpressionResult(expression, NullValueStatus.DefinitelyNotNull);

				return VisitorResult.ForBoolValue(dataAfterExpression, expressionResult);
			}

			VisitorResult HandleExpressionResult(Expression expression, VisitorResult result) {
				RegisterExpressionResult(expression, result.NullableReturnResult);

				return result;
			}

			public override VisitorResult VisitAssignmentExpression(AssignmentExpression assignmentExpression, VariableStatusInfo data)
			{
				if (assignmentExpression.Operator != AssignmentOperatorType.Assign)
					throw new NotImplementedException();

				var tentativeResult = assignmentExpression.Left.AcceptVisitor(this, data);
				tentativeResult = assignmentExpression.Right.AcceptVisitor(this, tentativeResult.Variables);

				var leftIdentifier = assignmentExpression.Left as IdentifierExpression;
				if (leftIdentifier != null) {
					var resolveResult = analysis.context.Resolve(leftIdentifier);
					if (resolveResult.IsError) {
						return HandleExpressionResult(assignmentExpression, data, NullValueStatus.Error);
					}
					var local = resolveResult as LocalResolveResult;
					if (local != null) {
						var result = new VisitorResult();
						result.NullableReturnResult = tentativeResult.NullableReturnResult;
						result.Variables = tentativeResult.Variables.Clone();
						var oldValue = result.Variables [local.Variable.Name];
						if (oldValue != NullValueStatus.CapturedUnknown) {
							//Captured variables remain captured, and they may become null/not null
							//at any time in a different thread.
							result.Variables [local.Variable.Name] = tentativeResult.NullableReturnResult;
						}
						return HandleExpressionResult(assignmentExpression, result);
					}
				}

				return tentativeResult;
			}

			public override VisitorResult VisitIdentifierExpression(IdentifierExpression identifierExpression, VariableStatusInfo data)
			{
				var resolveResult = analysis.context.Resolve(identifierExpression);
				if (resolveResult.IsError) {
					return HandleExpressionResult(identifierExpression, data, NullValueStatus.Error);
				}
				var local = resolveResult as LocalResolveResult;
				if (local != null) {
					var value = data [local.Variable.Name];
					if (value == NullValueStatus.CapturedUnknown)
						value = NullValueStatus.Unknown;
					return HandleExpressionResult(identifierExpression, data, value);
				}
				if (resolveResult.IsCompileTimeConstant) {
					object value = resolveResult.ConstantValue;
					if (value == null) {
						return HandleExpressionResult(identifierExpression, data, NullValueStatus.DefinitelyNull);
					}
					var boolValue = value as bool?;
					if (boolValue != null) {
						return VisitorResult.ForBoolValue(data, (bool)boolValue);
					}
					return HandleExpressionResult(identifierExpression, data, NullValueStatus.DefinitelyNotNull);
				}
				return HandleExpressionResult(identifierExpression, data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression, VariableStatusInfo data)
			{
				return HandleExpressionResult(nullReferenceExpression, data, NullValueStatus.DefinitelyNull);
			}

			public override VisitorResult VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, VariableStatusInfo data)
			{
				return HandleExpressionResult(primitiveExpression, data, NullValueStatus.DefinitelyNotNull);
			}

			public override VisitorResult VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, VariableStatusInfo data)
			{
				return HandleExpressionResult(parenthesizedExpression, parenthesizedExpression.Expression.AcceptVisitor(this, data));
			}

			public override VisitorResult VisitConditionalExpression(ConditionalExpression conditionalExpression, VariableStatusInfo data)
			{
				var tentativeBaseResult = conditionalExpression.Condition.AcceptVisitor(this, data);

				if (tentativeBaseResult.KnownBoolResult == true) {
					return HandleExpressionResult(conditionalExpression, conditionalExpression.TrueExpression.AcceptVisitor(this, tentativeBaseResult.TruePathVariables));
				}
				if (tentativeBaseResult.KnownBoolResult == false) {
					return HandleExpressionResult(conditionalExpression, conditionalExpression.FalseExpression.AcceptVisitor(this, tentativeBaseResult.FalsePathVariables));
				}

				//No known bool result
				var trueCase = conditionalExpression.TrueExpression.AcceptVisitor(this, tentativeBaseResult.TruePathVariables);
				var falseCase = conditionalExpression.FalseExpression.AcceptVisitor(this, tentativeBaseResult.FalsePathVariables);

				return HandleExpressionResult(conditionalExpression, VisitorResult.OrOperation(trueCase, falseCase));
			}

			public override VisitorResult VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, VariableStatusInfo data)
			{
				//Let's not evaluate the sides just yet because of ??, && and ||

				//We'll register the results here (with HandleExpressionResult)
				//so each Visit*Expression won't have to do it itself
				switch (binaryOperatorExpression.Operator) {
					case BinaryOperatorType.ConditionalAnd:
						return HandleExpressionResult(binaryOperatorExpression, VisitConditionalAndExpression(binaryOperatorExpression, data));
					case BinaryOperatorType.ConditionalOr:
						return HandleExpressionResult(binaryOperatorExpression, VisitConditionalOrExpression(binaryOperatorExpression, data));
					case BinaryOperatorType.NullCoalescing:
						return HandleExpressionResult(binaryOperatorExpression, VisitNullCoalescing(binaryOperatorExpression, data));
					case BinaryOperatorType.Equality:
						return HandleExpressionResult(binaryOperatorExpression, VisitEquality(binaryOperatorExpression, data));
					case BinaryOperatorType.InEquality:
						return HandleExpressionResult(binaryOperatorExpression, VisitEquality(binaryOperatorExpression, data).Negated);
					default:
						return HandleExpressionResult(binaryOperatorExpression, VisitOtherBinaryExpression(binaryOperatorExpression, data));
				}
			}

			VisitorResult VisitOtherBinaryExpression(BinaryOperatorExpression binaryOperatorExpression, VariableStatusInfo data)
			{
				var leftTentativeResult = binaryOperatorExpression.Left.AcceptVisitor(this, data);
				var rightTentativeResult = binaryOperatorExpression.Right.AcceptVisitor(this, leftTentativeResult.Variables);

				//TODO: Assuming operators are not overloaded by users
				// (or, if they are, that they retain similar behavior to the default ones)

				switch (binaryOperatorExpression.Operator) {
					case BinaryOperatorType.LessThan:
					case BinaryOperatorType.GreaterThan:
						//Operations < and > with nulls always return false
						//Those same operations will other values may or may not return false
						if (leftTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNull &&
							rightTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNull) {
							return VisitorResult.ForBoolValue(rightTentativeResult.Variables, false);
						}
						//We don't know what the value is, but we know that both true and false are != null.
						return VisitorResult.ForValue(rightTentativeResult.Variables, NullValueStatus.DefinitelyNotNull);
					case BinaryOperatorType.LessThanOrEqual:
					case BinaryOperatorType.GreaterThanOrEqual:
						if (leftTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNull) {
							if (rightTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNull)
								return VisitorResult.ForBoolValue(rightTentativeResult.Variables, true);
							if (rightTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNotNull)
								return VisitorResult.ForBoolValue(rightTentativeResult.Variables, false);
						} else if (leftTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNotNull) {
							if (rightTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNull)
								return VisitorResult.ForBoolValue(rightTentativeResult.Variables, false);
						}

						return VisitorResult.ForValue(rightTentativeResult.Variables, NullValueStatus.Unknown);
					default:
						//Anything else: null + anything == anything + null == null.
						//not null + not null = not null
						if (leftTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNull) {
							return VisitorResult.ForValue(rightTentativeResult.Variables, NullValueStatus.DefinitelyNull);
						}
						if (leftTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNotNull) {
							if (rightTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNull)
								return VisitorResult.ForValue(rightTentativeResult.Variables, NullValueStatus.DefinitelyNull);
							if (rightTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNotNull)
								return VisitorResult.ForValue(rightTentativeResult.Variables, NullValueStatus.DefinitelyNotNull);
						}

						return VisitorResult.ForValue(rightTentativeResult.Variables, NullValueStatus.Unknown);
				}
			}

			VisitorResult VisitEquality(BinaryOperatorExpression binaryOperatorExpression, VariableStatusInfo data)
			{
				//TODO: Should this check for user operators?

				var tentativeLeftResult = binaryOperatorExpression.Left.AcceptVisitor(this, data);
				var tentativeRightResult = binaryOperatorExpression.Right.AcceptVisitor(this, tentativeLeftResult.Variables);

				if (tentativeLeftResult.KnownBoolResult != null && tentativeLeftResult.KnownBoolResult == tentativeRightResult.KnownBoolResult) {
					return VisitorResult.ForBoolValue(tentativeRightResult.Variables, true);
				}

				if (tentativeLeftResult.KnownBoolResult != null && tentativeLeftResult.KnownBoolResult == !tentativeRightResult.KnownBoolResult) {
					return VisitorResult.ForBoolValue(tentativeRightResult.Variables, false);
				}

				if (tentativeLeftResult.NullableReturnResult.IsDefiniteValue()) {
					if (tentativeRightResult.NullableReturnResult.IsDefiniteValue()) {
						return VisitorResult.ForBoolValue(tentativeRightResult.Variables, tentativeLeftResult.NullableReturnResult == tentativeRightResult.NullableReturnResult);
					}
				}

				var result = new VisitorResult();
				result.Variables = tentativeRightResult.Variables;
				result.NullableReturnResult = NullValueStatus.Unknown;
				result.ConditionalBranchInfo = new ConditionalBranchInfo();

				if (tentativeRightResult.NullableReturnResult.IsDefiniteValue()) {
					var match = IdentifierPattern.Match(binaryOperatorExpression.Left);

					if (match.Success) {
						var identifier = match.Get<IdentifierExpression>("identifier").Single();
						var localVariableResult = analysis.context.Resolve(identifier) as LocalResolveResult;
						if (localVariableResult != null) {
							bool isNull = (tentativeRightResult.NullableReturnResult == NullValueStatus.DefinitelyNull);
							result.ConditionalBranchInfo.TrueResultVariableNullStates [identifier.Identifier] = isNull;
							result.ConditionalBranchInfo.FalseResultVariableNullStates [identifier.Identifier] = !isNull;
						}
					}
				}

				if (tentativeRightResult.NullableReturnResult.IsDefiniteValue()) {
					var match = IdentifierPattern.Match(binaryOperatorExpression.Right);

					if (match.Success) {
						var identifier = match.Get<IdentifierExpression>("identifier").Single();
						var localVariableResult = analysis.context.Resolve(identifier) as LocalResolveResult;
						if (localVariableResult != null) {
							bool isNull = (tentativeLeftResult.NullableReturnResult == NullValueStatus.DefinitelyNull);
							result.ConditionalBranchInfo.TrueResultVariableNullStates [identifier.Identifier] = isNull;
							result.ConditionalBranchInfo.FalseResultVariableNullStates [identifier.Identifier] = !isNull;
						}
					}
				}

				return result;
			}

			VisitorResult VisitConditionalAndExpression(BinaryOperatorExpression binaryOperatorExpression, VariableStatusInfo data)
			{
				var tentativeLeftResult = binaryOperatorExpression.Left.AcceptVisitor(this, data);
				if (tentativeLeftResult.KnownBoolResult == false) {
					return tentativeLeftResult;
				}

				var truePath = tentativeLeftResult.TruePathVariables;
				var tentativeRightResult = binaryOperatorExpression.Right.AcceptVisitor(this, truePath);

				return VisitorResult.AndOperation(tentativeLeftResult, tentativeRightResult);
			}

			VisitorResult VisitConditionalOrExpression(BinaryOperatorExpression binaryOperatorExpression, VariableStatusInfo data)
			{
				var tentativeLeftResult = binaryOperatorExpression.Left.AcceptVisitor(this, data);
				if (tentativeLeftResult.KnownBoolResult == true) {
					return tentativeLeftResult;
				}

				var falsePath = tentativeLeftResult.FalsePathVariables;
				var tentativeRightResult = binaryOperatorExpression.Right.AcceptVisitor(this, falsePath);

				return VisitorResult.OrOperation(tentativeLeftResult, tentativeRightResult);
			}

			VisitorResult VisitNullCoalescing(BinaryOperatorExpression binaryOperatorExpression, VariableStatusInfo data)
			{
				var leftTentativeResult = binaryOperatorExpression.Left.AcceptVisitor(this, data);
				if (leftTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNotNull) {
					return leftTentativeResult;
				}

				//If the right side is found, then the left side is known to be null
				var newData = leftTentativeResult.Variables;
				var leftIdentifier = binaryOperatorExpression.Left as IdentifierExpression;
				if (leftIdentifier != null) {
					var resolveResult = analysis.context.Resolve(leftIdentifier);
					if (resolveResult.IsError) {
						return VisitorResult.ForValue(data, NullValueStatus.Error);
					}
					var localResolveResult = resolveResult as LocalResolveResult;
					if (localResolveResult != null) {
						string name = localResolveResult.Variable.Name;
						if (newData [name] != NullValueStatus.CapturedUnknown) {
							newData = newData.Clone();
							newData [name] = NullValueStatus.DefinitelyNotNull;
						}
					}
				}

				var rightTentativeResult = binaryOperatorExpression.Right.AcceptVisitor(this, newData);

				var mergedVariables = rightTentativeResult.Variables;
				var nullValue = rightTentativeResult.NullableReturnResult;

				if (leftTentativeResult.NullableReturnResult != NullValueStatus.DefinitelyNull) {
					mergedVariables = mergedVariables.Clone();
					mergedVariables.ReceiveIncoming(leftTentativeResult.Variables);
					if (nullValue == NullValueStatus.DefinitelyNull) {
						nullValue = NullValueStatus.PotentiallyNull;
					}
				}

				return VisitorResult.ForValue(mergedVariables, nullValue);
			}

			public override VisitorResult VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, VariableStatusInfo data)
			{
				//TODO: Again, what to do when overloaded operators are found?

				var tentativeResult = unaryOperatorExpression.Expression.AcceptVisitor(this, data);

				if (unaryOperatorExpression.Operator == UnaryOperatorType.Not) {
					return HandleExpressionResult(unaryOperatorExpression, tentativeResult.Negated);
				}
				return HandleExpressionResult(unaryOperatorExpression, tentativeResult);
			}

			public override VisitorResult VisitInvocationExpression(InvocationExpression invocationExpression, VariableStatusInfo data)
			{
				//TODO: Handle some common methods such as string.IsNullOrEmpty

				data = invocationExpression.Target.AcceptVisitor(this, data).Variables;

				foreach (var argument in invocationExpression.Arguments) {
					var directionExpression = argument as DirectionExpression;
					if (directionExpression != null) {
						var identifier = directionExpression.Expression as IdentifierExpression;
						if (identifier != null && data [identifier.Identifier] != NullValueStatus.CapturedUnknown) {
							//TODO: Check for scope and nullable types
							//out and ref parameters do *NOT* capture the variable (since they must stop changing it by the time they return)
							data = data.Clone();
							data [identifier.Identifier] = NullValueStatus.Unknown;
						}
						continue;
					}
					data = argument.AcceptVisitor(this, data).Variables;
				}

				//TODO: Some functions return non-nullable types
				return HandleExpressionResult(invocationExpression, data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, VariableStatusInfo data)
			{
				var targetResult = memberReferenceExpression.Target.AcceptVisitor(this, data);
				//TODO: If the target is an identifier, then that might mean that variable is not nullable
				// however, for that we must first check if the member is an extension method (which tend to violate a couple rules)
				//TODO: Check if type is not nullable
				return HandleExpressionResult(memberReferenceExpression, targetResult.Variables, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, VariableStatusInfo data)
			{
				foreach (var argument in objectCreateExpression.Arguments) {
					var directionExpression = argument as DirectionExpression;
					if (directionExpression != null) {
						var identifier = directionExpression.Expression as IdentifierExpression;
						if (identifier != null && data [identifier.Identifier] != NullValueStatus.CapturedUnknown) {
							//TODO: Check for scope and nullable types
							//out and ref parameters do *NOT* capture the variable (since they must stop changing it by the time they return)
							data = data.Clone();
							data [identifier.Identifier] = NullValueStatus.Unknown;
						}
						continue;
					}
					data = argument.AcceptVisitor(this, data).Variables;
				}

				//Constructors never return null
				return HandleExpressionResult(objectCreateExpression, data, NullValueStatus.DefinitelyNotNull);
			}

			public override VisitorResult VisitLambdaExpression(LambdaExpression lambdaExpression, VariableStatusInfo data)
			{
				var newData = data.Clone();

				var identifiers = lambdaExpression.Descendants.OfType<IdentifierExpression>();
				foreach (var identifier in identifiers) {
					//Check if it is in a "change-null-state" context
					//For instance, x++ does not change the null state
					//but `x = y` does.
					if (identifier.Role == AssignmentExpression.LeftRole) {
						var parent = (AssignmentExpression)identifier.Parent;
						if (parent.Operator != AssignmentOperatorType.Assign) {
							continue;
						}
					} else {
						//No other context matters
						//Captured variables are never passed by reference (out/ref)
						continue;
					}

					//At this point, we know there's a good chance the variable has been changed
					//TODO: Do we need to check if the type is nullable
					//TODO: Do we need to check if the variable is in a relevant scope?

					newData [identifier.Identifier] = NullValueStatus.CapturedUnknown;
				}

				//The lambda itself is known not to be null
				return HandleExpressionResult(lambdaExpression, newData, NullValueStatus.DefinitelyNotNull);
			}
		}
	}
}

