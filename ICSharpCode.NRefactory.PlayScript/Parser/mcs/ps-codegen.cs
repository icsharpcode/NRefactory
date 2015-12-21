// Copyright 2013 Zynga Inc.
//	
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//		
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.

using System;
//using Mono.CSharp;
using System.IO;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.NRefactory.MonoCSharp;

namespace ICSharpCode.NRefactory.MonoPlayScript
{
	public static class CodeGenerator
	{
		public static void GenerateCode (ModuleContainer module, ParserSession session, Report report)
		{
			GenerateDynamicPartialClasses(module, session, report);
			if (report.Errors > 0)
				return;

			GenerateEmbedClasses(module, session, report);
			if (report.Errors > 0)
				return;

		}

		public static void FindDynamicClasses(TypeContainer container, List<Class> classes) 
		{
			foreach (var cont in container.Containers) {
				if (cont is Class) {

					// Is class marked as dynamic?
					var cl = cont as Class;
					if (cl.IsAsDynamicClass && !(cl.BaseType != null && cl.BaseType.IsAsDynamicClass)) {
						classes.Add ((Class)cont);
					}
				}

				// Recursively find more classes
				if (cont.Containers != null)
					FindDynamicClasses(cont, classes);
			}
		}

		public static void GenerateDynamicPartialClasses(ModuleContainer module, ParserSession session, Report report)
		{
			List<Class> classes = new List<Class>();
			FindDynamicClasses(module, classes);

			if (classes.Count == 0)
				return;

			var os = new StringWriter();

			os.Write (@"
// Generated dynamic class partial classes

");

			foreach (var cl in classes) {
				os.Write (@"
namespace {1} {{

	partial class {2} : PlayScript.IDynamicClass {{

		private PlayScript.IDynamicClass __dynamicProps;

		dynamic PlayScript.IDynamicClass.__GetDynamicValue(string name) {{
			object value = null;
			if (__dynamicProps != null) {{
				value = __dynamicProps.__GetDynamicValue(name);
			}}
			return value;
		}}

		bool PlayScript.IDynamicClass.__TryGetDynamicValue(string name, out object value) {{
			if (__dynamicProps != null) {{
				return __dynamicProps.__TryGetDynamicValue(name, out value);
			}} else {{
				value = PlayScript.Undefined._undefined;
				return false;
			}}
		}}
			
		void PlayScript.IDynamicClass.__SetDynamicValue(string name, object value) {{
			if (__dynamicProps == null) {{
				__dynamicProps = new PlayScript.DynamicProperties(this);
			}}
			__dynamicProps.__SetDynamicValue(name, value);
		}}

		bool PlayScript.IDynamicClass.__DeleteDynamicValue(object name) {{
			if (__dynamicProps != null) {{
				return __dynamicProps.__DeleteDynamicValue(name);
			}}
			return false;
		}}
			
		bool PlayScript.IDynamicClass.__HasDynamicValue(string name) {{
			if (__dynamicProps != null) {{
				return __dynamicProps.__HasDynamicValue(name);
			}}
			return false;
		}}

		System.Collections.IEnumerable PlayScript.IDynamicClass.__GetDynamicNames() {{
			if (__dynamicProps != null) {{
				return __dynamicProps.__GetDynamicNames();
			}}
			return null;
		}}
	}}
}}

", PsConsts.PsRootNamespace, ((ITypeDefinition)cl).Namespace, cl.MemberName.Basename);
			}

			string fileStr = os.ToString();
			String path;
			//TODO: This is an ugly use of try/catch and needs cleaned up
			try {
				path = Path.Combine (Path.GetDirectoryName (Path.GetFullPath (module.Compiler.Settings.OutputFile)), "dynamic.g.cs");
			} catch {
				path = Path.Combine (Path.GetTempPath (), "dynamic.g.cs");
			}
			File.WriteAllText(path, fileStr);

			byte[] byteArray = Encoding.ASCII.GetBytes( fileStr );
			var input = new MemoryStream( byteArray, false );
			var reader = new SeekableStreamReader (input, System.Text.Encoding.UTF8);

			SourceFile file = new SourceFile(path, path, 0);
			file.FileType = SourceFileType.CSharp;

			Driver.Parse (reader, file, module, session, report);

		}

		private static int _embedCount = 1;

		private class EmbedData {
			public int _index;
			public string _className;
			public Field _field;

			public string source = "null";
			public string mimeType = "null";
			public string embedAsCFF = "null";
			public string fontFamily = "null";
			public string symbol = "null";
		}

		private static void FindEmbedFields(ModuleContainer module, ClassOrStruct cl, List<EmbedData> embeds) {
			foreach (var m in cl.Members) {
				var f = m as Field;
				if (f == null || f.OptAttributes == null || f.OptAttributes.Attrs.Count == 0)
					continue;

				if (!(f.TypeExpression is TypeExpression) || f.TypeExpression.Type != module.Compiler.BuiltinTypes.Type)
					continue;

				ICSharpCode.NRefactory.MonoPlayScript.Attribute embedAttr = null;
				foreach (var attr in f.OptAttributes.Attrs) {
					if (attr.Name == "Embed") {
						embedAttr = attr;
						break;
					}
				}

				if (embedAttr == null)
					continue;

				var e = new EmbedData();
				e._index = _embedCount;
				_embedCount++;
				e._className = "__EmbedLoader" + e._index;
				e._field = f;

				e.source = e.mimeType = e.embedAsCFF = e.fontFamily = e.symbol = "null";

				foreach (NamedArgument arg in embedAttr.NamedArguments) {
					if (!(arg.Expr is StringLiteral))
						continue;
					var s = ((StringLiteral)(arg.Expr)).GetValueAsLiteral();
					switch (arg.Name) {
					case "source": e.source = s; break;
					case "mimeType": e.mimeType = s; break;
					case "embedAsCFF": e.embedAsCFF = s; break;
					case "fontFamily": e.fontFamily = s; break;
					case "symbol": e.symbol = s; break;
					}
				}

				embeds.Add (e);

			}
		}

		private static void FindEmbedClasses(ModuleContainer module, TypeContainer container, List<EmbedData> embeds) 
		{
			foreach (var cont in container.Containers) {
				if (cont is ClassOrStruct) {
					
					// Is class marked as dynamic?
					var cl = cont as ClassOrStruct;
					FindEmbedFields (module, cl, embeds);
				}
				
				// Recursively find more classes
				if (cont.Containers != null)
					FindEmbedClasses(module, cont, embeds);
			}
		}

		public static void GenerateEmbedClasses(ModuleContainer module, ParserSession session, Report report)
		{
			List<EmbedData> embeds = new List<EmbedData>();
			FindEmbedClasses(module, module, embeds);
			if (embeds.Count == 0)
				return;

			var os = new StringWriter();
			
			os.Write (@"
// Generated embed loader classes

");

			foreach (var e in embeds) {

				var loc = e._field.Location;

				e._field.Initializer = new TypeOf(new MemberAccess(new SimpleName("_embed_loaders", loc), e._className), loc);

				os.Write (@"
namespace _embed_loaders {{

	internal class {1} : PlayScript.EmbedLoader {{

		public {1}() : base({2}, {3}, {4}, {5}, {6}) {{
		}}
	}}
}}

", PsConsts.PsRootNamespace, e._className, e.source, e.mimeType, e.embedAsCFF, e.fontFamily, e.symbol);
			}
			
			string fileStr = os.ToString();
			//TODO: This is an ugly use of try/catch and needs cleaned up
			String path;
			try {
				path = Path.Combine (Path.GetDirectoryName (Path.GetFullPath (module.Compiler.Settings.OutputFile)), "embed.g.cs");
			} catch {
				path = Path.Combine (Path.GetTempPath (), "embed.g.cs");
			}

			File.WriteAllText(path, fileStr);
			
			byte[] byteArray = Encoding.ASCII.GetBytes( fileStr );
			var input = new MemoryStream( byteArray, false );
			var reader = new SeekableStreamReader (input, System.Text.Encoding.UTF8);
			
			SourceFile file = new SourceFile(path, path, 0);
			file.FileType = SourceFileType.CSharp;
			
			Driver.Parse (reader, file, module, session, report);
		}

	}


}

