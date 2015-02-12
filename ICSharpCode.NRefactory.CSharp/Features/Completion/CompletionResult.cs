//
// CompletionResult.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	public class CompletionResult : IReadOnlyList<ICompletionData>
	{
		public static readonly CompletionResult Empty = new CompletionResult ();

		readonly List<ICompletionData> data = new List<ICompletionData> ();

		public string DefaultCompletionString {
			get;
			internal set;
		}

		public bool AutoCompleteEmptyMatch {
			get;
			set;
		}

		public bool AutoCompleteEmptyMatchOnCurlyBracket {
			get;
			set;
		}

		public bool AutoSelect {
			get;
			set;
		}

		public bool CloseOnSquareBrackets {
			get;
			set;
		}

		/// <summary>
		/// If true the completion result isn't a continuation and can start anything.
		/// </summary>
		public bool InsertTemplatesInList {
			get;
			set;
		}

		public readonly List<IMethodSymbol> PossibleDelegates = new List<IMethodSymbol>();

		#region IReadOnlyList<ICompletionData> implemenation
		public IEnumerator<ICompletionData> GetEnumerator()
		{
			return data.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ((System.Collections.IEnumerable)data).GetEnumerator();
		}
		
		public ICompletionData this[int index] {
			get {
				return data [index];
			}
		}

		public int Count {
			get {
				return data.Count;
			}
		}
		#endregion
		
		internal CompletionResult()
		{
			AutoSelect = true;
			AutoCompleteEmptyMatchOnCurlyBracket = true;
		}
		
		internal void AddData (ICompletionData completionData)
		{
			Console.WriteLine ("add :" + completionData.DisplayText);
			Console.WriteLine ("----" + Environment.StackTrace);
			data.Add(completionData); 
		}

		internal void AddRange (IEnumerable<ICompletionData> completionData)
		{
			data.AddRange(completionData); 
		}

		public static CompletionResult Create(IEnumerable<ICompletionData> data)
		{
			var result = new CompletionResult();
			result.data.AddRange(data);
			return result;
		}
	}
}

