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
		
		int ParameterCount {
			get;
		}
		
		/// <summary>
		/// Returns the markup to use to represent the specified method overload
		/// in the parameter information window.
		/// </summary>
		string GetHeading (string[] parameterDescription, int currentParameter);
		
		/// <summary>
		/// Returns the markup for the description to use to represent the specified method overload
		/// in the parameter information window.
		/// </summary>
		string GetDescription (int currentParameter);
		
		/// <summary>
		/// Returns the text to use to represent the specified parameter
		/// </summary>
		string GetParameterDescription (int paramIndex);
		
		/// <summary>
		/// Gets the name of the parameter.
		/// </summary>
		string GetParameterName (int currentParameter);

		/// <summary>
		/// Used for the params lists. (for example "params" in c#).
		/// </summary>
		bool AllowParameterList { get; }
	}
}