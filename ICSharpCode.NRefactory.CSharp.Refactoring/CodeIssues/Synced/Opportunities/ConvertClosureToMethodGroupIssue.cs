//
// SimplifyAnonymousMethodToDelegateIssue.cs
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

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[DiagnosticAnalyzer]
	[ExportDiagnosticAnalyzer("Convert anonymous method to method group", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(Description = "Anonymous method or lambda expression can be simplified to method group.", AnalysisDisableKeyword = "ConvertClosureToMethodGroup")]
	public class ConvertClosureToMethodGroupIssue : GatherVisitorCodeIssueProvider
	{
		internal const string DiagnosticId  = "ConvertClosureToMethodGroupIssue";
		const string MessageFormat          = "Replace with method group";
		const string Category               = IssueCategories.Opportunities;

		static readonly DiagnosticDescriptor Rule1 = new DiagnosticDescriptor (DiagnosticId, "Anonymous method can be simplified to method group", MessageFormat, Category, DiagnosticSeverity.Info, true);
		static readonly DiagnosticDescriptor Rule2 = new DiagnosticDescriptor (DiagnosticId, "Lambda expression can be simplified to method group", MessageFormat, Category, DiagnosticSeverity.Info, true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule1, Rule2);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<ConvertClosureToMethodGroupIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base (semanticModel, addDiagnostic, cancellationToken)
			{
			}
//
//			static readonly Pattern pattern = new Choice {
//				new BlockStatement {
//					new ReturnStatement (new AnyNode ("invoke")) 
//				},
//				new AnyNode ("invoke")
//			};
//
//			static InvocationExpression AnalyzeBody(AstNode body)
//			{
//				var m = pattern.Match(body);
//				if (m.Success)
//					return m.Get("invoke").Single () as InvocationExpression;
//				return null;
//			}
//
//			static bool IsSimpleTarget(Expression target)
//			{
//				if (target is IdentifierExpression)
//					return true;
//				var mref = target as MemberReferenceExpression;
//				if (mref != null)
//					return IsSimpleTarget (mref.Target);
//				var pref = target as PointerReferenceExpression;
//				if (pref != null)
//					return IsSimpleTarget (pref.Target);
//				return false;
//			}
//
//			void AnalyzeExpression(AstNode expression, AstNode body, AstNodeCollection<ParameterDeclaration> parameters)
//			{
//				var invocation = AnalyzeBody(body);
//				if (invocation == null)
//					return;
//				if (!IsSimpleTarget (invocation.Target))
//					return;
//				var rr = ctx.Resolve (invocation) as CSharpInvocationResolveResult;
//				if (rr == null)
//					return;
//				var lambdaParameters = parameters.ToList();
//				var arguments = rr.GetArgumentsForCall();
//				if (lambdaParameters.Count != arguments.Count)
//					return;
//				for (int i = 0; i < arguments.Count; i++) {
//					var arg = UnpackImplicitIdentityOrReferenceConversion(arguments[i]) as LocalResolveResult;
//					if (arg == null || arg.Variable.Name != lambdaParameters[i].Name)
//						return;
//				}
//				var returnConv = ctx.GetConversion(invocation);
//				if (returnConv.IsExplicit || !(returnConv.IsIdentityConversion || returnConv.IsReferenceConversion))
//					return;
//				var validTypes = TypeGuessing.GetValidTypes (ctx.Resolver, expression).ToList ();
//
//				// search for method group collisions
//				var targetResult = ctx.Resolve(invocation.Target) as MethodGroupResolveResult;
//				if (targetResult != null) {
//					foreach (var t in validTypes) {
//						if (t.Kind != TypeKind.Delegate)
//							continue;
//						var invokeMethod = t.GetDelegateInvokeMethod();
//
//						foreach (var otherMethod in targetResult.Methods) {
//							if (otherMethod == rr.Member)
//								continue;
//							if (ParameterListComparer.Instance.Equals(otherMethod.Parameters, invokeMethod.Parameters))
//								return;
//						}
//					}
//				}
//
//				bool isValidReturnType = false;
//				foreach (var t in validTypes) {
//					if (t.Kind != TypeKind.Delegate)
//						continue;
//					var invokeMethod = t.GetDelegateInvokeMethod();
//					isValidReturnType = rr.Member.ReturnType == invokeMethod.ReturnType || rr.Member.ReturnType.GetAllBaseTypes().Contains(invokeMethod.ReturnType);
//					if (isValidReturnType)
//						break;
//				}
//				if (!isValidReturnType)
//					return;
//
//				if (rr.IsDelegateInvocation) {
//					if (!validTypes.Contains(rr.Member.DeclaringType))
//						return;
//				}
//
//				AddIssue(new CodeIssue(expression,
			//				         expression is AnonymousMethodExpression ? ctx.TranslateString("Anonymous method can be simplified to method group") : ctx.TranslateString("Lambda expression can be simplified to method group"), 
//				         ctx.TranslateString(), script =>  {
//					if (validTypes.Any (t => t.FullName == "System.Func" && t.TypeParameterCount == 1 + parameters.Count) && validTypes.Any (t => t.FullName == "System.Action")) {
//						if (rr != null && rr.Member.ReturnType.Kind != TypeKind.Void) {
//							var builder = ctx.CreateTypeSystemAstBuilder (expression);
//							var type = builder.ConvertType(new TopLevelTypeName("System", "Func", 1));
//							var args = type.GetChildrenByRole(Roles.TypeArgument);
//							args.Clear ();
//							foreach (var pde in parameters) {
//								args.Add (builder.ConvertType (ctx.Resolve (pde).Type));
//							}
//							args.Add (builder.ConvertType (rr.Member.ReturnType));
//							script.Replace(expression, new CastExpression (type, invocation.Target.Clone()));
//							return;
//						}
//					}
//					script.Replace(expression, invocation.Target.Clone());
//				}));
//			}
//			
//			static ResolveResult UnpackImplicitIdentityOrReferenceConversion(ResolveResult rr)
//			{
//				var crr = rr as ConversionResolveResult;
//				if (crr != null && crr.Conversion.IsImplicit && (crr.Conversion.IsIdentityConversion || crr.Conversion.IsReferenceConversion))
//					return crr.Input;
//				return rr;
//			}
//
//			public override void VisitLambdaExpression(LambdaExpression lambdaExpression)
//			{
//				base.VisitLambdaExpression(lambdaExpression);
//				AnalyzeExpression(lambdaExpression, lambdaExpression.Body, lambdaExpression.Parameters);
//			}
//
//			public override void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
//			{
//				base.VisitAnonymousMethodExpression(anonymousMethodExpression);
//				AnalyzeExpression(anonymousMethodExpression, anonymousMethodExpression.Body, anonymousMethodExpression.Parameters);
//			}
//
		}
	}

	[ExportCodeFixProvider(ConvertClosureToMethodGroupIssue.DiagnosticId, LanguageNames.CSharp)]
	public class ConvertClosureToMethodGroupFixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return ConvertClosureToMethodGroupIssue.DiagnosticId;
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