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

namespace DocGenerator
{
	class Program
	{
		static void Main(string[] args)
		{
			var codeActions = typeof(ICSharpCode.NRefactory6.CSharp.DescriptionAttribute).Assembly.GetTypes()
				.Where(t => t.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(ExportCodeRefactoringProviderAttribute).FullName));

			var codeIssues = typeof(ICSharpCode.NRefactory6.CSharp.DescriptionAttribute).Assembly.GetTypes()
				.Where(t => t.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(DiagnosticAnalyzerAttribute).FullName));

			XDocument codeActionsDocument = XDocument.Load(@"..\NR6Pack\CodeActions.html.template");
			var codeActionsNode = codeActionsDocument.Descendants("{http://www.w3.org/1999/xhtml}ul").First();

			foreach (var codeAction in codeActions) {
				codeActionsNode.Add(new XElement("{http://www.w3.org/1999/xhtml}li", string.Format("{0} ({1})", GetActionDescription(codeAction), codeAction.Name)));
			}

			codeActionsDocument.Save(@"..\NR6Pack\CodeActions.html");

			XDocument codeIssuesDocument = XDocument.Load(@"..\NR6Pack\CodeIssues.html.template");
			var codeIssuesNode = codeIssuesDocument.Descendants("{http://www.w3.org/1999/xhtml}ul").First();

			foreach (var codeIssue in codeIssues) {
				codeIssuesNode.Add(new XElement("{http://www.w3.org/1999/xhtml}li", string.Format("{0} ({1})", GetIssueDescription(codeIssue), codeIssue.Name)));
			}

			codeIssuesDocument.Save(@"..\NR6Pack\CodeIssues.html");
		}

		private static string GetActionDescription(Type t)
		{
			var description = t.GetCustomAttributes(false).OfType<ICSharpCode.NRefactory6.CSharp.DescriptionAttribute>().FirstOrDefault();
			if (description != null && description.Description.Length > 0)
				return description.Description;
			var exportAttribute = t.GetCustomAttributes(false).OfType<ExportCodeRefactoringProviderAttribute>().First();
			return exportAttribute.Name;
		}

		private static string GetIssueDescription(Type t)
		{
			var description = t.GetCustomAttributes(false).OfType<ICSharpCode.NRefactory6.CSharp.DescriptionAttribute>().FirstOrDefault();
			if (description != null && description.Description.Length > 0)
				return description.Description;
            return t.GetField("Description", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null)?.ToString() ?? "";
		}
	}
}
