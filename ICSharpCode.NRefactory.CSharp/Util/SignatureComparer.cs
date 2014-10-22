using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp
{
	public static class SignatureComparer
	{
		public static bool HaveSameSignature (IList<IParameterSymbol> parameters1, IList<IParameterSymbol> parameters2)
		{
			return Microsoft.CodeAnalysis.Shared.Utilities.SignatureComparer.Instance.HaveSameSignature(parameters1, parameters2);
		}

		public static bool HaveSameSignature (IPropertySymbol property1, IPropertySymbol property2, bool caseSensitive)
		{
			return Microsoft.CodeAnalysis.Shared.Utilities.SignatureComparer.Instance.HaveSameSignature(property1, property2, caseSensitive);
		}

		public static bool HaveSameSignature (ISymbol symbol1, ISymbol symbol2, bool caseSensitive)
		{
			return Microsoft.CodeAnalysis.Shared.Utilities.SignatureComparer.Instance.HaveSameSignature(symbol1, symbol2, caseSensitive);
		}

		public static bool HaveSameSignature (IMethodSymbol method1, IMethodSymbol method2, bool caseSensitive, bool compareParameterName = false, bool isParameterCaseSensitive = false)
		{
			return Microsoft.CodeAnalysis.Shared.Utilities.SignatureComparer.Instance.HaveSameSignature(method1, method2, caseSensitive, compareParameterName, isParameterCaseSensitive);
		}

		public static bool HaveSameSignature (IList<IParameterSymbol> parameters1, IList<IParameterSymbol> parameters2, bool compareParameterName, bool isCaseSensitive)
		{
			return Microsoft.CodeAnalysis.Shared.Utilities.SignatureComparer.Instance.HaveSameSignature(parameters1, parameters2, compareParameterName, isCaseSensitive);
		}
	}
}