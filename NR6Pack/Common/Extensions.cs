using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ICSharpCode.NRefactory6.CSharp
{
	[Flags]
	enum BindingFlags
	{
		Public = 1,
		NonPublic = 2,
		Static = 4,
		Instance = 8
	}

	static class ReflectionWrappers
	{
		public static Attribute[] GetCustomAttributes(this Type type, bool inherit)
		{
			return type.GetTypeInfo().GetCustomAttributes(inherit).ToArray();
		}

		public static MethodInfo GetMethod(this Type type, string name)
		{
			return type.GetTypeInfo().GetDeclaredMethod(name);
		}

		public static MethodInfo GetMethod(this Type type, string name, Type[] arguments)
		{
			return type.GetTypeInfo().GetDeclaredMethods(name).Single(m => m.GetParameters().Select(p => p.ParameterType).SequenceEqual(arguments));
		}

		public static MethodInfo GetMethod(this Type type, string name, BindingFlags flags)
		{
			var methods = type.GetTypeInfo().GetDeclaredMethods(name);
			bool isStatic = (flags & BindingFlags.Static) == BindingFlags.Static;
			bool isInstance = (flags & BindingFlags.Instance) == BindingFlags.Instance;
			bool isPublic = (flags & BindingFlags.Public) == BindingFlags.Public;
			bool isNonPublic = (flags & BindingFlags.NonPublic) == BindingFlags.NonPublic;

			return methods.Single(m => m.IsStatic == isStatic && m.IsStatic != isInstance && m.IsPublic == isPublic && m.IsPrivate == isNonPublic);
		}

		public static PropertyInfo GetProperty(this Type type, string name)
		{
			return type.GetTypeInfo().GetDeclaredProperty(name);
		}

		public static FieldInfo GetField(this Type type, string name)
		{
			return type.GetTypeInfo().GetDeclaredField(name);
		}

		public static PropertyInfo[] GetProperties(this Type type)
		{
			return type.GetTypeInfo().DeclaredProperties.ToArray();
		}

		public static MethodInfo[] GetMethods(this Type type)
		{
			return type.GetTypeInfo().DeclaredMethods.ToArray();
		}

		public static FieldInfo GetField(this Type type, string name, BindingFlags flags)
		{
			// flags are not supported!
			return type.GetTypeInfo().GetDeclaredField(name);
		}
	}
}
