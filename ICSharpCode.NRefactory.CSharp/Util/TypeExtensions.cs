//
// TypeExtensions.cs
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
using Microsoft.CodeAnalysis.Simplification;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Threading;

namespace ICSharpCode.NRefactory6.CSharp
{
	public static class TypeExtensions
	{
		public static TypeSyntax GenerateTypeSyntax(this ITypeSymbol typeSymbol, SyntaxAnnotation simplifierAnnotation = null)
        {
			var typeSyntax = Microsoft.CodeAnalysis.CSharp.Extensions.ITypeSymbolExtensions.GenerateTypeSyntax(typeSymbol);
			if (simplifierAnnotation != null)
				return typeSyntax.WithAdditionalAnnotations(simplifierAnnotation);
			return typeSyntax;
        }
		
		#region GetDelegateInvokeMethod
		/// <summary>
		/// Gets the invoke method for a delegate type.
		/// </summary>
		/// <remarks>
		/// Returns null if the type is not a delegate type; or if the invoke method could not be found.
		/// </remarks>
		public static IMethodSymbol GetDelegateInvokeMethod(this ITypeSymbol type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (type.TypeKind == TypeKind.Delegate)
				return type.GetMembers ("Invoke").OfType<IMethodSymbol>().FirstOrDefault(m => m.MethodKind == MethodKind.DelegateInvoke);
			return null;
		}
		#endregion
		
		public static Task<IEnumerable<INamedTypeSymbol>> FindImplementingTypesAsync(this INamedTypeSymbol type, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Microsoft.CodeAnalysis.FindSymbols.DependentTypeFinder.FindImplementingTypesAsync(type, solution, projects, cancellationToken);
        }
 	}
}

