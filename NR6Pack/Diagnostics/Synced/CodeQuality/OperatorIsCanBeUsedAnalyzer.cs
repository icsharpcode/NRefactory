//
// ReplaceWithIsOperatorAnalyzer.cs
//
// Author:
//	   Ji Kun <jikun.nus@gmail.com>
//
// Copyright (c) 2013 Ji Kun
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
// THE SOFTWARE.using System;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class OperatorIsCanBeUsedAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.OperatorIsCanBeUsedAnalyzerID, 
			GettextCatalog.GetString("Operator Is can be used instead of comparing object GetType() and instances of System.Type object"),
			GettextCatalog.GetString("Operator 'is' can be used"), 
			DiagnosticAnalyzerCategories.CodeQualityIssues, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.OperatorIsCanBeUsedAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryGetDiagnostic(nodeContext, out diagnostic))
						nodeContext.ReportDiagnostic(diagnostic);
				},
				SyntaxKind.EqualsExpression
			);
		}

		static bool TryGetDiagnostic (SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);
			if (nodeContext.IsFromGeneratedCode())
				return false;
			var node = nodeContext.Node as BinaryExpressionSyntax;
			InvocationExpressionSyntax invocation;
			TypeOfExpressionSyntax typeOf;
			if(!Matches(node.Left, node.Right, out invocation, out typeOf) && !Matches(node.Right, node.Left, out invocation, out typeOf))
				return false;

			var type = nodeContext.SemanticModel.GetTypeInfo(typeOf.Type).Type;
			if (type == null || !type.IsSealed)
				return false;
			diagnostic = Diagnostic.Create (
				descriptor,
				node.GetLocation ()
			);
			return true;
		}

		static bool Matches(ExpressionSyntax member, ExpressionSyntax typeofExpr, out InvocationExpressionSyntax invoc, out TypeOfExpressionSyntax typeOf)
		{
			invoc = member as InvocationExpressionSyntax;
			typeOf = typeofExpr as TypeOfExpressionSyntax;
			if (invoc == null || typeOf == null)
				return false;

			var memberAccess = invoc.Expression as MemberAccessExpressionSyntax;
			return memberAccess != null && memberAccess.Name.Identifier.ValueText == "GetType"; 
		}
	}
}