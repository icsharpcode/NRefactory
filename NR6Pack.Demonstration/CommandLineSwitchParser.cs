using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NR6Pack.Demonstration
{
	public enum CommandLineSwitches
	{
		A,
		B,
		C,
		D,
		V
	}

	public class CommandLineSwitchParser
	{
		// Bulb: private is optional -> suggestion to remove it
		private CommandLineSwitches switches;
		private string[] parameters;

		// Info: Empty default constructor is optional and can be removed
		public CommandLineSwitchParser()
		{

		}

		// As soon as this constructor is activated, the default constructor above is no more marked as optional
		//public CommandLineSwitchParser(string[] parameters)
		//{
		//	this.parameters = parameters;
		//}

		// Info: Empty destructor can be removed, too.
		~CommandLineSwitchParser()
		{

		}

		public string[] Parameters
		{
			get
			{
				return parameters;
			}

			set
			{
				parameters = value;
			}
		}

		// CodeAction: Add another accessor (means 'set { ... }' here)
		public CommandLineSwitches Switches
		{
            get
			{
				return switches;
			}
		}

		// CodeAction: Method can be converted to non-virtual
		public virtual void Parse()
		{
			// CodeAction: Remove braces from 'if' and vice versa
			if (Parameters == null)
			{
				return;
			}

			// CodeAction: Use 'var' instead of 'string' on param
			// CodeAction: Conversion of foreach to for loop
			switches = 0;
			foreach (string param in Parameters)
			{
				switch (param)
				{
					case "-a":
						// CodeIssue should warn here about bit operation on enum, which is not annotated with [Flags]
						switches |= CommandLineSwitches.A;
						break;
					case "-b":
						switches |= CommandLineSwitches.B;
						break;
					case "-c":
						switches |= CommandLineSwitches.C;
						break;
					case "-d":
						switches |= CommandLineSwitches.D;
						break;
					case "-v":
						switches |= CommandLineSwitches.V;
						break;
				}
			}
		}
	}
}
