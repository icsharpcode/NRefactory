// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.NRefactory.CSharp.Analysis
{
	/// <summary>
	/// Statement reachability analysis.
	/// </summary>
	public sealed class ReachabilityAnalysis
	{
		HashSet<Statement> reachableStatements = new HashSet<Statement>();
		HashSet<Statement> reachableEndPoints = new HashSet<Statement>();
		HashSet<ControlFlowNode> visitedNodes = new HashSet<ControlFlowNode>();
		Stack<ControlFlowNode> stack = new Stack<ControlFlowNode>();
		RecursiveDetectorVisitor recursiveDetectorVisitor = null;
		
		private ReachabilityAnalysis() {}
		
		public static ReachabilityAnalysis Create(Statement statement, CSharpAstResolver resolver = null, RecursiveDetectorVisitor recursiveDetectorVisitor = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var cfgBuilder = new ControlFlowGraphBuilder();
			var cfg = cfgBuilder.BuildControlFlowGraph(statement, resolver, cancellationToken);
			return Create(cfg, recursiveDetectorVisitor, cancellationToken);
		}
		
		internal static ReachabilityAnalysis Create(Statement statement, Func<AstNode, CancellationToken, ResolveResult> resolver, CSharpTypeResolveContext typeResolveContext, CancellationToken cancellationToken)
		{
			var cfgBuilder = new ControlFlowGraphBuilder();
			var cfg = cfgBuilder.BuildControlFlowGraph(statement, resolver, typeResolveContext, cancellationToken);
			return Create(cfg, null, cancellationToken);
		}
		
		public static ReachabilityAnalysis Create(IList<ControlFlowNode> controlFlowGraph, RecursiveDetectorVisitor recursiveDetectorVisitor = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (controlFlowGraph == null)
				throw new ArgumentNullException("controlFlowGraph");
			ReachabilityAnalysis ra = new ReachabilityAnalysis();
			ra.recursiveDetectorVisitor = recursiveDetectorVisitor;
			// Analysing a null node can result in an empty control flow graph
			if (controlFlowGraph.Count > 0) {
				ra.stack.Push(controlFlowGraph[0]);
				while (ra.stack.Count > 0) {
					cancellationToken.ThrowIfCancellationRequested();
					ra.MarkReachable(ra.stack.Pop());
				}
			}
			ra.stack = null;
			ra.visitedNodes = null;
			return ra;
		}
		
		void MarkReachable(ControlFlowNode node)
		{
			if (node.PreviousStatement != null)
				reachableEndPoints.Add(node.PreviousStatement);
			if (node.NextStatement != null) {
				reachableStatements.Add(node.NextStatement);
				if (IsRecursive(node.NextStatement)) {
					return;
				}
			}
			foreach (var edge in node.Outgoing) {
				if (visitedNodes.Add(edge.To))
					stack.Push(edge.To);
			}
		}

		bool IsRecursive(Statement statement)
		{
			if (recursiveDetectorVisitor == null)
				return false;
			recursiveDetectorVisitor.Clear();
			statement.AcceptVisitor(recursiveDetectorVisitor);
			return recursiveDetectorVisitor.Result;
		}
		
		public IEnumerable<Statement> ReachableStatements {
			get { return reachableStatements; }
		}
		
		public bool IsReachable(Statement statement)
		{
			return reachableStatements.Contains(statement);
		}
		
		public bool IsEndpointReachable(Statement statement)
		{
			return reachableEndPoints.Contains(statement);
		}

		public class RecursiveDetectorVisitor : DepthFirstAstVisitor
		{
			/// <summary>
			/// If the last-to-pop element
			/// </summary>
			Stack<bool> recursiveStack = new Stack<bool>();

			public void Clear() {
				recursiveStack.Clear();
			}

			public bool Result
			{
				get {
					return recursiveStack.Single();
				}
			}

			protected bool CurrentResult
			{
				get {
					return recursiveStack.Peek();
				}
			}

			protected void PushResult(bool result)
			{
				recursiveStack.Push(result);
			}

			protected bool PopResult()
			{
				return recursiveStack.Pop();
			}

			public override void VisitConditionalExpression(ConditionalExpression conditionalExpression)
			{
				conditionalExpression.Condition.AcceptVisitor(this);
				if (CurrentResult) {
					return;
				}
				PopResult();

				conditionalExpression.TrueExpression.AcceptVisitor(this);
				if (!CurrentResult) {
					return;
				}
				PopResult();

				conditionalExpression.FalseExpression.AcceptVisitor(this);
			}

			protected override void VisitChildren(AstNode node)
			{
				VisitNodeList(node.Children);
			}

			public override void VisitCSharpTokenNode(CSharpTokenNode token)
			{
				PushResult(false);
			}

			public override void VisitComment(Comment comment)
			{
				PushResult(false);
			}

			public override void VisitPrimitiveExpression(PrimitiveExpression primitiveExpression)
			{
				PushResult(false);
			}

			public override void VisitLambdaExpression(LambdaExpression lambdaExpression)
			{
				PushResult(false);
			}

			public override void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
			{
				PushResult(false);
			}

			void VisitNodeList(IEnumerable<AstNode> nodes) {
				foreach (var initializer in nodes) {
					initializer.AcceptVisitor(this);
					bool result = PopResult();
					if (result) {
						PushResult(true);
						return;
					}
				}

				PushResult(false);
			}
		}
	}
}
