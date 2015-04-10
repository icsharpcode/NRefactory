//
// UseMethodAnyAnalyzer.cs
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
	public class UseMethodAnyAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.UseMethodAnyAnalyzerID, 
			GettextCatalog.GetString("Replace usages of 'Count()' with call to 'Any()'"),
			GettextCatalog.GetString("Use '{0}' for increased performance"), 
			DiagnosticAnalyzerCategories.PracticesAndImprovements, 
			DiagnosticSeverity.Info, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.UseMethodAnyAnalyzerID)
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

//		class GatherVisitor : GatherVisitorBase<UseMethodAnyAnalyzer>
//		{
//			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
//				: base (semanticModel, addDiagnostic, cancellationToken)
//			{
//			}
////
////			void AddDiagnosticAnalyzer2(BinaryOperatorExpression binaryOperatorExpression, Expression expr)
////			{
////			}
////
////			readonly AstNode anyPattern =
////				new Choice {
////					PatternHelper.CommutativeOperatorWithOptionalParentheses(
////						new NamedNode ("invocation", new InvocationExpression(new MemberReferenceExpression(new AnyNode("expr"), "Count"))),
////						BinaryOperatorType.InEquality,
////						new PrimitiveExpression(0)
////					),
////					new BinaryOperatorExpression (
////						PatternHelper.OptionalParentheses(new NamedNode ("invocation", new InvocationExpression(new MemberReferenceExpression(new AnyNode("expr"), "Count")))),
////						BinaryOperatorType.GreaterThan,
////						PatternHelper.OptionalParentheses(new PrimitiveExpression(0))
////					),
////					new BinaryOperatorExpression (
////						PatternHelper.OptionalParentheses(new PrimitiveExpression(0)),
////						BinaryOperatorType.LessThan,
////						PatternHelper.OptionalParentheses(new NamedNode ("invocation", new InvocationExpression(new MemberReferenceExpression(new AnyNode("expr"), "Count"))))
////					),
////					new BinaryOperatorExpression (
////						PatternHelper.OptionalParentheses(new NamedNode ("invocation", new InvocationExpression(new MemberReferenceExpression(new AnyNode("expr"), "Count")))),
////						BinaryOperatorType.GreaterThanOrEqual,
////						PatternHelper.OptionalParentheses(new PrimitiveExpression(1))
////					),
////					new BinaryOperatorExpression (
////						PatternHelper.OptionalParentheses(new PrimitiveExpression(1)),
////						BinaryOperatorType.LessThanOrEqual,
////						PatternHelper.OptionalParentheses(new NamedNode ("invocation", new InvocationExpression(new MemberReferenceExpression(new AnyNode("expr"), "Count"))))
////					)
////			};
////
////			readonly AstNode notAnyPattern =
////				new Choice {
////					PatternHelper.CommutativeOperatorWithOptionalParentheses(
////						new NamedNode ("invocation", new InvocationExpression(new MemberReferenceExpression(new AnyNode("expr"), "Count"))),
////						BinaryOperatorType.Equality,
////						new PrimitiveExpression(0)
////					),
////					new BinaryOperatorExpression (
////						PatternHelper.OptionalParentheses(new NamedNode ("invocation", new InvocationExpression(new MemberReferenceExpression(new AnyNode("expr"), "Count")))),
////						BinaryOperatorType.LessThan,
////						PatternHelper.OptionalParentheses(new PrimitiveExpression(1))
////					),
////					new BinaryOperatorExpression (
////						PatternHelper.OptionalParentheses(new PrimitiveExpression(1)),
////						BinaryOperatorType.GreaterThan,
////						PatternHelper.OptionalParentheses(new NamedNode ("invocation", new InvocationExpression(new MemberReferenceExpression(new AnyNode("expr"), "Count"))))
////					),
////					new BinaryOperatorExpression (
////						PatternHelper.OptionalParentheses(new NamedNode ("invocation", new InvocationExpression(new MemberReferenceExpression(new AnyNode("expr"), "Count")))),
////						BinaryOperatorType.LessThanOrEqual,
////						PatternHelper.OptionalParentheses(new PrimitiveExpression(0))
////					),
////					new BinaryOperatorExpression (
////						PatternHelper.OptionalParentheses(new PrimitiveExpression(0)),
////						BinaryOperatorType.GreaterThanOrEqual,
////						PatternHelper.OptionalParentheses(new NamedNode ("invocation", new InvocationExpression(new MemberReferenceExpression(new AnyNode("expr"), "Count"))))
////					)
////				};
////
////			void AddMatch(BinaryOperatorExpression binaryOperatorExpression, Match match, bool negateAny)
////			{
////				AddDiagnosticAnalyzer(new CodeIssue(
////					binaryOperatorExpression,
////					ctx.TranslateString(""), 
////					script =>  {
////						Expression expr = new InvocationExpression(new MemberReferenceExpression(match.Get<Expression>("expr").First().Clone(), "Any"));
////						if (negateAny)
////							expr = new UnaryOperatorExpression(UnaryOperatorType.Not, expr);
////						script.Replace(binaryOperatorExpression, expr);
////					}
////				));
////			}
////
////			bool CheckMethod(Match match)
////			{
////				var invocation = match.Get<Expression>("invocation").First();
////				var rr = ctx.Resolve(invocation) as CSharpInvocationResolveResult;
////				if (rr == null || rr.IsError)
////					return false;
////				var method = rr.Member as IMethod;
////				return 
////					method != null &&
////					method.IsExtensionMethod &&
////					method.DeclaringTypeDefinition.Namespace == "System.Linq" && 
////					method.DeclaringTypeDefinition.Name == "Enumerable";
////			}
////
////			public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
////			{
////				base.VisitBinaryOperatorExpression(binaryOperatorExpression);
////				var match = anyPattern.Match(binaryOperatorExpression);
////				if (match.Success && CheckMethod (match)) {
////					AddMatch(binaryOperatorExpression, match, false);
////					return;
////				}
////				match = notAnyPattern.Match(binaryOperatorExpression);
////				if (match.Success && CheckMethod (match)) {
////					AddMatch(binaryOperatorExpression, match, true);
////					return;
////				}
////			}
//		}
	}
}