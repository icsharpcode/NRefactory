// 
// ConstantConditionAnalyzer.cs
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

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ConstantConditionAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.ConstantConditionAnalyzerID,
			GettextCatalog.GetString ("Condition is always 'true' or always 'false'"),
			GettextCatalog.GetString ("Condition is always '{0}'"),
			DiagnosticAnalyzerCategories.CodeQualityIssues,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor (NRefactoryDiagnosticIDs.ConstantConditionAnalyzerID)
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize (AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction (
				(nodeContext) => {
					Check (nodeContext, ((IfStatementSyntax)nodeContext.Node).Condition);
				},
				new SyntaxKind [] { SyntaxKind.IfStatement }
			);

			context.RegisterSyntaxNodeAction (
				(nodeContext) => {
					Check (nodeContext, ((WhileStatementSyntax)nodeContext.Node).Condition);
				},
				new SyntaxKind [] { SyntaxKind.WhileStatement }
			);

			context.RegisterSyntaxNodeAction (
				(nodeContext) => {
					Check (nodeContext, ((DoStatementSyntax)nodeContext.Node).Condition);
				},
				new SyntaxKind [] { SyntaxKind.DoStatement }
			);

			context.RegisterSyntaxNodeAction (
				(nodeContext) => {
					Check (nodeContext, ((ConditionalExpressionSyntax)nodeContext.Node).Condition);
				},
				new SyntaxKind [] { SyntaxKind.ConditionalExpression }
			);

			context.RegisterSyntaxNodeAction (
				(nodeContext) => {
					Check (nodeContext, ((ForStatementSyntax)nodeContext.Node).Condition);
				},
				new SyntaxKind [] { SyntaxKind.ForStatement }
			);
		}

		void Check (SyntaxNodeAnalysisContext nodeContext, ExpressionSyntax condition)
		{
			if (condition.IsKind (SyntaxKind.TrueLiteralExpression) || condition.IsKind (SyntaxKind.FalseLiteralExpression))
				return;

			var resolveResult = nodeContext.SemanticModel.GetConstantValue (condition);
			if (!resolveResult.HasValue || !(resolveResult.Value is bool))
				return;

			var value = (bool)resolveResult.Value;

			nodeContext.ReportDiagnostic (Diagnostic.Create (
				descriptor.Id,
				descriptor.Category,
				string.Format (descriptor.MessageFormat.ToString (), value),
				descriptor.DefaultSeverity,
				descriptor.DefaultSeverity,
				descriptor.IsEnabledByDefault,
				4,
				descriptor.Title,
				descriptor.Description,
				descriptor.HelpLinkUri,
				condition.GetLocation (),
				null,
				new [] { value.ToString () }
			));
		}

		////			void RemoveText(Script script, TextLocation start, TextLocation end)
		////			{
		////				var startOffset = script.GetCurrentOffset(start);
		////				var endOffset = script.GetCurrentOffset(end);
		////				if (startOffset < endOffset)
		////					script.RemoveText(startOffset, endOffset - startOffset);
		////			}
		//		}
	}
}