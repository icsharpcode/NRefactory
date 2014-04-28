// 
// IParameterDataProvider.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	/// <summary>
	/// Provides intellisense information for a collection of parametrized members.
	/// </summary>
	public interface IParameterHintingData
	{
		/// <summary>
		/// Gets the symbol for which the parameter should be created.
		/// </summary>
		ISymbol Symbol {
			get;
		}
	}
	
	public static class IParameterHintingDataExtensions
	{
		static ImmutableArray<IParameterSymbol> GetParameterList (IParameterHintingData data)
		{
			var ms = data.Symbol as IMethodSymbol;
			if (ms != null)
				return ms.Parameters;
				
			var ps = data.Symbol as IPropertySymbol;
			if (ps != null)
				return ps.Parameters;

			return ImmutableArray<IParameterSymbol>.Empty;
		}
		
		static ImmutableArray<ITypeParameterSymbol> GetTypeParameterList (IParameterHintingData data)
		{
			var ms = data.Symbol as IMethodSymbol;
			if (ms != null)
				return ms.TypeParameters;
				
			var ps = data.Symbol as INamedTypeSymbol;
			if (ps != null)
				return ps.TypeParameters;

			return ImmutableArray<ITypeParameterSymbol>.Empty;
		}
		
		public static string GetTypeParameterName (this IParameterHintingData data, int currentParameter)
		{
			if (data == null)
				throw new ArgumentNullException("data");
			var list = GetTypeParameterList (data);
			if (currentParameter < 0 || currentParameter >= list.Length)
				throw new ArgumentOutOfRangeException ("currentParameter");
			return list [currentParameter].Name;
		}

		public static int GetTypeParameterCount (this IParameterHintingData data)
		{
			if (data == null)
				throw new ArgumentNullException("data");
			return GetTypeParameterList (data).Length;
		}

		public static string GetParameterName (this IParameterHintingData data, int currentParameter)
		{
			if (data == null)
				throw new ArgumentNullException("data");
			var list = GetParameterList (data);
			if (currentParameter < 0 || currentParameter >= list.Length)
				throw new ArgumentOutOfRangeException ("currentParameter");
			return list [currentParameter].Name;
		}

		public static int GetParameterCount (this IParameterHintingData data)
		{
			if (data == null)
				throw new ArgumentNullException("data");
			return GetParameterList (data).Length;
		}

		public static bool GetIsParameterListAllowed(this IParameterHintingData data)
		{
			if (data == null)
				throw new ArgumentNullException("data");
			var param = GetParameterList (data).LastOrDefault ();
			return param != null && param.IsParams;
		}
	}
}