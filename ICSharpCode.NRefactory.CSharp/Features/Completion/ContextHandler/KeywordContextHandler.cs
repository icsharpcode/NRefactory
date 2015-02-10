//
// KeywordContextHandler.cs
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
using System.Threading.Tasks;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{

//	public class CompletionEngineCache
//	{
//		public List<INamespace>  namespaces;
//		public ICompletionData[] importCompletion;
//	}

	class KeywordContextHandler : CompletionContextHandler
	{
		public async override Task<IEnumerable<ICompletionData>> GetCompletionDataAsync (CompletionResult completionResult, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, CancellationToken cancellationToken)
		{
			var ctx = await completionContext.GetSyntaxContextAsync (engine.Workspace, cancellationToken).ConfigureAwait (false);
			var model = await completionContext.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
			var result = new List<ICompletionData> ();

			var factory = engine.Factory;
			var parent = ctx.TargetToken.Parent;
			if (parent != null && parent.IsKind(SyntaxKind.ArrayRankSpecifier))
				return result;
			if (ctx.IsIsOrAsTypeContext) {
				foreach (var kw in primitiveTypesKeywords)
					result.Add (factory.CreateGenericData(kw, GenericDataType.Keyword));
				return result;
			}
			if (parent != null) {
				if (parent.CSharpKind() == SyntaxKind.IdentifierName) {
					if (ctx.LeftToken.Parent.CSharpKind() == SyntaxKind.IdentifierName &&
					    parent.Parent != null && parent.Parent.CSharpKind() == SyntaxKind.ParenthesizedExpression ||
					    ctx.LeftToken.Parent.CSharpKind() == SyntaxKind.CatchDeclaration)
						return result;
				}
				if (parent.CSharpKind() == SyntaxKind.NamespaceDeclaration) {
					var decl = parent as NamespaceDeclarationSyntax;
					if (decl.OpenBraceToken.Span.Length > 0 &&
					    decl.OpenBraceToken.SpanStart > ctx.TargetToken.SpanStart)
						return result;
				}
				if (parent.CSharpKind() == SyntaxKind.ClassDeclaration ||
				    parent.CSharpKind() == SyntaxKind.StructDeclaration ||
					parent.CSharpKind() == SyntaxKind.InterfaceDeclaration) {
					foreach (var kw in typeLevelKeywords)
						result.Add (factory.CreateGenericData(kw, GenericDataType.Keyword));
					return result;
				} 
				if (parent.CSharpKind() == SyntaxKind.EnumDeclaration ||
				    parent.CSharpKind() == SyntaxKind.DelegateDeclaration ||
				    parent.CSharpKind() == SyntaxKind.PredefinedType ||
				    parent.CSharpKind() == SyntaxKind.TypeParameterList ||
				    parent.CSharpKind() == SyntaxKind.QualifiedName ||
				    parent.CSharpKind() == SyntaxKind.SimpleMemberAccessExpression) {
					return result;
				}
			}
			if (parent.IsKind(SyntaxKind.AttributeList)) {
				if (parent.Parent.Parent == null || parent.Parent.Parent.IsKind(SyntaxKind.CompilationUnit)) {
					result.Add(factory.CreateGenericData("assembly", GenericDataType.AttributeTarget));
					result.Add(factory.CreateGenericData("module", GenericDataType.AttributeTarget));
					result.Add(factory.CreateGenericData("type", GenericDataType.AttributeTarget));
				} else {
					result.Add(factory.CreateGenericData("param", GenericDataType.AttributeTarget));
					result.Add(factory.CreateGenericData("field", GenericDataType.AttributeTarget));
					result.Add(factory.CreateGenericData("property", GenericDataType.AttributeTarget));
					result.Add(factory.CreateGenericData("method", GenericDataType.AttributeTarget));
					result.Add(factory.CreateGenericData("event", GenericDataType.AttributeTarget));
				}
				result.Add(factory.CreateGenericData("return", GenericDataType.AttributeTarget));
			}
			if (ctx.IsInstanceContext) {
				if (ctx.LeftToken.Parent.Ancestors().Any(a => a is SwitchStatementSyntax || a is BlockSyntax && a.ToFullString().IndexOf("switch", StringComparison.Ordinal) > 0)) {
					result.Add(factory.CreateGenericData("case", GenericDataType.Keyword));
				}
			}
		
			var forEachStatementSyntax = parent as ForEachStatementSyntax;
			if (forEachStatementSyntax != null) {
				if (forEachStatementSyntax.Type.Span.Length > 0 &&
					forEachStatementSyntax.Identifier.Span.Length > 0 &&
					forEachStatementSyntax.InKeyword.Span.Length == 0) {
					result.Add(factory.CreateGenericData("in", GenericDataType.Keyword));
					return result;
				}
			}
			if (parent != null && parent.CSharpKind() == SyntaxKind.ArgumentList) {
				result.Add(factory.CreateGenericData("out", GenericDataType.Keyword));
				result.Add(factory.CreateGenericData("ref", GenericDataType.Keyword));
			} else if (parent != null && parent.CSharpKind() == SyntaxKind.ParameterList) {
				result.Add(factory.CreateGenericData("out", GenericDataType.Keyword));
				result.Add(factory.CreateGenericData("ref", GenericDataType.Keyword));
				result.Add(factory.CreateGenericData("params", GenericDataType.Keyword));
				foreach (var kw in primitiveTypesKeywords)
					result.Add(factory.CreateGenericData(kw, GenericDataType.Keyword));

				if (ctx.IsParameterTypeContext) {
					bool isFirst = ctx.LeftToken.GetPreviousToken().IsKind(SyntaxKind.OpenParenToken);
					if (isFirst)
						result.Add(factory.CreateGenericData("this", GenericDataType.Keyword));
				}

				return result;
			} else {
				result.Add(factory.CreateGenericData("var", GenericDataType.Keyword));
				result.Add(factory.CreateGenericData("dynamic", GenericDataType.Keyword));
			}

			if (parent != null && parent.Parent != null && parent.IsKind(SyntaxKind.BaseList) && parent.Parent.IsKind(SyntaxKind.EnumDeclaration)) {
				foreach (var kw in validEnumBaseTypes)
					result.Add(factory.CreateGenericData(kw, GenericDataType.Keyword));
				return result;
			}
			if (parent != null &&
				parent.Parent != null &&
				parent.Parent.IsKind(SyntaxKind.FromClause)) {
				foreach (var kw in linqKeywords)
					result.Add(factory.CreateGenericData(kw, GenericDataType.Keyword));
			}
			if (ctx.IsGlobalStatementContext || parent == null || parent is NamespaceDeclarationSyntax) {
				foreach (var kw in globalLevelKeywords)
					result.Add(factory.CreateGenericData(kw, GenericDataType.Keyword));
				return result;
			} else {
				foreach (var kw in typeLevelKeywords)
					result.Add(factory.CreateGenericData(kw, GenericDataType.Keyword));
			}

			foreach (var kw in primitiveTypesKeywords)
				result.Add(factory.CreateGenericData(kw, GenericDataType.Keyword));

			foreach (var kw in statementStartKeywords)
				result.Add(factory.CreateGenericData(kw, GenericDataType.Keyword));
			
			foreach (var kw in expressionLevelKeywords)
				result.Add(factory.CreateGenericData(kw, GenericDataType.Keyword));

			if (ctx.IsPreProcessorKeywordContext) {
				foreach (var kw in preprocessorKeywords)
					result.Add(factory.CreateGenericData (kw, GenericDataType.PreprocessorKeyword));
			}
			
			if (ctx.IsPreProcessorExpressionContext) {
				var parseOptions = model.SyntaxTree.Options as CSharpParseOptions;
				foreach (var define in parseOptions.PreprocessorSymbolNames) {
					result.Add(factory.CreateGenericData (define, GenericDataType.PreprocessorSymbol));
				}
			}
			if (parent.IsKind(SyntaxKind.TypeParameterConstraintClause)) {
				result.Add(factory.CreateGenericData ("new()", GenericDataType.PreprocessorKeyword));
			}
			return result;
		} 
		
		static readonly string[] preprocessorKeywords = {
			"else",
			"elif",
			"endif",
			"define",
			"undef",
			"warning",
			"error",
			"pragma",
			"line",
			"line hidden",
			"line default",
			"region",
			"endregion"
		};

		static readonly string[] validEnumBaseTypes = {
			"byte",
			"sbyte",
			"short",
			"int",
			"long",
			"ushort",
			"uint",
			"ulong"
		};

		static readonly string[] expressionLevelKeywords = {
			"as",
			"is",
			"else",
			"out",
			"ref",
			"null",
			"delegate",
			"default",
			"true",
			"false"
		};

		static readonly string[] primitiveTypesKeywords = {
			"void",
			"object",
			"bool",
			"byte",
			"sbyte",
			"char",
			"short",
			"int",
			"long",
			"ushort",
			"uint",
			"ulong",
			"float",
			"double",
			"decimal",
			"string"
		};

		static readonly string[] statementStartKeywords = { 
			"base", "new", "sizeof", "this", 
			"true", "false", "typeof", "checked", "unchecked", "from", "break", "checked",
			"unchecked", "const", "continue", "do", "finally", "fixed", "for", "foreach",
			"goto", "if", "lock", "return", "stackalloc", "switch", "throw", "try", "unsafe", 
			"using", "while", "yield",
			"catch"
		};

		static readonly string[] globalLevelKeywords = {
			"namespace", "using", "extern", "public", "internal", 
			"class", "interface", "struct", "enum", "delegate",
			"abstract", "sealed", "static", "unsafe", "partial"
		};

		static readonly string[] typeLevelKeywords = {
			"public", "internal", "protected", "private", "async",
			"class", "interface", "struct", "enum", "delegate",
			"abstract", "sealed", "static", "unsafe", "partial",
			"const", "event", "extern", "fixed", "new", 
			"operator", "explicit", "implicit", 
			"override", "readonly", "virtual", "volatile"
		};

		static readonly string[] linqKeywords = {
			"from",
			"where",
			"select",
			"group",
			"into",
			"orderby",
			"join",
			"let",
			"in",
			"on",
			"equals",
			"by",
			"ascending",
			"descending"
		};

		static readonly string[] parameterTypePredecessorKeywords = {
			"out",
			"ref",
			"params"
		};
	}

}
