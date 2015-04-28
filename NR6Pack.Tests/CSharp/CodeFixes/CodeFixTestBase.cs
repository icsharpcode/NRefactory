// 
// ContextActionTestBase.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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
using NUnit.Framework;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using System.Threading;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using ICSharpCode.NRefactory6.CSharp.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace ICSharpCode.NRefactory6.CSharp.CodeFixes
{
	public abstract class CodeFixTestBase
	{
		[SetUp]
		public virtual void SetUp()
		{
		}
		
		internal static string HomogenizeEol (string str)
		{
			var sb = new StringBuilder ();
			for (int i = 0; i < str.Length; i++) {
				var ch = str [i];
				var possibleNewline = NewLine.GetDelimiterLength(ch, i + 1 < str.Length ? str [i + 1] : '\0');
				if (possibleNewline > 0) {
					sb.AppendLine ();
					if (possibleNewline == 2)
						i++;
				} else {
					sb.Append (ch);
				}
			}
			return sb.ToString ();
		}

		public void Test<T> (string input, string output, int action = 0, bool expectErrors = false, CSharpParseOptions parseOptions = null)
			where T : CodeFixProvider, new ()
		{
			Test(new T(), input, output, action, expectErrors, parseOptions);
		}
		
		public void Test (CodeFixProvider provider, string input, string output, int action = 0, bool expectErrors = false, CSharpParseOptions parseOptions = null)
		{
			string result = HomogenizeEol (RunContextAction (provider, HomogenizeEol (input), action, expectErrors, parseOptions));
			bool passed = result == output;
			if (!passed) {
				Console.WriteLine ("-----------Expected:");
				Console.WriteLine (output);
				Console.WriteLine ("-----------Got:");
				Console.WriteLine (result);
			}
			Assert.AreEqual (HomogenizeEol (output), result);
		}

		internal static List<Microsoft.CodeAnalysis.CodeActions.CodeAction> GetActions<T>(string input) where T : CodeFixProvider, new ()
		{
			InspectionActionTestBase.TestWorkspace workspace;
			Document doc;
			return GetActions(new T(), input, out workspace, out doc);
		}

		static string ParseText(string input, out TextSpan selectedSpan)
		{
			int start = -1, end = -1;
			var result = new StringBuilder(input.Length);
			int upper = input.Length;
			for (int i = 0; i < upper; i++) {
				var ch = input [i];
				if (ch == '$' && start < 0) {
					start = i;
					continue;
				} else if (ch == '$' && end < 0) {
					end = i - 1;
					continue;
				}
				result.Append(ch);
			}

			if (start < 0) {
				selectedSpan = TextSpan.FromBounds (0, 0);
            } else {
				selectedSpan = TextSpan.FromBounds (start, end);
			}
			return result.ToString();
		}

		static List<CodeAction> GetActions(CodeFixProvider action, string input, out InspectionActionTestBase.TestWorkspace workspace, out Document doc, CSharpParseOptions parseOptions = null)
		{
			TextSpan selectedSpan;
			string text = ParseText(input, out selectedSpan);
			workspace = new InspectionActionTestBase.TestWorkspace();
			var projectId = ProjectId.CreateNewId();
			var documentId = DocumentId.CreateNewId(projectId);
			if (parseOptions == null) {
				parseOptions = new CSharpParseOptions (
					LanguageVersion.CSharp6,
					DocumentationMode.Diagnose | DocumentationMode.Parse,
					SourceCodeKind.Regular,
					ImmutableArray.Create ("DEBUG", "TEST")
				);
			}
			workspace.Options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, false);
			workspace.Open(ProjectInfo.Create(
				projectId,
				VersionStamp.Create(),
				"TestProject",
				"TestProject",
				LanguageNames.CSharp,
				null,
				null,
				new CSharpCompilationOptions (
					OutputKind.DynamicallyLinkedLibrary,
					"",
					"",
					"Script",
					null,
					OptimizationLevel.Debug,
					false,
					true
				),
				parseOptions,
				new [] {
					DocumentInfo.Create(
						documentId,
						"a.cs",
						null,
						SourceCodeKind.Regular,
						TextLoader.From(TextAndVersion.Create(SourceText.From(text), VersionStamp.Create())) 
					)
				},
				null,
				InspectionActionTestBase.DefaultMetadataReferences
			)
			);
			doc = workspace.CurrentSolution.GetProject(projectId).GetDocument(documentId);
			var actions = new List<Tuple<CodeAction, ImmutableArray<Diagnostic>>> ();
			var model = doc.GetSemanticModelAsync ().Result;
			var diagnostics = model.GetDiagnostics ();
			if (diagnostics.Length == 0)
				return new List<CodeAction> ();
			
			foreach (var d in diagnostics) {
				if (action.FixableDiagnosticIds.Contains (d.Id)) {

					if (selectedSpan.Start > 0)
						Assert.AreEqual (selectedSpan, d.Location.SourceSpan, "Activation span does not match.");
					
					var context = new CodeFixContext (doc, d.Location.SourceSpan, diagnostics.Where (d2 => d2.Location.SourceSpan == d.Location.SourceSpan).ToImmutableArray (), (arg1, arg2) => actions.Add (Tuple.Create (arg1, arg2)), default(CancellationToken));
					action.RegisterCodeFixesAsync (context);
				}
			}


			return actions.Select (a => a.Item1).ToList ();
		}

		protected string RunContextAction (CodeFixProvider action, string input, int actionIndex = 0, bool expectErrors = false, CSharpParseOptions parseOptions = null)
		{
			Document doc;
			InspectionActionTestBase.TestWorkspace workspace;
			var actions = GetActions(action, input, out workspace, out doc, parseOptions);
			if (actions.Count < actionIndex)
				Console.WriteLine ("invalid input is:" + input);
			var a = actions[actionIndex];
			foreach (var op in a.GetOperationsAsync(default(CancellationToken)).Result) {
				op.Apply(workspace, default(CancellationToken));
			}
			return workspace.CurrentSolution.GetDocument(doc.Id).GetTextAsync().Result.ToString();
		}
		
		
		protected void TestWrongContext (CodeFixProvider action, string input)
		{
			Document doc;
			ICSharpCode.NRefactory6.CSharp.Diagnostics.InspectionActionTestBase.TestWorkspace workspace;
			var actions = GetActions(action, input, out workspace, out doc);
			Assert.IsTrue (actions == null || actions.Count == 0, action.GetType () + " shouldn't be valid there.");
		}


		protected void TestWrongContext<T> (string input) where T : CodeFixProvider, new ()
		{
			TestWrongContext(new T(), input);
		}

//		protected List<CodeAction> GetActions<T> (string input) where T : CodeActionProvider, new ()
//		{
//			var ctx = TestRefactoringContext.Create(input);
//			ctx.FormattingOptions = formattingOptions;
//			return new T().GetActions(ctx).ToList();
//		}
//
//		protected void TestActionDescriptions (CodeActionProvider provider, string input, params string[] expected)
//		{
//			var ctx = TestRefactoringContext.Create(input);
//			ctx.FormattingOptions = formattingOptions;
//			var actions = provider.GetActions(ctx).ToList();
//			Assert.AreEqual(
//				expected,
//				actions.Select(a => a.Description).ToArray());
//		}
	}
}
