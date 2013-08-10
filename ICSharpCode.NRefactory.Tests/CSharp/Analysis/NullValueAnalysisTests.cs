//
// NullValueAnalysisTests.cs
//
// Author:
//       Luís Reis <luiscubal@gmail.com
//
// Copyright (c) 2013 Luís Reis
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
using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using System.Threading;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.NRefactory.CSharp.Analysis
{
	[TestFixture]
	public class NullValueAnalysisTests
	{
		NullValueAnalysis CreateNullValueAnalysis(MethodDeclaration methodDeclaration)
		{
			var type = new TypeDeclaration {
				Name = "DummyClass",
				ClassType = ClassType.Class
			};
			type.Members.Add(methodDeclaration);
			var tree = new SyntaxTree() { FileName = "test.cs" };
			tree.Members.Add(type);

			IProjectContent pc = new CSharpProjectContent();
			pc = pc.AddAssemblyReferences(CecilLoaderTests.Mscorlib);
			pc = pc.AddOrUpdateFiles(new[] { tree.ToTypeSystem() });
			var compilation = pc.CreateCompilation();
			var resolver = new CSharpResolver(compilation);
			var astResolver = new CSharpAstResolver(resolver, tree);
			return new NullValueAnalysis(methodDeclaration, astResolver, CancellationToken.None);
		}

		ParameterDeclaration CreatePrimitiveParameter(string typeKeyword = "string", string parameterName = "p")
		{
			return new ParameterDeclaration(new PrimitiveType(typeKeyword), parameterName);
		}

		[Test]
		public void TestSimple()
		{
			var method = new MethodDeclaration {
				Body = new BlockStatement {
					new ExpressionStatement(new AssignmentExpression(
						new IdentifierExpression("p"), new NullReferenceExpression())),
					new ExpressionStatement(new AssignmentExpression(
						new IdentifierExpression("p"), new PrimitiveExpression("Hello"))),
					new ReturnStatement()
				}
			};
			method.Parameters.Add(CreatePrimitiveParameter());

			var analysis = CreateNullValueAnalysis(method);
			var stmt1 = method.Body.Statements.First();
			var stmt2 = method.Body.Statements.ElementAt(1);
			
			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(stmt1, "p"));
			Assert.AreEqual(NullValueStatus.DefinitelyNull, analysis.GetVariableStatusBeforeStatement(stmt2, "p"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusAfterStatement(stmt2, "p"));
		}

		[Test]
		public void TestIfStatement()
		{
			var method = new MethodDeclaration {
				Body = new BlockStatement {
					new IfElseStatement {
						Condition = new BinaryOperatorExpression(new IdentifierExpression("p"),
						                                         BinaryOperatorType.Equality,
						                                         new NullReferenceExpression()),
						TrueStatement = new ExpressionStatement(new AssignmentExpression(
							new IdentifierExpression("p"),
							new PrimitiveExpression("Hello")))
					},
					new ReturnStatement()
				}
			};
			method.Parameters.Add(CreatePrimitiveParameter());

			var analysis = CreateNullValueAnalysis(method);
			var stmt1 = (IfElseStatement) method.Body.Statements.First();
			var stmt2 = (ExpressionStatement) stmt1.TrueStatement;
			var stmt3 = (ReturnStatement)method.Body.Statements.ElementAt(1);

			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(stmt1, "p"));
			Assert.AreEqual(NullValueStatus.DefinitelyNull, analysis.GetVariableStatusBeforeStatement(stmt2, "p"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusAfterStatement(stmt2, "p"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusBeforeStatement(stmt3, "p"));
		}
	}
}

