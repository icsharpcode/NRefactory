//
// NamespaceExtensions.cs
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
using System.Threading;

namespace ICSharpCode.NRefactory6.VisualBasic
{
	public static class NamespaceExtensions
	{
		public static IEnumerable<INamedTypeSymbol> GetAllTypes(this INamespaceSymbol namespaceSymbol, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (namespaceSymbol == null)
				throw new ArgumentNullException("namespaceSymbol");
			var stack = new Stack<INamespaceOrTypeSymbol>();
			stack.Push(namespaceSymbol);

			while (stack.Count > 0) {
				if (cancellationToken.IsCancellationRequested)
					yield break;
				var current = stack.Pop();
				var currentNs = current as INamespaceSymbol;
				if (currentNs != null) {
					foreach (var member in currentNs.GetMembers())
						stack.Push(member);
				} else {
					var namedType = (INamedTypeSymbol)current;
					foreach (var nestedType in namedType.GetTypeMembers())
						stack.Push(nestedType);
					yield return namedType;
				}
			}
		}
		
	
	}
}

