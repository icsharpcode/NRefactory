//
// InsertionResult.cs
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

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	public sealed class InsertionResult
	{
		/// <summary>
		/// Gets the context the insertion is invoked at.
		/// </summary>
		public CodeRefactoringContext Context { get; private set; }

		/// <summary>
		/// Gets the node that should be inserted.
		/// </summary>
		public SyntaxNode Node { get; private set; }

		/// <summary>
		/// Gets the type the node should be inserted to.
		/// </summary>
		public INamedTypeSymbol Type { get; private set; }

		/// <summary>
		/// Gets the location of the type part the node should be inserted to.
		/// </summary>
		public Location Location { get; private set; }

		public InsertionResult (CodeRefactoringContext context, SyntaxNode node, INamedTypeSymbol type, Location location)
		{
			this.Context = context;
			this.Node = node;
			this.Type = type;
			this.Location = location;
		}

		public static Location GuessCorrectLocation(CodeRefactoringContext context, System.Collections.Immutable.ImmutableArray<Location> locations)
		{
			foreach (var loc in locations) {
				if (context.Document.FilePath == loc.SourceTree.FilePath)
					return loc;
			}
			return locations [0];
		}
	}
}

