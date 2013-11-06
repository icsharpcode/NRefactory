using System;
using System.IO;

namespace ICSharpCode.NRefactory.TypeSystem {
	[Serializable]
	public class LinkedResource : IAssemblyResource {
		private string _name;
		private string _filename;
		private string _basepath;
		private bool _isPublic;

		public string Name { get { return _name; } }

		public string LinkedFileName { get { return _filename; } }

		public AssemblyResourceType Type { get { return AssemblyResourceType.Linked; } }

		public bool IsPublic { get { return _isPublic; } }

		public LinkedResource(string name, string filename, string basepath, bool isPublic) {
			_name = name;
			_filename = filename;
			_basepath = basepath;
			_isPublic = isPublic;
		}

		public Stream GetResourceStream() {
			return File.Open(Path.Combine(_basepath, _filename), FileMode.Open, FileAccess.Read);
		}
	}
}