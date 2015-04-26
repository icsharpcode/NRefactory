using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICSharpCode.NRefactory6.CSharp
{
	/// <summary>
	/// Marker attribute for not yet ported analyzers or refactorings.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class NotPortedYetAttribute : Attribute
	{
	}
}
