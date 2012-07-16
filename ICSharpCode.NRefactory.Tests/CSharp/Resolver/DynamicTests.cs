using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver {
	[TestFixture]
	public class DynamicTests : ResolverTestBase {
		[Test]
		public void AccessToDynamicMember() {
			string program = @"using System;
class TestClass {
	void F() {
		dynamic obj = null;
		$obj.SomeProperty$ = 10;
	}
}";
			var rr = Resolve<DynamicMemberResolveResult>(program);
			Assert.That(rr.Type.Kind, Is.EqualTo(TypeKind.Dynamic));
			Assert.That(rr.Target is LocalResolveResult && ((LocalResolveResult)rr.Target).Variable.Name == "obj");
			Assert.That(rr.Member, Is.EqualTo("SomeProperty"));
		}

		[Test]
		public void DynamicInvocation() {
			string program = @"using System;
class TestClass {
	void F() {
		dynamic obj = null;
		int a = 0;
		string b = null;
		$obj.SomeMethod(a, b)$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.Type.Kind, Is.EqualTo(TypeKind.Dynamic));
			Assert.That(rr.IsIndexing, Is.False);
			Assert.That(rr.Target, Is.InstanceOf<DynamicMemberResolveResult>());
			var dynamicMember = (DynamicMemberResolveResult)rr.Target;
			Assert.That(dynamicMember.Target is LocalResolveResult && ((LocalResolveResult)dynamicMember.Target).Variable.Name == "obj");
			Assert.That(dynamicMember.Member, Is.EqualTo("SomeMethod"));
			Assert.That(rr.Arguments.Count, Is.EqualTo(2));
			Assert.That(rr.Arguments[0].Name, Is.Null);
			Assert.That(rr.Arguments[0].Value is ConversionResolveResult && ((ConversionResolveResult)rr.Arguments[0].Value).Input is LocalResolveResult && ((LocalResolveResult)((ConversionResolveResult)rr.Arguments[0].Value).Input).Variable.Name == "a");
			Assert.That(rr.Arguments[1].Name, Is.Null);
			Assert.That(rr.Arguments[1].Value is ConversionResolveResult && ((ConversionResolveResult)rr.Arguments[1].Value).Input is LocalResolveResult && ((LocalResolveResult)((ConversionResolveResult)rr.Arguments[1].Value).Input).Variable.Name == "b");
		}

		[Test]
		public void DynamicInvocationWithNamedArguments() {
			string program = @"using System;
class TestClass {
	void F() {
		dynamic obj = null;
		int a = 0, x = 0;
		string b = null;
		$obj.SomeMethod(x, param1: a, param2: b)$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.Type.Kind, Is.EqualTo(TypeKind.Dynamic));
			Assert.That(rr.IsIndexing, Is.False);
			Assert.That(rr.Target, Is.InstanceOf<DynamicMemberResolveResult>());
			var dynamicMember = (DynamicMemberResolveResult)rr.Target;
			Assert.That(dynamicMember.Target is LocalResolveResult && ((LocalResolveResult)dynamicMember.Target).Variable.Name == "obj");
			Assert.That(dynamicMember.Member, Is.EqualTo("SomeMethod"));
			Assert.That(rr.Arguments.Count, Is.EqualTo(3));
			Assert.That(rr.Arguments[0].Name, Is.Null);
			Assert.That(rr.Arguments[0].Value is ConversionResolveResult && ((ConversionResolveResult)rr.Arguments[0].Value).Input is LocalResolveResult && ((LocalResolveResult)((ConversionResolveResult)rr.Arguments[0].Value).Input).Variable.Name == "x");
			Assert.That(rr.Arguments[1].Name, Is.EqualTo("param1"));
			Assert.That(rr.Arguments[1].Value is ConversionResolveResult && ((ConversionResolveResult)rr.Arguments[1].Value).Input is LocalResolveResult && ((LocalResolveResult)((ConversionResolveResult)rr.Arguments[1].Value).Input).Variable.Name == "a");
			Assert.That(rr.Arguments[2].Name, Is.EqualTo("param2"));
			Assert.That(rr.Arguments[2].Value is ConversionResolveResult && ((ConversionResolveResult)rr.Arguments[2].Value).Input is LocalResolveResult && ((LocalResolveResult)((ConversionResolveResult)rr.Arguments[2].Value).Input).Variable.Name == "b");
		}

		[Test]
		public void TwoDynamicInvocationsInARow() {
			string program = @"using System;
class TestClass {
	void F() {
		dynamic obj = null;
		int a = 0, b = 0;
		$obj.SomeMethod(a)(b)$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.Type.Kind, Is.EqualTo(TypeKind.Dynamic));
			Assert.That(rr.IsIndexing, Is.False);
			Assert.That(rr.Target, Is.InstanceOf<DynamicInvocationResolveResult>());
			var innerInvocation = (DynamicInvocationResolveResult)rr.Target;
			Assert.That(innerInvocation.Target, Is.InstanceOf<DynamicMemberResolveResult>());
			var dynamicMember = (DynamicMemberResolveResult)innerInvocation.Target;
			Assert.That(dynamicMember.Target is LocalResolveResult && ((LocalResolveResult)dynamicMember.Target).Variable.Name == "obj");
			Assert.That(dynamicMember.Member, Is.EqualTo("SomeMethod"));
			Assert.That(innerInvocation.IsIndexing, Is.False);
			Assert.That(innerInvocation.Arguments.Count, Is.EqualTo(1));
			Assert.That(innerInvocation.Arguments[0].Name, Is.Null);
			Assert.That(innerInvocation.Arguments[0].Value is ConversionResolveResult && ((ConversionResolveResult)innerInvocation.Arguments[0].Value).Input is LocalResolveResult && ((LocalResolveResult)((ConversionResolveResult)innerInvocation.Arguments[0].Value).Input).Variable.Name == "a");
			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments[0].Name, Is.Null);
			Assert.That(rr.Arguments[0].Value is ConversionResolveResult && ((ConversionResolveResult)rr.Arguments[0].Value).Input is LocalResolveResult && ((LocalResolveResult)((ConversionResolveResult)rr.Arguments[0].Value).Input).Variable.Name == "b");
		}

		[Test]
		public void InvocationWithDynamicArgumentWithOneApplicableMethod() {
			string program = @"using System;
class TestClass {
	public void SomeMethod(int a) {}
	public void SomeMethod(int a, string b) {}

	void F() {
		dynamic obj = null;
		var x = $this.SomeMethod(obj)$;
	}
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.That(rr.Member.Name, Is.EqualTo("SomeMethod"));
			Assert.That(((IParameterizedMember)rr.Member).Parameters.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			var cr = rr.Arguments[0] as ConversionResolveResult;
			Assert.That(cr, Is.Not.Null);
			Assert.That(cr.Conversion.IsImplicit, Is.True);
			Assert.That(cr.Conversion.IsDynamicConversion, Is.True);
			Assert.That(cr.Input is LocalResolveResult && ((LocalResolveResult)cr.Input).Variable.Name == "obj");
		}

		[Test]
		public void InvocationWithDynamicArgumentWithTwoApplicableMethods() {
			string program = @"using System;
class TestClass {
	public void SomeMethod(int a) {}
	public void SomeMethod(string a) {}
	public void SomeMethod(int a, string b) {}

	void F() {
		dynamic obj = null;
		var x = $SomeMethod(obj)$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.IsIndexing, Is.False);

			var mg = rr.Target as MethodGroupResolveResult;
			Assert.That(mg, Is.Not.Null, "Expected a MethodGroup");
			Assert.That(mg.TargetResult, Is.InstanceOf<ThisResolveResult>());
			Assert.That(mg.MethodName, Is.EqualTo("SomeMethod"));
			Assert.That(mg.Methods.All(m => m.Parameters.Count == 1));
			Assert.That(mg.Methods.Select(m => m.Parameters[0].Type.Name), Is.EquivalentTo(new[] { "Int32", "String" }));
			Assert.That(mg.Methods.All(m => m.Name == "SomeMethod" && m.DeclaringType.Name == "TestClass"));

			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments[0].Value is LocalResolveResult && ((LocalResolveResult)rr.Arguments[0].Value).Variable.Name == "obj");
		}

		[Test]
		public void InvocationWithDynamicArgumentWithTwoApplicableStaticMethods() {
			string program = @"using System;
class TestClass {
	public static void SomeMethod(int a) {}
	public static void SomeMethod(string a) {}
	public static void SomeMethod(int a, string b) {}

	void F() {
		dynamic obj = null;
		var x = $SomeMethod(obj)$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.IsIndexing, Is.False);

			var mg = rr.Target as MethodGroupResolveResult;
			Assert.That(mg, Is.Not.Null, "Expected a MethodGroup");
			Assert.That(mg.TargetResult, Is.InstanceOf<TypeResolveResult>());
			Assert.That(mg.MethodName, Is.EqualTo("SomeMethod"));
			Assert.That(mg.Methods.All(m => m.Parameters.Count == 1));
			Assert.That(mg.Methods.Select(m => m.Parameters[0].Type.Name), Is.EquivalentTo(new[] { "Int32", "String" }));
			Assert.That(mg.Methods.All(m => m.Name == "SomeMethod" && m.DeclaringType.Name == "TestClass"));

			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments[0].Value is LocalResolveResult && ((LocalResolveResult)rr.Arguments[0].Value).Variable.Name == "obj");
		}

		[Test]
		public void InvocationWithDynamicArgumentWithApplicableStaticAndNonStaticMethodsFavorTheNonStaticOne() {
			string program = @"using System;
class TestClass {
	public static void SomeMethod(int a) {}
	public void SomeMethod(string a) {}
	public static void SomeMethod(int a, string b) {}

	void F() {
		dynamic obj = null;
		var x = $SomeMethod(obj)$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.IsIndexing, Is.False);

			var mg = rr.Target as MethodGroupResolveResult;
			Assert.That(mg, Is.Not.Null, "Expected a MethodGroup");
			Assert.That(mg.TargetResult, Is.InstanceOf<ThisResolveResult>());
			Assert.That(mg.MethodName, Is.EqualTo("SomeMethod"));
			Assert.That(mg.Methods.All(m => m.Parameters.Count == 1));
			Assert.That(mg.Methods.Select(m => m.Parameters[0].Type.Name), Is.EquivalentTo(new[] { "Int32", "String" }));
			Assert.That(mg.Methods.All(m => m.Name == "SomeMethod" && m.DeclaringType.Name == "TestClass"));

			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments[0].Value is LocalResolveResult && ((LocalResolveResult)rr.Arguments[0].Value).Variable.Name == "obj");
		}

		[Test]
		public void InvocationWithDynamicArgumentWithTwoApplicableExtensionMethods() {
			string program = @"using System;
static class OtherClass {
	public void SomeMethod(this TestClass x, int a) {}
	public void SomeMethod(this TestClass x, string a) {}
	public void SomeMethod(this TestClass x, int a, string b) {}
}
class TestClass {
	void F() {
		dynamic obj = null;
		var x = $this.SomeMethod(obj)$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.IsIndexing, Is.False);

			var mg = rr.Target as MethodGroupResolveResult;
			Assert.That(mg, Is.Not.Null, "Expected a MethodGroup");
			Assert.That(mg.TargetResult, Is.InstanceOf<ThisResolveResult>());
			Assert.That(mg.MethodName, Is.EqualTo("SomeMethod"));
			Assert.That(mg.Methods.All(m => m.Parameters.Count == 2) && mg.Methods.All(m => m.Parameters[0].Type.Name == "TestClass"));
			Assert.That(mg.Methods.Select(m => m.Parameters[1].Type.Name), Is.EquivalentTo(new[] { "Int32", "String" }));
			Assert.That(mg.Methods.All(m => m.Name == "SomeMethod" && m.DeclaringType.Name == "OtherClass"));

			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments[0].Value is LocalResolveResult && ((LocalResolveResult)rr.Arguments[0].Value).Variable.Name == "obj");
		}

		[Test]
		public void InvocationWithDynamicArgumentWithTwoApplicableMethodsAndNamedArguments() {
			string program = @"using System;
class TestClass {
	public void SomeMethod(int a, int i) {}
	public void SomeMethod(string a, int i) {}
	public void SomeMethod(int a, string b, int i) {}

	void F() {
		dynamic obj = null;
		int idx = 0;
		var x = $this.SomeMethod(a: obj, i: idx)$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.IsIndexing, Is.False);

			var mg = rr.Target as MethodGroupResolveResult;
			Assert.That(mg, Is.Not.Null, "Expected a MethodGroup");
			Assert.That(mg.TargetResult, Is.InstanceOf<ThisResolveResult>());
			Assert.That(mg.MethodName, Is.EqualTo("SomeMethod"));
			Assert.That(mg.Methods.All(m => m.Parameters.Count == 2) && mg.Methods.All(m => m.Parameters[1].Type.Name == "Int32"));
			Assert.That(mg.Methods.Select(m => m.Parameters[0].Type.Name), Is.EquivalentTo(new[] { "Int32", "String" }));
			Assert.That(mg.Methods.All(m => m.Name == "SomeMethod" && m.DeclaringType.Name == "TestClass"));

			Assert.That(rr.Arguments.Count, Is.EqualTo(2));
			Assert.That(rr.Arguments[0].Name, Is.EqualTo("a"));
			Assert.That(rr.Arguments[0].Value is LocalResolveResult && ((LocalResolveResult)rr.Arguments[0].Value).Variable.Name == "obj");
			Assert.That(rr.Arguments[1].Name, Is.EqualTo("i"));
			Assert.That(rr.Arguments[1].Value is ConversionResolveResult && ((ConversionResolveResult)rr.Arguments[1].Value).Input is LocalResolveResult && ((LocalResolveResult)((ConversionResolveResult)rr.Arguments[1].Value).Input).Variable.Name == "idx");
		}

		[Test]
		public void IndexingDynamicObjectWithUnnamedArguments() {
			string program = @"using System;
class TestClass {
	void F() {
		dynamic obj = null;
		int a = 0, b = 0;
		object o = $obj[a]$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.Type.Kind, Is.EqualTo(TypeKind.Dynamic));
			Assert.That(rr.IsIndexing, Is.True);
			Assert.That(rr.Target is LocalResolveResult && ((LocalResolveResult)rr.Target).Variable.Name == "obj");
			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments[0].Name, Is.Null);
			Assert.That(rr.Arguments[0].Value is ConversionResolveResult && ((ConversionResolveResult)rr.Arguments[0].Value).Input is LocalResolveResult && ((LocalResolveResult)((ConversionResolveResult)rr.Arguments[0].Value).Input).Variable.Name == "a");
		}

		[Test]
		public void IndexingDynamicObjectWithNamedArguments() {
			string program = @"using System;
class TestClass {
	void F() {
		dynamic obj = null;
		int a = 0, b = 0;
		$obj[arg1: a, arg2: b]$ = 1;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.Type.Kind, Is.EqualTo(TypeKind.Dynamic));
			Assert.That(rr.IsIndexing, Is.True);
			Assert.That(rr.Target is LocalResolveResult && ((LocalResolveResult)rr.Target).Variable.Name == "obj");
			Assert.That(rr.Arguments.Count, Is.EqualTo(2));
			Assert.That(rr.Arguments[0].Name, Is.EqualTo("arg1"));
			Assert.That(rr.Arguments[0].Value is ConversionResolveResult && ((ConversionResolveResult)rr.Arguments[0].Value).Input is LocalResolveResult && ((LocalResolveResult)((ConversionResolveResult)rr.Arguments[0].Value).Input).Variable.Name == "a");
			Assert.That(rr.Arguments[1].Name, Is.EqualTo("arg2"));
			Assert.That(rr.Arguments[1].Value is ConversionResolveResult && ((ConversionResolveResult)rr.Arguments[1].Value).Input is LocalResolveResult && ((LocalResolveResult)((ConversionResolveResult)rr.Arguments[1].Value).Input).Variable.Name == "b");
		}

		[Test]
		public void IndexingWithDynamicArgumentWithOneApplicableIndexer() {
			string program = @"using System;
class TestClass {
	public int this[int a] { get { return 0; } }
	public int this[int a, string b] { get { return 0; } }

	void F() {
		dynamic obj = null;
		var x = $this[obj]$;
	}
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.That(rr.Member.Name, Is.EqualTo("Item"));
			Assert.That(((IParameterizedMember)rr.Member).Parameters.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			var cr = rr.Arguments[0] as ConversionResolveResult;
			Assert.That(cr, Is.Not.Null);
			Assert.That(cr.Conversion.IsImplicit, Is.True);
			Assert.That(cr.Conversion.IsDynamicConversion, Is.True);
			Assert.That(cr.Input is LocalResolveResult && ((LocalResolveResult)cr.Input).Variable.Name == "obj");
		}

		[Test]
		public void IndexingWithDynamicArgumentWithTwoApplicableIndexersAndUnnamedArguments() {
			string program = @"using System;
class TestClass {
	public int this[int a] { get { return 0; } }
	public int this[string a] { get { return 0; } }
	void F() {
		dynamic obj = null;
		var x = $this[obj]$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.Type.Kind, Is.EqualTo(TypeKind.Dynamic));
			Assert.That(rr.IsIndexing, Is.True);
			Assert.That(rr.Target, Is.InstanceOf<ThisResolveResult>());
			Assert.That(rr.Arguments.Count, Is.EqualTo(1));
			Assert.That(rr.Arguments[0].Name, Is.Null);
			Assert.That(rr.Arguments[0].Value is LocalResolveResult && ((LocalResolveResult)rr.Arguments[0].Value).Variable.Name == "obj");
		}

		[Test]
		public void IndexingWithDynamicArgumentWithTwoApplicableIndexersAndNamedArguments() {
			string program = @"using System;
class TestClass {
	public int this[int a, int i] { get { return 0; } }
	public int this[string a, int i] { get { return 0; } }
	void F() {
		dynamic obj = null;
		int idx = 0;
		var x = $this[a: obj, i: idx]$;
	}
}";
			var rr = Resolve<DynamicInvocationResolveResult>(program);
			Assert.That(rr.Type.Kind, Is.EqualTo(TypeKind.Dynamic));
			Assert.That(rr.IsIndexing, Is.True);
			Assert.That(rr.Target, Is.InstanceOf<ThisResolveResult>());
			Assert.That(rr.Arguments.Count, Is.EqualTo(2));
			Assert.That(rr.Arguments[0].Name, Is.EqualTo("a"));
			Assert.That(rr.Arguments[0].Value is LocalResolveResult && ((LocalResolveResult)rr.Arguments[0].Value).Variable.Name == "obj");
			Assert.That(rr.Arguments[1].Name, Is.EqualTo("i"));
			Assert.That(rr.Arguments[1].Value is ConversionResolveResult && ((ConversionResolveResult)rr.Arguments[1].Value).Input is LocalResolveResult && ((LocalResolveResult)((ConversionResolveResult)rr.Arguments[1].Value).Input).Variable.Name == "idx");
		}
	}
}
