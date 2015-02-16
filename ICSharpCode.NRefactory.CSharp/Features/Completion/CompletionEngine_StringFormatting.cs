//
// CompletionEngine_StringFormatting.cs
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	public partial class CompletionEngine
	{
		CompletionResult HandleStringFormatItems(Document document, SemanticModel semanticModel, int position, SyntaxContext ctx)
		{
			if (ctx.TargetToken.Parent == null || !ctx.TargetToken.Parent.IsKind(SyntaxKind.StringLiteralExpression) ||
				ctx.TargetToken.Parent.Parent == null || !ctx.TargetToken.Parent.Parent.IsKind(SyntaxKind.Argument) ||
				ctx.TargetToken.Parent.Parent.Parent == null || !ctx.TargetToken.Parent.Parent.Parent.IsKind(SyntaxKind.ArgumentList) ||
				ctx.TargetToken.Parent.Parent.Parent.Parent == null || !ctx.TargetToken.Parent.Parent.Parent.Parent.IsKind(SyntaxKind.InvocationExpression)) {
				return CompletionResult.Empty;
			}
			var formatArgument = GetFormatItemNumber(document, position);
			var invocationExpression = ctx.TargetToken.Parent.Parent.Parent.Parent as InvocationExpressionSyntax;
			var info = semanticModel.GetSymbolInfo(invocationExpression);
			return GetFormatCompletionData(semanticModel, invocationExpression, formatArgument, info.Symbol);
		}

		static int GetFormatItemNumber(Document document, int offset)
		{
			int number = 0;
			var o = offset - 2;
			var text = document.GetTextAsync().Result;
			while (o > 0) {
				char ch = text[o];
				if (ch == '{')
					return number;
				if (!char.IsDigit(ch))
					break;
				number = number * 10 + ch - '0';
				o--;
			}
			return -1;
		}

		static readonly DateTime curDate = DateTime.Now;

		IEnumerable<ICompletionData> GenerateNumberFormatitems(bool isFloatingPoint)
		{
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "D", "decimal", 123);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "D5", "decimal", 123);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "C", "currency", 123);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "C0", "currency", 123);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "E", "exponential", 1.23E4);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "E2", "exponential", 1.234);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "e2", "exponential", 1.234);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "F", "fixed-point", 123.45);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "F1", "fixed-point", 123.45);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "G", "general", 1.23E+56);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "g2", "general", 1.23E+56);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "N", "number", 12345.68);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "N1", "number", 12345.68);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "P", "percent", 12.34);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "P1", "percent", 12.34);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "R", "round-trip", 0.1230000001);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "X", "hexadecimal", 1234);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "x8", "hexadecimal", 1234);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "0000", "custom", 123);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "####", "custom", 123);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "##.###", "custom", 1.23);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "##.000", "custom", 1.23);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "## 'items'", "custom", 12);
		}

		IEnumerable<ICompletionData> GenerateDateTimeFormatitems()
		{
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "D", "long date", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "d", "short date", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "F", "full date long", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "f", "full date short", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "G", "general long", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "g", "general short", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "M", "month", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "O", "ISO 8601", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "R", "RFC 1123", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "s", "sortable", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "T", "long time", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "t", "short time", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "U", "universal full", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "u", "universal sortable", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "Y", "year month", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "yy-MM-dd", "custom", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "yyyy MMMMM dd", "custom", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "yy-MMM-dd ddd", "custom", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "yyyy-M-d dddd", "custom", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "hh:mm:ss t z", "custom", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "hh:mm:ss tt zz", "custom", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "HH:mm:ss tt zz", "custom", curDate);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "HH:m:s tt zz", "custom", curDate);

		}

		[Flags]
		enum TestEnum
		{
			EnumCaseName = 0,
			Flag1 = 1,
			Flag2 = 2,
			Flags
		}

		IEnumerable<ICompletionData> GenerateEnumFormatitems()
		{
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "G", "string value", TestEnum.EnumCaseName);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "F", "flags value", TestEnum.Flags);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "D", "integer value", TestEnum.Flags);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "X", "hexadecimal", TestEnum.Flags);
		}

		IEnumerable<ICompletionData> GenerateTimeSpanFormatitems()
		{
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "c", "invariant", new TimeSpan(0, 1, 23, 456));
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "G", "general long", new TimeSpan(0, 1, 23, 456));
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "g", "general short", new TimeSpan(0, 1, 23, 456));
		}

		static Guid defaultGuid = Guid.NewGuid();

		IEnumerable<ICompletionData> GenerateGuidFormatitems()
		{
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "N", "digits", defaultGuid);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "D", "hypens", defaultGuid);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "B", "braces", defaultGuid);
			yield return factory.CreateFormatItemCompletionData(DefaultKeyHandler, "P", "parentheses", defaultGuid);
		}

		CompletionResult GetFormatCompletionForType(ITypeSymbol type)
		{
			switch (type.ToString()) {
				case "long":
				case "System.Int64":
				case "ulong":
				case "System.UInt64":
				case "int":
				case "System.Int32":
				case "uint":
				case "System.UInt32":
				case "short":
				case "System.Int16":
				case "ushort":
				case "System.UInt16":
				case "byte":
				case "System.Byte":
				case "sbyte":
				case "System.SByte":
					return CompletionResult.Create(GenerateNumberFormatitems(false));
				case "float":
				case "System.Single":
				case "double":
				case "System.Double":
				case "decimal":
				case "System.Decimal":
					return CompletionResult.Create(GenerateNumberFormatitems(true));
				case "System.Enum":
					return CompletionResult.Create(GenerateEnumFormatitems());
				case "System.DateTime":
					return CompletionResult.Create(GenerateDateTimeFormatitems());
				case "System.TimeSpan":
					return CompletionResult.Create(GenerateTimeSpanFormatitems());
				case "System.Guid":
					return CompletionResult.Create(GenerateGuidFormatitems());
			}
			return CompletionResult.Empty;
		}

		CompletionResult GetFormatCompletionData(SemanticModel semanticModel, InvocationExpressionSyntax invocationExpression, int formatArgument, ISymbol symbol)
		{
			if (symbol.Kind != SymbolKind.Method)
				return CompletionResult.Empty;
			var method = symbol as IMethodSymbol;
			if (method == null)
				return CompletionResult.Empty;
			if (method.Name == "ToString") {
				return GetFormatCompletionForType(method.ContainingType);
			} else {
				ExpressionSyntax fmtArgumets;
				IList<ExpressionSyntax> args;
				if (FormatStringHelper.TryGetFormattingParameters(semanticModel, invocationExpression, out fmtArgumets, out args, null)) {
					if (formatArgument  + 1< args.Count) {
						var invokeArgument = semanticModel.GetSymbolInfo(args[formatArgument + 1]);
						return GetFormatCompletionForType(invokeArgument.Symbol.GetReturnType());
					}
					return CompletionResult.Create(GenerateNumberFormatitems(false)
						.Concat(GenerateDateTimeFormatitems())
						.Concat(GenerateTimeSpanFormatitems())
						.Concat(GenerateEnumFormatitems())
						.Concat(GenerateGuidFormatitems()));
				}
			}
			return CompletionResult.Empty;
		}
	}
}