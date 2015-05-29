using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp
{

	class CaseCorrector
	{
		readonly static Type typeInfo;
		readonly static MethodInfo caseCorrectAsyncMethod;

		static CaseCorrector ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.CaseCorrection.CaseCorrector" + ReflectionNamespaces.WorkspacesAsmName, true);

			Annotation = (SyntaxAnnotation)typeInfo.GetField ("Annotation", BindingFlags.Public | BindingFlags.Static).GetValue (null);

			caseCorrectAsyncMethod = typeInfo.GetMethod ("CaseCorrectAsync", new [] {
			typeof(Document),
			typeof(SyntaxAnnotation),
			typeof(CancellationToken)
		});
		}

		public static readonly SyntaxAnnotation Annotation;

		public static Task<Document> CaseCorrectAsync (Document document, SyntaxAnnotation annotation, CancellationToken cancellationToken)
		{
			try {
				return (Task<Document>)caseCorrectAsyncMethod.Invoke (null, new object [] { document, annotation, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture (ex.InnerException).Throw ();
				return null;
			}
		}
	}
}