//
// CSharpCodeGenerationService.cs
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
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis;
using System.Threading;
using System.Reflection;

namespace ICSharpCode.NRefactory6.CSharp.CodeGeneration
{


	public class CSharpCodeGenerationService
	{
		readonly static Type typeInfo;
		readonly object instance;

		readonly static MethodInfo createEventDeclarationMethod;
		readonly static MethodInfo createFieldDeclaration;
		readonly static MethodInfo createMethodDeclaration;
		readonly static MethodInfo createPropertyDeclaration;
		readonly static MethodInfo createNamedTypeDeclaration;
		readonly static MethodInfo createNamespaceDeclaration;

		static CSharpCodeGenerationService ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationService" + ReflectionNamespaces.CSWorkspacesAsmName, true);
			addMethod = typeInfo.GetMethod ("AddMethod", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			createEventDeclarationMethod = typeInfo.GetMethod ("CreateEventDeclaration", BindingFlags.Instance | BindingFlags.Public);
			createFieldDeclaration = typeInfo.GetMethod ("CreateFieldDeclaration", BindingFlags.Instance | BindingFlags.Public);
			createMethodDeclaration = typeInfo.GetMethod ("CreateMethodDeclaration", BindingFlags.Instance | BindingFlags.Public);
			createPropertyDeclaration = typeInfo.GetMethod ("CreatePropertyDeclaration", BindingFlags.Instance | BindingFlags.Public);
			createNamedTypeDeclaration = typeInfo.GetMethod ("CreateNamedTypeDeclaration", BindingFlags.Instance | BindingFlags.Public);
			createNamespaceDeclaration = typeInfo.GetMethod ("CreateNamespaceDeclaration", BindingFlags.Instance | BindingFlags.Public);

		}

		public CSharpCodeGenerationService(HostLanguageServices languageServices)
		{
			instance = Activator.CreateInstance (typeInfo, new object[] {
				languageServices
			});
		}

		public CSharpCodeGenerationService (Workspace workspace)
		{
			var csharpLanguageServices = workspace.Services.GetLanguageServices (LanguageNames.CSharp);

			this.instance = Activator.CreateInstance (typeInfo, new [] { csharpLanguageServices });
		}


		static MethodInfo addMethod;

		/// <summary>
		/// Adds a method into destination.
		/// </summary>
		public TDeclarationNode AddMethod<TDeclarationNode>(TDeclarationNode destination, IMethodSymbol method, CodeGenerationOptions options = null, CancellationToken cancellationToken = default(CancellationToken)) where TDeclarationNode : SyntaxNode
		{
			return (TDeclarationNode)addMethod.MakeGenericMethod (typeof (TDeclarationNode)).Invoke (instance, new object[] { destination, method, options, cancellationToken });
		}


		/// <summary>
		/// Returns a newly created event declaration node from the provided event.
		/// </summary
		public SyntaxNode CreateEventDeclaration(IEventSymbol @event, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified)
		{
			return (SyntaxNode)createEventDeclarationMethod.Invoke (instance, new object[] { @event, destination, null });
		}

		/// <summary>
		/// Returns a newly created field declaration node from the provided field.
		/// </summary>
		public SyntaxNode CreateFieldDeclaration(IFieldSymbol field, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified)
		{
			return (SyntaxNode)createFieldDeclaration.Invoke (instance, new object[] { @field, destination, null });
		}

		/// <summary>
		/// Returns a newly created method declaration node from the provided method.
		/// </summary>
		public SyntaxNode CreateMethodDeclaration(IMethodSymbol method, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified)
		{
			return (SyntaxNode)createMethodDeclaration.Invoke (instance, new object[] { @method, destination, null });
		}

		/// <summary>
		/// Returns a newly created property declaration node from the provided property.
		/// </summary>
		public SyntaxNode CreatePropertyDeclaration(IPropertySymbol property, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified)
		{
			return (SyntaxNode)createPropertyDeclaration.Invoke (instance, new object[] { @property, destination, null });
		}

		/// <summary>
		/// Returns a newly created named type declaration node from the provided named type.
		/// </summary>
		public SyntaxNode CreateNamedTypeDeclaration(INamedTypeSymbol namedType, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified)
		{
			return (SyntaxNode)createNamedTypeDeclaration.Invoke (instance, new object[] { @namedType, destination, null });
		}

		/// <summary>
		/// Returns a newly created namespace declaration node from the provided namespace.
		/// </summary>
		public SyntaxNode CreateNamespaceDeclaration(INamespaceSymbol @namespace, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified)
		{
			return (SyntaxNode)createNamespaceDeclaration.Invoke (instance, new object[] { @namespace, destination, null });
		}

	}
}

