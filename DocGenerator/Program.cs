using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace DocGenerator
{
	class Program
	{
		static void Main(string[] args)
		{
			var types = typeof(ICSharpCode.NRefactory6.CSharp.DescriptionAttribute).Assembly.GetTypes()
				.Where(t => t.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(ExportCodeRefactoringProviderAttribute).FullName));

			foreach (var t in types) {
				Console.WriteLine("<li>{0} ({1})</li>", HttpUtility.HtmlEncode(GetDescription(t)), t.Name);
			}

			Console.ReadKey();
		}

		private static string GetDescription(Type t)
		{
			var description = t.GetCustomAttributes(false).OfType<ICSharpCode.NRefactory6.CSharp.DescriptionAttribute>().FirstOrDefault();
			if (description != null && description.Description.Length > 0)
				return description.Description;
			var exportAttribute = t.GetCustomAttributes(false).OfType<ExportCodeRefactoringProviderAttribute>().First();
			return exportAttribute.Name;
		}
	}
}
