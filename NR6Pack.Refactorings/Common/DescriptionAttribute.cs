using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICSharpCode.NRefactory6.CSharp
{
	public class DescriptionAttribute : Attribute
	{
		string description;

		public DescriptionAttribute(string description)
		{
			Description = description;
		}

		public string Description
		{
			get { return description; }
			private set { description = value; }
		}
	}
}
