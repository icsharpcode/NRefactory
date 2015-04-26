using System;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	[Flags]
	public enum Modifiers
	{
		None       = 0,

		Private   = 0x0001,
		Internal  = 0x0002,
		Protected = 0x0004,
		Public    = 0x0008,

		Abstract  = 0x0010,
		Virtual   = 0x0020,
		Sealed    = 0x0040,
		Static    = 0x0080,
		Override  = 0x0100,
		Readonly  = 0x0200,
		Const     = 0x0400,
		New       = 0x0800,
		Partial   = 0x1000,

		Extern    = 0x2000,
		Volatile  = 0x4000,
		Unsafe    = 0x8000,
		Async     = 0x10000,

		VisibilityMask = Private | Internal | Protected | Public,

		/// <summary>
		/// Special value used to match any modifiers during pattern matching.
		/// </summary>
		Any = unchecked((int)0x80000000)
	}
}

