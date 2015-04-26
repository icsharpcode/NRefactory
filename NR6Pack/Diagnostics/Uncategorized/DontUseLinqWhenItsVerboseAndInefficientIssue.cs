// DontUseLinqWhenItsVerboseAndInefficientIssue.cs
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
	[DiagnosticAnalyzer]
	[ExportDiagnosticAnalyzer("", LanguageNames.CSharp)]
	[NRefactoryCodeDiagnosticAnalyzer(Description = "", AnalysisDisableKeyword = "")]
	[IssueDescription("Use of Linq methods when there's a better alternative",
	                  Description="Detects usage of Linq when there's a simpler and faster alternative",
	                  Category=IssueCategories.CodeQualityIssues,
	                  Severity=Severity.Warning)]
	public class DontUseLinqWhenItsVerboseAndInefficientDiagnosticAnalyzer : GatherVisitorCodeIssueProvider
	{
		class LinqMethod {
			internal string FullName;
			internal bool IsLast;
			/// <summary>
			/// Indicates that the method should be considered bad even when used alone.
			/// </summary>
			internal bool IsPoorStyleAlone;
			/// <summary>
			/// The number of parameters the definition has.
			/// </summary>
			internal int ParameterCount;
		}

		static readonly List<LinqMethod> LinqMethods = new List<LinqMethod> {
			new LinqMethod { FullName = "System.Linq.Enumerable.First", IsLast = true, ParameterCount = 1, IsPoorStyleAlone = true },
			new LinqMethod { FullName = "System.Linq.Enumerable.Last", IsLast = true, ParameterCount = 1 },
			new LinqMethod { FullName = "System.Linq.Enumerable.ElementAt", IsLast = true, ParameterCount = 2, IsPoorStyleAlone = true },
			new LinqMethod { FullName = "System.Linq.Enumerable.Count", IsLast = true, ParameterCount = 1, IsPoorStyleAlone = true },
			new LinqMethod { FullName = "System.Linq.Enumerable.Any", IsLast = true, ParameterCount = 1 },
			new LinqMethod { FullName = "System.Linq.Enumerable.Skip", ParameterCount = 2 },
			//Take(n) is problematic -- it has a weird behavior if n > Count()
			//new LinqMethod { FullName = "System.Linq.Enumerable.Take", ParameterCount = 2 },
			new LinqMethod { FullName = "System.Linq.Enumerable.Reverse", ParameterCount = 1 }
		};

		internal const string DiagnosticId  = "";
		const string Description            = "";
		const string MessageFormat          = "";
		const string Category               = IssueCategories.CodeQualityIssues;

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor (DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(Rule);
			}
		}

		protected override CSharpSyntaxWalker CreateVisitor (SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
		{
			return new GatherVisitor(semanticModel, addDiagnostic, cancellationToken);
		}

		class GatherVisitor : GatherVisitorBase<DontUseLinqWhenItsVerboseAndInefficientIssue>
		{
			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
				: base(semanticModel, addDiagnostic, cancellationToken)
			{
			}

			public override void VisitInvocationExpression(InvocationExpression invocationExpression)
			{
				InvocationExpression outerInvocationExpression = invocationExpression;
				//Note the invocations are in reverse order, so x.Foo().Bar() will have [0] be the Bar() and [1] be the Foo()
				List<InvocationExpression> invocations = new List<InvocationExpression>();
				LinqMethod outerMethod = null;
				Expression target = null;

				for (;;) {
					var resolveResult = ctx.Resolve(invocationExpression) as MemberResolveResult;
					if (resolveResult == null || !(resolveResult.Member is IMethod)) {
						break;
					}

					var method = LinqMethods.FirstOrDefault(candidate => candidate.FullName == resolveResult.Member.FullName &&
					                                        candidate.ParameterCount == ((IMethod) resolveResult.Member.MemberDefinition).Parameters.Count);
					if (method == null || (invocations.Any() && method.IsLast)) {
						break;
					}

					var mre = invocationExpression.Target as MemberReferenceExpression;
					if (mre == null) {
						break;
					}

					if (outerMethod == null) {
						outerMethod = method;
					}
					invocations.Add(invocationExpression);

					target = mre.Target;

					var newInvocation = target as InvocationExpression;
					if (newInvocation == null) {
						break;
					}

					invocationExpression = newInvocation;
				}

				if (target == null) {
					base.VisitInvocationExpression(invocationExpression);
					return;
				}
				if (!outerMethod.IsPoorStyleAlone && invocations.Count == 1) {
					base.VisitInvocationExpression(invocationExpression);
					return;
				}

				var currentTypeDeclaration = outerInvocationExpression.GetParent<TypeDeclaration>();
				var currentTypeResolveResult = ctx.Resolve(currentTypeDeclaration) as TypeResolveResult;
				if (currentTypeResolveResult == null) {
					base.VisitInvocationExpression(invocationExpression);
					return;
				}

				var currentTypeDefinition = currentTypeResolveResult.Type.GetDefinition();

				var targetResolveResult = ctx.Resolve(target);
				if (!CanIndex(currentTypeDefinition, targetResolveResult)) {
					base.VisitInvocationExpression(invocationExpression);
					return;
				}

				string countPropertyName = GetCountProperty(currentTypeDefinition, targetResolveResult);

				string lastInvocationName = ((MemberReferenceExpression)invocations[0].Target).MemberName;

				bool endsReversed = invocations.Count(invocation => ((MemberReferenceExpression)invocation.Target).MemberName == "Reverse") % 2 != 0;
				bool requiresCount = lastInvocationName == "Count" || lastInvocationName == "Any" ||
					(endsReversed ? lastInvocationName == "First" || lastInvocationName == "ElementAt" : lastInvocationName == "Last");

				if (countPropertyName == null && requiresCount) {
					base.VisitInvocationExpression(invocationExpression);
					return;
				}

				AddDiagnosticAnalyzer(new CodeIssue(invocations.Last().LParToken.StartLocation,
				                       invocations.First().RParToken.EndLocation,
				                       ctx.TranslateString("Use of Linq method when there's a better alternative"),
				                       ctx.TranslateString("Replace method by simpler version"),
				                       script => {

					Expression startOffset = null;
					Expression endOffset = null;

					bool reversed = false;
					foreach (var invocation in invocations.AsEnumerable().Reverse()) {
						string invocationName = ((MemberReferenceExpression)invocation.Target).MemberName;

						switch(invocationName) {
							case "Skip":
								Expression offset = reversed ? endOffset : startOffset;
								if (offset == null)
									offset = invocation.Arguments.Last().Clone();
								else
									offset = new BinaryOperatorExpression(offset,
									                                      BinaryOperatorType.Add,
									                                      invocation.Arguments.Last().Clone());

								if (reversed)
									endOffset = offset;
								else
									startOffset = offset;

								break;
							case "Reverse":
								reversed = !reversed;
								break;
							case "First":
							case "ElementAt":
							case "Last":
							{
								bool fromEnd = (invocationName == "Last") ^ reversed;
								Expression index = invocationName == "ElementAt" ? invocation.Arguments.Last().Clone() : null;
								Expression baseOffset = fromEnd ? endOffset : startOffset;
								//Our indexWithOffset is baseOffset + index
								//A baseOffset/index of null is considered "0".

								Expression indexWithOffset = baseOffset == null ? index :
									index == null ? baseOffset :
										new BinaryOperatorExpression(baseOffset, BinaryOperatorType.Add, index);

								Expression indexerExpression = indexWithOffset;
								if (fromEnd) {
									var endExpression = new BinaryOperatorExpression(new MemberReferenceExpression(target.Clone(), countPropertyName),
									                                                 BinaryOperatorType.Subtract,
									                                                 new PrimitiveExpression(1));
									if (indexerExpression == null) {
										indexerExpression = endExpression;
									} else {
										indexerExpression = new BinaryOperatorExpression(endExpression,
										                                                 BinaryOperatorType.Subtract,
										                                                 new ParenthesizedExpression(indexerExpression));
									}
								}

								indexerExpression = indexerExpression ?? new PrimitiveExpression(0);

								var newExpression = new IndexerExpression(target.Clone(),
								                                          indexerExpression);

								script.Replace(outerInvocationExpression, newExpression);
								break;
							}
							case "Count":
							case "Any":
							{
								Expression takenMembers;
								if (startOffset == null) {
									takenMembers = endOffset;
								} else if (endOffset == null) {
									takenMembers = startOffset;
								} else {
									takenMembers = new BinaryOperatorExpression(startOffset,
									                                            BinaryOperatorType.Add,
									                                            endOffset);
								}

								var countExpression = new MemberReferenceExpression(target.Clone(), countPropertyName);

								Expression newExpression;
								if (invocationName == "Count") {
									if (takenMembers == null)
										newExpression = countExpression;
									else
										newExpression = new BinaryOperatorExpression(countExpression,
										                                             BinaryOperatorType.Subtract,
										                                             new ParenthesizedExpression(takenMembers));
								} else {
									newExpression = new BinaryOperatorExpression(countExpression,
									                                             BinaryOperatorType.GreaterThan,
									                                             new ParenthesizedExpression(takenMembers));
								}

								script.Replace(outerInvocationExpression, newExpression);
								break;
							}
						}
					}
				}));

				base.VisitInvocationExpression(invocationExpression);
			}

			bool CanIndex(ITypeDefinition currentTypeDefinition, ResolveResult targetResolveResult)
			{
				if (targetResolveResult.Type is ArrayType) {
					return true;
				}

				var memberLookup = new MemberLookup(currentTypeDefinition, ctx.Compilation.MainAssembly);
				var indexers = memberLookup.LookupIndexers(targetResolveResult).ToList();

				return indexers.SelectMany(methodList => methodList).Any(
					member => ((IProperty)member).CanGet && ((IProperty)member).Getter.Parameters.Count == 1);
			}

			string GetCountProperty(ITypeDefinition currentTypeDefinition, ResolveResult targetResolveResult)
			{
				var memberLookup = new MemberLookup(currentTypeDefinition, ctx.Compilation.MainAssembly);

				string countProperty = TryProperty(memberLookup, targetResolveResult, "Count");
				if (countProperty != null) {
					return countProperty;
				}

				return TryProperty(memberLookup, targetResolveResult, "Length");
			}

			string TryProperty(MemberLookup memberLookup, ResolveResult targetResolveResult, string name)
			{
				var countResolveResult = memberLookup.Lookup(targetResolveResult, name, EmptyList<IType>.Instance, false);
				var countPropertyResolveResult = countResolveResult as MemberResolveResult;
				if (countPropertyResolveResult == null) {
					return null;
				}

				var property = countPropertyResolveResult.Member as IProperty;
				if (property == null || !property.CanGet) {
					return null;
				}

				return name;
			}
		}
	}

	[ExportCodeFixProvider(.DiagnosticId, LanguageNames.CSharp)]
	public class FixProvider : ICodeFixProvider
	{
		public IEnumerable<string> GetFixableDiagnosticIds()
		{
			yield return .DiagnosticId;
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