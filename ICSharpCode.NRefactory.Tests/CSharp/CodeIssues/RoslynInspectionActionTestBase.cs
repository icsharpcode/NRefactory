//
// EmptyStatementIssueTests.cs
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
using System.Linq;
using NUnit.Framework;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Host.Mef;
using System.Reflection;

namespace ICSharpCode.NRefactory6.CSharp.CodeIssues
{
	public class RoslynInspectionActionTestBase
	{
		static MetadataReference mscorlib = new MetadataFileReference(typeof(Console).Assembly.Location);
		static MetadataReference systemAssembly = new MetadataFileReference(typeof(System.ComponentModel.BrowsableAttribute).Assembly.Location);
		static MetadataReference systemXmlLinq = new MetadataFileReference(typeof(System.Xml.Linq.XElement).Assembly.Location);
		static MetadataReference systemCore = new MetadataFileReference(typeof(Enumerable).Assembly.Location);
		internal static MetadataReference[] DefaultMetadataReferences = new MetadataReference[] {
			mscorlib,
			systemAssembly,
			systemCore,
			systemXmlLinq
		};
		
		static Dictionary<string, ICodeFixProvider> providers = new Dictionary<string, ICodeFixProvider>();

		static RoslynInspectionActionTestBase()
		{
			foreach (var provider in typeof(IssueCategories).Assembly.GetTypes().Where(t => t.GetCustomAttributes(typeof(ExportCodeFixProviderAttribute), false).Length > 0)) {
				var attr = (ExportCodeFixProviderAttribute)provider.GetCustomAttributes(typeof(ExportCodeFixProviderAttribute), false) [0];
				providers.Add(attr.Name, (ICodeFixProvider)Activator.CreateInstance(provider)); 
			}

		}

		public static string GetUniqueName()
		{
			return Guid.NewGuid().ToString("D");
		}

		public static CSharpCompilation CreateCompilation(
			IEnumerable<SyntaxTree> trees,
			IEnumerable<MetadataReference> references = null,
			CSharpCompilationOptions compOptions = null,
			string assemblyName = "")
		{
			if (compOptions == null) {
				compOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
			}

			return CSharpCompilation.Create(
				assemblyName == "" ? GetUniqueName() : assemblyName,
				trees,
				references,
				compOptions);
		}


		public static CSharpCompilation CreateCompilationWithMscorlib(
			IEnumerable<SyntaxTree> source,
			IEnumerable<MetadataReference> references = null,
			CSharpCompilationOptions compOptions = null,
			string assemblyName = "")
		{
			var refs = new List<MetadataReference>();
			if (references != null) {
				refs.AddRange(references);
			}

			refs.AddRange(DefaultMetadataReferences);

			return CreateCompilation(source, refs, compOptions, assemblyName);
		}

		internal class TestWorkspace : Workspace
		{
			readonly static MefHostServices services = MefHostServices.Create(new [] { 
				typeof(MefHostServices).Assembly,
				typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions).Assembly
			});
			
			
			public TestWorkspace(string workspaceKind = "Test") : base(services , workspaceKind)
			{
				foreach (var a in MefHostServices.DefaultAssemblies) {
					Console.WriteLine (a.FullName);
				}
			}
			
			public void ChangeDocument (DocumentId id, SourceText text)
			{
				ChangedDocumentText(id, text);
			}
			protected override void ChangedDocumentText(DocumentId id, SourceText text)
			{
				var document = CurrentSolution.GetDocument(id);
				if (document != null)
					OnDocumentTextChanged(id, text, PreservationMode.PreserveValue);
			}

			public void Open(ProjectInfo projectInfo)
			{
				var sInfo = SolutionInfo.Create(
					            SolutionId.CreateNewId(),
					            VersionStamp.Create(),
					            null,
					            new [] { projectInfo }
				            );
				OnSolutionAdded(sInfo);
			}
		}

		static void RunFix(Workspace workspace, ProjectId projectId, DocumentId documentId, Diagnostic diagnostic, int index = 0)
		{
			ICodeFixProvider provider;
			if (providers.TryGetValue(diagnostic.Id, out provider)) {
				var document = workspace.CurrentSolution.GetProject(projectId).GetDocument(documentId);
				var action = provider.GetFixesAsync(document, TextSpan.FromBounds(0, 0), new[] { diagnostic }, default(CancellationToken)).Result.ElementAtOrDefault(index);
				if (action == null)
					return;
				foreach (var op in action.GetOperationsAsync(default(CancellationToken)).Result) {
					op.Apply(workspace, default(CancellationToken));
				}
			}
		}

		protected static void TestWrongContext<T>(ISyntaxNodeAnalyzer<T> analyzer, string input)
		{
			Test(analyzer, input, 0);
		}
		
		protected static void Test<T>(ISyntaxNodeAnalyzer<T> analyzer, string input, int expectedDiagnostics = 1, string output = null, int issueToFix = -1, int actionToRun = 0)
		{
			var syntaxTree = CSharpSyntaxTree.ParseText(input);
			 
			var compilation = CreateCompilationWithMscorlib(new [] { syntaxTree });

			var diagnostics = new List<Diagnostic>();

			AnalyzerDriver.GetDiagnostics(compilation,
				System.Collections.Immutable.ImmutableArray<IDiagnosticAnalyzer>.Empty.Add(analyzer),
				CancellationToken.None
			); 
			
			Assert.AreEqual(expectedDiagnostics, diagnostics.Count);
			
			if (output == null)
				return;

			var workspace = new TestWorkspace();
			var projectId = ProjectId.CreateNewId();
			var documentId = DocumentId.CreateNewId(projectId);
			workspace.Open(ProjectInfo.Create(
				projectId,
				VersionStamp.Create(),
				"", "", LanguageNames.CSharp, null, null, null, null,
				new [] {
					DocumentInfo.Create(
						documentId, 
						"a.cs",
						null,
						SourceCodeKind.Regular,
						TextLoader.From(TextAndVersion.Create(SourceText.From(input), VersionStamp.Create())))
				}
			)); 
			if (issueToFix < 0) {
				foreach (var v in diagnostics) {
					RunFix(workspace, projectId, documentId, v);
				}
			} else {
				RunFix(workspace, projectId, documentId, diagnostics.ElementAt(issueToFix), actionToRun);
			}
			
			var txt = workspace.CurrentSolution.GetProject(projectId).GetDocument(documentId).GetTextAsync().Result.ToString();
			if (output != txt) {
				Console.WriteLine("expected:");
				Console.WriteLine(output);
				Console.WriteLine("got:");
				Console.WriteLine(txt);
				Assert.Fail();
			}
		}

		class TestDiagnosticAnalyzer<T> : IDiagnosticAnalyzer
		{
			readonly ISyntaxNodeAnalyzer<T> t;

			public TestDiagnosticAnalyzer(ISyntaxNodeAnalyzer<T> t)
			{
				this.t = t;
			}

			#region IDiagnosticAnalyzer implementation

			System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> IDiagnosticAnalyzer.SupportedDiagnostics {
				get {
					return t.SupportedDiagnostics;
				}
			}

			#endregion
		}
	}

}

