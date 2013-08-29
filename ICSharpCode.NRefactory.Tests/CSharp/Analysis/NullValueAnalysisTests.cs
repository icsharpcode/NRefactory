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
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using System.Threading;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.Analysis
{
	[TestFixture]
	public class NullValueAnalysisTests
	{
		class StubbedRefactoringContext : BaseRefactoringContext
		{
			bool supportsVersion5;

			StubbedRefactoringContext(CSharpAstResolver resolver, bool supportsVersion5) :
				base(resolver, CancellationToken.None) {
				this.supportsVersion5 = supportsVersion5;
			}

			internal static StubbedRefactoringContext Create(SyntaxTree tree, bool supportsVersion5 = true)
			{
				IProjectContent pc = new CSharpProjectContent();
				pc = pc.AddAssemblyReferences(CecilLoaderTests.Mscorlib);
				pc = pc.AddOrUpdateFiles(new[] {
					tree.ToTypeSystem()
				});
				var compilation = pc.CreateCompilation();
				var resolver = new CSharpAstResolver(compilation, tree);

				return new StubbedRefactoringContext(resolver, supportsVersion5);
			}

			#region implemented abstract members of BaseRefactoringContext
			public override int GetOffset(TextLocation location)
			{
				throw new NotImplementedException();
			}
			public override ICSharpCode.NRefactory.Editor.IDocumentLine GetLineByOffset(int offset)
			{
				throw new NotImplementedException();
			}
			public override TextLocation GetLocation(int offset)
			{
				throw new NotImplementedException();
			}
			public override string GetText(int offset, int length)
			{
				throw new NotImplementedException();
			}
			public override string GetText(ICSharpCode.NRefactory.Editor.ISegment segment)
			{
				throw new NotImplementedException();
			}
			#endregion


			public override bool Supports(Version version)
			{
				if (supportsVersion5)
					return version.Major <= 5;
				return version.Major <= 4;
			}
		}

		static NullValueAnalysis CreateNullValueAnalysis(SyntaxTree tree, MethodDeclaration methodDeclaration, bool supportsCSharp5 = true)
		{
			var ctx = StubbedRefactoringContext.Create(tree, supportsCSharp5);
			return new NullValueAnalysis(ctx, methodDeclaration, CancellationToken.None);
		}

		static NullValueAnalysis CreateNullValueAnalysis(MethodDeclaration methodDeclaration)
		{
			var type = new TypeDeclaration {
				Name = "DummyClass",
				ClassType = ClassType.Class
			};
			type.Members.Add(methodDeclaration);
			var tree = new SyntaxTree { FileName = "test.cs" };
			tree.Members.Add(type);

			return CreateNullValueAnalysis(tree, methodDeclaration);
		}

		static ParameterDeclaration CreatePrimitiveParameter(string typeKeyword, string parameterName)
		{
			return new ParameterDeclaration(new PrimitiveType(typeKeyword), parameterName);
		}

		static ParameterDeclaration CreateStringParameter(string parameterName = "p")
		{
			return CreatePrimitiveParameter("string", parameterName);
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
			method.Parameters.Add(CreateStringParameter());

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
			method.Parameters.Add(CreateStringParameter());

			var analysis = CreateNullValueAnalysis(method);
			var stmt1 = (IfElseStatement)method.Body.Statements.First();
			var stmt2 = (ExpressionStatement)stmt1.TrueStatement;
			var stmt3 = (ReturnStatement)method.Body.Statements.ElementAt(1);

			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(stmt1, "p"));
			Assert.AreEqual(NullValueStatus.DefinitelyNull, analysis.GetVariableStatusBeforeStatement(stmt2, "p"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusAfterStatement(stmt2, "p"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusBeforeStatement(stmt3, "p"));
		}

		[Test]
		public void TestEndlessLoop()
		{
			var method = new MethodDeclaration {
				Body = new BlockStatement {
					new VariableDeclarationStatement(new PrimitiveType("string"),
					                                 "p2", new NullReferenceExpression()),
					new WhileStatement {
						Condition = new BinaryOperatorExpression(new IdentifierExpression("p1"),
						                                 BinaryOperatorType.Equality,
						                                 new NullReferenceExpression()),
						EmbeddedStatement = new ExpressionStatement(
							new AssignmentExpression(new IdentifierExpression("p2"),
						                         AssignmentOperatorType.Assign,
						                         new PrimitiveExpression("")))
					},
					new ReturnStatement()
				}
			};
			method.Parameters.Add(CreateStringParameter("p1"));

			var analysis = CreateNullValueAnalysis(method);
			var stmt1 = (WhileStatement)method.Body.Statements.ElementAt(1);
			var stmt2 = (ExpressionStatement)stmt1.EmbeddedStatement;
			var stmt3 = (ReturnStatement)method.Body.Statements.ElementAt(2);

			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(stmt1, "p1"));
			Assert.AreEqual(NullValueStatus.DefinitelyNull, analysis.GetVariableStatusBeforeStatement(stmt1, "p2"));
			Assert.AreEqual(NullValueStatus.DefinitelyNull, analysis.GetVariableStatusBeforeStatement(stmt2, "p1"));
			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(stmt2, "p2"));
			Assert.AreEqual(NullValueStatus.DefinitelyNull, analysis.GetVariableStatusAfterStatement(stmt2, "p1"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusAfterStatement(stmt2, "p2"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusBeforeStatement(stmt3, "p1"));
			Assert.AreEqual(NullValueStatus.DefinitelyNull, analysis.GetVariableStatusBeforeStatement(stmt3, "p2"));
		}

		[Test]
		public void TestLoop()
		{
			var method = new MethodDeclaration {
				Body = new BlockStatement {
					new VariableDeclarationStatement(new PrimitiveType("string"),
					                                 "p2", new NullReferenceExpression()),
					new WhileStatement {
						Condition = new BinaryOperatorExpression(new IdentifierExpression("p1"),
						                                         BinaryOperatorType.Equality,
						                                         new NullReferenceExpression()),
						EmbeddedStatement = new BlockStatement {
							new ExpressionStatement(
								new AssignmentExpression(new IdentifierExpression("p2"),
						                         AssignmentOperatorType.Assign,
						                         new PrimitiveExpression(""))),
							new ExpressionStatement(
								new AssignmentExpression(new IdentifierExpression("p1"),
							                         AssignmentOperatorType.Assign,
							                         new PrimitiveExpression("")))
						}
					},
					new ReturnStatement()
				}
			};
			method.Parameters.Add(CreateStringParameter("p1"));

			var analysis = CreateNullValueAnalysis(method);
			var stmt1 = (WhileStatement)method.Body.Statements.ElementAt(1);
			var stmt2 = (ExpressionStatement)((BlockStatement)stmt1.EmbeddedStatement).Statements.Last();
			var stmt3 = (ReturnStatement)method.Body.Statements.ElementAt(2);

			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(stmt1, "p1"));
			Assert.AreEqual(NullValueStatus.DefinitelyNull, analysis.GetVariableStatusBeforeStatement(stmt1, "p2"));
			Assert.AreEqual(NullValueStatus.DefinitelyNull, analysis.GetVariableStatusBeforeStatement(stmt2, "p1"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusBeforeStatement(stmt2, "p2"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusAfterStatement(stmt2, "p1"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusAfterStatement(stmt2, "p2"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusBeforeStatement(stmt3, "p1"));
			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(stmt3, "p2"));
		}

		ExpressionStatement MakeStatement(Expression expr)
		{
			return new ExpressionStatement(expr);
		}

		[Test]
		public void TestForLoop()
		{
			var forStatement = new ForStatement();
			forStatement.Initializers.Add(MakeStatement(new AssignmentExpression(new IdentifierExpression("p2"),
			                                                                     new PrimitiveExpression(""))));
			forStatement.Condition = new BinaryOperatorExpression(new IdentifierExpression("p1"),
			                                                      BinaryOperatorType.Equality,
			                                                      new NullReferenceExpression());
			forStatement.Iterators.Add(MakeStatement(new AssignmentExpression(new IdentifierExpression("p2"),
			                                                                  AssignmentOperatorType.Assign,
			                                                                  new NullReferenceExpression())));
			forStatement.EmbeddedStatement = MakeStatement(new AssignmentExpression(new IdentifierExpression("p1"),
			                                                                        AssignmentOperatorType.Assign,
			                                                                        new PrimitiveExpression("")));
			var method = new MethodDeclaration {
				Body = new BlockStatement {
					forStatement,
					new ReturnStatement()
				}
			};
			method.Parameters.Add(CreateStringParameter("p1"));
			method.Parameters.Add(CreateStringParameter("p2"));

			var returnStatement = (ReturnStatement)method.Body.Statements.Last();
			var content = forStatement.EmbeddedStatement;

			var analysis = CreateNullValueAnalysis(method);

			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(forStatement, "p1"));
			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(forStatement, "p2"));
			Assert.AreEqual(NullValueStatus.DefinitelyNull, analysis.GetVariableStatusBeforeStatement(content, "p1"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusBeforeStatement(content, "p2"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusAfterStatement(content, "p2"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusBeforeStatement(returnStatement, "p1"));
			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(returnStatement, "p2"));
		}

		[Test]
		public void TestNullCoallescing()
		{
			var method = new MethodDeclaration {
				Body = new BlockStatement {
					new ExpressionStatement(new AssignmentExpression(new IdentifierExpression("p1"),
					                                                 new BinaryOperatorExpression(new IdentifierExpression("p1"),
					                             BinaryOperatorType.NullCoalescing,
					                             new PrimitiveExpression(""))))
				}
			};

			method.Parameters.Add(CreateStringParameter("p1"));

			var analysis = CreateNullValueAnalysis(method);
			var stmt = method.Body.Statements.Single();

			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusAfterStatement(stmt, "p1"));
		}

		[Test]
		public void TestCapturedLambdaVariables()
		{
			var method = new MethodDeclaration {
				Body = new BlockStatement {
					new VariableDeclarationStatement(AstType.Create("System.Action"),
					                                 "action",
					                                 new LambdaExpression {
						Body = new BlockStatement {
							MakeStatement(new AssignmentExpression(new IdentifierExpression("p1"),
							                                       new NullReferenceExpression()))
						}
					}),
					MakeStatement(new AssignmentExpression(new IdentifierExpression("p1"),
					                                       new NullReferenceExpression())),
					new ExpressionStatement(new InvocationExpression(new IdentifierExpression("action"))),
					MakeStatement(new AssignmentExpression(new IdentifierExpression("p3"),
					                                       new IdentifierExpression("p1")))
				}
			};

			method.Parameters.Add(CreateStringParameter("p1"));
			method.Parameters.Add(CreateStringParameter("p2"));
			method.Parameters.Add(CreateStringParameter("p3"));

			var analysis = CreateNullValueAnalysis(method);
			var declareLambda = (VariableDeclarationStatement)method.Body.Statements.First();
			var lastStatement = (ExpressionStatement)method.Body.Statements.Last();

			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(declareLambda, "p1"));
			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(declareLambda, "p2"));
			Assert.AreEqual(NullValueStatus.CapturedUnknown, analysis.GetVariableStatusBeforeStatement(lastStatement, "p1"));
			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(lastStatement, "p2"));
			Assert.AreEqual(NullValueStatus.Unknown, analysis.GetVariableStatusAfterStatement(lastStatement, "p3"));
		}

		[Test]
		public void TestInvocation()
		{
			var parser = new CSharpParser();
			var tree = parser.Parse(@"
delegate void MyDelegate(string p1, out string p2);
class TestClass
{
	void TestMethod()
	{
		string p1 = null;
		string p2 = null;
		MyDelegate del = (string a, out string b) => { b = a; };
		del(p1 = """", out p2);
	}
}
", "test.cs");
			Assert.AreEqual(0, tree.Errors.Count);

			var method = tree.Descendants.OfType<MethodDeclaration>().Single();
			var analysis = CreateNullValueAnalysis(tree, method);

			var lastStatement = (ExpressionStatement)method.Body.Statements.Last();

			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusAfterStatement(lastStatement, "p1"));
			Assert.AreEqual(NullValueStatus.Unknown, analysis.GetVariableStatusAfterStatement(lastStatement, "p2"));
		}

		[Test]
		public void TestUncaptureVariable()
		{
			var parser = new CSharpParser();
			var tree = parser.Parse(@"
using System;
class TestClass
{
	void TestMethod()
	{
		while (true) {
			string p1 = null;
			Action action = () => { p1 = """"; };
		}
	}
}
", "test.cs");
			Assert.AreEqual(0, tree.Errors.Count);

			var method = tree.Descendants.OfType<MethodDeclaration>().Single();
			var analysis = CreateNullValueAnalysis(tree, method);

			var whileStatement = (WhileStatement)method.Body.Statements.Single();
			var whileBlock = (BlockStatement)whileStatement.EmbeddedStatement;
			var actionStatement = whileBlock.Statements.Last();

			Assert.AreEqual(NullValueStatus.DefinitelyNull, analysis.GetVariableStatusBeforeStatement(actionStatement, "p1"));
			Assert.AreEqual(NullValueStatus.CapturedUnknown, analysis.GetVariableStatusAfterStatement(actionStatement, "p1"));
		}

		[Test]
		public void TestSimpleForeach()
		{
			var parser = new CSharpParser();
			var tree = parser.Parse(@"
using System;
class TestClass
{
	void TestMethod()
	{
		int? accum = 0;
		foreach (var x in new int[] { 1, 2, 3}) {
			accum += x;
		}
	}
}
", "test.cs");
			Assert.AreEqual(0, tree.Errors.Count);

			var method = tree.Descendants.OfType<MethodDeclaration>().Single();
			var analysis = CreateNullValueAnalysis(tree, method);

			var lastStatement = (ForeachStatement)method.Body.Statements.Last();
			var content = (BlockStatement)lastStatement.EmbeddedStatement;

			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusBeforeStatement(content, "x"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusBeforeStatement(lastStatement, "accum"));
		}

		[Test]
		public void TestNonNullEnumeratedValue()
		{
			var parser = new CSharpParser();
			var tree = parser.Parse(@"
using System;
class TestClass
{
	void TestMethod()
	{
		int? accum = 0;
		foreach (var x in new int?[] { 1, 2, 3}) {
			accum += x;
		}
	}
}
", "test.cs");
			Assert.AreEqual(0, tree.Errors.Count);

			var method = tree.Descendants.OfType<MethodDeclaration>().Single();
			var analysis = CreateNullValueAnalysis(tree, method);

			var lastStatement = (ForeachStatement)method.Body.Statements.Last();
			var content = (BlockStatement)lastStatement.EmbeddedStatement;

			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusBeforeStatement(content, "x"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusBeforeStatement(lastStatement, "accum"));
		}

		[Test]
		public void TestCapturedForeachInCSharp5()
		{
			var parser = new CSharpParser();
			var tree = parser.Parse(@"
using System;
class TestClass
{
	void TestMethod()
	{
		int? accum = 0;
		foreach (var x in new int?[] { 1, 2, 3 }) {
			Action action = () => { x = null; };
			accum += x;
		}
	}
}
", "test.cs");
			Assert.AreEqual(0, tree.Errors.Count);

			var method = tree.Descendants.OfType<MethodDeclaration>().Single();
			var analysis = CreateNullValueAnalysis(tree, method, true);

			var foreachStatement = (ForeachStatement)method.Body.Statements.ElementAt(1);
			var foreachBody = (BlockStatement)foreachStatement.EmbeddedStatement;
			var action = foreachBody.Statements.First();
			var lastStatement = method.Body.Statements.Last();

			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusBeforeStatement(action, "x"));
			Assert.AreEqual(NullValueStatus.CapturedUnknown, analysis.GetVariableStatusAfterStatement(action, "x"));
			Assert.AreEqual(NullValueStatus.Unknown, analysis.GetVariableStatusAfterStatement(lastStatement, "accum"));
		}

		[Test]
		public void TestCapturedForeachInCSharp4()
		{
			var parser = new CSharpParser();
			var tree = parser.Parse(@"
using System;
class TestClass
{
	void TestMethod()
	{
		int? accum = 0;
		foreach (var x in new int?[] { 1, 2, 3}) {
			Action action = () => { x = null; };
			accum += x;
		}
	}
}
", "test.cs");
			Assert.AreEqual(0, tree.Errors.Count);

			var method = tree.Descendants.OfType<MethodDeclaration>().Single();
			var analysis = CreateNullValueAnalysis(tree, method, false);

			var foreachStatement = (ForeachStatement)method.Body.Statements.ElementAt(1);
			var foreachBody = (BlockStatement)foreachStatement.EmbeddedStatement;
			var action = foreachBody.Statements.First();
			var lastStatement = method.Body.Statements.Last();

			Assert.AreEqual(NullValueStatus.CapturedUnknown, analysis.GetVariableStatusBeforeStatement(action, "x"));
			Assert.AreEqual(NullValueStatus.CapturedUnknown, analysis.GetVariableStatusAfterStatement(action, "x"));
			Assert.AreEqual(NullValueStatus.Unknown, analysis.GetVariableStatusAfterStatement(lastStatement, "accum"));
		}

		[Test]
		public void TestForCapture()
		{
			var parser = new CSharpParser();
			var tree = parser.Parse(@"
using System;
class TestClass
{
	string TestMethod(string p)
	{
		for (int? i = 0; i < 10; ++i) {
			int? inI = i;
			Action action = () => { i = null; inI = null; };
		}
	}
}
", "test.cs");
			Assert.AreEqual(0, tree.Errors.Count);

			var method = tree.Descendants.OfType<MethodDeclaration>().Single();
			var analysis = CreateNullValueAnalysis(tree, method);
			var forStatement = (ForStatement)method.Body.Statements.Single();
			var forBody = (BlockStatement)forStatement.EmbeddedStatement;
			var actionStatement = forBody.Statements.Last();

			Assert.AreEqual(NullValueStatus.CapturedUnknown, analysis.GetVariableStatusBeforeStatement(actionStatement, "i"));
			Assert.AreEqual(NullValueStatus.CapturedUnknown, analysis.GetVariableStatusAfterStatement(actionStatement, "i"));
			Assert.AreEqual(NullValueStatus.Unknown, analysis.GetVariableStatusBeforeStatement(actionStatement, "inI"));
			Assert.AreEqual(NullValueStatus.CapturedUnknown, analysis.GetVariableStatusAfterStatement(actionStatement, "inI"));
		}

		[Test]
		public void TestExpressionState()
		{
			var parser = new CSharpParser();
			var tree = parser.Parse(@"
delegate void MyDelegate(string p1, out string p2);
class TestClass
{
	string TestMethod(string p)
	{
		if (p != null) p = """";
		return p;
	}
}
", "test.cs");
			Assert.AreEqual(0, tree.Errors.Count);

			var method = tree.Descendants.OfType<MethodDeclaration>().Single();
			var analysis = CreateNullValueAnalysis(tree, method);

			var lastStatement = (ReturnStatement)method.Body.Statements.Last();
			var expr = lastStatement.Expression;

			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetExpressionResult(expr));
		}

		[Test]
		public void TestCompileConstants()
		{
			var parser = new CSharpParser();
			var tree = parser.Parse(@"
class TestClass
{
	const int? value1 = null;
	const bool value2 = true;
	void TestMethod()
	{
		int? p1 = value2 ? value1 : 0;
	}
}
", "test.cs");
			Assert.AreEqual(0, tree.Errors.Count);

			var method = tree.Descendants.OfType<MethodDeclaration>().Single();
			var analysis = CreateNullValueAnalysis(tree, method);

			var lastStatement = (VariableDeclarationStatement)method.Body.Statements.Last();

			Assert.AreEqual(NullValueStatus.DefinitelyNull, analysis.GetVariableStatusAfterStatement(lastStatement, "p1"));
		}

		[Test]
		public void TestConditionalAnd()
		{
			var method = new MethodDeclaration {
				Body = new BlockStatement {
					new IfElseStatement {
						Condition = new BinaryOperatorExpression(
								new BinaryOperatorExpression(new IdentifierExpression("p1"),
						                                         BinaryOperatorType.Equality,
						                                         new NullReferenceExpression()),
								BinaryOperatorType.ConditionalAnd,
								new BinaryOperatorExpression(new IdentifierExpression("p2"),
						                                     BinaryOperatorType.Equality,
						                                     new NullReferenceExpression())),
						TrueStatement = new ExpressionStatement(new AssignmentExpression(
							new IdentifierExpression("p1"),
							new PrimitiveExpression("Hello")))
					},
					new ReturnStatement()
				}
			};
			method.Parameters.Add(CreateStringParameter("p1"));
			method.Parameters.Add(CreateStringParameter("p2"));

			var analysis = CreateNullValueAnalysis(method);
			var stmt1 = (IfElseStatement)method.Body.Statements.First();
			var stmt2 = (ExpressionStatement)stmt1.TrueStatement;
			var stmt3 = (ReturnStatement)method.Body.Statements.ElementAt(1);

			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(stmt1, "p1"));
			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(stmt1, "p2"));
			Assert.AreEqual(NullValueStatus.DefinitelyNull, analysis.GetVariableStatusBeforeStatement(stmt2, "p1"));
			Assert.AreEqual(NullValueStatus.DefinitelyNull, analysis.GetVariableStatusBeforeStatement(stmt2, "p2"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusAfterStatement(stmt2, "p1"));
			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(stmt3, "p2"));
		}

		[Test]
		public void TestConditionalOr()
		{
			var method = new MethodDeclaration {
				Body = new BlockStatement {
					new IfElseStatement {
						Condition = new UnaryOperatorExpression(UnaryOperatorType.Not,
						                                        new BinaryOperatorExpression(
							new BinaryOperatorExpression(new IdentifierExpression("p1"),
						                             BinaryOperatorType.Equality,
						                             new NullReferenceExpression()),
							BinaryOperatorType.ConditionalOr,
							new BinaryOperatorExpression(new IdentifierExpression("p2"),
						                             BinaryOperatorType.Equality,
						                             new NullReferenceExpression()))),
						TrueStatement = new ExpressionStatement(new AssignmentExpression(
							new IdentifierExpression("p1"),
							new NullReferenceExpression()))
					},
					new ReturnStatement()
				}
			};
			method.Parameters.Add(CreateStringParameter("p1"));
			method.Parameters.Add(CreateStringParameter("p2"));

			var analysis = CreateNullValueAnalysis(method);
			var stmt1 = (IfElseStatement)method.Body.Statements.First();
			var stmt2 = (ExpressionStatement)stmt1.TrueStatement;
			var stmt3 = (ReturnStatement)method.Body.Statements.ElementAt(1);

			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(stmt1, "p1"));
			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(stmt1, "p2"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusBeforeStatement(stmt2, "p1"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusBeforeStatement(stmt2, "p2"));
			Assert.AreEqual(NullValueStatus.DefinitelyNull, analysis.GetVariableStatusAfterStatement(stmt2, "p1"));
			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(stmt3, "p2"));
		}

		[Test]
		public void TestFinally()
		{
			var parser = new CSharpParser();
			var tree = parser.Parse(@"
class TestClass
{
	void TestMethod()
	{
		int? x = 1;
		int? y = 1;
		try {
			x = 2;
			x = null;
		} finally {
			y = null;
		}
	}
}
", "test.cs");
			Assert.AreEqual(0, tree.Errors.Count);

			var method = tree.Descendants.OfType<MethodDeclaration>().Single();
			var analysis = CreateNullValueAnalysis(tree, method);

			var tryFinally = (TryCatchStatement) method.Body.Statements.Last();
			var finallyStatement = tryFinally.FinallyBlock.Statements.Single();

			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusBeforeStatement(finallyStatement, "x"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusBeforeStatement(finallyStatement, "y"));
			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusAfterStatement(finallyStatement, "x"));
			Assert.AreEqual(NullValueStatus.DefinitelyNull, analysis.GetVariableStatusAfterStatement(finallyStatement, "y"));
			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusAfterStatement(tryFinally, "x"));
			Assert.AreEqual(NullValueStatus.DefinitelyNull, analysis.GetVariableStatusAfterStatement(tryFinally, "y"));
		}

		[Test]
		public void TestFinallyCapture()
		{
			var parser = new CSharpParser();
			var tree = parser.Parse(@"
using System;
class TestClass
{
	void TestMethod()
	{
		int? x = 1;
		try {
			Action a = () => { x = null; };
		} finally {
			Console.WriteLine(x);
		}
	}
}
", "test.cs");
			Assert.AreEqual(0, tree.Errors.Count);

			var method = tree.Descendants.OfType<MethodDeclaration>().Single();
			var analysis = CreateNullValueAnalysis(tree, method);

			var tryFinally = (TryCatchStatement) method.Body.Statements.Last();
			var finallyStatement = tryFinally.FinallyBlock.Statements.Single();

			Assert.AreEqual(NullValueStatus.CapturedUnknown, analysis.GetVariableStatusBeforeStatement(finallyStatement, "x"));
		}


		[Test]
		public void TestReturnInFinally()
		{
			var parser = new CSharpParser();
			var tree = parser.Parse(@"
class TestClass
{
	void TestMethod()
	{
		int? x = 1;
		int? y = 1;
		try {
			x = null;
			return;
		} finally {
			y = null;
		}
	}
}
", "test.cs");
			Assert.AreEqual(0, tree.Errors.Count);

			var method = tree.Descendants.OfType<MethodDeclaration>().Single();
			var analysis = CreateNullValueAnalysis(tree, method);

			var tryFinally = (TryCatchStatement) method.Body.Statements.Last();
			var finallyStatement = tryFinally.FinallyBlock.Statements.Single();

			//Make sure it's not unreachable
			Assert.AreEqual(NullValueStatus.Unknown, analysis.GetVariableStatusAfterStatement(finallyStatement, "x"));
			Assert.AreEqual(NullValueStatus.DefinitelyNull, analysis.GetVariableStatusAfterStatement(finallyStatement, "y"));
		}

		[Test]
		public void TestTryCatch()
		{
			var parser = new CSharpParser();
			var tree = parser.Parse(@"
using System;
class TestClass
{
	void TestMethod()
	{
		int? x = 1;
		int? y = 2;
		int? z = 3;
		try {
			x = null;
		} catch (Exception e) {
			x = null;
			y = 3;
			z = null;
		}
	}
}
", "test.cs");
			Assert.AreEqual(0, tree.Errors.Count);

			var method = tree.Descendants.OfType<MethodDeclaration>().Single();
			var analysis = CreateNullValueAnalysis(tree, method);

			var tryCatch = (TryCatchStatement) method.Body.Statements.Last();
			var catchStatement = tryCatch.CatchClauses.First().Body.Statements.First();

			Assert.AreEqual(NullValueStatus.Unknown, analysis.GetVariableStatusBeforeStatement(catchStatement, "x"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusBeforeStatement(catchStatement, "e"));
			Assert.AreEqual(NullValueStatus.DefinitelyNull, analysis.GetVariableStatusAfterStatement(tryCatch, "x"));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusAfterStatement(tryCatch, "y"));
			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusAfterStatement(tryCatch, "z"));
		}

		[Test]
		public void TestLinq()
		{
			var parser = new CSharpParser();
			var tree = parser.Parse(@"
using System;
class TestClass
{
	void TestMethod()
	{
		int?[] collection = new int?[10];
		var x = from item in collection
				where item != null
				select item
				into item2
				let item3 = item2
				select item3;
	}
}
", "test.cs");
			Assert.AreEqual(0, tree.Errors.Count);

			var method = tree.Descendants.OfType<MethodDeclaration>().Single();
			var analysis = CreateNullValueAnalysis(tree, method);

			var linqStatement = (VariableDeclarationStatement) method.Body.Statements.Last();
			var linqExpression = (QueryExpression)linqStatement.Variables.Single().Initializer;
			var continuation = (QueryContinuationClause)linqExpression.Clauses.First();
			var itemInWhere = ((BinaryOperatorExpression)((QueryWhereClause)continuation.PrecedingQuery.Clauses.ElementAt(1)).Condition).Left;
			var itemInSelect = ((QuerySelectClause)continuation.PrecedingQuery.Clauses.ElementAt(2)).Expression;
			var item2InLet = ((QueryLetClause)linqExpression.Clauses.ElementAt(1)).Expression;
			var item3InSelect = ((QuerySelectClause)linqExpression.Clauses.Last()).Expression;
			
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetVariableStatusAfterStatement(linqStatement, "x"));
			Assert.AreEqual(NullValueStatus.Unknown, analysis.GetExpressionResult(itemInWhere));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetExpressionResult(itemInSelect));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetExpressionResult(item2InLet));
			Assert.AreEqual(NullValueStatus.DefinitelyNotNull, analysis.GetExpressionResult(item3InSelect));
		}

		[Test]
		public void TestNoExecutionLinq()
		{
			var parser = new CSharpParser();
			var tree = parser.Parse(@"
using System;
class TestClass
{
	void TestMethod()
	{
		int?[] collection = new int?[0];
		var x = from item in collection
				where (collection = null) != null
				select item;
	}
}
", "test.cs");
			Assert.AreEqual(0, tree.Errors.Count);

			var method = tree.Descendants.OfType<MethodDeclaration>().Single();
			var analysis = CreateNullValueAnalysis(tree, method);

			var linqStatement = (VariableDeclarationStatement) method.Body.Statements.Last();

			Assert.AreEqual(NullValueStatus.PotentiallyNull, analysis.GetVariableStatusAfterStatement(linqStatement, "collection"));
		}
	}
}

