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

namespace ICSharpCode.NRefactory6.CSharp.CodeIssues
{
	public class RoslynInspectionActionTestBase 
	{
		static MetadataReference mscorlib = new MetadataFileReference (typeof(Console).Assembly.Location);
		static MetadataReference systemAssembly = new MetadataFileReference (typeof(System.ComponentModel.BrowsableAttribute).Assembly.Location);
		static MetadataReference systemXmlLinq = new MetadataFileReference (typeof(System.Xml.Linq.XElement).Assembly.Location);
		static MetadataReference systemCore = new MetadataFileReference (typeof(Enumerable).Assembly.Location);
		internal static MetadataReference[] DefaultMetadataReferences = new MetadataReference[] {
			mscorlib,
			systemAssembly,
			systemCore,
			systemXmlLinq
		};

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
			if (compOptions == null)
			{
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
			if (references != null)
			{
				refs.AddRange(references);
			}

			refs.AddRange(DefaultMetadataReferences);

			return CreateCompilation(source, refs, compOptions, assemblyName);
		}

		protected static void Test<T> (ISyntaxNodeAnalyzer<T> analyzer, string input, int expectedDiagnostics = 1, string output = null)
		{
			var syntaxTree = CSharpSyntaxTree.ParseText(input);
			 
			var compilation = CreateCompilationWithMscorlib(new [] { syntaxTree });

			var diagnostics = new List<Diagnostic>();

			AnalyzerDriver.RunAnalyzers(compilation.GetSemanticModel(syntaxTree),
				new Microsoft.CodeAnalysis.Text.TextSpan (0, syntaxTree.Length),
				System.Collections.Immutable.ImmutableArray<IDiagnosticAnalyzer>.Empty.Add(analyzer),
				diagnostics.Add
			); 

			Assert.AreEqual(expectedDiagnostics, diagnostics.Count);
			foreach (var v in diagnostics) {
				Console.WriteLine (v);
			
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
			IEnumerable<DiagnosticDescriptor> IDiagnosticAnalyzer.GetSupportedDiagnostics()
			{
				return t.GetSupportedDiagnostics();
			}
			#endregion
		}
	}

}

