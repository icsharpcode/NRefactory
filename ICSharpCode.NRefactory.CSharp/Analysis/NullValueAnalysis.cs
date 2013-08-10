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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;
using System.Security.Policy;
using System.Runtime.InteropServices;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Refactoring;

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

	public class NullValueAnalysis
	{
		sealed class VariableStatusInfo : IEquatable<VariableStatusInfo>
		{
			public Dictionary<string, NullValueStatus> VariableStatus = new Dictionary<string, NullValueStatus>();

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
				foreach (string variable in VariableStatus.Keys.Concat(incomingState.VariableStatus.Keys))
				{
					var newValue = CombineStatus(this [variable], incomingState [variable]);
					if (this [variable] != newValue) {
						this [variable] = newValue;
						changed = true;
					}
				}

				return changed;
			}

			static NullValueStatus CombineStatus(NullValueStatus oldValue, NullValueStatus incomingValue)
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

				if (oldValue == NullValueStatus.Unknown) {
					return incomingValue == NullValueStatus.Unknown ?
						NullValueStatus.Unknown : NullValueStatus.PotentiallyNull;
				}

				if (oldValue == NullValueStatus.DefinitelyNull) {
					return incomingValue == NullValueStatus.DefinitelyNull ?
						NullValueStatus.DefinitelyNull : NullValueStatus.PotentiallyNull;
				}

				if (oldValue == NullValueStatus.DefinitelyNotNull) {
					return incomingValue == NullValueStatus.DefinitelyNotNull ?
						NullValueStatus.DefinitelyNotNull : NullValueStatus.PotentiallyNull;
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
				return obj1 == null ?
					obj2 == null : obj1.Equals(obj2);
			}

			public static bool operator !=(VariableStatusInfo obj1, VariableStatusInfo obj2) {
				return !(obj1 == obj2);
			}
		}

		sealed class NullAnalysisNode : ControlFlowNode
		{
			public readonly VariableStatusInfo VariableState = new VariableStatusInfo();

			public NullAnalysisNode(Statement previousStatement, Statement nextStatement, ControlFlowNodeType type)
				: base(previousStatement, nextStatement, type)
			{
			}

			public bool ReceiveIncoming(VariableStatusInfo incomingState)
			{
				return VariableState.ReceiveIncoming(incomingState);
			}
		}

		sealed class NullAnalysisGraphBuilder : ControlFlowGraphBuilder
		{
			protected override ControlFlowNode CreateNode(Statement previousStatement, Statement nextStatement, ControlFlowNodeType type)
			{
				return new NullAnalysisNode(previousStatement, nextStatement, type);
			}
		}

		CSharpAstResolver resolver;
		readonly NullAnalysisVisitor visitor;
		List<NullAnalysisNode> allNodes;
		HashSet<NullAnalysisNode> nodesToVisit = new HashSet<NullAnalysisNode>();
		Dictionary<Statement, NullAnalysisNode> nodeBeforeStatementDict;
		Dictionary<Statement, NullAnalysisNode> nodeAfterStatementDict;
		Dictionary<Expression, VariableStatusInfo> statusBeforeExpression = new Dictionary<Expression, VariableStatusInfo>();
		Statement rootStatement;

		public NullValueAnalysis(MethodDeclaration methodDeclaration, CSharpAstResolver resolver, CancellationToken cancellationToken)
			: this(methodDeclaration.Body, resolver, methodDeclaration.Parameters, cancellationToken)
		{
		}

		public NullValueAnalysis(Statement rootStatement, CSharpAstResolver resolver, IEnumerable<ParameterDeclaration> parameters, CancellationToken cancellationToken)
		{
			if (rootStatement == null)
				throw new ArgumentNullException("rootStatement");
			if (resolver == null)
				throw new ArgumentNullException("resolver");

			this.resolver = resolver;
			this.rootStatement = rootStatement;
			this.visitor = new NullAnalysisVisitor(this);

			NullAnalysisGraphBuilder cfgBuilder = new NullAnalysisGraphBuilder();
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
				var resolveResult = resolver.Resolve(parameter.Type);
				node.VariableState.VariableStatus [name] = GetInitialVariableStatus(resolveResult);
			}

			nodesToVisit.Add(node);
		}

		NullValueStatus GetInitialVariableStatus(ResolveResult resolveResult)
		{
			var typeResolveResult = resolveResult as TypeResolveResult;
			if (typeResolveResult == null) {
				return NullValueStatus.Error;
			}
			var type = typeResolveResult.Type;
			switch (type.IsReferenceType) {
				case true:
					return NullValueStatus.PotentiallyNull;
				case false:
					return type.FullName == "System.Nullable`1" ? NullValueStatus.PotentiallyNull : NullValueStatus.DefinitelyNotNull;
				case null:
					return NullValueStatus.Error;
			}

			Debug.Assert(false);
			return NullValueStatus.Error;
		}

		void VisitNodes()
		{
			while (nodesToVisit.Any()) {
				var node = nodesToVisit.First();
				nodesToVisit.Remove(node);

				Visit(node);
			}
		}

		void Visit(NullAnalysisNode node)
		{
			var nextStatement = node.NextStatement;
			VariableStatusInfo outgoingStatusInfo = node.VariableState;
			ConditionalBranchInfo branchInfo = null;

			if (nextStatement != null) {
				var result = nextStatement.AcceptVisitor(visitor, node.VariableState);
				Debug.Assert(result != null);

				outgoingStatusInfo = result.Variables;
				branchInfo = result.ConditionalBranchInfo;
			}

			foreach (var outgoingEdge in node.Outgoing) {
				var edgeInfo = outgoingStatusInfo.Clone();
				if (branchInfo != null) {
					switch (outgoingEdge.Type) {
						case ControlFlowEdgeType.ConditionTrue:
							foreach (var trueState in branchInfo.TrueConditionVariableNullStates) {
								edgeInfo.VariableStatus [trueState.Key] = trueState.Value ? NullValueStatus.DefinitelyNull : NullValueStatus.DefinitelyNotNull;
							}
							break;
						case ControlFlowEdgeType.ConditionFalse:
							foreach (var falseState in branchInfo.FalseConditionVariableNullStates) {
								edgeInfo.VariableStatus [falseState.Key] = falseState.Value ? NullValueStatus.DefinitelyNull : NullValueStatus.DefinitelyNotNull;
							}
							break;
					}
				}

				var outgoingNode = (NullAnalysisNode) outgoingEdge.To;
				if (outgoingNode.ReceiveIncoming(edgeInfo)) {
					nodesToVisit.Add(outgoingNode);
				}
			}
		}

		public NullValueStatus GetVariableStatusBeforeExpression(Expression expr, string variableName)
		{
			if (expr == null)
				throw new ArgumentNullException("expr");
			if (variableName == null)
				throw new ArgumentNullException("variableName");

			VariableStatusInfo info;
			if (statusBeforeExpression.TryGetValue(expr, out info)) {
				return info [variableName];
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
			public Dictionary<string, bool> TrueConditionVariableNullStates = new Dictionary<string, bool>();
			public Dictionary<string, bool> FalseConditionVariableNullStates = new Dictionary<string, bool>();
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

			public static VisitorResult ForValue(VariableStatusInfo oldResult, NullValueStatus newValue)
			{
				var clone = new VisitorResult();
				clone.NullableReturnResult = newValue;
				clone.Variables = oldResult.Clone();
				return clone;
			}
		}

		class NullAnalysisVisitor : DepthFirstAstVisitor<VariableStatusInfo, VisitorResult>
		{
			NullValueAnalysis analysis;

			public NullAnalysisVisitor(NullValueAnalysis analysis) {
				this.analysis = analysis;
			}

			public override VisitorResult VisitBlockStatement(BlockStatement blockStatement, VariableStatusInfo data)
			{
				//We'll visit the child statements later (we'll visit each one directly from the CFG)
				//As such this is mostly a dummy node.
				return new VisitorResult { Variables = data };
			}

			public override VisitorResult VisitExpressionStatement(ExpressionStatement expressionStatement, VariableStatusInfo data)
			{
				return expressionStatement.Expression.AcceptVisitor(this, data);
			}

			public override VisitorResult VisitAssignmentExpression(AssignmentExpression assignmentExpression, VariableStatusInfo data)
			{
				var tentativeResult = assignmentExpression.Left.AcceptVisitor(this, data);
				tentativeResult = assignmentExpression.Right.AcceptVisitor(this, tentativeResult.Variables);

				var leftIdentifier = assignmentExpression.Left as IdentifierExpression;
				if (leftIdentifier != null) {
					var resolveResult = analysis.resolver.Resolve(leftIdentifier);
					if (resolveResult.IsError) {
						return VisitorResult.ForValue(data, NullValueStatus.Error);
					}
					var local = resolveResult as LocalResolveResult;
					if (local != null) {
						var result = new VisitorResult();
						result.NullableReturnResult = tentativeResult.NullableReturnResult;
						result.Variables = tentativeResult.Variables.Clone();
						result.Variables.VariableStatus [local.Variable.Name] = tentativeResult.NullableReturnResult;
						return result;
					}
				}

				return tentativeResult;
			}

			public override VisitorResult VisitIdentifierExpression(IdentifierExpression identifierExpression, VariableStatusInfo data)
			{
				var resolveResult = analysis.resolver.Resolve(identifierExpression);
				if (resolveResult.IsError) {
					return VisitorResult.ForValue(data, NullValueStatus.Error);
				}
				var local = resolveResult as LocalResolveResult;
				if (local != null) {
					return VisitorResult.ForValue(data, data.VariableStatus [local.Variable.Name]);
				}
				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression, VariableStatusInfo data)
			{
				return VisitorResult.ForValue(data, NullValueStatus.DefinitelyNull);
			}

			public override VisitorResult VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, VariableStatusInfo data)
			{
				return VisitorResult.ForValue(data, NullValueStatus.DefinitelyNotNull);
			}

			public override VisitorResult VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, VariableStatusInfo data)
			{
				return parenthesizedExpression.Expression.AcceptVisitor(this, data);
			}

			public override VisitorResult VisitReturnStatement(ReturnStatement returnStatement, VariableStatusInfo data)
			{
				if (returnStatement.Expression.IsNull)
					return VisitorResult.ForValue(data, NullValueStatus.Unknown);
				return returnStatement.Expression.AcceptVisitor(this, data);
			}
		}
	}
}

