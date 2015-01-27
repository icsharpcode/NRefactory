using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NR6Pack.BuildTools
{
	class Program
	{
		static void Main(string[] args)
		{
			bool isDebug = (args.FirstOrDefault() ?? "") == "Debug";
			string basePath = @"..\NR6Pack.Vsix\";
			string templateFile = basePath + "source.extension.template.vsixmanifest";
			string targetFile = basePath + "source.extension.vsixmanifest";
			int version = isDebug ? new Random().Next(65534) + 1 : 0;
			File.WriteAllText(targetFile, File.ReadAllText(templateFile).Replace("%%version%%", version.ToString()));
		}
	}
}
