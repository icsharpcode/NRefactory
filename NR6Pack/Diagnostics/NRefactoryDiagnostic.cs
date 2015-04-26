using System;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
{
	sealed class NRefactoryDiagnosticDiagnostic : Diagnostic
	{
		readonly DiagnosticDescriptor descriptor;
		readonly DiagnosticSeverity severity;
		readonly int warningLevel;
		readonly Location location;
		readonly IReadOnlyList<Location> additionalLocations;
		readonly object[] messageArgs;

		static readonly object[] emptyArray = new object[0];
		static readonly IReadOnlyList<Location> emptyList = new List<Location>();

		NRefactoryDiagnosticDiagnostic(
			DiagnosticDescriptor descriptor,
			DiagnosticSeverity severity, 
			int warningLevel, 
			Location location,
			IEnumerable<Location> additionalLocations,
			object[] messageArgs,
			string[] customTags = null)
		{
			if ((warningLevel == 0 && severity != DiagnosticSeverity.Error) ||
				(warningLevel != 0 && severity == DiagnosticSeverity.Error))
			{
				throw new ArgumentException("warningLevel");
			}

			this.descriptor = descriptor;
			this.severity = severity;
			this.warningLevel = warningLevel;
			this.location = location;
			this.additionalLocations = additionalLocations == null ? emptyList: additionalLocations.ToImmutableArray();

			this.messageArgs = messageArgs ?? emptyArray;
			this.CustomTags = customTags ?? emptyTags;
		}

		internal static NRefactoryDiagnosticDiagnostic Create(
			DiagnosticDescriptor descriptor,
			DiagnosticSeverity severity,
			int warningLevel,
			Location location,
			IEnumerable<Location> additionalLocations,
			object[] messageArgs)
		{
			return new NRefactoryDiagnosticDiagnostic(descriptor, severity, warningLevel, location, additionalLocations, messageArgs);
		}

		static readonly string[] emptyTags = new string[0];
		internal static NRefactoryDiagnosticDiagnostic Create(string id, LocalizableString title, string category, LocalizableString message, LocalizableString description, string helpLink,
			DiagnosticSeverity severity, DiagnosticSeverity defaultSeverity,
			bool isEnabledByDefault, int warningLevel, Location location,
			IEnumerable<Location> additionalLocations, string[] customTags)
		{
			var descriptor = new DiagnosticDescriptor(id, title, message,
				category, defaultSeverity, isEnabledByDefault, description, helpLink);
			return new NRefactoryDiagnosticDiagnostic(descriptor, severity, warningLevel, location, additionalLocations, messageArgs: null, customTags: customTags);
		}

		public override DiagnosticDescriptor Descriptor
		{
			get { return this.descriptor; }
		}

		public override string Id
		{
			get { return this.descriptor.Id; }
		}

		public override string GetMessage(IFormatProvider formatProvider = null)
		{
			if (this.messageArgs.Length == 0)
			{
				return this.descriptor.MessageFormat.ToString(formatProvider);
			}

			var localizedMessageFormat = this.descriptor.MessageFormat.ToString(formatProvider);
			return string.Format(formatProvider, localizedMessageFormat, this.messageArgs);
		}

		public string[] CustomTags {
			get;
			private set;
		}

		public override DiagnosticSeverity Severity
		{
			get { return this.severity; }
		}

		public override int WarningLevel
		{
			get { return this.warningLevel; }
		}

		public override Location Location
		{
			get { return this.location; }
		}

		public override IReadOnlyList<Location> AdditionalLocations
		{
			get { return this.additionalLocations; }
		}

		public override bool Equals(Diagnostic obj)
		{
			var other = obj as NRefactoryDiagnosticDiagnostic;
			return other != null
				&& this.descriptor == other.descriptor
				&& this.messageArgs.SequenceEqual(other.messageArgs)
				&& this.location == other.location
				&& this.severity == other.severity
				&& this.warningLevel == other.warningLevel;
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as Diagnostic);
		}

		public override int GetHashCode()
		{
			return this.descriptor.GetHashCode() ^ this.messageArgs.GetHashCode() ^ this.location.GetHashCode() ^ this.severity.GetHashCode() ^ this.warningLevel;
		}
	}
}