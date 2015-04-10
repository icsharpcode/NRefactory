//
// SimplifyLinqExpressionAnalyzer.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
	public class SimplifyLinqExpressionAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.SimplifyLinqExpressionAnalyzerID, 
			GettextCatalog.GetString("Simplify LINQ expression"),
			GettextCatalog.GetString("Simplify LINQ expression"), 
			DiagnosticAnalyzerCategories.PracticesAndImprovements, 
			DiagnosticSeverity.Info, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.SimplifyLinqExpressionAnalyzerID),
			customTags: DiagnosticCustomTags.Unnecessary
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			//context.RegisterSyntaxNodeAction(
			//	(nodeContext) => {
			//		Diagnostic diagnostic;
			//		if (TryGetDiagnostic (nodeContext, out diagnostic)) {
			//			nodeContext.ReportDiagnostic(diagnostic);
			//		}
			//	}, 
			//	new SyntaxKind[] { SyntaxKind.None }
			//);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			//var node = nodeContext.Node as ;
			//diagnostic = Diagnostic.Create (descriptor, node.GetLocation ());
			//return true;
			return false;
		}

//		class GatherVisitor : GatherVisitorBase<SimplifyLinqExpressionAnalyzer>
//		{
//			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
//				: base (semanticModel, addDiagnostic, cancellationToken)
//			{
//			}

////			static readonly Expression simpleExpression = new Choice {
////				new UnaryOperatorExpression (UnaryOperatorType.Not, new AnyNode()),
////				new BinaryOperatorExpression(new AnyNode(), BinaryOperatorType.Equality, new AnyNode()),
////				new BinaryOperatorExpression(new AnyNode(), BinaryOperatorType.InEquality, new AnyNode())
////			};
////
////			static readonly AstNode argumentPattern = new Choice {
////				new LambdaExpression  {
////					Parameters = { new ParameterDeclaration(PatternHelper.AnyType(true), Pattern.AnyString) },
////					Body = new Choice {
////						new NamedNode ("expr", simpleExpression),
////						new BlockStatement { new ReturnStatement(new NamedNode ("expr", simpleExpression))}
////					} 
////				},
////				new AnonymousMethodExpression {
////					Parameters = { new ParameterDeclaration(PatternHelper.AnyType(true), Pattern.AnyString) },
////					Body = new BlockStatement { new ReturnStatement(new NamedNode ("expr", simpleExpression))}
////				}
////			};
////
////			public override void VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression)
////			{
////				base.VisitUnaryOperatorExpression(unaryOperatorExpression);
////				if (unaryOperatorExpression.Operator != UnaryOperatorType.Not)
////					return;
////				var invocation =  CSharpUtil.GetInnerMostExpression(unaryOperatorExpression.Expression) as InvocationExpression;
////				if (invocation == null)
////					return; 
////				var rr = ctx.Resolve(invocation) as CSharpInvocationResolveResult;
////				if (rr == null || rr.IsError)
////					return;
////
////				if (rr.Member.DeclaringType.Name != "Enumerable" || rr.Member.DeclaringType.Namespace != "System.Linq")
////					return;
////				if (!new[] { "All", "Any" }.Contains(rr.Member.Name))
////					return;
////				if (invocation.Arguments.Count != 1)
////					return;
////				var arg = invocation.Arguments.First();
////				var match = argumentPattern.Match(arg);
////				if (!match.Success)
////					return;
////				AddDiagnosticAnalyzer(new CodeIssue (
////					unaryOperatorExpression,
////					ctx.TranslateString(""),
////					ctx.TranslateString("Simplify LINQ expression"),
////					s => {
////						var target = invocation.Target.Clone() as MemberReferenceExpression;
////						target.MemberName = target.MemberName == "All" ? "Any" : "All";
////
////						var expr = arg.Clone();
////						var nmatch = argumentPattern.Match(expr);
////						var cond = nmatch.Get<Expression>("expr").Single();
////						cond.ReplaceWith(CSharpUtil.InvertCondition(cond));
////						var simplifiedInvocation = new InvocationExpression(
////							target,
////							expr
////						);
////						s.Replace(unaryOperatorExpression, simplifiedInvocation);
////					}
////				));
////			}
//		}
	}
}