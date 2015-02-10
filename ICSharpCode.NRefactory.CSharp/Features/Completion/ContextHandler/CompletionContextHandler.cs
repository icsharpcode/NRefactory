//
// CompletionContextHandler.cs
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
using System.Linq;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using System.Threading.Tasks;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{

	//	public class CompletionEngineCache
	//	{
	//		public List<INamespace>  namespaces;
	//		public ICompletionData[] importCompletion;
	//	}

	abstract class CompletionContextHandler
	{
		public abstract Task<IEnumerable<ICompletionData>> GetCompletionDataAsync (CompletionResult result, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, CancellationToken cancellationToken = default(CancellationToken));

		static readonly char[] csharpCommitChars = {
			' ', '{', '}', '[', ']', '(', ')', '.', ',', ':',
			';', '+', '-', '*', '/', '%', '&', '|', '^', '!',
			'~', '=', '<', '>', '?', '@', '#', '\'', '\"', '\\'
		};

		public virtual bool IsCommitCharacter (ICompletionData completionItem, char ch, string textTypedSoFar)
		{
			return csharpCommitChars.Contains (ch);
		}

		public virtual bool IsTriggerCharacter (SourceText text, int position)
		{
			var ch = text [position];
			return ch == '.' || // simple member access
				ch == '#' || // pre processor directives 
				ch == '>' && position >= 1 && text [position - 1] == '-' || // pointer member access
				ch == ':' && position >= 1 && text [position - 1] == ':' || // alias name
				IsStartingNewWord (text, position);
		}

		internal static bool IsTriggerAfterSpaceOrStartOfWordCharacter(SourceText text, int characterPosition)
		{
			var ch = text[characterPosition];
			Console.WriteLine ("ch: " + ((int)ch) +"/"+ IsStartingNewWord(text, characterPosition));
			return ch == ' ' || IsStartingNewWord(text, characterPosition);
		}

		internal static bool IsStartingNewWord (SourceText text, int position)
		{
			var ch = text [position];
			if (!SyntaxFacts.IsIdentifierStartCharacter (ch))
				return false;

			if (position > 0 && IsWordCharacter (text [position - 1]))
				return false;

			if (position < text.Length - 1 && IsWordCharacter (text [position + 1]))
				return false;

			return true;
		}

		static bool IsWordCharacter (char ch)
		{
			return SyntaxFacts.IsIdentifierStartCharacter (ch) || SyntaxFacts.IsIdentifierPartCharacter (ch);
		}

		protected static bool IsOnStartLine(int position, SourceText text, int startLine)
		{
			return text.Lines.IndexOf(position) == startLine;
		}
	}
}
