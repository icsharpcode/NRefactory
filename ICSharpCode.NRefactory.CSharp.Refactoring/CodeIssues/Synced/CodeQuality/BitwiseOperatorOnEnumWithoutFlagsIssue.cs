// 
// BitwiseOperationOnNonFlagsEnumIssue.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang <lightyang0@gmail.com>
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Threading;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "BitwiseOperatorOnEnumWithoutFlags")]
	public class BitwiseOperatorOnEnumWithoutFlagsIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "BitwiseOperatorOnEnumWithoutFlagsIssue";
		const string Description            = "Bitwise operation on enum which has no [Flags] attribute";
		const string MessageFormat          = "Bitwise operation on enum not marked with [Flags] attribute";
		const string Category               = IssueCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true, "Bitwise operation on enum which has no [Flags] attribute");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<BitwiseOperatorOnEnumWithoutFlagsIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}

			bool IsNonFlagsEnum (ExpressionSyntax expr)
			{
				var type = semanticModel.GetTypeInfo(expr).Type;
				if (type == null || type.TypeKind != TypeKind.Enum)
					return false;

				// check [Flags]
				return !type.GetAttributes().Any (attr => 
					attr.AttributeClass.Name == "FlagsAttribute" &&
					attr.AttributeClass.ContainingNamespace.ToDisplayString() == "System"
				);
			}

			public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
			{
				base.VisitPrefixUnaryExpression(node);
				if (!node.IsKind(SyntaxKind.BitwiseNotExpression))
					return;
				if (IsNonFlagsEnum (node.Operand))
					AddIssue (Diagnostic.Create(Rule, Location.Create(semanticModel.SyntaxTree, node.OperatorToken.Span)));
			}

			public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
			{
				base.VisitAssignmentExpression(node);
				switch (node.Kind())  {
					case SyntaxKind.OrAssignmentExpression:
					case SyntaxKind.AndAssignmentExpression:
					case SyntaxKind.ExclusiveOrAssignmentExpression:
						if (IsNonFlagsEnum(node.Left) || IsNonFlagsEnum(node.Right))
							AddIssue(Diagnostic.Create(Rule, Location.Create(semanticModel.SyntaxTree, node.OperatorToken.Span)));
						break;
				}
			}

			public override void VisitBinaryExpression(BinaryExpressionSyntax node)
			{
				base.VisitBinaryExpression(node);
				switch (node.Kind())  {
					case SyntaxKind.BitwiseAndExpression:
					case SyntaxKind.BitwiseOrExpression:
					case SyntaxKind.ExclusiveOrExpression:
						if (IsNonFlagsEnum(node.Left) || IsNonFlagsEnum(node.Right))
							AddIssue(Diagnostic.Create(Rule, Location.Create(semanticModel.SyntaxTree, node.OperatorToken.Span)));
						break;
				}
			}
		}
	}
}