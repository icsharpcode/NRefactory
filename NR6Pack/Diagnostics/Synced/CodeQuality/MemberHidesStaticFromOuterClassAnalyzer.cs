//
// MemberHidesStaticFromOuterClass.cs
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
	[NotPortedYet]
	public class MemberHidesStaticFromOuterClassAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			NRefactoryDiagnosticIDs.MemberHidesStaticFromOuterClassAnalyzerID, 
			GettextCatalog.GetString("Member hides static member from outer class"),
			GettextCatalog.GetString("{0} '{1}' hides {2} from outer class"), 
			DiagnosticAnalyzerCategories.CodeQualityIssues, 
			DiagnosticSeverity.Warning, 
			isEnabledByDefault: true,
			helpLinkUri: HelpLink.CreateFor(NRefactoryDiagnosticIDs.MemberHidesStaticFromOuterClassAnalyzerID)
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
			if (nodeContext.IsFromGeneratedCode())
				return false;
			//var node = nodeContext.Node as ;
			//diagnostic = Diagnostic.Create (descriptor, node.GetLocation ());
			//return true;
			return false;
		}

//		class GatherVisitor : GatherVisitorBase<MemberHidesStaticFromOuterClassAnalyzer>
//		{
//			//readonly List<List<IMember>> staticMembers = new List<List<IMember>>();

//			public GatherVisitor(SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
//				: base (semanticModel, addDiagnostic, cancellationToken)
//			{
//			}

////			public override void VisitBlockStatement(BlockStatement blockStatement)
////			{
////				// SKIP
////			}
////
////			public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
////			{
////				var rr = ctx.Resolve(typeDeclaration);
////
////				staticMembers.Add(new List<IMember>(rr.Type.GetMembers(m => m.IsStatic)));
////				base.VisitTypeDeclaration(typeDeclaration);
////				staticMembers.RemoveAt(staticMembers.Count - 1); 
////			}
////
////			void Check(string name, AstNode nodeToMark, string memberType)
////			{
////				for (int i = 0; i < staticMembers.Count - 1; i++) {
////					var member = staticMembers[i].FirstOrDefault(m => m.Name == name);
////					if (member == null)
////						continue;
////					string outerMemberType;
////					switch (member.SymbolKind) {
////						case SymbolKind.Field:
////							outerMemberType = ctx.TranslateString("field");
////							break;
////						case SymbolKind.Property:
////							outerMemberType = ctx.TranslateString("property");
////							break;
////						case SymbolKind.Event:
////							outerMemberType = ctx.TranslateString("event");
////							break;
////						case SymbolKind.Method:
////							outerMemberType = ctx.TranslateString("method");
////							break;
////						default:
////							outerMemberType = ctx.TranslateString("member");
////							break;
////					}
////					AddDiagnosticAnalyzer(new CodeIssue(nodeToMark,
//			//						string.Format(ctx.TranslateString("{0} '{1}' hides {2} from outer class"),
////							memberType, member.Name, outerMemberType)));
////					return;
////				}
////			}
////
////			public override void VisitEventDeclaration(EventDeclaration eventDeclaration)
////			{
////				foreach (var init in eventDeclaration.Variables) {
////					Check(init.Name, init.NameToken, ctx.TranslateString("Event"));
////				}
////			}
////
////			public override void VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration)
////			{
////				Check(eventDeclaration.Name, eventDeclaration.NameToken, ctx.TranslateString("Event"));
////			}
////
////			public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
////			{
////				foreach (var init in fieldDeclaration.Variables) {
////					Check(init.Name, init.NameToken, ctx.TranslateString("Field"));
////				}
////			}
////
////			public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
////			{
////				Check(propertyDeclaration.Name, propertyDeclaration.NameToken, ctx.TranslateString("Property"));
////			}
////
////			public override void VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration)
////			{
////				Check(fixedFieldDeclaration.Name, fixedFieldDeclaration.NameToken, ctx.TranslateString("Fixed field"));
////			}
////
////			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
////			{
////				Check(methodDeclaration.Name, methodDeclaration.NameToken, ctx.TranslateString("Method"));
////			}
//		}
	}
}