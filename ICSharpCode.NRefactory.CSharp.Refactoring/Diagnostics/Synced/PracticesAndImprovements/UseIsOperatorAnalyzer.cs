//
// UseIsOperatorAnalyzer.cs
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
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "UseIsOperator")]
	public class UseIsOperatorAnalyzer : GatherVisitorDiagnosticAnalyzer
	{
		internal const string DiagnosticId  = "UseIsOperatorAnalyzer";
		const string Description            = "'is' operator can be used";
		const string MessageFormat          = "Use 'is' operator";
		const string Category               = DiagnosticAnalyzerCategories.PracticesAndImprovements;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Info, true, "Use 'is' operator");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<UseIsOperatorAnalyzer>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

//			static readonly Expression pattern1 = new InvocationExpression(
//				new MemberReferenceExpression(new TypeOfExpression(new AnyNode("Type")), "IsAssignableFrom"),
//				new InvocationExpression(
//				new MemberReferenceExpression(new AnyNode("object"), "GetType")
//			)
//			);
//			static readonly Expression pattern2 = new InvocationExpression(
//				new MemberReferenceExpression(new TypeOfExpression(new AnyNode("Type")), "IsInstanceOfType"),
//				new AnyNode("object")
//			);
//
//
//
//			void AddDiagnosticAnalyzer(AstNode invocationExpression, Match match, bool negate = false)
//			{
//				AddDiagnosticAnalyzer(new CodeIssue(
//					invocationExpression,
//					ctx.TranslateString(""),
//					ctx.TranslateString(""), 
//					s => {
//						Expression expression = new IsExpression(CSharpUtil.AddParensForUnaryExpressionIfRequired(match.Get<Expression>("object").Single().Clone()), match.Get<AstType>("Type").Single().Clone());
//						if (negate)
//							expression = new UnaryOperatorExpression (UnaryOperatorType.Not, new ParenthesizedExpression(expression));
//						s.Replace(invocationExpression, expression);
//					}
//				));
//			}
//
//			public override void VisitInvocationExpression(InvocationExpression invocationExpression)
//			{
//				base.VisitInvocationExpression(invocationExpression);
//				var match = pattern1.Match(invocationExpression);
//				if (!match.Success)
//					match = pattern2.Match(invocationExpression);
//				if (match.Success) {
//					AddDiagnosticAnalyzer(invocationExpression, match);
//				}
//			}

			/* Unfortunately not quite the same :/
			static readonly AstNode equalityComparePattern =
				PatternHelper.CommutativeOperator(
					PatternHelper.OptionalParentheses(new TypeOfExpression(new AnyNode("Type"))),
					BinaryOperatorType.Equality,
					PatternHelper.OptionalParentheses(new InvocationExpression(
						new MemberReferenceExpression(new AnyNode("object"), "GetType")
					))
				);
			static readonly AstNode inEqualityComparePattern =
				PatternHelper.CommutativeOperator(
					PatternHelper.OptionalParentheses(new TypeOfExpression(new AnyNode("Type"))),
					BinaryOperatorType.InEquality,
					PatternHelper.OptionalParentheses(new InvocationExpression(
					new MemberReferenceExpression(new AnyNode("object"), "GetType")
					))
					);
			public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
			{
				base.VisitBinaryOperatorExpression(binaryOperatorExpression);
				var match = equalityComparePattern.Match(binaryOperatorExpression);
				if (match.Success) {
					AddDiagnosticAnalyzer(new CodeIssue(binaryOperatorExpression, match);
					return;
				}

				match = inEqualityComparePattern.Match(binaryOperatorExpression);
				if (match.Success) {
					AddDiagnosticAnalyzer(new CodeIssue(binaryOperatorExpression, match, true);
					return;
				}
			}*/
		}
	}
}