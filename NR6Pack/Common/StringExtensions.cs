using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICSharpCode.NRefactory6.CSharp
{
	static class StringExtensions
	{
		public static bool Any(this string str, Func<char, bool> predicate)
		{
			for (int i = 0; i < str.Length; i++)
			{
				if (predicate(str[i]))
				{
					return true;
				}
			}

			return false;
		}
	}
}
