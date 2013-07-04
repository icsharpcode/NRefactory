using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.CSharp;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.TypeSystem {
	[TestFixture]
	public class InheritanceHelperTests {
		[Test]
		public void DynamicAndObjectShouldBeConsideredTheSameTypeWhenMatchingSignatures() {
			string program = @"using System.Collections.Generic;
public class Base {
	public virtual void M1(object p) {}
	public virtual void M2(List<object> p) {}
	public virtual object M3() { return null; }
	public virtual List<object> M4() { return null; }
	public virtual void M5(dynamic p) {}
	public virtual void M6(List<dynamic> p) {}
	public virtual dynamic M7() { return null; }
	public virtual List<dynamic> M8() { return null; }
}

public class Derived : Base {
	public override void M1(dynamic p) {}
	public override void M2(List<dynamic> p) {}
	public override dynamic M3() { return null; }
	public override List<dynamic> M4() { return null; }
	public override void M5(object p) {}
	public override void M6(List<object> p) {}
	public override object M7() { return null; }
	public override List<object> M8() { return null; }
}";

			var unresolvedFile = new CSharpParser().Parse(program, "program.cs").ToTypeSystem();
			var compilation = new CSharpProjectContent().AddAssemblyReferences(CecilLoaderTests.Mscorlib).AddOrUpdateFiles(unresolvedFile).CreateCompilation();

			var dtype = (ITypeDefinition)ReflectionHelper.ParseReflectionName("Derived").Resolve(compilation);
			var btype = (ITypeDefinition)ReflectionHelper.ParseReflectionName("Base").Resolve(compilation);

			foreach (var name in new[] { "M1", "M2", "M3", "M4", "M5", "M6", "M7", "M8" }) {
				Assert.That(InheritanceHelper.GetBaseMember(dtype.Methods.Single(m => m.Name == name)), Is.EqualTo(btype.Methods.Single(m => m.Name == name)), name + " does not match");
			}

			foreach (var name in new[] { "M1", "M2", "M3", "M4", "M5", "M6", "M7", "M8" }) {
				Assert.That(InheritanceHelper.GetDerivedMember(btype.Methods.Single(m => m.Name == name), dtype), Is.EqualTo(dtype.Methods.Single(m => m.Name == name)), name + " does not match");
			}
		}
	}
}
