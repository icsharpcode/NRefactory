//
// OverrideCompletionHandlerTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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

﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using NUnit.Framework;
using ICSharpCode.NRefactory6.CSharp.Completion;

namespace ICSharpCode.NRefactory6.CSharp.CodeCompletion
{
	public class CompletionTestBase
	{

		protected void VerifyItemExists(string input, params string[] items)
		{
			var provider = CodeCompletionBugTests.CreateProvider(input.Replace("$$", "$"));

			foreach (var item in provider)
				Console.WriteLine(item.DisplayText);
			if (items != null) {
				foreach (var item in items) {
					Assert.IsNotNull(provider.Find(item), "item '" + item + "' not found.");	
				}
			}
		}

		protected void VerifyItemIsAbsent(string input, params string[] items)
		{
			var provider = CodeCompletionBugTests.CreateProvider(input.Replace("$$", "$"));

			foreach (var item in provider)
				Console.WriteLine(item.DisplayText);
			if (items != null) {
				foreach (var item in items) {
					Assert.IsNull(provider.Find(item), "item '" + item + "' found but shouldn't.");	
				}
			}
		}

	}

}