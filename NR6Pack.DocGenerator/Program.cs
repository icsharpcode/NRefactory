using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Reflection;

namespace NR6Pack.DocGenerator
{
	class Program
	{
		static void Main(string[] args)
		{
			var codeRefactorings = typeof(ICSharpCode.NRefactory6.CSharp.Diagnostics.NRefactoryDiagnosticIDs).Assembly.GetTypes()
				.Where(t => t.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(ExportCodeRefactoringProviderAttribute).FullName))
				.ToArray();

			var codeAnalyzers = typeof(ICSharpCode.NRefactory6.CSharp.Diagnostics.NRefactoryDiagnosticIDs).Assembly.GetTypes()
				.Where(t => t.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(DiagnosticAnalyzerAttribute).FullName))
				.ToArray();

			XDocument codeRefactoringsDocument = XDocument.Load(@"..\NR6Pack\CodeActions.html.template");
			var codeRefactoringsNode = codeRefactoringsDocument.Descendants("{http://www.w3.org/1999/xhtml}ul").First();
			var codeRefactoringsCountNode = codeRefactoringsDocument.Descendants("{http://www.w3.org/1999/xhtml}p").First();
			codeRefactoringsCountNode.Value = string.Format("{0} code refactorings available!", codeRefactorings.Length);

			foreach (var codeRefactoring in codeRefactorings)
			{
				string description = GetRefactoringDescription(codeRefactoring);
				string line = (description == null) ? string.Format("{0}", codeRefactoring.Name) : string.Format("{0} ({1})", description, codeRefactoring.Name);
				codeRefactoringsNode.Add(new XElement("{http://www.w3.org/1999/xhtml}li", line));
			}

			codeRefactoringsDocument.Save(@"..\NR6Pack\CodeActions.html");

			XDocument codeAnalyzersDocument = XDocument.Load(@"..\NR6Pack\CodeIssues.html.template");
			var codeAnalyzersNode = codeAnalyzersDocument.Descendants("{http://www.w3.org/1999/xhtml}ul").First();
			var codeAnalyzersCountNode = codeAnalyzersDocument.Descendants("{http://www.w3.org/1999/xhtml}p").First();
			codeAnalyzersCountNode.Value = string.Format("{0} code analyzers available!", codeAnalyzers.Length);

			foreach (var codeAnalyzer in codeAnalyzers)
			{
				string description = GetAnalyzerDescription(codeAnalyzer);
				string line = (description == null) ? string.Format("{0}", codeAnalyzer.Name) : string.Format("{0} ({1})", description, codeAnalyzer.Name);
				codeAnalyzersNode.Add(new XElement("{http://www.w3.org/1999/xhtml}li", line));
			}

			codeAnalyzersDocument.Save(@"..\NR6Pack\CodeIssues.html");
		}

		private static string GetRefactoringDescription(Type t)
		{
			var exportAttribute = t.GetCustomAttributes(false).OfType<ExportCodeRefactoringProviderAttribute>().First();
			return exportAttribute.Name;
		}

		private static string GetAnalyzerDescription(Type t)
		{
			var descriptor = t.GetField("descriptor", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) as Microsoft.CodeAnalysis.DiagnosticDescriptor;
			return descriptor?.Title.ToString();
		}
	}
}
