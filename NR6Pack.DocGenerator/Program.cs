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
using Microsoft.CodeAnalysis.CodeFixes;

namespace NR6Pack.DocGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var missingMDWriter = new StreamWriter(@"..\NR6Pack\missing.md", false, Encoding.UTF8))
            {
                missingMDWriter.WriteLine("Things not ported yet");
                missingMDWriter.WriteLine("=====================");
                missingMDWriter.WriteLine("");

                var codeRefactorings = typeof(ICSharpCode.NRefactory6.CSharp.NotPortedYetAttribute).Assembly.GetTypes()
                    .Where(t => t.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(ExportCodeRefactoringProviderAttribute).FullName))
                    .OrderBy(t => t.Name)
                    .ToArray();

                var codeAnalyzers = typeof(ICSharpCode.NRefactory6.CSharp.NotPortedYetAttribute).Assembly.GetTypes()
                    .Where(t => t.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(DiagnosticAnalyzerAttribute).FullName))
                    .OrderBy(t => t.Name)
                    .ToArray();

                var codeFixes = typeof(ICSharpCode.NRefactory6.CSharp.NotPortedYetAttribute).Assembly.GetTypes()
                    .Where(t => t.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(ExportCodeFixProviderAttribute).FullName))
                    .OrderBy(t => t.Name)
                    .ToArray();

                missingMDWriter.WriteLine("*Refactorings*");
                missingMDWriter.WriteLine("");

                var codeRefactoringsDocument = XDocument.Load(@"..\NR6Pack\CodeRefactorings.html.template");
                var codeRefactoringsNode = codeRefactoringsDocument.Descendants("{http://www.w3.org/1999/xhtml}ul").First();
                var codeRefactoringsCountNode = codeRefactoringsDocument.Descendants("{http://www.w3.org/1999/xhtml}p").First();
                int codeRefactoringsCount = 0;

                foreach (var codeRefactoring in codeRefactorings)
                {
                    if (codeRefactoring.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(ICSharpCode.NRefactory6.CSharp.NotPortedYetAttribute).FullName))
                    {
                        missingMDWriter.WriteLine(string.Format("* {0}", codeRefactoring.Name));
                    }
                    else
                    {
                        var description = GetRefactoringDescription(codeRefactoring);
                        var line = (description == null) ? string.Format("{0}", codeRefactoring.Name) : string.Format("{0} ({1})", description, codeRefactoring.Name);
                        codeRefactoringsNode.Add(new XElement("{http://www.w3.org/1999/xhtml}li", line));
                        codeRefactoringsCount++;
                    }
                }

                codeRefactoringsCountNode.Value = string.Format("{0} code refactorings available!", codeRefactoringsCount);

                missingMDWriter.WriteLine("");
                codeRefactoringsDocument.Save(@"..\NR6Pack\CodeRefactorings.html");

                missingMDWriter.WriteLine("*Analyzers*");
                missingMDWriter.WriteLine("");

                var codeAnalyzersDocument = XDocument.Load(@"..\NR6Pack\CodeAnalyzers.html.template");
                var codeAnalyzersNode = codeAnalyzersDocument.Descendants("{http://www.w3.org/1999/xhtml}ul").First();
                var codeAnalyzersCountNode = codeAnalyzersDocument.Descendants("{http://www.w3.org/1999/xhtml}p").First();
                int codeAnalyzersCount = 0;

                foreach (var codeAnalyzer in codeAnalyzers)
                {
                    if (codeAnalyzer.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(ICSharpCode.NRefactory6.CSharp.NotPortedYetAttribute).FullName))
                    {
                        missingMDWriter.WriteLine(string.Format("* {0}", codeAnalyzer.Name));
                    }
                    else
                    {
                        var description = GetAnalyzerDescription(codeAnalyzer);
                        var line = (description == null) ? string.Format("{0}", codeAnalyzer.Name) : string.Format("{0} ({1})", description, codeAnalyzer.Name);
                        codeAnalyzersNode.Add(new XElement("{http://www.w3.org/1999/xhtml}li", line));
                        codeAnalyzersCount++;
                    }
                }

                codeAnalyzersCountNode.Value = string.Format("{0} code analyzers available!", codeAnalyzersCount);

                missingMDWriter.WriteLine("");
                codeAnalyzersDocument.Save(@"..\NR6Pack\CodeAnalyzers.html");

                var codeFixesDocument = XDocument.Load(@"..\NR6Pack\CodeFixes.html.template");
                var codeFixesNode = codeFixesDocument.Descendants("{http://www.w3.org/1999/xhtml}ul").First();
                var codeFixesCountNode = codeFixesDocument.Descendants("{http://www.w3.org/1999/xhtml}p").First();
                int codeFixesCount = 0;

                foreach (var codeFix in codeFixes)
                {
                    if (!codeFix.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(ICSharpCode.NRefactory6.CSharp.NotPortedYetAttribute).FullName))
                    {
                        if (CodeFixRelatesToNRAnalyzer(codeFix))
                        {
                            codeFixesNode.Add(new XElement("{http://www.w3.org/1999/xhtml}li", string.Format("{0}", codeFix.Name)));
                            codeFixesCount++;
                        }
                    }
                }

                codeFixesCountNode.Value = string.Format("{0} code fixes available!", codeFixesCount);

                codeFixesDocument.Save(@"..\NR6Pack\CodeFixes.html");
            }
        }

        static string GetRefactoringDescription(Type t)
        {
            var exportAttribute = t.GetCustomAttributes(false).OfType<ExportCodeRefactoringProviderAttribute>().First();
            return exportAttribute.Name;
        }

        static string GetAnalyzerDescription(Type t)
        {
            var descriptor = t.GetField("descriptor", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) as Microsoft.CodeAnalysis.DiagnosticDescriptor;
            return descriptor?.Title.ToString();
        }

        static bool CodeFixRelatesToNRAnalyzer(Type codeFixType)
        {
            var codeFixInstance = Activator.CreateInstance(codeFixType) as CodeFixProvider;
            if (codeFixInstance != null)
            {
                if (!codeFixInstance.FixableDiagnosticIds.Any(id => id.StartsWith("NR")))
                {
                    // Try to find an appropriate analyzer class in NR6Pack
                    string analyzerClassName = codeFixType.FullName.Replace("CodeFixProvider", "Analyzer");
                    var analyzerClass = codeFixType.Assembly.GetType(analyzerClassName, false);
                    return analyzerClass == null;
                }
            }

            return false;
        }
    }
}
