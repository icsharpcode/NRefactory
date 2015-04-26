////
//// DiagnosticFactory.cs
////
//// Author:
////       Mike Krüger <mkrueger@xamarin.com>
////
//// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
////
//// Permission is hereby granted, free of charge, to any person obtaining a copy
//// of this software and associated documentation files (the "Software"), to deal
//// in the Software without restriction, including without limitation the rights
//// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//// copies of the Software, and to permit persons to whom the Software is
//// furnished to do so, subject to the following conditions:
////
//// The above copyright notice and this permission notice shall be included in
//// all copies or substantial portions of the Software.
////
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//// THE SOFTWARE.
//using System;
//using Microsoft.CodeAnalysis;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Security.Policy;
//using System.Globalization;
//
//namespace ICSharpCode.NRefactory6.CSharp.Diagnostics
//{
//	public sealed class DiagnosticFactory
//	{
//		public static object Create(NRefactoryDiagnosticDescriptor descriptor, Location location, params object[] messageArgs)
//		{
//			if (descriptor == null) {
//				throw new ArgumentNullException("descriptor");
//			}
//			string text = descriptor.MessageTemplate;
//			if (messageArgs != null) {
//				text = string.Format(text, messageArgs);
//			}
//			return Create(descriptor.Id, descriptor.Kind, text, descriptor.Severity, (descriptor.Severity != DiagnosticSeverity.Warning) ? 0 : 1, false, location ?? Location.None, descriptor.IssueMarker, null);
//		}
//
//		public static Diagnostic Create(string id, string kind, string message, DiagnosticSeverity severity, int warningLevel, bool isWarningAsError, Location location = null, IssueMarker issueMarker = IssueMarker.WavedLine, IEnumerable<Location> additionalLocations = null)
//		{
//			if (id == null) {
//				throw new ArgumentNullException("id");
//			}
//			if (kind == null) {
//				throw new ArgumentNullException("kind");
//			}
//			if (message == null) {
//				throw new ArgumentNullException("message");
//			}
//			return new NRefactoryDiagnostic(id, kind, message, severity, warningLevel, isWarningAsError, location ?? Location.None, additionalLocations, issueMarker, new object[0]);
//		}
//	}
//
//	public sealed class NRefactoryDiagnostic : Diagnostic
//	{
//
//		readonly string id;
//
//		readonly object[] args;
//
//		readonly ImmutableList<Location> additionalLocations;
//
//		readonly Location location;
//
//		readonly bool isWarningAsError;
//
//		readonly int warningLevel;
//
//		readonly DiagnosticSeverity severity;
//
//		readonly string message;
//
//		readonly string kind;
//
//		//
//		// Properties
//		//
//		public override IReadOnlyList<Location> AdditionalLocations {
//			get {
//				return this.additionalLocations;
//			}
//		}
//
//		public override string Id {
//			get {
//				return this.id;
//			}
//		}
//
//		public override bool IsWarningAsError {
//			get {
//				return this.isWarningAsError;
//			}
//		}
//
//		public override string Kind {
//			get {
//				return this.kind;
//			}
//		}
//
//		public override Location Location {
//			get {
//				return this.location;
//			}
//		}
//
//		public override DiagnosticSeverity Severity {
//			get {
//				return this.severity;
//			}
//		}
//
//		public override int WarningLevel {
//			get {
//				return this.warningLevel;
//			}
//		}
//
//		public IssueMarker IssueMarker {
//			get;
//			private set;
//		}
//
//		internal NRefactoryDiagnostic(string id, string kind, string message, DiagnosticSeverity severity, int warningLevel, bool isWarningAsError, Location location, IEnumerable<Location> additionalLocations, IssueMarker issueMarker, params object[] args)
//		{
//			if (isWarningAsError && severity != DiagnosticSeverity.Warning) {
//				throw new ArgumentException("isWarningAsError");
//			}
//			if ((warningLevel == 0 && severity == DiagnosticSeverity.Warning) || (warningLevel != 0 && severity != DiagnosticSeverity.Warning)) {
//				throw new ArgumentException("warningLevel");
//			}
//			if (args == null) {
//				throw new ArgumentNullException("args");
//			}
//			this.id = id;
//			this.kind = kind;
//			this.message = message;
//			this.severity = severity;
//			this.warningLevel = warningLevel;
//			this.isWarningAsError = isWarningAsError;
//			this.location = location;
//			this.additionalLocations = ImmutableList<Location>.Empty.AddRange(additionalLocations);
//			this.args = args;
//			this.IssueMarker = issueMarker;
//		}
//
//		public override string GetMessage(CultureInfo culture = null)
//		{
//			return string.Format(this.message, this.args);
//		}
//		
//		protected override Diagnostic WithLocation (Location location)
//		{
//			if (location == null) {
//				throw new ArgumentNullException ("location");
//			}
//			if (location != this.location) {
//				return new NRefactoryDiagnostic (this.id, this.kind, this.message, this.severity, this.warningLevel, this.isWarningAsError, location, this.additionalLocations, this.IssueMarker, new object[0]);
//			}
//			return this;
//		}
//
//		protected override Diagnostic WithWarningAsError (bool isWarningAsError)
//		{
//			if (this.isWarningAsError != isWarningAsError) {
//				return new NRefactoryDiagnostic (this.id, this.kind, this.message, this.severity, this.warningLevel, isWarningAsError, this.location, this.additionalLocations, this.IssueMarker, new object[0]);
//			}
//			return this;
//		}
//		
//		public override bool Equals(Diagnostic obj)
//		{
//			return Equals((object)obj);
//		}
//		
//		
//		public override bool Equals(object obj)
//		{
//			if (obj == null)
//				return false;
//			if (ReferenceEquals(this, obj))
//				return true;
//			if (obj.GetType() != typeof(NRefactoryDiagnostic))
//				return false;
//			NRefactoryDiagnostic other = (NRefactoryDiagnostic)obj;
//			return id == other.id && args == other.args && additionalLocations == other.additionalLocations && location == other.location && isWarningAsError == other.isWarningAsError && warningLevel == other.warningLevel && severity == other.severity && message == other.message && kind == other.kind && AdditionalLocations == other.AdditionalLocations && Id == other.Id && IsWarningAsError == other.IsWarningAsError && Kind == other.Kind && Location == other.Location && Severity == other.Severity && WarningLevel == other.WarningLevel && IssueMarker == other.IssueMarker;
//		}
//		
//		public override int GetHashCode()
//		{
//			unchecked {
//				return (id != null ? id.GetHashCode() : 0) ^ (args != null ? args.GetHashCode() : 0) ^ (additionalLocations != null ? additionalLocations.GetHashCode() : 0) ^ (location != null ? location.GetHashCode() : 0) ^ isWarningAsError.GetHashCode() ^ warningLevel.GetHashCode() ^ severity.GetHashCode() ^ (message != null ? message.GetHashCode() : 0) ^ (kind != null ? kind.GetHashCode() : 0) ^ (AdditionalLocations != null ? AdditionalLocations.GetHashCode() : 0) ^ (Id != null ? Id.GetHashCode() : 0) ^ IsWarningAsError.GetHashCode() ^ (Kind != null ? Kind.GetHashCode() : 0) ^ (Location != null ? Location.GetHashCode() : 0) ^ Severity.GetHashCode() ^ WarningLevel.GetHashCode() ^ IssueMarker.GetHashCode();
//			}
//		}
//	}
//}
//
