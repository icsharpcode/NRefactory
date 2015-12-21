//
// conversion.cs: various routines for implementing conversions.
//
// Authors:
//   Miguel de Icaza (miguel@ximian.com)
//   Ravi Pratap (ravi@ximian.com)
//   Marek Safar (marek.safar@gmail.com)
//
// Copyright 2001, 2002, 2003 Ximian, Inc.
// Copyright 2003-2008 Novell, Inc.
// Copyright 2011 Xamarin Inc (http://www.xamarin.com)
//

using System;
using System.Collections.Generic;

#if STATIC
using IKVM.Reflection.Emit;
#else
using System.Reflection.Emit;
#endif

using Mono.PlayScript;

namespace ICSharpCode.NRefactory.MonoPlayScript {

	//
	// A container class for all the conversion operations
	//
	static class Convert
	{
		[Flags]
		public enum UserConversionRestriction
		{
			None = 0,
			ImplicitOnly = 1,
			ProbingOnly = 1 << 1,
			NullableSourceOnly = 1 << 2

		}
		//
		// From a one-dimensional array-type S[] to System.Collections.IList<T> and base
		// interfaces of this interface, provided there is an implicit reference conversion
		// from S to T.
		//
		static bool ArrayToIList (ArrayContainer array, TypeSpec list, bool isExplicit)
		{
			if (array.Rank != 1 || !list.IsArrayGenericInterface)
				return false;

			var arg_type = list.TypeArguments[0];
			if (array.Element == arg_type)
				return true;

			//
			// Reject conversion from T[] to IList<U> even if T has U dependency
			//
			if (arg_type.IsGenericParameter)
				return false;

			if (isExplicit)
				return ExplicitReferenceConversionExists (array.Element, arg_type, null);

			return ImplicitReferenceConversionExists (array.Element, arg_type, null, false);
		}
		
		static bool IList_To_Array(TypeSpec list, ArrayContainer array)
		{
			if (array.Rank != 1 || !list.IsArrayGenericInterface)
				return false;

			var arg_type = list.TypeArguments[0];
			if (array.Element == arg_type)
				return true;
			
			return ImplicitReferenceConversionExists (array.Element, arg_type, null, false) || ExplicitReferenceConversionExists (array.Element, arg_type, null);
		}

		public static Expression ImplicitTypeParameterConversion (Expression expr, TypeParameterSpec expr_type, TypeSpec target_type)
		{
			//
			// From T to a type parameter U, provided T depends on U
			//
			if (target_type.IsGenericParameter) {
				if (expr_type.TypeArguments != null && expr_type.HasDependencyOn (target_type)) {
					if (expr == null)
						return EmptyExpression.Null;

					if (expr_type.IsReferenceType && !((TypeParameterSpec) target_type).IsReferenceType)
						return new BoxedCast (expr, target_type);

					return new ClassCast (expr, target_type);
				}

				return null;
			}

			//
			// LAMESPEC: From T to dynamic type because it's like T to object
			//
			if (target_type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
				if (expr == null)
					return EmptyExpression.Null;

				if (expr_type.IsReferenceType)
					return new ClassCast (expr, target_type);

				return new BoxedCast (expr, target_type);
			}

			//
			// From T to its effective base class C
			// From T to any base class of C (it cannot contain dynamic or be of dynamic type)
			// From T to any interface implemented by C
			//
			var base_type = expr_type.GetEffectiveBase ();
			if (base_type == target_type || TypeSpec.IsBaseClass (base_type, target_type, false) || base_type.ImplementsInterface (target_type, true)) {
				if (expr == null)
					return EmptyExpression.Null;

				if (expr_type.IsReferenceType)
					return new ClassCast (expr, target_type);

				return new BoxedCast (expr, target_type);
			}

			if (target_type.IsInterface && expr_type.IsConvertibleToInterface (target_type)) {
				if (expr == null)
					return EmptyExpression.Null;

				if (expr_type.IsReferenceType)
					return new ClassCast (expr, target_type);

				return new BoxedCast (expr, target_type);
			}

			return null;
		}

		static Expression ExplicitTypeParameterConversionFromT (Expression source, TypeSpec source_type, TypeSpec target_type)
		{
			var target_tp = target_type as TypeParameterSpec;
			if (target_tp != null) {
				//
				// From a type parameter U to T, provided T depends on U
				//
				if (target_tp.TypeArguments != null && target_tp.HasDependencyOn (source_type)) {
					return source == null ? EmptyExpression.Null : new ClassCast (source, target_type);
				}
			}

			//
			// From T to any interface-type I provided there is not already an implicit conversion from T to I
			//
			if (target_type.IsInterface)
				return source == null ? EmptyExpression.Null : new ClassCast (source, target_type, true);

			return null;
		}

		static Expression ExplicitTypeParameterConversionToT (Expression source, TypeSpec source_type, TypeParameterSpec target_type)
		{
			//
			// From the effective base class C of T to T and from any base class of C to T
			//
			var effective = target_type.GetEffectiveBase ();
			if (TypeSpecComparer.IsEqual (effective, source_type) || TypeSpec.IsBaseClass (effective, source_type, false))
				return source == null ? EmptyExpression.Null : new ClassCast (source, target_type);

			return null;
		}

		public static Expression ImplicitReferenceConversion (Expression expr, TypeSpec target_type, bool explicit_cast, ResolveContext opt_ec, bool upconvert_only)
		{
			TypeSpec expr_type = expr.Type;

			if (expr_type.Kind == MemberKind.TypeParameter)
				return ImplicitTypeParameterConversion (expr, (TypeParameterSpec) expr.Type, target_type);

			//
			// from the null type to any reference-type.
			//
			NullLiteral nl = expr as NullLiteral;
			if (nl != null) {
				return nl.ConvertImplicitly (target_type, null);
			}

			if (ImplicitReferenceConversionExists (expr_type, target_type, opt_ec, upconvert_only)) {
				// 
				// Avoid wrapping implicitly convertible reference type
				//
				if (!explicit_cast)
					return expr;

				return EmptyCast.Create (expr, target_type, opt_ec);
			}

			return null;
		}

		//
		// Implicit reference conversions
		//
		public static bool ImplicitReferenceConversionExists (TypeSpec expr_type, TypeSpec target_type, ResolveContext opt_ec, bool upconvert_only)
		{
			return ImplicitReferenceConversionExists (expr_type, target_type, true, opt_ec, upconvert_only);
		}

		public static bool ImplicitReferenceConversionExists (TypeSpec expr_type, TypeSpec target_type, bool refOnlyTypeParameter, ResolveContext opt_ec, bool upconvert_only)
		{
			var isPlayScript = (opt_ec == null) ? false : opt_ec.IsPlayScript;

			// It's here only to speed things up
			if (target_type.IsStruct)
				return false;


			switch (expr_type.Kind) {
			case MemberKind.TypeParameter:
				return ImplicitTypeParameterConversion (null, (TypeParameterSpec) expr_type, target_type) != null &&
					(!refOnlyTypeParameter || TypeSpec.IsReferenceType (expr_type));

			case MemberKind.Class:
				//
				// From any class-type to dynamic (+object to speed up common path)
				//
				if (target_type.BuiltinType == BuiltinTypeSpec.Type.Object || target_type.BuiltinType == BuiltinTypeSpec.Type.Dynamic)
					return true;

				if (target_type.IsClass) {
					//
					// Identity conversion, including dynamic erasure
					//
					if (TypeSpecComparer.IsEqual (expr_type, target_type))
						return true;

					//
					// From any class-type S to any class-type T, provided S is derived from T
					//
					return TypeSpec.IsBaseClass (expr_type, target_type, true);
				}

				//
				// From any class-type S to any interface-type T, provided S implements T
				//
				if (target_type.IsInterface)
					return expr_type.ImplementsInterface (target_type, true);

				return false;

			case MemberKind.ArrayType:
				//
				// Identity array conversion
				//
				if (expr_type == target_type)
					return true;

				//
				// From any array-type to System.Array
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.Array:
				case BuiltinTypeSpec.Type.Object:
				case BuiltinTypeSpec.Type.Dynamic:
					return true;
				}

				var expr_type_array = (ArrayContainer) expr_type;
				var target_type_array = target_type as ArrayContainer;

				//
				// From an array-type S to an array-type of type T
				//
				if (target_type_array != null && expr_type_array.Rank == target_type_array.Rank) {

					//
					// Disable this conversion for PlayScript, specifically for
					// the case of passing an array to a function which accepts
					// var args. We want the function to receive 1 parameter of
					// type array, rather than n parameters.
					//
					if (isPlayScript)
						return false;

					//
					// Both SE and TE are reference-types. TE check is defered
					// to ImplicitReferenceConversionExists
					//
					TypeSpec expr_element_type = expr_type_array.Element;
					if (!TypeSpec.IsReferenceType (expr_element_type))
						return false;

					//
					// An implicit reference conversion exists from SE to TE
					//
					return ImplicitReferenceConversionExists (expr_element_type, target_type_array.Element, opt_ec, upconvert_only);
				}

				//
				// From any array-type to the interfaces it implements
				//
				if (target_type.IsInterface) {
					if (expr_type.ImplementsInterface (target_type, false))
						return true;

					// from an array-type of type T to IList<T>
					if (ArrayToIList (expr_type_array, target_type, false))
						return true;
				}

				return false;

			case MemberKind.Delegate:
				//
				// From any delegate-type to System.Delegate (and its base types)
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.Delegate:
				case BuiltinTypeSpec.Type.MulticastDelegate:
				case BuiltinTypeSpec.Type.Object:
				case BuiltinTypeSpec.Type.Dynamic:
					return true;
				}

				//
				// Identity conversion, including dynamic erasure
				//
				if (TypeSpecComparer.IsEqual (expr_type, target_type))
					return true;

				//
				// From any delegate-type to the interfaces it implements
				// From any reference-type to an delegate type if is variance-convertible
				//
				return expr_type.ImplementsInterface (target_type, false) || TypeSpecComparer.Variant.IsEqual (expr_type, target_type);

			case MemberKind.Interface:
				//
				// Identity conversion, including dynamic erasure
				//
				if (TypeSpecComparer.IsEqual (expr_type, target_type))
					return true;

				//
				// From any interface type S to interface-type T
				// From any reference-type to an interface if is variance-convertible
				//
				if (target_type.IsInterface)
					return TypeSpecComparer.Variant.IsEqual (expr_type, target_type) || expr_type.ImplementsInterface (target_type, true);

				return target_type.BuiltinType == BuiltinTypeSpec.Type.Object || target_type.BuiltinType == BuiltinTypeSpec.Type.Dynamic;

			case MemberKind.InternalCompilerType:
				//
				// from the null literal to any reference-type.
				//
				if (expr_type == InternalType.NullLiteral) {
					// Exlude internal compiler types
					if (target_type.Kind == MemberKind.InternalCompilerType)
						return target_type.BuiltinType == BuiltinTypeSpec.Type.Dynamic;

					return TypeSpec.IsReferenceType (target_type) || target_type.Kind == MemberKind.PointerType;
				}

				//
				// Implicit dynamic conversion
				//
				if (expr_type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
					switch (target_type.Kind) {
					case MemberKind.ArrayType:
					case MemberKind.Class:
					case MemberKind.Delegate:
					case MemberKind.Interface:
					case MemberKind.TypeParameter:
						return true;
					}

					// dynamic to __arglist
					if (target_type == InternalType.Arglist)
						return true;

					return false;
				}

				break;
			}

			return false;
		}


		public static Expression ImplicitPlayScriptConversion (Expression expr, TypeSpec target_type, ResolveContext opt_ec, bool upconvert_only)
		{
			if (!ImplicitPlayScriptConversionExists (expr.Type, target_type, opt_ec, upconvert_only))
				return null;
			
			TypeSpec expr_type = expr.Type;
			
			// PlayScript references can always be implicitly cast to bool
			if (target_type.BuiltinType == BuiltinTypeSpec.Type.Bool) {
				if (expr_type.BuiltinType == BuiltinTypeSpec.Type.Bool) {
					// already a boolean
					return expr;
				}

				if (expr is NullLiteral) {
					// cast null to false
					return new BoolConstant(opt_ec.BuiltinTypes, false, expr.Location);
				}

				// if its a class or interface reference then just compare against null, else call the more expensive boolean conversion function
				// strings and objects still have to go through the more expensive test
				if ((expr_type.IsClass || expr_type.IsInterface) && (expr_type.BuiltinType != BuiltinTypeSpec.Type.String) && (expr_type.BuiltinType != BuiltinTypeSpec.Type.Object)) {
					// test against null
					return new Binary(Binary.Operator.Inequality, expr, new NullLiteral(expr.Location)).Resolve(opt_ec);
				} else {
					// PlayScript: Call the "Boolean()" static method to convert a dynamic to a bool.  EXPENSIVE, but hey..
					Arguments args = new Arguments (1);
					if (BuiltinTypeSpec.IsPrimitiveType (expr_type))
						args.Add (new Argument (new BoxedCast (expr, target_type)));
					else
						args.Add (new Argument(EmptyCast.Create(expr, opt_ec.BuiltinTypes.Object, opt_ec)));

					var function = new MemberAccess (new MemberAccess (
						new SimpleName (PsConsts.PsRootNamespace, expr.Location), "Boolean_fn", expr.Location), "Boolean", expr.Location);

					return new Invocation (function, args).Resolve (opt_ec);
				}
			}
			
			// PlayScript references can always be implicitly cast to string
			if (expr_type.BuiltinType != BuiltinTypeSpec.Type.String && target_type.BuiltinType == BuiltinTypeSpec.Type.String) {
				if (expr_type.BuiltinType == BuiltinTypeSpec.Type.String) {
					// already a string
					return expr;
				}

				Arguments args = new Arguments (1);

				// Use a dynamic conversion where possible to take advantage of type hints
				if (expr_type.IsDynamic) {
					args.Add (new Argument (expr));
					return new DynamicConversion (target_type, 0, args, expr.Location).Resolve (opt_ec);
				}

				// PlayScript: Call the "CastToString()" static method to convert a dynamic to a string.  EXPENSIVE, but hey..
				if (BuiltinTypeSpec.IsPrimitiveType (expr_type))
					args.Add (new Argument (new BoxedCast (expr, target_type)));
				else
					args.Add (new Argument(EmptyCast.Create(expr, opt_ec.BuiltinTypes.Object, opt_ec)));

				var function = new MemberAccess (new MemberAccess (
					new SimpleName (PsConsts.PsRootNamespace, expr.Location), "String_fn", expr.Location), "CastToString", expr.Location);

				return new Invocation (function, args).Resolve (opt_ec);
			}

			// Can always cast between Object (Dynamic) and * (AsUntyped)
			if ((expr_type.IsDynamic || TypeManager.IsAsUndefined (expr_type, opt_ec)) && target_type.IsDynamic) {
				if (expr_type == target_type)
					return expr; // nothing to do

				Arguments args = new Arguments (1);
				args.Add (new Argument (expr));
				return new DynamicConversion (target_type, 0, args, expr.Location).Resolve (opt_ec);
			}

			return null;
		}

		public static bool ImplicitPlayScriptConversionExists (TypeSpec expr_type, TypeSpec target_type, ResolveContext opt_ec, bool upconvert_only)
		{
			if (opt_ec == null)
				return false;

			//
			// Can always cast between Object (Dynamic) and * (AsUntyped),
			// even in C#. This is to support using the "*" type in C#.
			//
			if ((expr_type.IsDynamic || TypeManager.IsAsUndefined (expr_type, opt_ec)) && target_type.IsDynamic)
				return true;

			if (opt_ec.FileType != SourceFileType.PlayScript || upconvert_only)
				return false;

			//
			// PlayScript types can always be implicitly cast to bool
			//
			if (target_type.BuiltinType == BuiltinTypeSpec.Type.Bool)
				return true;

			//
			// PlayScript types can always be implicitly cast to string
			//
			if (target_type.BuiltinType == BuiltinTypeSpec.Type.String)
				return true;

			return false;
		}

		public static Expression ImplicitBoxingConversion (Expression expr, TypeSpec expr_type, TypeSpec target_type)
		{
			switch (target_type.BuiltinType) {
			//
			// From any non-nullable-value-type to the type object and dynamic
			//
			case BuiltinTypeSpec.Type.Object:
			case BuiltinTypeSpec.Type.Dynamic:
			//
			// From any non-nullable-value-type to the type System.ValueType
			//
			case BuiltinTypeSpec.Type.ValueType:
				//
				// No ned to check for nullable type as underlying type is always convertible
				//
				if (!TypeSpec.IsValueType (expr_type))
					return null;

				return expr == null ? EmptyExpression.Null : new BoxedCast (expr, target_type);

			case BuiltinTypeSpec.Type.Enum:
				//
				// From any enum-type to the type System.Enum.
				//
				if (expr_type.IsEnum)
					return expr == null ? EmptyExpression.Null : new BoxedCast (expr, target_type);

				break;
			}

			//
			// From a nullable-type to a reference type, if a boxing conversion exists from
			// the underlying type to the reference type
			//
			if (expr_type.IsNullableType) {
				if (!TypeSpec.IsReferenceType (target_type))
					return null;

				var res = ImplicitBoxingConversion (expr, Nullable.NullableInfo.GetUnderlyingType (expr_type), target_type);

				// "cast" underlying type to target type to emit correct InvalidCastException when
				// underlying hierarchy changes without recompilation
				if (res != null && expr != null)
					res = new UnboxCast (res, target_type);

				return res;
			}

			//
			// A value type has a boxing conversion to an interface type I if it has a boxing conversion
			// to an interface or delegate type I0 and I0 is variance-convertible to I
			//
			if (target_type.IsInterface && TypeSpec.IsValueType (expr_type) && expr_type.ImplementsInterface (target_type, true)) {
				return expr == null ? EmptyExpression.Null : new BoxedCast (expr, target_type);
			}

			return null;
		}

		public static Expression ImplicitNulableConversion (ResolveContext ec, Expression expr, TypeSpec target_type)
		{
			TypeSpec expr_type = expr.Type;

			//
			// From null to any nullable type
			//
			if (expr_type == InternalType.NullLiteral)
				return ec == null ? EmptyExpression.Null : Nullable.LiftedNull.Create (target_type, expr.Location);

			// S -> T?
			TypeSpec t_el = Nullable.NullableInfo.GetUnderlyingType (target_type);

			// S? -> T?
			if (expr_type.IsNullableType)
				expr_type = Nullable.NullableInfo.GetUnderlyingType (expr_type);

			//
			// Predefined implicit identity or implicit numeric conversion
			// has to exist between underlying type S and underlying type T
			//

			// conversion exists only mode
			if (ec == null) {
				if (TypeSpecComparer.IsEqual (expr_type, t_el))
					return EmptyExpression.Null;

				if (expr is Constant)
					return ((Constant) expr).ConvertImplicitly (t_el, ec);

				return ImplicitNumericConversion (null, expr_type, t_el, ec, false);
			}

			Expression unwrap;
			if (expr_type != expr.Type)
				unwrap = Nullable.Unwrap.Create (expr);
			else
				unwrap = expr;

			Expression conv = unwrap;
			if (!TypeSpecComparer.IsEqual (expr_type, t_el)) {
				if (conv is Constant)
					conv = ((Constant)conv).ConvertImplicitly (t_el, ec);
				else
					conv = ImplicitNumericConversion (conv, expr_type, t_el, ec, false);

				if (conv == null)
					return null;
			}
			
			if (expr_type != expr.Type)
				return new Nullable.LiftedConversion (conv, unwrap, target_type).Resolve (ec);

			return Nullable.Wrap.Create (conv, target_type);
		}

		/// <summary>
		///   Implicit Numeric Conversions.
		///
		///   expr is the expression to convert, returns a new expression of type
		///   target_type or null if an implicit conversion is not possible.
		/// </summary>
		public static Expression ImplicitNumericConversion (Expression expr, TypeSpec target_type, ResolveContext opt_ec, bool upconvert_only)
		{
			return ImplicitNumericConversion (expr, expr.Type, target_type, opt_ec, upconvert_only);
		}

		public static bool ImplicitNumericConversionExists (TypeSpec expr_type, TypeSpec target_type, ResolveContext opt_ec, bool upconvert_only)
		{
			return ImplicitNumericConversion (null, expr_type, target_type, opt_ec, upconvert_only) != null;
		}

		static Expression ImplicitNumericConversion (Expression expr, TypeSpec expr_type, TypeSpec target_type, ResolveContext opt_ec, bool upconvert_only)
		{
			var isPlayScript = (opt_ec == null) ? false : opt_ec.IsPlayScript;

			switch (expr_type.BuiltinType) {
			case BuiltinTypeSpec.Type.SByte:
				//
				// From sbyte to short, int, long, float, double, decimal
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.Int:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I4);
				case BuiltinTypeSpec.Type.Long:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I8);
				case BuiltinTypeSpec.Type.Double:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R8);
				case BuiltinTypeSpec.Type.Float:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R4);
				case BuiltinTypeSpec.Type.Short:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I2);
				case BuiltinTypeSpec.Type.Decimal:
					return expr == null ? EmptyExpression.Null : new OperatorCast (expr, target_type);
				case BuiltinTypeSpec.Type.Bool:
					if (isPlayScript)
						return expr == null ? EmptyExpression.Null : new Binary(Binary.Operator.Inequality, expr, new  IntLiteral(opt_ec.BuiltinTypes, 0, expr.Location)).Resolve(opt_ec);
					break;
				}
				break;
			case BuiltinTypeSpec.Type.Byte:
				//
				// From byte to short, ushort, int, uint, long, ulong, float, double, decimal
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.Int:
				case BuiltinTypeSpec.Type.UInt:
				case BuiltinTypeSpec.Type.Short:
				case BuiltinTypeSpec.Type.UShort:
					return expr == null ? EmptyExpression.Null : EmptyCast.Create (expr, target_type, opt_ec);
				case BuiltinTypeSpec.Type.ULong:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_U8);
				case BuiltinTypeSpec.Type.Long:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I8);
				case BuiltinTypeSpec.Type.Float:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R4);
				case BuiltinTypeSpec.Type.Double:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R8);
				case BuiltinTypeSpec.Type.Decimal:
					return expr == null ? EmptyExpression.Null : new OperatorCast (expr, target_type);
				case BuiltinTypeSpec.Type.Bool:
					if (isPlayScript)
						return expr == null ? EmptyExpression.Null : new Binary(Binary.Operator.Inequality, expr, new UIntLiteral(opt_ec.BuiltinTypes, 0, expr.Location)).Resolve(opt_ec);
					break;
				}
				break;
			case BuiltinTypeSpec.Type.Short:
				//
				// From short to int, long, float, double, decimal
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.Int:
					return expr == null ? EmptyExpression.Null : EmptyCast.Create (expr, target_type, opt_ec);
				case BuiltinTypeSpec.Type.Long:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I8);
				case BuiltinTypeSpec.Type.Double:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R8);
				case BuiltinTypeSpec.Type.Float:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R4);
				case BuiltinTypeSpec.Type.Decimal:
					return expr == null ? EmptyExpression.Null : new OperatorCast (expr, target_type);
				case BuiltinTypeSpec.Type.Bool:
					if (isPlayScript)
						return expr == null ? EmptyExpression.Null : new Binary(Binary.Operator.Inequality, expr, new IntLiteral(opt_ec.BuiltinTypes, 0, expr.Location)).Resolve(opt_ec);
					break;
				}
				break;
			case BuiltinTypeSpec.Type.UShort:
				//
				// From ushort to int, uint, long, ulong, float, double, decimal
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.Int:
				case BuiltinTypeSpec.Type.UInt:
					return expr == null ? EmptyExpression.Null : EmptyCast.Create (expr, target_type, opt_ec);
				case BuiltinTypeSpec.Type.ULong:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_U8);
				case BuiltinTypeSpec.Type.Long:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I8);
				case BuiltinTypeSpec.Type.Double:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R8);
				case BuiltinTypeSpec.Type.Float:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R4);
				case BuiltinTypeSpec.Type.Decimal:
					return expr == null ? EmptyExpression.Null : new OperatorCast (expr, target_type);
				case BuiltinTypeSpec.Type.Bool:
					if (isPlayScript)
						return expr == null ? EmptyExpression.Null : new Binary (Binary.Operator.Inequality, expr, new UIntLiteral (opt_ec.BuiltinTypes, 0, expr.Location)).Resolve (opt_ec);
					break;
				}
				break;
			case BuiltinTypeSpec.Type.Int:
				//
				// From int to long, float, double, decimal
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.Long:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I8);
				case BuiltinTypeSpec.Type.Double:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R8);
				case BuiltinTypeSpec.Type.Float:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R4);
				case BuiltinTypeSpec.Type.Decimal:
					return expr == null ? EmptyExpression.Null : new OperatorCast (expr, target_type);
				case BuiltinTypeSpec.Type.UInt:
					if (isPlayScript && !upconvert_only)
						return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_U4);
					break;
				case BuiltinTypeSpec.Type.ULong:
					if (isPlayScript && !upconvert_only)
						return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_U8);
					break;
				case BuiltinTypeSpec.Type.Bool:
					if (isPlayScript)
						return expr == null ? EmptyExpression.Null : new Binary(Binary.Operator.Inequality, expr, new IntLiteral(opt_ec.BuiltinTypes, 0, expr.Location)).Resolve(opt_ec);
					break;
				}
				break;
			case BuiltinTypeSpec.Type.UInt:
				//
				// From uint to long, ulong, float, double, decimal
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.Long:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_U8);
				case BuiltinTypeSpec.Type.ULong:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_U8);
				case BuiltinTypeSpec.Type.Double:
					return expr == null ? EmptyExpression.Null : new OpcodeCastDuplex (expr, target_type, OpCodes.Conv_R_Un, OpCodes.Conv_R8);
				case BuiltinTypeSpec.Type.Float:
					return expr == null ? EmptyExpression.Null : new OpcodeCastDuplex (expr, target_type, OpCodes.Conv_R_Un, OpCodes.Conv_R4);
				case BuiltinTypeSpec.Type.Decimal:
					return expr == null ? EmptyExpression.Null : new OperatorCast (expr, target_type);
				case BuiltinTypeSpec.Type.Int:
					if (isPlayScript && !upconvert_only)
						return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I4);
					break;
				case BuiltinTypeSpec.Type.Bool:
					if (isPlayScript)
						return expr == null ? EmptyExpression.Null : new Binary(Binary.Operator.Inequality, expr, new UIntLiteral(opt_ec.BuiltinTypes, 0, expr.Location)).Resolve(opt_ec);
					break;
				}
				break;
			case BuiltinTypeSpec.Type.Long:
				//
				// From long to float, double, decimal
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.Double:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R8);
				case BuiltinTypeSpec.Type.Float:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R4);
				case BuiltinTypeSpec.Type.Decimal:
					return expr == null ? EmptyExpression.Null : new OperatorCast (expr, target_type);
				case BuiltinTypeSpec.Type.Int:
					if (isPlayScript && !upconvert_only)
						return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I4);
					break;
				case BuiltinTypeSpec.Type.UInt:
					if (isPlayScript && !upconvert_only)
						return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_U4);
					break;
				case BuiltinTypeSpec.Type.ULong:
					if (isPlayScript && !upconvert_only)
						return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_U8);
					break;
				case BuiltinTypeSpec.Type.Bool:
					if (isPlayScript)
						return expr == null ? EmptyExpression.Null : new Binary(Binary.Operator.Inequality, expr, new LongLiteral(opt_ec.BuiltinTypes, 0L, expr.Location)).Resolve (opt_ec);
					break;
				}
				break;
			case BuiltinTypeSpec.Type.ULong:
				//
				// From ulong to float, double, decimal
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.Double:
					return expr == null ? EmptyExpression.Null : new OpcodeCastDuplex (expr, target_type, OpCodes.Conv_R_Un, OpCodes.Conv_R8);
				case BuiltinTypeSpec.Type.Float:
					return expr == null ? EmptyExpression.Null : new OpcodeCastDuplex (expr, target_type, OpCodes.Conv_R_Un, OpCodes.Conv_R4);
				case BuiltinTypeSpec.Type.Decimal:
					return expr == null ? EmptyExpression.Null : new OperatorCast (expr, target_type);
				case BuiltinTypeSpec.Type.Int:
					if (isPlayScript && !upconvert_only)
						return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I4);
					break;
				case BuiltinTypeSpec.Type.UInt:
					if (isPlayScript && !upconvert_only)
						return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_U4);
					break;
				case BuiltinTypeSpec.Type.Long:
					if (isPlayScript && !upconvert_only)
						return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I8);
					break;
				case BuiltinTypeSpec.Type.Bool:
					if (isPlayScript)
						return expr == null ? EmptyExpression.Null : new Binary(Binary.Operator.Inequality, expr, new ULongLiteral(opt_ec.BuiltinTypes, 0L, expr.Location)).Resolve(opt_ec);
					break;
				}
				break;
			case BuiltinTypeSpec.Type.Char:
				//
				// From char to ushort, int, uint, long, ulong, float, double, decimal
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.UShort:
				case BuiltinTypeSpec.Type.Int:
				case BuiltinTypeSpec.Type.UInt:
					return expr == null ? EmptyExpression.Null : EmptyCast.Create (expr, target_type, opt_ec);
				case BuiltinTypeSpec.Type.ULong:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_U8);
				case BuiltinTypeSpec.Type.Long:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I8);
				case BuiltinTypeSpec.Type.Float:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R4);
				case BuiltinTypeSpec.Type.Double:
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R8);
				case BuiltinTypeSpec.Type.Decimal:
					return expr == null ? EmptyExpression.Null : new OperatorCast (expr, target_type);
				}
				break;
			case BuiltinTypeSpec.Type.Float:
				//
				// From float to double
				//
				if (target_type.BuiltinType == BuiltinTypeSpec.Type.Double)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R8);
				//
				// PlayScript only - from float to int, uint, bool
				//
				if (isPlayScript && !upconvert_only) {
					switch (target_type.BuiltinType) {
					case BuiltinTypeSpec.Type.Int:
						return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I4);
					case BuiltinTypeSpec.Type.UInt:
						return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_U4);
					case BuiltinTypeSpec.Type.Bool:
						return expr == null ? EmptyExpression.Null : new Binary(Binary.Operator.Inequality, expr, new FloatLiteral(opt_ec.BuiltinTypes, 0.0f, expr.Location)).Resolve(opt_ec);
					}
				}
				break;
			case BuiltinTypeSpec.Type.Double:
				//
				// PlayScript only - from double to int, uint, float, bool
				//
				if (isPlayScript && !upconvert_only) {
					switch (target_type.BuiltinType) {
					case BuiltinTypeSpec.Type.Int:
						return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I4);
					case BuiltinTypeSpec.Type.UInt:
						return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_U4);
					case BuiltinTypeSpec.Type.Float:
						return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R4);
					case BuiltinTypeSpec.Type.Bool:
						return expr == null ? EmptyExpression.Null : new Binary (Binary.Operator.Inequality, expr, new DoubleLiteral (opt_ec.BuiltinTypes, 0.0, expr.Location)).Resolve (opt_ec);
					}
				}
				break;
			}

			return null;
		}
			
		public static bool ImplicitConversionExists (ResolveContext ec, Expression expr, TypeSpec target_type, bool upconvert_only = false)
		{
			if (ImplicitStandardConversionExists (ec, expr, target_type, upconvert_only))
				return true;

			if (expr.Type == InternalType.AnonymousMethod) {
				if (!target_type.IsDelegate && !target_type.IsExpressionTreeType)
					return false;

				AnonymousMethodExpression ame = (AnonymousMethodExpression) expr;
				return ame.ImplicitStandardConversionExists (ec, target_type);
			}

			// Conversion from __arglist to System.ArgIterator
			if (expr.Type == InternalType.Arglist)
				return target_type == ec.Module.PredefinedTypes.ArgIterator.TypeSpec;

			return UserDefinedConversion (ec, expr, target_type,
				UserConversionRestriction.ImplicitOnly | UserConversionRestriction.ProbingOnly, Location.Null) != null;
		}

		public static bool ImplicitStandardConversionExists (ResolveContext rc, Expression expr, TypeSpec target_type, bool upconvert_only = false)
		{
			if (expr.eclass == ExprClass.MethodGroup) {
				// PlayScript can implicitly cast unique methods/lambdas to dynamic/delegate types.
				if (rc.IsPlayScript && !target_type.IsDelegate && 
				    (target_type.IsDynamic || target_type == rc.BuiltinTypes.Delegate)) {
					MethodGroupExpr mg = expr as MethodGroupExpr;
					if (mg != null && mg.Candidates.Count == 1) {
						return true;
					}
				}
				if (target_type.IsDelegate && rc.Module.Compiler.Settings.Version != LanguageVersion.ISO_1) {
					MethodGroupExpr mg = expr as MethodGroupExpr;
					if (mg != null)
						return DelegateCreation.ImplicitStandardConversionExists (rc, mg, target_type);
				}

				return false;
			}

			return ImplicitStandardConversionExists (expr, target_type, rc, upconvert_only);
		}

		//
		// Implicit standard conversion (only core conversions are used here)
		//
		public static bool ImplicitStandardConversionExists (Expression expr, TypeSpec target_type, ResolveContext opt_ec, bool upconvert_only = false)
		{
			//
			// Identity conversions
			// Implicit numeric conversions
			// Implicit nullable conversions
			// Implicit reference conversions
			// Boxing conversions
			// Implicit constant expression conversions
			// Implicit conversions involving type parameters
			//

			TypeSpec expr_type = expr.Type;

			if (expr_type == target_type)
				return true;

			if (target_type.IsNullableType)
				return ImplicitNulableConversion (null, expr, target_type) != null;

			if (ImplicitNumericConversion (null, expr_type, target_type, opt_ec, upconvert_only) != null)
				return true;

			if (ImplicitPlayScriptConversionExists (expr_type, target_type, opt_ec, upconvert_only))
				return true;

			if (ImplicitReferenceConversionExists (expr_type, target_type, false, opt_ec, upconvert_only))
				return true;

			if (ImplicitBoxingConversion (null, expr_type, target_type) != null)
				return true;
			
			//
			// Implicit Constant Expression Conversions
			//
			if (expr is IntConstant){
				int value = ((IntConstant) expr).Value;
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.SByte:
					if (value >= SByte.MinValue && value <= SByte.MaxValue)
						return true;
					break;
				case BuiltinTypeSpec.Type.Byte:
					if (value >= 0 && value <= Byte.MaxValue)
						return true;
					break;
				case BuiltinTypeSpec.Type.Short:
					if (value >= Int16.MinValue && value <= Int16.MaxValue)
						return true;
					break;
				case BuiltinTypeSpec.Type.UShort:
					if (value >= UInt16.MinValue && value <= UInt16.MaxValue)
						return true;
					break;
				case BuiltinTypeSpec.Type.UInt:
					if (value >= 0)
						return true;
					break;
				case BuiltinTypeSpec.Type.ULong:
					 //
					 // we can optimize this case: a positive int32
					 // always fits on a uint64.  But we need an opcode
					 // to do it.
					 //
					if (value >= 0)
						return true;

					break;
				}
			}

			if (expr is LongConstant && target_type.BuiltinType == BuiltinTypeSpec.Type.ULong){
				//
				// Try the implicit constant expression conversion
				// from long to ulong, instead of a nice routine,
				// we just inline it
				//
				long v = ((LongConstant) expr).Value;
				if (v >= 0)
					return true;
			}

			if (expr is IntegralConstant && target_type.IsEnum) {
				var i = (IntegralConstant) expr;
				//
				// LAMESPEC: csc allows any constant like 0 values to be converted, including const float f = 0.0
				//
				// An implicit enumeration conversion permits the decimal-integer-literal 0
				// to be converted to any enum-type and to any nullable-type whose underlying
				// type is an enum-type
				//
				return i.IsZeroInteger;
			}

			//
			// Implicit dynamic conversion for remaining value types. It should probably
			// go somewhere else
			//
			if (expr_type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
				switch (target_type.Kind) {
				case MemberKind.Struct:
				case MemberKind.Enum:
					return true;
				}

				return false;
			}

			//
			// In an unsafe context implicit conversions is extended to include
			//
			// From any pointer-type to the type void*
			// From the null literal to any pointer-type.
			//
			// LAMESPEC: The specification claims this conversion is allowed in implicit conversion but
			// in reality implicit standard conversion uses it
			//
			if (target_type.IsPointer && expr.Type.IsPointer && ((PointerContainer) target_type).Element.Kind == MemberKind.Void)
				return true;

			//
			// Struct identity conversion, including dynamic erasure
			//
			if (expr_type.IsStruct && TypeSpecComparer.IsEqual (expr_type, target_type))
				return true;

			return false;
		}

		/// <summary>
		///  Finds "most encompassed type" according to the spec (13.4.2)
		///  amongst the methods in the MethodGroupExpr
		/// </summary>
		public static TypeSpec FindMostEncompassedType (IList<TypeSpec> types, ResolveContext opt_ec)
		{
			TypeSpec best = null;
			EmptyExpression expr;

			foreach (TypeSpec t in types) {
				if (best == null) {
					best = t;
					continue;
				}

				expr = new EmptyExpression (t);
				if (ImplicitStandardConversionExists (expr, best, opt_ec))
					best = t;
			}

			expr = new EmptyExpression (best);
			foreach (TypeSpec t in types) {
				if (best == t)
					continue;
				if (!ImplicitStandardConversionExists (expr, t, opt_ec)) {
					best = null;
					break;
				}
			}

			return best;
		}

		//
		// Finds the most encompassing type (type into which all other
		// types can convert to) amongst the types in the given set
		//
		static TypeSpec FindMostEncompassingType (IList<TypeSpec> types, ResolveContext opt_ec)
		{
			if (types.Count == 0)
				return null;

			if (types.Count == 1)
				return types [0];

			TypeSpec best = null;
			for (int i = 0; i < types.Count; ++i) {
				int ii = 0;
				for (; ii < types.Count; ++ii) {
					if (ii == i)
						continue;

					var expr = new EmptyExpression (types[ii]);
					if (!ImplicitStandardConversionExists (expr, types [i], opt_ec)) {
						ii = 0;
						break;
					}
				}

				if (ii == 0)
					continue;

				if (best == null) {
					best = types[i];
					continue;
				}

				// Indicates multiple best types
				return InternalType.FakeInternalType;
			}

			return best;
		}

		//
		// Finds the most specific source Sx according to the rules of the spec (13.4.4)
		// by making use of FindMostEncomp* methods. Applies the correct rules separately
		// for explicit and implicit conversion operators.
		//
		static TypeSpec FindMostSpecificSource (ResolveContext rc, List<MethodSpec> list, TypeSpec sourceType, Expression source, bool apply_explicit_conv_rules)
		{
			TypeSpec[] src_types_set = null;

			//
			// Try exact match first, if any operator converts from S then Sx = S
			//
			for (int i = 0; i < list.Count; ++i) {
				TypeSpec param_type = list [i].Parameters.Types [0];

				if (param_type == sourceType)
					return param_type;

				if (src_types_set == null)
					src_types_set = new TypeSpec [list.Count];

				src_types_set [i] = param_type;
			}

			//
			// Explicit Conv rules
			//
			if (apply_explicit_conv_rules) {
				var candidate_set = new List<TypeSpec> ();

				foreach (TypeSpec param_type in src_types_set){
					if (ImplicitStandardConversionExists (rc, source, param_type))
						candidate_set.Add (param_type);
				}

				if (candidate_set.Count != 0) {
					if (source.eclass == ExprClass.MethodGroup)
						return InternalType.FakeInternalType;

					return FindMostEncompassedType (candidate_set, rc);
				}
			}

			//
			// Final case
			//
			if (apply_explicit_conv_rules)
				return FindMostEncompassingType (src_types_set, rc);
			else
				return FindMostEncompassedType (src_types_set, rc);
		}

		/// <summary>
		///  Finds the most specific target Tx according to section 13.4.4
		/// </summary>
		static public TypeSpec FindMostSpecificTarget (IList<MethodSpec> list,
							   TypeSpec target, bool apply_explicit_conv_rules, ResolveContext opt_ec)
		{
			List<TypeSpec> tgt_types_set = null;

			//
			// If any operator converts to T then Tx = T
			//
			foreach (var mi in list){
				TypeSpec ret_type = mi.ReturnType;
				if (ret_type == target)
					return ret_type;

				if (tgt_types_set == null) {
					tgt_types_set = new List<TypeSpec> (list.Count);
				} else if (tgt_types_set.Contains (ret_type)) {
					continue;
				}

				tgt_types_set.Add (ret_type);
			}

			//
			// Explicit conv rules
			//
			if (apply_explicit_conv_rules) {
				var candidate_set = new List<TypeSpec> ();

				foreach (TypeSpec ret_type in tgt_types_set) {
					var expr = new EmptyExpression (ret_type);

					if (ImplicitStandardConversionExists (expr, target, opt_ec))
						candidate_set.Add (ret_type);
				}

				if (candidate_set.Count != 0)
					return FindMostEncompassingType (candidate_set, opt_ec);
			}

			//
			// Okay, final case !
			//
			if (apply_explicit_conv_rules)
				return FindMostEncompassedType (tgt_types_set, opt_ec);
			else
				return FindMostEncompassingType (tgt_types_set, opt_ec);
		}

		/// <summary>
		///  User-defined Implicit conversions
		/// </summary>
		static public Expression ImplicitUserConversion (ResolveContext ec, Expression source, TypeSpec target, Location loc)
		{
			return UserDefinedConversion (ec, source, target, UserConversionRestriction.ImplicitOnly, loc);
		}

		/// <summary>
		///  User-defined Explicit conversions
		/// </summary>
		static Expression ExplicitUserConversion (ResolveContext ec, Expression source, TypeSpec target, Location loc)
		{
			return UserDefinedConversion (ec, source, target, 0, loc);
		}

		static void FindApplicableUserDefinedConversionOperators (ResolveContext rc, IList<MemberSpec> operators, Expression source, TypeSpec target, UserConversionRestriction restr, ref List<MethodSpec> candidates)
		{
			if (source.Type.IsInterface) {
				// Neither A nor B are interface-types
				return;
			}

			// For a conversion operator to be applicable, it must be possible
			// to perform a standard conversion from the source type to
			// the operand type of the operator, and it must be possible
			// to perform a standard conversion from the result type of
			// the operator to the target type.

			Expression texpr = null;

			foreach (MethodSpec op in operators) {
				
				// Can be null because MemberCache.GetUserOperator does not resize the array
				if (op == null)
					continue;

				var t = op.Parameters.Types[0];
				if (source.Type != t && !ImplicitStandardConversionExists (rc, source, t, false)) {
					if ((restr & UserConversionRestriction.ImplicitOnly) != 0)
						continue;

					if (!ImplicitStandardConversionExists (new EmptyExpression (t), source.Type, rc, false))
							continue;
				}

				if ((restr & UserConversionRestriction.NullableSourceOnly) != 0 && !t.IsNullableType)
					continue;

				t = op.ReturnType;

				if (t.IsInterface)
					continue;

				if (target != t) {
					if (t.IsNullableType)
						t = Nullable.NullableInfo.GetUnderlyingType (t);

					if (!ImplicitStandardConversionExists (new EmptyExpression (t), target, rc)) {
						if ((restr & UserConversionRestriction.ImplicitOnly) != 0)
							continue;

						if (texpr == null)
							texpr = new EmptyExpression (target);

						if (!ImplicitStandardConversionExists (texpr, t, rc))
							continue;
					}
				}

				if (candidates == null)
					candidates = new List<MethodSpec> ();

				candidates.Add (op);
			}
		}

		//
		// User-defined conversions
		//
		public static Expression UserDefinedConversion (ResolveContext rc, Expression source, TypeSpec target, UserConversionRestriction restr, Location loc)
		{
			List<MethodSpec> candidates = null;

			//
			// If S or T are nullable types, source_type and target_type are their underlying types
			// otherwise source_type and target_type are equal to S and T respectively.
			//
			TypeSpec source_type = source.Type;
			TypeSpec target_type = target;
			Expression source_type_expr;
			bool nullable_source = false;
			var implicitOnly = (restr & UserConversionRestriction.ImplicitOnly) != 0;

			if (source_type.IsNullableType) {
				// No unwrapping conversion S? -> T for non-reference types
				if (implicitOnly && !TypeSpec.IsReferenceType (target_type) && !target_type.IsNullableType) {
					source_type_expr = source;
				} else {
					source_type_expr = Nullable.Unwrap.CreateUnwrapped (source);
					source_type = source_type_expr.Type;
					nullable_source = true;
				}
			} else {
				source_type_expr = source;
			}

			if (target_type.IsNullableType)
				target_type = Nullable.NullableInfo.GetUnderlyingType (target_type);

			// Only these containers can contain a user defined implicit or explicit operators
			const MemberKind user_conversion_kinds = MemberKind.Class | MemberKind.Struct | MemberKind.TypeParameter;

			if ((source_type.Kind & user_conversion_kinds) != 0 && source_type.BuiltinType != BuiltinTypeSpec.Type.Decimal) {
				bool declared_only = source_type.IsStruct;

				var operators = MemberCache.GetUserOperator (source_type, Operator.OpType.Implicit, declared_only);
				if (operators != null) {
					FindApplicableUserDefinedConversionOperators (rc, operators, source_type_expr, target_type, restr, ref candidates);
				}

				if (!implicitOnly) {
					operators = MemberCache.GetUserOperator (source_type, Operator.OpType.Explicit, declared_only);
					if (operators != null) {
						FindApplicableUserDefinedConversionOperators (rc, operators, source_type_expr, target_type, restr, ref candidates);
					}
				}
			}

			if ((target.Kind & user_conversion_kinds) != 0 && target_type.BuiltinType != BuiltinTypeSpec.Type.Decimal) {
				bool declared_only = target.IsStruct || implicitOnly;

				var operators = MemberCache.GetUserOperator (target_type, Operator.OpType.Implicit, declared_only);
				if (operators != null) {
					FindApplicableUserDefinedConversionOperators (rc, operators, source_type_expr, target_type, restr, ref candidates);
				}

				if (!implicitOnly) {
					operators = MemberCache.GetUserOperator (target_type, Operator.OpType.Explicit, declared_only);
					if (operators != null) {
						FindApplicableUserDefinedConversionOperators (rc, operators, source_type_expr, target_type, restr, ref candidates);
					}
				}
			}

			if (candidates == null)
				return null;

			//
			// Find the most specific conversion operator
			//
			MethodSpec most_specific_operator;
			TypeSpec s_x, t_x;
			if (candidates.Count == 1) {
				most_specific_operator = candidates[0];
				s_x = most_specific_operator.Parameters.Types[0];
				t_x = most_specific_operator.ReturnType;
			} else {
				//
				// Pass original source type to find the best match against input type and
				// not the unwrapped expression
				//
				s_x = FindMostSpecificSource (rc, candidates, source.Type, source_type_expr, !implicitOnly);
				if (s_x == null)
					return null;

				t_x = FindMostSpecificTarget (candidates, target, !implicitOnly, rc);
				if (t_x == null)
					return null;

				most_specific_operator = null;
				for (int i = 0; i < candidates.Count; ++i) {
					if (candidates[i].ReturnType == t_x && candidates[i].Parameters.Types[0] == s_x) {
						most_specific_operator = candidates[i];
						break;
					}
				}

				if (most_specific_operator == null) {
					//
					// Unless running in probing more
					//
					if ((restr & UserConversionRestriction.ProbingOnly) == 0) {
						MethodSpec ambig_arg = candidates [0];
						most_specific_operator = candidates [1];
						/*
						foreach (var candidate in candidates) {
							if (candidate.ReturnType == t_x)
								most_specific_operator = candidate;
							else if (candidate.Parameters.Types[0] == s_x)
								ambig_arg = candidate;
						}
						*/
						
						rc.Report.Error (457, loc,
							"Ambiguous user defined operators `{0}' and `{1}' when converting from `{2}' to `{3}'",
							ambig_arg.GetSignatureForError (), most_specific_operator.GetSignatureForError (),
							source.Type.GetSignatureForError (), target.GetSignatureForError ());
					}

					return ErrorExpression.Instance;
				}
			}

			//
			// Convert input type when it's different to best operator argument
			//
			if (s_x != source_type) {
				var c = source as Constant;
				if (c != null) {
					source = c.Reduce (rc, s_x);
					if (source == null)
						c = null;
				}

				if (c == null) {
					source = implicitOnly ?
						ImplicitConversionStandard (rc, source_type_expr, s_x, loc) :
						ExplicitConversionStandard (rc, source_type_expr, s_x, loc);
				}
			} else {
				source = source_type_expr;
			}

			source = new UserCast (most_specific_operator, source, loc).Resolve (rc);

			//
			// Convert result type when it's different to best operator return type
			//
			if (t_x != target_type) {
				//
				// User operator is of T?
				//
				if (t_x.IsNullableType && (target.IsNullableType || !implicitOnly)) {
					//
					// User operator return type does not match target type we need
					// yet another conversion. This should happen for promoted numeric
					// types only
					//
					if (t_x != target) {
						var unwrap = Nullable.Unwrap.CreateUnwrapped (source);

						source = implicitOnly ?
							ImplicitConversionStandard (rc, unwrap, target_type, loc) :
							ExplicitConversionStandard (rc, unwrap, target_type, loc);

						if (source == null)
							return null;

						if (target.IsNullableType)
							source = new Nullable.LiftedConversion (source, unwrap, target).Resolve (rc);
					}
				} else {
					source = implicitOnly ?
						ImplicitConversionStandard (rc, source, target_type, loc) :
						ExplicitConversionStandard (rc, source, target_type, loc);

					if (source == null)
						return null;
				}
			}


			//
			// Source expression is of nullable type and underlying conversion returns
			// only non-nullable type we need to lift it manually
			//
			if (nullable_source && !s_x.IsNullableType)
				return new Nullable.LiftedConversion (source, source_type_expr, target).Resolve (rc);

			//
			// Target is of nullable type but source type is not, wrap the result expression
			//
			if (target.IsNullableType && !t_x.IsNullableType)
				source = Nullable.Wrap.Create (source, target);

			return source;
		}

		/// <summary>
		///   Converts implicitly the resolved expression `expr' into the
		///   `target_type'.  It returns a new expression that can be used
		///   in a context that expects a `target_type'.
		/// </summary>
		static public Expression ImplicitConversion (ResolveContext ec, Expression expr,
							     TypeSpec target_type, Location loc, bool upconvert_only = false)
		{
			Expression e;

			if (target_type == null)
				throw new Exception ("Target type is null");

			e = ImplicitConversionStandard (ec, expr, target_type, loc, false, upconvert_only);
			if (e != null)
				return e;

			e = ImplicitUserConversion (ec, expr, target_type, loc);
			if (e != null)
				return e;

			return null;
		}


		/// <summary>
		///   Attempts to apply the `Standard Implicit
		///   Conversion' rules to the expression `expr' into
		///   the `target_type'.  It returns a new expression
		///   that can be used in a context that expects a
		///   `target_type'.
		///
		///   This is different from `ImplicitConversion' in that the
		///   user defined implicit conversions are excluded.
		/// </summary>
		static public Expression ImplicitConversionStandard (ResolveContext ec, Expression expr,
								     TypeSpec target_type, Location loc)
		{
			return ImplicitConversionStandard (ec, expr, target_type, loc, false, false);
		}

		static Expression ImplicitConversionStandard (ResolveContext ec, Expression expr, TypeSpec target_type, Location loc, bool explicit_cast, bool upconvert_only)
		{
			if (expr.eclass == ExprClass.MethodGroup){
				if (!target_type.IsDelegate){
					if (ec.IsPlayScript && 
					    (target_type.IsDynamic || target_type == ec.BuiltinTypes.Delegate)) {
						MethodGroupExpr mg = expr as MethodGroupExpr;
						if (mg != null) {
							if (mg.Candidates.Count != 1) {
								ec.Report.Error (7021, loc, "Ambiguous overloaded methods `{0}' when assigning to Function or Object type", mg.Name);
								return null;
							}
							var ms = (MethodSpec)mg.Candidates[0];
							var del_type = Delegate.CreateDelegateTypeFromMethodSpec(ec, ms, loc);

							// If return is "Delegate", we create a var args anonymous method which calls the target method..
							if (del_type == ec.BuiltinTypes.Delegate) {
								var objArrayType = new ComposedCast (
									new TypeExpression(ec.BuiltinTypes.Object, loc),  
									ComposedTypeSpecifier.CreateArrayDimension (1, loc));
								var parameters = new ParametersCompiled(new Parameter[] {
									new ParamsParameter(objArrayType, "args", null, loc) }, false);
								var dynCall = new AnonymousMethodExpression(expr.Location, parameters, new TypeExpression(ms.ReturnType, loc));
								var block = new ParametersBlock (ec.CurrentBlock, parameters, expr.Location);
								dynCall.Block = block;
								var args = new Arguments (3);
								args.Add (new Argument(new TypeOf(new TypeExpression(ms.DeclaringType, loc), loc)));
								args.Add (new Argument(new StringLiteral(ec.BuiltinTypes, ms.Name, loc)));
								args.Add (new Argument(new SimpleName("args", loc)));
								var call = new Invocation (new MemberAccess(new MemberAccess(new SimpleName("PlayScript", loc), "Support", loc), "VarArgCall", loc), args);
								if (ms.ReturnType == ec.BuiltinTypes.Void) {
									block.AddStatement (new StatementExpression(call));
								} else {
									block.AddStatement (new Return(call, loc));
								}
								return dynCall.Resolve (ec);
							} else { 
								// Otherwise cast to the specific delegate type
								return new ImplicitDelegateCreation (del_type, mg, loc).Resolve (ec);
							}
						}
					}
					return null;
				}

				//
				// Only allow anonymous method conversions on post ISO_1
				//
				if (ec.Module.Compiler.Settings.Version != LanguageVersion.ISO_1){
					MethodGroupExpr mg = expr as MethodGroupExpr;
					if (mg != null)
						return new ImplicitDelegateCreation (target_type, mg, loc).Resolve (ec);
				}
			}

			TypeSpec expr_type = expr.Type;
			Expression e;

			if (expr_type == target_type) {
				if (expr_type != InternalType.NullLiteral && expr_type != InternalType.AnonymousMethod)
					return expr;
				return null;
			}

			// Auto convert types to type objects..
			if (ec.IsPlayScript && expr is FullNamedExpression &&
			    (target_type.BuiltinType == BuiltinTypeSpec.Type.Type ||
			    target_type.BuiltinType == BuiltinTypeSpec.Type.Object ||
			    target_type.BuiltinType == BuiltinTypeSpec.Type.Dynamic)) {
				FullNamedExpression type_expr = (FullNamedExpression)expr;
				if (expr_type != null) {
					if (expr_type.MemberDefinition.Namespace == PsConsts.PsRootNamespace) {
						switch (expr_type.Name) {
						case "String":
							type_expr = new TypeExpression (ec.BuiltinTypes.String, type_expr.Location);
							break;
						case "Number":
							type_expr = new TypeExpression (ec.BuiltinTypes.Double, type_expr.Location);
							break;
						case "Boolean":
							type_expr = new TypeExpression (ec.BuiltinTypes.Bool, type_expr.Location);
							break;
						}
					} else if (expr_type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
						type_expr = new TypeExpression (ec.Module.PredefinedTypes.AsExpandoObject.Resolve(), type_expr.Location);
					}
				}
				return new TypeOf (type_expr, expr.Location).Resolve (ec);
			}

			if (expr_type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {

				e = ImplicitPlayScriptConversion (expr, target_type, ec, upconvert_only);
				if (e != null)
					return e;

				switch (target_type.Kind) {
				case MemberKind.ArrayType:
				case MemberKind.Class:
					if (target_type.BuiltinType == BuiltinTypeSpec.Type.Object)
						return EmptyCast.Create (expr, target_type, ec);

					goto case MemberKind.Struct;
				case MemberKind.Struct:
				case MemberKind.Delegate:
				case MemberKind.Enum:
				case MemberKind.Interface:
				case MemberKind.TypeParameter:
					Arguments args = new Arguments (1);
					args.Add (new Argument (expr));
					return new DynamicConversion (target_type, explicit_cast ? CSharpBinderFlags.ConvertExplicit : 0, args, loc).Resolve (ec);
				}

				return null;
			}



			if (target_type.IsNullableType)
				return ImplicitNulableConversion (ec, expr, target_type);

			//
			// Attempt to do the implicit constant expression conversions
			//
			Constant c = expr as Constant;
			if (c != null) {
				try {
					c = c.ConvertImplicitly (target_type, ec, upconvert_only);
				} catch {
					throw new InternalErrorException ("Conversion error", loc);
				}
				if (c != null)
					return c;
			}

			e = ImplicitNumericConversion (expr, expr_type, target_type, ec, upconvert_only);
			if (e != null)
				return e;

			e = ImplicitPlayScriptConversion (expr, target_type, ec, upconvert_only);
			if (e != null)
				return e;

			e = ImplicitReferenceConversion (expr, target_type, explicit_cast, ec, upconvert_only);
			if (e != null)
				return e;

			e = ImplicitBoxingConversion (expr, expr_type, target_type);
			if (e != null)
				return e;

			if (expr is IntegralConstant && target_type.IsEnum){
				var i = (IntegralConstant) expr;
				//
				// LAMESPEC: csc allows any constant like 0 values to be converted, including const float f = 0.0
				//
				// An implicit enumeration conversion permits the decimal-integer-literal 0
				// to be converted to any enum-type and to any nullable-type whose underlying
				// type is an enum-type
				//
				if (i.IsZeroInteger) {
					// Recreate 0 literal to remove any collected conversions
					return new EnumConstant (new IntLiteral (ec.BuiltinTypes, 0, i.Location), target_type);
				}
			}

			var target_pc = target_type as PointerContainer;
			if (target_pc != null) {
				if (expr_type.IsPointer) {
					//
					// Pointer types are same when they have same element types
					//
					if (expr_type == target_pc)
						return expr;

					if (target_pc.Element.Kind == MemberKind.Void)
						return EmptyCast.Create (expr, target_type, ec);
				}

				if (expr_type == InternalType.NullLiteral)
					return new NullPointer (target_type, loc);
			}

			if (expr_type == InternalType.AnonymousMethod){
				AnonymousMethodExpression ame = (AnonymousMethodExpression) expr;
				if (ec.IsPlayScript && 
				    (target_type.IsDynamic || target_type == ec.BuiltinTypes.Delegate)) {
					var del_type = Delegate.CreateDelegateType (ec, ame.AsParameters, ame.AsReturnType.ResolveAsType(ec), loc);
					return new Cast(new TypeExpression(del_type, loc), expr, loc).Resolve(ec);
				}
				Expression am = ame.Compatible (ec, target_type);
				if (am != null)
					return am.Resolve (ec);

				// Avoid CS1503 after CS1661
				return ErrorExpression.Instance;
			}

			if (expr_type == InternalType.Arglist && target_type == ec.Module.PredefinedTypes.ArgIterator.TypeSpec)
				return expr;

			//
			// dynamic erasure conversion on value types
			//
			if (expr_type.IsStruct && TypeSpecComparer.IsEqual (expr_type, target_type))
				return expr_type == target_type ? expr : EmptyCast.Create (expr, target_type, ec);

			return null;
		}

		/// <summary>
		///   Attempts to implicitly convert `source' into `target_type', using
		///   ImplicitConversion.  If there is no implicit conversion, then
		///   an error is signaled
		/// </summary>
		static public Expression ImplicitConversionRequired (ResolveContext ec, Expression source,
								     TypeSpec target_type, Location loc)
		{
			Expression e = ImplicitConversion (ec, source, target_type, loc);
			if (e != null)
				return e;

			source.Error_ValueCannotBeConverted (ec, target_type, false);

			return null;
		}

		/// <summary>
		///   Performs the explicit numeric conversions
		///
		/// There are a few conversions that are not part of the C# standard,
		/// they were interim hacks in the C# compiler that were supposed to
		/// become explicit operators in the UIntPtr class and IntPtr class,
		/// but for historical reasons it did not happen, so the C# compiler
		/// ended up with these special hacks.
		///
		/// See bug 59800 for details.
		///
		/// The conversion are:
		///   UIntPtr->SByte
		///   UIntPtr->Int16
		///   UIntPtr->Int32
		///   IntPtr->UInt64
		///   UInt64->IntPtr
		///   SByte->UIntPtr
		///   Int16->UIntPtr
		///
		/// </summary>
		public static Expression ExplicitNumericConversion (ResolveContext rc, Expression expr, TypeSpec target_type)
		{
			// Not all predefined explicit numeric conversion are
			// defined here, for some of them (mostly IntPtr/UIntPtr) we
			// defer to user-operator handling which is now perfect but
			// works for now
			//
			// LAMESPEC: Undocumented IntPtr/UIntPtr conversions
			// IntPtr -> uint uses int
			// UIntPtr -> long uses ulong
			//

			switch (expr.Type.BuiltinType) {
			case BuiltinTypeSpec.Type.Bool:
				//
				// From bool to sbyte, byte, short,
				// ushort, int, uint, long, ulong,
				// char, float or decimal
				//
				if (rc.IsPlayScript) {
					switch (target_type.BuiltinType) {
					case BuiltinTypeSpec.Type.SByte:
						return new ConvCast(new Conditional(expr, new IntLiteral(rc.BuiltinTypes, 1, expr.Location), new IntLiteral(rc.BuiltinTypes, 0, expr.Location), expr.Location).Resolve (rc), target_type, ConvCast.Mode.I4_I1);
					case BuiltinTypeSpec.Type.Byte:
						return new ConvCast(new Conditional(expr, new UIntLiteral(rc.BuiltinTypes, 1, expr.Location), new UIntLiteral(rc.BuiltinTypes, 0, expr.Location), expr.Location).Resolve (rc), target_type, ConvCast.Mode.U4_U1);
					case BuiltinTypeSpec.Type.Short:
						return new ConvCast(new Conditional(expr, new IntLiteral(rc.BuiltinTypes, 1, expr.Location), new IntLiteral(rc.BuiltinTypes, 0, expr.Location), expr.Location).Resolve (rc), target_type, ConvCast.Mode.I4_I2);
					case BuiltinTypeSpec.Type.UShort:
						return new ConvCast(new Conditional(expr, new UIntLiteral(rc.BuiltinTypes, 1, expr.Location), new UIntLiteral(rc.BuiltinTypes, 0, expr.Location), expr.Location).Resolve (rc), target_type, ConvCast.Mode.U4_U2);
					case BuiltinTypeSpec.Type.Int:
						return new Conditional(expr, new IntLiteral(rc.BuiltinTypes, 1, expr.Location), new IntLiteral(rc.BuiltinTypes, 0, expr.Location), expr.Location).Resolve (rc);
					case BuiltinTypeSpec.Type.UInt:
						return new Conditional(expr, new UIntLiteral(rc.BuiltinTypes, 1, expr.Location), new UIntLiteral(rc.BuiltinTypes, 0, expr.Location), expr.Location).Resolve (rc);
					case BuiltinTypeSpec.Type.Long:
						return new Conditional(expr, new LongLiteral(rc.BuiltinTypes, 1, expr.Location), new LongLiteral(rc.BuiltinTypes, 0, expr.Location), expr.Location).Resolve (rc);
					case BuiltinTypeSpec.Type.ULong:
						return new Conditional(expr, new ULongLiteral(rc.BuiltinTypes, 1, expr.Location), new ULongLiteral(rc.BuiltinTypes, 0, expr.Location), expr.Location).Resolve (rc);
					case BuiltinTypeSpec.Type.Char:
						return new Conditional(expr, new CharLiteral(rc.BuiltinTypes, '1', expr.Location), new CharLiteral(rc.BuiltinTypes, '0', expr.Location), expr.Location).Resolve (rc);
					case BuiltinTypeSpec.Type.Float:
						return new Conditional(expr, new FloatLiteral(rc.BuiltinTypes, 1f, expr.Location), new FloatLiteral(rc.BuiltinTypes, 0f, expr.Location), expr.Location).Resolve (rc);
					case BuiltinTypeSpec.Type.Double:
						return new Conditional(expr, new DoubleLiteral(rc.BuiltinTypes, 1, expr.Location), new DoubleLiteral(rc.BuiltinTypes, 0, expr.Location), expr.Location).Resolve (rc);
					case BuiltinTypeSpec.Type.Decimal:
						return new Conditional(expr, new DecimalLiteral(rc.BuiltinTypes, 1, expr.Location), new DecimalLiteral(rc.BuiltinTypes, 0, expr.Location), expr.Location).Resolve (rc);
					}
				}
				break;
			case BuiltinTypeSpec.Type.SByte:
				//
				// From sbyte to byte, ushort, uint, ulong, char, uintptr
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.Byte:
					return new ConvCast (expr, target_type, ConvCast.Mode.I1_U1);
				case BuiltinTypeSpec.Type.UShort:
					return new ConvCast (expr, target_type, ConvCast.Mode.I1_U2);
				case BuiltinTypeSpec.Type.UInt:
					return new ConvCast (expr, target_type, ConvCast.Mode.I1_U4);
				case BuiltinTypeSpec.Type.ULong:
					return new ConvCast (expr, target_type, ConvCast.Mode.I1_U8);
				case BuiltinTypeSpec.Type.Char:
					return new ConvCast (expr, target_type, ConvCast.Mode.I1_CH);

				// One of the built-in conversions that belonged in the class library
				case BuiltinTypeSpec.Type.UIntPtr:
					return new OperatorCast (new ConvCast (expr, rc.BuiltinTypes.ULong, ConvCast.Mode.I1_U8), target_type, target_type, true);

				// PlayScript explicit casts..
				case BuiltinTypeSpec.Type.Bool:
					if (rc.IsPlayScript)
						return new Binary(Binary.Operator.Inequality, expr, new IntLiteral(rc.BuiltinTypes, 0, expr.Location)).Resolve (rc);
					break;
				}
				break;
			case BuiltinTypeSpec.Type.Byte:
				//
				// From byte to sbyte and char
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.SByte:
					return new ConvCast (expr, target_type, ConvCast.Mode.U1_I1);
				case BuiltinTypeSpec.Type.Char:
					return new ConvCast (expr, target_type, ConvCast.Mode.U1_CH);

				// PlayScript explicit casts..
				case BuiltinTypeSpec.Type.Bool:
					if (rc.IsPlayScript)
						return new Binary(Binary.Operator.Inequality, expr, new UIntLiteral(rc.BuiltinTypes, 0, expr.Location)).Resolve (rc);
					break;
				}
				break;
			case BuiltinTypeSpec.Type.Short:
				//
				// From short to sbyte, byte, ushort, uint, ulong, char, uintptr
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.SByte:
					return new ConvCast (expr, target_type, ConvCast.Mode.I2_I1);
				case BuiltinTypeSpec.Type.Byte:
					return new ConvCast (expr, target_type, ConvCast.Mode.I2_U1);
				case BuiltinTypeSpec.Type.UShort:
					return new ConvCast (expr, target_type, ConvCast.Mode.I2_U2);
				case BuiltinTypeSpec.Type.UInt:
					return new ConvCast (expr, target_type, ConvCast.Mode.I2_U4);
				case BuiltinTypeSpec.Type.ULong:
					return new ConvCast (expr, target_type, ConvCast.Mode.I2_U8);
				case BuiltinTypeSpec.Type.Char:
					return new ConvCast (expr, target_type, ConvCast.Mode.I2_CH);

				// One of the built-in conversions that belonged in the class library
				case BuiltinTypeSpec.Type.UIntPtr:
					return new OperatorCast (new ConvCast (expr, rc.BuiltinTypes.ULong, ConvCast.Mode.I2_U8), target_type, target_type, true);

				// PlayScript explicit casts..
				case BuiltinTypeSpec.Type.Bool:
					if (rc.IsPlayScript)
						return new Binary(Binary.Operator.Inequality, expr, new IntLiteral(rc.BuiltinTypes, 0, expr.Location)).Resolve (rc);
					break;
				}
				break;
			case BuiltinTypeSpec.Type.UShort:
				//
				// From ushort to sbyte, byte, short, char
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.SByte:
					return new ConvCast (expr, target_type, ConvCast.Mode.U2_I1);
				case BuiltinTypeSpec.Type.Byte:
					return new ConvCast (expr, target_type, ConvCast.Mode.U2_U1);
				case BuiltinTypeSpec.Type.Short:
					return new ConvCast (expr, target_type, ConvCast.Mode.U2_I2);
				case BuiltinTypeSpec.Type.Char:
					return new ConvCast (expr, target_type, ConvCast.Mode.U2_CH);

				// PlayScript explicit casts..
				case BuiltinTypeSpec.Type.Bool:
					if (rc.IsPlayScript)
						return new Binary(Binary.Operator.Inequality, expr, new UIntLiteral(rc.BuiltinTypes, 0, expr.Location)).Resolve (rc);
					break;
				}
				break;
			case BuiltinTypeSpec.Type.Int:
				//
				// From int to sbyte, byte, short, ushort, uint, ulong, char, uintptr
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.SByte:
					return new ConvCast (expr, target_type, ConvCast.Mode.I4_I1);
				case BuiltinTypeSpec.Type.Byte:
					return new ConvCast (expr, target_type, ConvCast.Mode.I4_U1);
				case BuiltinTypeSpec.Type.Short:
					return new ConvCast (expr, target_type, ConvCast.Mode.I4_I2);
				case BuiltinTypeSpec.Type.UShort:
					return new ConvCast (expr, target_type, ConvCast.Mode.I4_U2);
				case BuiltinTypeSpec.Type.UInt:
					return new ConvCast (expr, target_type, ConvCast.Mode.I4_U4);
				case BuiltinTypeSpec.Type.ULong:
					return new ConvCast (expr, target_type, ConvCast.Mode.I4_U8);
				case BuiltinTypeSpec.Type.Char:
					return new ConvCast (expr, target_type, ConvCast.Mode.I4_CH);

				// One of the built-in conversions that belonged in the class library
				case BuiltinTypeSpec.Type.UIntPtr:
					return new OperatorCast (new ConvCast (expr, rc.BuiltinTypes.ULong, ConvCast.Mode.I2_U8), target_type, target_type, true);

				// PlayScript explicit casts..
				case BuiltinTypeSpec.Type.Bool:
					if (rc.IsPlayScript)
						return new Binary(Binary.Operator.Inequality, expr, new IntLiteral(rc.BuiltinTypes, 0, expr.Location)).Resolve (rc);
					break;
				}
				break;
			case BuiltinTypeSpec.Type.UInt:
				//
				// From uint to sbyte, byte, short, ushort, int, char
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.SByte:
					return new ConvCast (expr, target_type, ConvCast.Mode.U4_I1);
				case BuiltinTypeSpec.Type.Byte:
					return new ConvCast (expr, target_type, ConvCast.Mode.U4_U1);
				case BuiltinTypeSpec.Type.Short:
					return new ConvCast (expr, target_type, ConvCast.Mode.U4_I2);
				case BuiltinTypeSpec.Type.UShort:
					return new ConvCast (expr, target_type, ConvCast.Mode.U4_U2);
				case BuiltinTypeSpec.Type.Int:
					return new ConvCast (expr, target_type, ConvCast.Mode.U4_I4);
				case BuiltinTypeSpec.Type.Char:
					return new ConvCast (expr, target_type, ConvCast.Mode.U4_CH);

				// PlayScript explicit casts..
				case BuiltinTypeSpec.Type.Bool:
					if (rc.IsPlayScript)
						return new Binary(Binary.Operator.Inequality, expr, new UIntLiteral(rc.BuiltinTypes, 0, expr.Location)).Resolve (rc);
					break;
				}
				break;
			case BuiltinTypeSpec.Type.Long:
				//
				// From long to sbyte, byte, short, ushort, int, uint, ulong, char
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.SByte:
					return new ConvCast (expr, target_type, ConvCast.Mode.I8_I1);
				case BuiltinTypeSpec.Type.Byte:
					return new ConvCast (expr, target_type, ConvCast.Mode.I8_U1);
				case BuiltinTypeSpec.Type.Short:
					return new ConvCast (expr, target_type, ConvCast.Mode.I8_I2);
				case BuiltinTypeSpec.Type.UShort:
					return new ConvCast (expr, target_type, ConvCast.Mode.I8_U2);
				case BuiltinTypeSpec.Type.Int:
					return new ConvCast (expr, target_type, ConvCast.Mode.I8_I4);
				case BuiltinTypeSpec.Type.UInt:
					return new ConvCast (expr, target_type, ConvCast.Mode.I8_U4);
				case BuiltinTypeSpec.Type.ULong:
					return new ConvCast (expr, target_type, ConvCast.Mode.I8_U8);
				case BuiltinTypeSpec.Type.Char:
					return new ConvCast (expr, target_type, ConvCast.Mode.I8_CH);

				// PlayScript explicit casts..
				case BuiltinTypeSpec.Type.Bool:
					if (rc.IsPlayScript)
						return new Binary(Binary.Operator.Inequality, expr, new LongLiteral(rc.BuiltinTypes, 0, expr.Location)).Resolve (rc);
					break;
				}
				break;
			case BuiltinTypeSpec.Type.ULong:
				//
				// From ulong to sbyte, byte, short, ushort, int, uint, long, char
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.SByte:
					return new ConvCast (expr, target_type, ConvCast.Mode.U8_I1);
				case BuiltinTypeSpec.Type.Byte:
					return new ConvCast (expr, target_type, ConvCast.Mode.U8_U1);
				case BuiltinTypeSpec.Type.Short:
					return new ConvCast (expr, target_type, ConvCast.Mode.U8_I2);
				case BuiltinTypeSpec.Type.UShort:
					return new ConvCast (expr, target_type, ConvCast.Mode.U8_U2);
				case BuiltinTypeSpec.Type.Int:
					return new ConvCast (expr, target_type, ConvCast.Mode.U8_I4);
				case BuiltinTypeSpec.Type.UInt:
					return new ConvCast (expr, target_type, ConvCast.Mode.U8_U4);
				case BuiltinTypeSpec.Type.Long:
					return new ConvCast (expr, target_type, ConvCast.Mode.U8_I8);
				case BuiltinTypeSpec.Type.Char:
					return new ConvCast (expr, target_type, ConvCast.Mode.U8_CH);

				// One of the built-in conversions that belonged in the class library
				case BuiltinTypeSpec.Type.IntPtr:
					return new OperatorCast (EmptyCast.Create (expr, rc.BuiltinTypes.Long, rc), target_type, true);

				// PlayScript explicit casts..
				case BuiltinTypeSpec.Type.Bool:
					if (rc.IsPlayScript)
						return new Binary(Binary.Operator.Inequality, expr, new ULongLiteral(rc.BuiltinTypes, 0, expr.Location)).Resolve (rc);
					break;
				}
				break;
			case BuiltinTypeSpec.Type.Char:
				//
				// From char to sbyte, byte, short
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.SByte:
					return new ConvCast (expr, target_type, ConvCast.Mode.CH_I1);
				case BuiltinTypeSpec.Type.Byte:
					return new ConvCast (expr, target_type, ConvCast.Mode.CH_U1);
				case BuiltinTypeSpec.Type.Short:
					return new ConvCast (expr, target_type, ConvCast.Mode.CH_I2);

				// PlayScript explicit casts..
				case BuiltinTypeSpec.Type.Bool:
					if (rc.IsPlayScript)
						return new Binary(Binary.Operator.Inequality, expr, new CharLiteral(rc.BuiltinTypes, '\x0', expr.Location)).Resolve (rc);
					break;
				}
				break;
			case BuiltinTypeSpec.Type.Float:
				//
				// From float to sbyte, byte, short,
				// ushort, int, uint, long, ulong, char
				// or decimal
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.SByte:
					return new ConvCast (expr, target_type, ConvCast.Mode.R4_I1);
				case BuiltinTypeSpec.Type.Byte:
					return new ConvCast (expr, target_type, ConvCast.Mode.R4_U1);
				case BuiltinTypeSpec.Type.Short:
					return new ConvCast (expr, target_type, ConvCast.Mode.R4_I2);
				case BuiltinTypeSpec.Type.UShort:
					return new ConvCast (expr, target_type, ConvCast.Mode.R4_U2);
				case BuiltinTypeSpec.Type.Int:
					return new ConvCast (expr, target_type, ConvCast.Mode.R4_I4);
				case BuiltinTypeSpec.Type.UInt:
					return new ConvCast (expr, target_type, ConvCast.Mode.R4_U4);
				case BuiltinTypeSpec.Type.Long:
					return new ConvCast (expr, target_type, ConvCast.Mode.R4_I8);
				case BuiltinTypeSpec.Type.ULong:
					return new ConvCast (expr, target_type, ConvCast.Mode.R4_U8);
				case BuiltinTypeSpec.Type.Char:
					return new ConvCast (expr, target_type, ConvCast.Mode.R4_CH);
				case BuiltinTypeSpec.Type.Decimal:
					return new OperatorCast (expr, target_type, true);

				// PlayScript explicit casts..
				case BuiltinTypeSpec.Type.Bool:
					if (rc.IsPlayScript)
						return new Binary(Binary.Operator.Inequality, expr, new FloatLiteral(rc.BuiltinTypes, 0f, expr.Location)).Resolve (rc);
					break;
				}
				break;
			case BuiltinTypeSpec.Type.Double:
				//
				// From double to sbyte, byte, short,
				// ushort, int, uint, long, ulong,
				// char, float or decimal
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.SByte:
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_I1);
				case BuiltinTypeSpec.Type.Byte:
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_U1);
				case BuiltinTypeSpec.Type.Short:
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_I2);
				case BuiltinTypeSpec.Type.UShort:
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_U2);
				case BuiltinTypeSpec.Type.Int:
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_I4);
				case BuiltinTypeSpec.Type.UInt:
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_U4);
				case BuiltinTypeSpec.Type.Long:
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_I8);
				case BuiltinTypeSpec.Type.ULong:
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_U8);
				case BuiltinTypeSpec.Type.Char:
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_CH);
				case BuiltinTypeSpec.Type.Float:
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_R4);
				case BuiltinTypeSpec.Type.Decimal:
					return new OperatorCast (expr, target_type, true);

				// PlayScript explicit casts..
				case BuiltinTypeSpec.Type.Bool:
					if (rc.IsPlayScript)
						return new Binary(Binary.Operator.Inequality, expr, new DoubleLiteral(rc.BuiltinTypes, 0, expr.Location)).Resolve (rc);
					break;
				}
				break;
			case BuiltinTypeSpec.Type.UIntPtr:
				//
				// Various built-in conversions that belonged in the class library
				//
				// from uintptr to sbyte, short, int32
				//
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.SByte:
					return new ConvCast (new OperatorCast (expr, expr.Type, rc.BuiltinTypes.UInt, true), target_type, ConvCast.Mode.U4_I1);
				case BuiltinTypeSpec.Type.Short:
					return new ConvCast (new OperatorCast (expr, expr.Type, rc.BuiltinTypes.UInt, true), target_type, ConvCast.Mode.U4_I2);
				case BuiltinTypeSpec.Type.Int:
					return EmptyCast.Create (new OperatorCast (expr, expr.Type, rc.BuiltinTypes.UInt, true), target_type, rc);
				case BuiltinTypeSpec.Type.UInt:
					return new OperatorCast (expr, expr.Type, target_type, true);
				case BuiltinTypeSpec.Type.Long:
					return EmptyCast.Create (new OperatorCast (expr, expr.Type, rc.BuiltinTypes.ULong, true), target_type, rc);
				}
				break;
			case BuiltinTypeSpec.Type.IntPtr:
				if (target_type.BuiltinType == BuiltinTypeSpec.Type.UInt)
					return EmptyCast.Create (new OperatorCast (expr, expr.Type, rc.BuiltinTypes.Int, true), target_type, rc);
				if (target_type.BuiltinType == BuiltinTypeSpec.Type.ULong)
					return EmptyCast.Create (new OperatorCast (expr, expr.Type, rc.BuiltinTypes.Long, true), target_type, rc);
				
				break;
			case BuiltinTypeSpec.Type.Decimal:
				// From decimal to sbyte, byte, short,
				// ushort, int, uint, long, ulong, char,
				// float, or double
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.SByte:
				case BuiltinTypeSpec.Type.Byte:
				case BuiltinTypeSpec.Type.Short:
				case BuiltinTypeSpec.Type.UShort:
				case BuiltinTypeSpec.Type.Int:
				case BuiltinTypeSpec.Type.UInt:
				case BuiltinTypeSpec.Type.Long:
				case BuiltinTypeSpec.Type.ULong:
				case BuiltinTypeSpec.Type.Char:
				case BuiltinTypeSpec.Type.Float:
				case BuiltinTypeSpec.Type.Double:
					return new OperatorCast (expr, expr.Type, target_type, true);

				// PlayScript explicit casts..
				case BuiltinTypeSpec.Type.Bool:
					if (rc.IsPlayScript)
						return new Binary(Binary.Operator.Inequality, expr, new DecimalLiteral(rc.BuiltinTypes, 0, expr.Location)).Resolve (rc);
					break;
				}

				break;
			}

			return null;
		}

		/// <summary>
		///  Returns whether an explicit reference conversion can be performed
		///  from source_type to target_type
		/// </summary>
		public static bool ExplicitReferenceConversionExists (TypeSpec source_type, TypeSpec target_type, ResolveContext opt_ec)
		{
			Expression e = ExplicitReferenceConversion (null, source_type, target_type, opt_ec);
			if (e == null)
				return false;

			if (e == EmptyExpression.Null)
				return true;

			throw new InternalErrorException ("Invalid probing conversion result");
		}

		/// <summary>
		///   Implements Explicit Reference conversions
		/// </summary>
		static Expression ExplicitReferenceConversion (Expression source, TypeSpec source_type, TypeSpec target_type, ResolveContext opt_ec)
		{
			//
			// From object to a generic parameter
			//
			if (source_type.BuiltinType == BuiltinTypeSpec.Type.Object && TypeManager.IsGenericParameter (target_type))
				return source == null ? EmptyExpression.Null : new UnboxCast (source, target_type);

			//
			// Explicit type parameter conversion from T
			//
			if (source_type.Kind == MemberKind.TypeParameter)
				return ExplicitTypeParameterConversionFromT (source, source_type, target_type);

			bool target_is_value_type = target_type.Kind == MemberKind.Struct || target_type.Kind == MemberKind.Enum;

			//
			// Unboxing conversion from System.ValueType to any non-nullable-value-type
			//
			if (source_type.BuiltinType == BuiltinTypeSpec.Type.ValueType && target_is_value_type)
				return source == null ? EmptyExpression.Null : new UnboxCast (source, target_type);

			//
			// From object or dynamic to any reference type or value type (unboxing)
			//
			if (source_type.BuiltinType == BuiltinTypeSpec.Type.Object || source_type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
				if (target_type.IsPointer)
					return null;

				return
					source == null ? EmptyExpression.Null :
					target_is_value_type ? new UnboxCast (source, target_type) :
					source is Constant ? (Expression) new EmptyConstantCast ((Constant) source, target_type) :
					new ClassCast (source, target_type);
			}

			//
			// From any class S to any class-type T, provided S is a base class of T
			//
			if (source_type.Kind == MemberKind.Class && TypeSpec.IsBaseClass (target_type, source_type, true))
				return source == null ? EmptyExpression.Null : new ClassCast (source, target_type);

			//
			// From any interface-type S to to any class type T, provided T is not
			// sealed, or provided T implements S.
			//
			// This also covers Explicit conversions involving type parameters
			// section From any interface type to T
			//
			if (source_type.Kind == MemberKind.Interface) {
				if (!target_type.IsSealed || target_type.ImplementsInterface (source_type, true)) {
					if (source == null)
						return EmptyExpression.Null;

					//
					// Unboxing conversion from any interface-type to any non-nullable-value-type that
					// implements the interface-type
					//
					return target_is_value_type ? new UnboxCast (source, target_type) : (Expression) new ClassCast (source, target_type);
				}

				//
				// From System.Collections.Generic.IList<T> and its base interfaces to a one-dimensional
				// array type S[], provided there is an implicit or explicit reference conversion from S to T.
				//
				var target_array = target_type as ArrayContainer;
				if (target_array != null && IList_To_Array (source_type, target_array))
					return source == null ? EmptyExpression.Null : new ClassCast (source, target_type);

				return null;
			}

			var source_array = source_type as ArrayContainer;
			if (source_array != null) {
				var target_array = target_type as ArrayContainer;
				if (target_array != null) {
					//
					// From System.Array to any array-type
					//
					if (source_type.BuiltinType == BuiltinTypeSpec.Type.Array)
						return source == null ? EmptyExpression.Null : new ClassCast (source, target_type);

					//
					// From an array type S with an element type Se to an array type T with an
					// element type Te provided all the following are true:
					//     * S and T differe only in element type, in other words, S and T
					//       have the same number of dimensions.
					//     * Both Se and Te are reference types
					//     * An explicit reference conversions exist from Se to Te
					//
					if (source_array.Rank == target_array.Rank) {

						source_type = source_array.Element;
						var target_element = target_array.Element;

						//
						// LAMESPEC: Type parameters are special cased somehow but
						// only when both source and target elements are type parameters
						//
						if ((source_type.Kind & target_element.Kind & MemberKind.TypeParameter) == MemberKind.TypeParameter) {
							//
							// Conversion is allowed unless source element type has struct constrain
							//
							if (TypeSpec.IsValueType (source_type))
								return null;
						} else {
							if (!TypeSpec.IsReferenceType (source_type))
								return null;
						}

						if (!TypeSpec.IsReferenceType (target_element))
							return null;

						if (ExplicitReferenceConversionExists (source_type, target_element, opt_ec))
							return source == null ? EmptyExpression.Null : new ClassCast (source, target_type);
							
						return null;
					}
				}

				//
				// From a single-dimensional array type S[] to System.Collections.Generic.IList<T> and its base interfaces, 
				// provided that there is an explicit reference conversion from S to T
				//
				if (ArrayToIList (source_array, target_type, true))
					return source == null ? EmptyExpression.Null : new ClassCast (source, target_type);

				return null;
			}

			//
			// From any class type S to any interface T, provides S is not sealed
			// and provided S does not implement T.
			//
			if (target_type.IsInterface && !source_type.IsSealed && !source_type.ImplementsInterface (target_type, true)) {
				return source == null ? EmptyExpression.Null : new ClassCast (source, target_type);
			}

			//
			// From System delegate to any delegate-type
			//
			if (source_type.BuiltinType == BuiltinTypeSpec.Type.Delegate && target_type.IsDelegate)
				return source == null ? EmptyExpression.Null : new ClassCast (source, target_type);

			//
			// From variant generic delegate to same variant generic delegate type
			//
			if (source_type.IsDelegate && target_type.IsDelegate && source_type.MemberDefinition == target_type.MemberDefinition) {
				var tparams = source_type.MemberDefinition.TypeParameters;
				var targs_src = source_type.TypeArguments;
				var targs_dst = target_type.TypeArguments;
				int i;
				for (i = 0; i < tparams.Length; ++i) {
					//
					// If TP is invariant, types have to be identical
					//
					if (TypeSpecComparer.IsEqual (targs_src[i], targs_dst[i]))
						continue;

					if (tparams[i].Variance == Variance.Covariant) {
						//
						//If TP is covariant, an implicit or explicit identity or reference conversion is required
						//
						if (ImplicitReferenceConversionExists (targs_src[i], targs_dst[i], opt_ec, false))
							continue;

						if (ExplicitReferenceConversionExists (targs_src[i], targs_dst[i], opt_ec))
							continue;

					} else if (tparams[i].Variance == Variance.Contravariant) {
						//
						//If TP is contravariant, both are either identical or reference types
						//
						if (TypeSpec.IsReferenceType (targs_src[i]) && TypeSpec.IsReferenceType (targs_dst[i]))
							continue;
					}

					break;
				}

				if (i == tparams.Length)
					return source == null ? EmptyExpression.Null : new ClassCast (source, target_type);
			}

			var tps = target_type as TypeParameterSpec;
			if (tps != null)
				return ExplicitTypeParameterConversionToT (source, source_type, tps);

			return null;
		}

		/// <summary>
		///   Performs an explicit conversion of the expression `expr' whose
		///   type is expr.Type to `target_type'.
		/// </summary>
		static public Expression ExplicitConversionCore (ResolveContext ec, Expression expr,
								 TypeSpec target_type, Location loc)
		{
			TypeSpec expr_type = expr.Type;

			// Explicit conversion includes implicit conversion and it used for enum underlying types too
			Expression ne = ImplicitConversionStandard (ec, expr, target_type, loc, true, false);
			if (ne != null)
				return ne;

			if (expr_type.IsEnum) {
				TypeSpec real_target = target_type.IsEnum ? EnumSpec.GetUnderlyingType (target_type) : target_type;
				Expression underlying = EmptyCast.Create (expr, EnumSpec.GetUnderlyingType (expr_type), ec);
				if (underlying.Type == real_target)
					ne = underlying;

				if (ne == null)
					ne = ImplicitNumericConversion (underlying, real_target, ec, false);

				if (ne == null)
					ne = ExplicitNumericConversion (ec, underlying, real_target);

				//
				// LAMESPEC: IntPtr and UIntPtr conversion to any Enum is allowed
				//
				if (ne == null && (real_target.BuiltinType == BuiltinTypeSpec.Type.IntPtr || real_target.BuiltinType == BuiltinTypeSpec.Type.UIntPtr))
					ne = ExplicitUserConversion (ec, underlying, real_target, loc);

				return ne != null ? EmptyCast.Create (ne, target_type, ec) : null;
			}

			if (target_type.IsEnum) {
				//
				// System.Enum can be unboxed to any enum-type
				//
				if (expr_type.BuiltinType == BuiltinTypeSpec.Type.Enum)
					return new UnboxCast (expr, target_type);

				TypeSpec real_target = target_type.IsEnum ? EnumSpec.GetUnderlyingType (target_type) : target_type;

				if (expr_type == real_target)
					return EmptyCast.Create (expr, target_type, ec);

				Constant c = expr as Constant;
				if (c != null) {
					c = c.TryReduce (ec, real_target);
					if (c != null)
						return c;
				} else {
					ne = ImplicitNumericConversion (expr, real_target, ec, false);
					if (ne != null)
						return EmptyCast.Create (ne, target_type, ec);

					ne = ExplicitNumericConversion (ec, expr, real_target);
					if (ne != null)
						return EmptyCast.Create (ne, target_type, ec);

					//
					// LAMESPEC: IntPtr and UIntPtr conversion to any Enum is allowed
					//
					if (expr_type.BuiltinType == BuiltinTypeSpec.Type.IntPtr || expr_type.BuiltinType == BuiltinTypeSpec.Type.UIntPtr) {
						ne = ExplicitUserConversion (ec, expr, real_target, loc);
						if (ne != null)
							return ExplicitConversionCore (ec, ne, target_type, loc);
					}
				}
			} else {
				ne = ExplicitNumericConversion (ec, expr, target_type);
				if (ne != null)
					return ne;
			}

			//
			// Skip the ExplicitReferenceConversion because we can not convert
			// from Null to a ValueType, and ExplicitReference wont check against
			// null literal explicitly
			//
			if (expr_type != InternalType.NullLiteral) {
				ne = ExplicitReferenceConversion (expr, expr_type, target_type, ec);
				if (ne != null)
					return ne;
			}

			if (ec.IsUnsafe){
				ne = ExplicitUnsafe (expr, target_type, ec);
				if (ne != null)
					return ne;
			}
			
			return null;
		}

		public static Expression ExplicitUnsafe (Expression expr, TypeSpec target_type, ResolveContext rc)
		{
			TypeSpec expr_type = expr.Type;

			if (target_type.IsPointer){
				if (expr_type.IsPointer)
					return EmptyCast.Create (expr, target_type, rc);

				switch (expr_type.BuiltinType) {
				case BuiltinTypeSpec.Type.SByte:
				case BuiltinTypeSpec.Type.Short:
				case BuiltinTypeSpec.Type.Int:
					return new OpcodeCast (expr, target_type, OpCodes.Conv_I);

				case BuiltinTypeSpec.Type.UShort:
				case BuiltinTypeSpec.Type.UInt:
				case BuiltinTypeSpec.Type.Byte:
					return new OpcodeCast (expr, target_type, OpCodes.Conv_U);

				case BuiltinTypeSpec.Type.Long:
					return new ConvCast (expr, target_type, ConvCast.Mode.I8_I);

				case BuiltinTypeSpec.Type.ULong:
					return new ConvCast (expr, target_type, ConvCast.Mode.U8_I);
				}
			}

			if (expr_type.IsPointer){
				switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.SByte:
					return new OpcodeCast (expr, target_type, OpCodes.Conv_I1);
				case BuiltinTypeSpec.Type.Byte:
					return new OpcodeCast (expr, target_type, OpCodes.Conv_U1);
				case BuiltinTypeSpec.Type.Short:
					return new OpcodeCast (expr, target_type, OpCodes.Conv_I2);
				case BuiltinTypeSpec.Type.UShort:
					return new OpcodeCast (expr, target_type, OpCodes.Conv_U2);
				case BuiltinTypeSpec.Type.Int:
					return new OpcodeCast (expr, target_type, OpCodes.Conv_I4);
				case BuiltinTypeSpec.Type.UInt:
					return new OpcodeCast (expr, target_type, OpCodes.Conv_U4);
				case BuiltinTypeSpec.Type.Long:
					return new ConvCast (expr, target_type, ConvCast.Mode.I_I8);
				case BuiltinTypeSpec.Type.ULong:
					return new OpcodeCast (expr, target_type, OpCodes.Conv_U8);
				}
			}
			return null;
		}

		/// <summary>
		///   Same as ExplicitConversion, only it doesn't include user defined conversions
		/// </summary>
		static public Expression ExplicitConversionStandard (ResolveContext ec, Expression expr,
								     TypeSpec target_type, Location l)
		{
			int errors = ec.Report.Errors;
			Expression ne = ImplicitConversionStandard (ec, expr, target_type, l);
			if (ec.Report.Errors > errors)
				return null;

			if (ne != null)
				return ne;

			ne = ExplicitNumericConversion (ec, expr, target_type);
			if (ne != null)
				return ne;

			ne = ExplicitReferenceConversion (expr, expr.Type, target_type, ec);
			if (ne != null)
				return ne;

			if (ec.IsUnsafe && expr.Type.IsPointer && target_type.IsPointer && ((PointerContainer)expr.Type).Element.Kind == MemberKind.Void)
				return EmptyCast.Create (expr, target_type, ec);

			expr.Error_ValueCannotBeConverted (ec, target_type, true);
			return null;
		}

		/// <summary>
		///   Performs an explicit conversion of the expression `expr' whose
		///   type is expr.Type to `target_type'.
		/// </summary>
		static public Expression ExplicitConversion (ResolveContext ec, Expression expr,
			TypeSpec target_type, Location loc)
		{
			Expression e = ExplicitConversionCore (ec, expr, target_type, loc);
			if (e != null) {
				//
				// Don't eliminate explicit precission casts
				//
				if (e == expr) {
					if (target_type.BuiltinType == BuiltinTypeSpec.Type.Float)
						return new OpcodeCast (expr, target_type, OpCodes.Conv_R4);
					
					if (target_type.BuiltinType == BuiltinTypeSpec.Type.Double)
						return new OpcodeCast (expr, target_type, OpCodes.Conv_R8);
				}
					
				return e;
			}

			TypeSpec expr_type = expr.Type;
			if (target_type.IsNullableType) {
				TypeSpec target;

				if (expr_type.IsNullableType) {
					target = Nullable.NullableInfo.GetUnderlyingType (target_type);
					Expression unwrap = Nullable.Unwrap.Create (expr);
					e = ExplicitConversion (ec, unwrap, target, expr.Location);
					if (e == null)
						return null;

					return new Nullable.LiftedConversion (e, unwrap, target_type).Resolve (ec);
				}
				if (expr_type.BuiltinType == BuiltinTypeSpec.Type.Object) {
					return new UnboxCast (expr, target_type);
				}

				target = TypeManager.GetTypeArguments (target_type) [0];
				e = ExplicitConversionCore (ec, expr, target, loc);
				if (e != null)
					return TypeSpec.IsReferenceType (expr.Type) ? new UnboxCast (expr, target_type) : Nullable.Wrap.Create (e, target_type);
			} else if (expr_type.IsNullableType) {
				e = ImplicitBoxingConversion (expr, Nullable.NullableInfo.GetUnderlyingType (expr_type), target_type);
				if (e != null)
					return e;

				e = Nullable.Unwrap.Create (expr, false);			
				e = ExplicitConversionCore (ec, e, target_type, loc);
				if (e != null)
					return EmptyCast.Create (e, target_type, ec);
			}
			
			e = ExplicitUserConversion (ec, expr, target_type, loc);

			if (e != null)
				return e;			

			expr.Error_ValueCannotBeConverted (ec, target_type, true);
			return null;
		}
	}
}
