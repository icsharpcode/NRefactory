// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp
{
	public enum DeclaredSymbolInfoKind : byte
	{
		Class,
		Constant,
		Constructor,
		Delegate,
		Enum,
		EnumMember,
		Event,
		Field,
		Indexer,
		Interface,
		Method,
		Module,
		Property,
		Struct
	}

	public struct DeclaredSymbolInfo
	{
		public string FilePath { get; }
		public string Name { get; }
//		public string ContainerDisplayName { get; }
		public string FullyQualifiedContainerName { get; }
		public DeclaredSymbolInfoKind Kind { get; }
		public TextSpan Span { get; }
		public ushort ParameterCount { get; }
		public ushort TypeParameterCount { get; }


		public DeclaredSymbolInfo(SyntaxNode node, string name, string fullyQualifiedContainerName, DeclaredSymbolInfoKind kind, TextSpan span, ushort parameterCount = 0, ushort typeParameterCount = 0)
			: this()
		{
			FilePath = node.SyntaxTree.FilePath;
			Name = string.Intern (name);
//			ContainerDisplayName = string.Intern (containerDisplayName);
			FullyQualifiedContainerName = fullyQualifiedContainerName;
			Kind = kind;
			Span = span;
			ParameterCount = parameterCount;
			TypeParameterCount = typeParameterCount;
		}

		public async Task<ISymbol> GetSymbolAsync(Document document, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var node = root.FindNode(Span);
			var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
			return symbol;
		}
	}
}
