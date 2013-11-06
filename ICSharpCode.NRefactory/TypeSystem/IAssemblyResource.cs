using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ICSharpCode.NRefactory.TypeSystem {
	public enum AssemblyResourceType {
		/// <summary>
		/// The resource is an embedded resource (created by the /resource csc command line)
		/// </summary>
		Embedded,
		/// <summary>
		/// The resource is a linked resource (created by the /linkresource csc command line)
		/// </summary>
		Linked,
	}

	public interface IAssemblyResource {
		/// <summary>
		/// Name of the resource.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Whether the resource is embedded or linked.
		/// </summary>
		AssemblyResourceType Type { get; }

		/// <summary>
		/// Name of the resource file for a linked resource.
		/// </summary>
		string LinkedFileName { get; }

		/// <summary>
		/// Whether the resource is public (as opposed to private).
		/// </summary>
		bool IsPublic { get; }

		/// <summary>
		/// Get the resource data as a stream. This method might throw an IOException if the resource is a linked resource and the file is not available.
		/// </summary>
		/// <returns></returns>
		Stream GetResourceStream();
	}
}
