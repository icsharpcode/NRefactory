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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[ExportDiagnosticAnalyzer("Bitwise operation on enum which has no [Flags] attribute", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(AnalysisDisableKeyword = "BitwiseOperatorOnEnumWithoutFlags")]
	public class BitwiseOperatorOnEnumWithoutFlagsIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "BitwiseOperatorOnEnumWithoutFlagsIssue";
		const string Description            = "Bitwise operation on enum which has no [Flags] attribute";
		const string MessageFormat          = "Bitwise Operations on enum not marked with [Flags] attribute";
		const string Category               = IssueCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true);

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

//			static bool IsBitwiseOperator (UnaryOperatorType op)
//			{
//				return op == UnaryOperatorType.BitNot;
//			}
//
//			static bool IsBitwiseOperator (AssignmentOperatorType op)
//			{
//				return op == AssignmentOperatorType.BitwiseAnd || op == AssignmentOperatorType.BitwiseOr ||
//					op == AssignmentOperatorType.ExclusiveOr;
//			}
//
//			static bool IsBitwiseOperator (BinaryOperatorType op)
//			{
//				return op == BinaryOperatorType.BitwiseAnd || op == BinaryOperatorType.BitwiseOr || 
//					op == BinaryOperatorType.ExclusiveOr;
//			}
//
//			bool IsNonFlagsEnum (Expression expr)
//			{
//				var resolveResult = ctx.Resolve (expr);
//				if (resolveResult == null || resolveResult.Type.Kind != TypeKind.Enum)
//					return false;
//
//				// check [Flags]
//				var typeDef = resolveResult.Type.GetDefinition ();
//				return typeDef != null &&
//					typeDef.Attributes.All (attr => attr.AttributeType.FullName != "System.FlagsAttribute");
//			}
//
//			private void AddIssue (CSharpTokenNode operatorToken)
//			{
//				AddIssue (new CodeIssue (operatorToken, ctx.TranslateString ("")));
//			}
//
//			public override void VisitUnaryOperatorExpression (UnaryOperatorExpression unaryOperatorExpression)
//			{
//				base.VisitUnaryOperatorExpression (unaryOperatorExpression);
//
//				if (!IsBitwiseOperator (unaryOperatorExpression.Operator))
//					return;
//				if (IsNonFlagsEnum (unaryOperatorExpression.Expression))
//					AddIssue (unaryOperatorExpression.OperatorToken);
//			}
//
//			public override void VisitAssignmentExpression (AssignmentExpression assignmentExpression)
//			{
//				base.VisitAssignmentExpression (assignmentExpression);
//
//				if (!IsBitwiseOperator (assignmentExpression.Operator))
//					return;
//				if (IsNonFlagsEnum (assignmentExpression.Right))
//					AddIssue (assignmentExpression.OperatorToken);
//			}
//
//			public override void VisitBinaryOperatorExpression (BinaryOperatorExpression binaryOperatorExpression)
//			{
//				base.VisitBinaryOperatorExpression (binaryOperatorExpression);
//
//				if (!IsBitwiseOperator (binaryOperatorExpression.Operator))
//					return;
//				if (IsNonFlagsEnum (binaryOperatorExpression.Left) || IsNonFlagsEnum (binaryOperatorExpression.Right))
//					AddIssue (binaryOperatorExpression.OperatorToken);
//			}
		}
	}

	[ExportCodeFixProvider(BitwiseOperatorOnEnumWithoutFlagsIssue.DiagnosticId, LanguageNames.CSharp)]
	public class BitwiseOperatorOnEnumWithoutFlagsFixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return BitwiseOperatorOnEnumWithoutFlagsIssue.DiagnosticId;
		}

		public async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var result = new List<CodeAction>();
			foreach (var diagonstic in diagnostics) {
				var node = root.FindNode(diagonstic.Location.SourceSpan);
				//if (!node.IsKind(SyntaxKind.BaseList))
				//	continue;
				var newRoot = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
				result.Add(CodeActionFactory.Create(node.Span, diagonstic.Severity, diagonstic.GetMessage(), document.WithSyntaxRoot(newRoot)));
			}
			return result;
		}
	}
}