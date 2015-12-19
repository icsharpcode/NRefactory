//
// dynamic.cs: support for dynamic expressions
//
// Authors: Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2009 Novell, Inc
// Copyright 2011 Xamarin Inc.
//

using System;
using System.Collections.Generic;
using System.Linq;
using SLE = System.Linq.Expressions;

#if NET_4_0 || MOBILE_DYNAMIC
using System.Dynamic;
#endif

namespace ICSharpCode.NRefactory.MonoCSharp
{
	//
	// A copy of Microsoft.CSharp/Microsoft.CSharp.RuntimeBinder/CSharpBinderFlags.cs
	// has to be kept in sync
	//
	[Flags]
	public enum CSharpBinderFlags
	{
		None = 0,
		CheckedContext = 1,
		InvokeSimpleName = 1 << 1,
		InvokeSpecialName = 1 << 2,
		BinaryOperationLogical = 1 << 3,
		ConvertExplicit = 1 << 4,
		ConvertArrayIndex = 1 << 5,
		ResultIndexed = 1 << 6,
		ValueFromCompoundAssignment = 1 << 7,
		ResultDiscarded = 1 << 8
	}

	[Flags]
	public enum DynamicOperation
	{
		Binary = 1,
		Convert = 1 << 1,
		GetIndex = 1 << 2,
		GetMember = 1 << 3,
		Invoke = 1 << 4,
		InvokeConstructor = 1 << 5,
		InvokeMember = 1 << 6,
		IsEvent = 1 << 7,
		SetIndex = 1 << 8,
		SetMember = 1 << 9,
		Unary = 1 << 10
	}

	//
	// Type expression with internal dynamic type symbol
	//
	class DynamicTypeExpr : TypeExpr
	{
		public DynamicTypeExpr (Location loc)
		{
			this.loc = loc;
		}

		public override TypeSpec ResolveAsType (IMemberContext ec, bool allowUnboundTypeArguments)
		{
			eclass = ExprClass.Type;
			type = ec.Module.Compiler.BuiltinTypes.Dynamic;
			return type;
		}
	}

	#region Dynamic runtime binder expressions

	//
	// Expression created from runtime dynamic object value by dynamic binder
	//
	public class RuntimeValueExpression : Expression, IDynamicAssign, IMemoryLocation
	{
#if !NET_4_0 && !MOBILE_DYNAMIC
		public class DynamicMetaObject
		{
			public TypeSpec RuntimeType;
			public TypeSpec LimitType;
			public SLE.Expression Expression;
		}
#endif

		readonly DynamicMetaObject obj;

		public RuntimeValueExpression (DynamicMetaObject obj, TypeSpec type)
		{
			this.obj = obj;
			this.type = type;
			this.eclass = ExprClass.Variable;
		}

		#region Properties

		public bool IsSuggestionOnly { get; set; }

		public DynamicMetaObject MetaObject {
			get { return obj; }
		}

		#endregion

		public void AddressOf (EmitContext ec, AddressOp mode)
		{
			throw new NotImplementedException ();
		}

		public override bool ContainsEmitWithAwait ()
		{
			throw new NotSupportedException ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			throw new NotSupportedException ();
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			return this;
		}

		public override Expression DoResolveLValue (ResolveContext ec, Expression right_side)
		{
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			throw new NotImplementedException ();
		}

		#region IAssignMethod Members

		public void Emit (EmitContext ec, bool leave_copy)
		{
			throw new NotImplementedException ();
		}

		public void EmitAssign (EmitContext ec, Expression source, bool leave_copy, bool isCompound)
		{
			throw new NotImplementedException ();
		}

		#endregion

		public SLE.Expression MakeAssignExpression (BuilderContext ctx, Expression source)
		{
			return obj.Expression;
		}

		public override SLE.Expression MakeExpression (BuilderContext ctx)
		{
#if STATIC
			return base.MakeExpression (ctx);
#else

#if NET_4_0 || MOBILE_DYNAMIC
				if (type.IsStruct && !obj.Expression.Type.IsValueType)
					return SLE.Expression.Unbox (obj.Expression, type.GetMetaInfo ());

				if (obj.Expression.NodeType == SLE.ExpressionType.Parameter) {
					if (((SLE.ParameterExpression) obj.Expression).IsByRef)
						return obj.Expression;
				}
	#endif

				return SLE.Expression.Convert (obj.Expression, type.GetMetaInfo ());
#endif
		}
	}

	//
	// Wraps runtime dynamic expression into expected type. Needed
	// to satify expected type check by dynamic binder and no conversion
	// is required (ResultDiscarded).
	//
	public class DynamicResultCast : ShimExpression
	{
		public DynamicResultCast (TypeSpec type, Expression expr)
			: base (expr)
		{
			this.type = type;
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			expr = expr.Resolve (ec);
			eclass = ExprClass.Value;
			return this;
		}

#if NET_4_0 || MOBILE_DYNAMIC
		public override SLE.Expression MakeExpression (BuilderContext ctx)
		{
#if STATIC
			return base.MakeExpression (ctx);
#else
			return SLE.Expression.Block (expr.MakeExpression (ctx), SLE.Expression.Default (type.GetMetaInfo ()));
#endif
		}
#endif
	}

	#endregion

	//
	// Creates dynamic binder expression
	//
	interface IDynamicBinder
	{
		Expression CreateCallSiteBinder (ResolveContext ec, Arguments args);
	}

	interface IDynamicCallSite
	{
		bool 	   UseCallSite(ResolveContext rc, Arguments args);
		Expression CreateCallSite(ResolveContext rc, Arguments args, bool isSet);
		Expression InvokeCallSite(ResolveContext rc, Expression site, Arguments args, TypeSpec returnType, bool isStatement);
	}

	//
	// Extends standard assignment interface for expressions
	// supported by dynamic resolver
	//
	interface IDynamicAssign : IAssignMethod
	{
		SLE.Expression MakeAssignExpression (BuilderContext ctx, Expression source);
	}

	//
	// Base dynamic expression statement creator
	//
	class DynamicExpressionStatement : ExpressionStatement
	{
		//
		// Binder flag dynamic constant, the value is combination of
		// flags known at resolve stage and flags known only at emit
		// stage
		//
		protected class BinderFlags : EnumConstant
		{
			readonly DynamicExpressionStatement statement;
			readonly CSharpBinderFlags flags;

			public BinderFlags (CSharpBinderFlags flags, DynamicExpressionStatement statement)
				: base (statement.loc)
			{
				this.flags = flags;
				this.statement = statement;
				eclass = 0;
			}

			protected override Expression DoResolve (ResolveContext ec)
			{
				Child = new IntConstant (ec.BuiltinTypes, (int) (flags | statement.flags), statement.loc);

				type = ec.Module.PredefinedTypes.GetBinderFlags(ec).Resolve ();
				eclass = Child.eclass;
				return this;
			}
		}

		readonly Arguments arguments;
		protected IDynamicBinder binder;
		protected Expression binder_expr;

		private bool isPlayScriptDynamicMode;
		private bool isPlayScriptAotMode;

		// Used by BinderFlags
		protected CSharpBinderFlags flags;

		TypeSpec binder_type;
		TypeParameters context_mvars;

		protected bool useDelegateInvoke;

		public DynamicExpressionStatement (IDynamicBinder binder, Arguments args, Location loc)
		{
			this.binder = binder;
			this.arguments = args;
			this.loc = loc;
		}

		public Arguments Arguments {
			get {
				return arguments;
			}
		}

		public override bool ContainsEmitWithAwait ()
		{
			return arguments.ContainsEmitWithAwait ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			ec.Report.Error (1963, loc, "An expression tree cannot contain a dynamic operation");
			return null;
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			if (DoResolveCore (rc))
			{
				var dc = (binder as IDynamicCallSite);
				if (rc.Module.PredefinedTypes.IsPlayScriptAotMode && (dc != null) && dc.UseCallSite(rc, arguments)) {
					this.useDelegateInvoke = false;
					arguments.CreateDynamicBinderArguments(rc);
					binder_expr = dc.CreateCallSite(rc, arguments, false);
				} else {
					this.useDelegateInvoke = true;
					binder_expr = binder.CreateCallSiteBinder (rc, arguments);
				}
			}

			return this;
		}

		protected bool DoResolveCore (ResolveContext rc)
		{
			foreach (var arg in arguments) {
				if (arg.Type == InternalType.VarOutType) {
					// Should be special error message about dynamic dispatch
					rc.Report.Error (8047, arg.Expr.Location, "Declaration expression cannot be used in this context");
				}
			}

			if (rc.CurrentTypeParameters != null && rc.CurrentTypeParameters[0].IsMethodTypeParameter)
				context_mvars = rc.CurrentTypeParameters;

			int errors = rc.Report.Errors;
			var pt = rc.Module.PredefinedTypes;

			binder_type = pt.GetBinder (rc).Resolve ();

			isPlayScriptDynamicMode = pt.IsPlayScriptDynamicMode;
			isPlayScriptAotMode = pt.IsPlayScriptAotMode;

			// NOTE: Use AsCallSite if in PlayScript AOT mode only.
			if (isPlayScriptAotMode) { 
				pt.AsCallSite.Resolve ();
				pt.AsCallSiteGeneric.Resolve ();
			} else {
				pt.CallSite.Resolve ();
				pt.CallSiteGeneric.Resolve ();
			}

			eclass = ExprClass.Value;

			if (type == null)
				type = rc.FileType == SourceFileType.PlayScript ? rc.BuiltinTypes.AsUntyped : rc.BuiltinTypes.Dynamic;

			if (rc.Report.Errors == errors)
				return true;

			if (isPlayScriptDynamicMode) {
				rc.Report.Error (7027, loc,
					"PlayScript dynamic operation cannot be compiled without `ascorlib.dll' assembly reference");
			} else {
				rc.Report.Error (1969, loc,
					"Dynamic operation cannot be compiled without `Microsoft.CSharp.dll' assembly reference");
			}
			return false;
		}

		public override void Emit (EmitContext ec)
		{
			EmitCall (ec, binder_expr, arguments,  false);
		}

		public override void EmitStatement (EmitContext ec)
		{
			EmitCall (ec, binder_expr, arguments, true);
		}

		private bool IsValidPlayScriptAotType(TypeSpec t, bool is_invoke)
		{
			return (t.BuiltinType == BuiltinTypeSpec.Type.Object ||
					  t.BuiltinType == BuiltinTypeSpec.Type.Int || 	// Specialize only on basic PlayScript types in AOT mode.
					  t.BuiltinType == BuiltinTypeSpec.Type.UInt || 	// (NOTE: We can still handle other types, but we box to Object).
					  t.BuiltinType == BuiltinTypeSpec.Type.Bool || 
					  t.BuiltinType == BuiltinTypeSpec.Type.Double || 
					  t.BuiltinType == BuiltinTypeSpec.Type.String) &&
					  !is_invoke;
		}

		protected void EmitCall (EmitContext ec, Expression binder, Arguments arguments, bool isStatement)
		{
			if (!useDelegateInvoke) {
				EmitCallWithInvoke(ec, binder, arguments, isStatement);
			} else {
				EmitCallWithDelegate(ec, binder, arguments, isStatement);
			}
		}

		protected void EmitCallWithInvoke (EmitContext ec, Expression binder, Arguments arguments, bool isStatement)
		{
			var module = ec.Module;

			var site_container = ec.CreateDynamicSite ();

			var block = ec.MemberContext is MethodCore ? 
				((MethodCore)ec.MemberContext).Block : 
				((ec.MemberContext is AbstractPropertyEventMethod) ? ((AbstractPropertyEventMethod)ec.MemberContext).Block : null);
			if (block == null)
				throw new InvalidOperationException ("Must have block when creating block context!");
			BlockContext bc = new BlockContext (ec.MemberContext, block, ec.BuiltinTypes.Void);

			FieldExpr site_field_expr = null;
			StatementExpression s = null;

			// create call site
			var call_site = binder;
			if (call_site != null) {
				// resolve call site
				call_site = call_site.Resolve(bc);

				// create field for call site
				var site_type_decl = call_site.Type;  
				var field = site_container.CreateCallSiteField (new TypeExpression(site_type_decl, loc), loc);
				if (field == null) {
					throw new InvalidOperationException("Could not create call site field");
				}

				// ???
				bool inflate_using_mvar = context_mvars != null && ec.IsAnonymousStoreyMutateRequired;

				// ???
				TypeSpec gt;
				if (inflate_using_mvar || context_mvars == null) {
					gt = site_container.CurrentType;
				} else {
					gt = site_container.CurrentType.MakeGenericType (module, context_mvars.Types);
				}

				// When site container already exists the inflated version has to be
				// updated manually to contain newly created field
				if (gt is InflatedTypeSpec && site_container.AnonymousMethodsCounter > 1) {
					var tparams = gt.MemberDefinition.TypeParametersCount > 0 ? gt.MemberDefinition.TypeParameters : TypeParameterSpec.EmptyTypes;
					var inflator = new TypeParameterInflator (module, gt, tparams, gt.TypeArguments);
					gt.MemberCache.AddMember (field.InflateMember (inflator));
				}

				site_field_expr = new FieldExpr (MemberCache.GetMember (gt, field), loc);

				s = new StatementExpression (new SimpleAssign (site_field_expr, call_site));
			}



			using (ec.With (BuilderContext.Options.OmitDebugInfo, true)) {
				if (s!= null && s.Resolve (bc)) {
					Statement init = new If (new Binary (Binary.Operator.Equality, site_field_expr, new NullLiteral (loc)), s, loc);
					init.Emit (ec);
				}

				// remove dynamics from argument list
				arguments.CastDynamicArgs(bc);

				IDynamicCallSite dynamicCallSite = (IDynamicCallSite)this.binder;
				Expression target = dynamicCallSite.InvokeCallSite(bc, site_field_expr, arguments, type, isStatement);
				if (target != null) 
					target = target.Resolve(bc);

				if (target != null)
				{
					var statement = target as ExpressionStatement;
					if (isStatement && statement != null)
					{
						statement.EmitStatement(ec);
					}
					else
					{
						if (!isStatement && (target.Type != type)) {
							// PlayScript: If doing an invoke, we have to cast the return type to the type expected by the expression..
							target = new Cast(new TypeExpression(type, loc), target, loc).Resolve (bc);
						} 

						target.Emit(ec);
					}
				}
			}

		}

		protected void EmitCallWithDelegate (EmitContext ec, Expression binder, Arguments arguments, bool isStatement)
		{
			//
			// This method generates all internal infrastructure for a dynamic call. The
			// reason why it's quite complicated is the mixture of dynamic and anonymous
			// methods. Dynamic itself requires a temporary class (ContainerX) and anonymous
			// methods can generate temporary storey as well (AnonStorey). Handling MVAR
			// type parameters rewrite is non-trivial in such case as there are various
			// combinations possible therefore the mutator is not straightforward. Secondly
			// we need to keep both MVAR(possibly VAR for anon storey) and type VAR to emit
			// correct Site field type and its access from EmitContext.
			//

			int dyn_args_count = arguments == null ? 0 : arguments.Count;
			int default_args = isStatement ? 1 : 2;
			var module = ec.Module;

			bool is_invoke = ((MemberAccess)((Invocation)binder).Exp).Name.StartsWith ("Invoke");

			TypeSpec callSite;
			TypeSpec callSiteGeneric;
			
			if (isPlayScriptAotMode) {
				callSite = module.PredefinedTypes.AsCallSite.TypeSpec;
				callSiteGeneric = module.PredefinedTypes.AsCallSiteGeneric.TypeSpec;
			} else {
				callSite = module.PredefinedTypes.CallSite.TypeSpec;
				callSiteGeneric = module.PredefinedTypes.CallSiteGeneric.TypeSpec;
			}

			bool has_ref_out_argument = false;
			var targs = new TypeExpression[dyn_args_count + default_args];
			targs[0] = new TypeExpression (callSite, loc);

			TypeExpression[] targs_for_instance = null;
			TypeParameterMutator mutator;

			var site_container = ec.CreateDynamicSite ();

			if (context_mvars != null) {
				TypeParameters tparam;
				TypeContainer sc = site_container;
				do {
					tparam = sc.CurrentTypeParameters;
					sc = sc.Parent;
				} while (tparam == null);

				mutator = new TypeParameterMutator (context_mvars, tparam);

				if (!ec.IsAnonymousStoreyMutateRequired) {
					targs_for_instance = new TypeExpression[targs.Length];
					targs_for_instance[0] = targs[0];
				}
			} else {
				mutator = null;
			}

			for (int i = 0; i < dyn_args_count; ++i) {
				Argument a = arguments[i];
				if (a.ArgType == Argument.AType.Out || a.ArgType == Argument.AType.Ref)
					has_ref_out_argument = true;

				var t = a.Type;

				// Convert any internal type like dynamic or null to object
				if (t.Kind == MemberKind.InternalCompilerType)
					t = ec.BuiltinTypes.Object;

				// PlayScript AOT mode - Convert all types to object if they are not basic AS types or this is an invocation.
				if (isPlayScriptAotMode && !IsValidPlayScriptAotType (t, is_invoke) && !(a.Expr is NullConstant)) {	// Always box to Object for invoke argument lists
					t = ec.BuiltinTypes.Object;
					arguments [i] = new Argument (new BoxedCast(a.Expr, ec.BuiltinTypes.Object));
				}

				if (targs_for_instance != null)
					targs_for_instance[i + 1] = new TypeExpression (t, loc);

				if (mutator != null)
					t = t.Mutate (mutator);

				targs[i + 1] = new TypeExpression (t, loc);
			}

			// Always use "object" as return type in AOT mode.
			var ret_type = type;
			if (isPlayScriptAotMode && !isStatement && !IsValidPlayScriptAotType (ret_type, is_invoke)) {
				ret_type = ec.BuiltinTypes.Object;
			}

			TypeExpr del_type = null;
			TypeExpr del_type_instance_access = null;
			if (!has_ref_out_argument) {
				string d_name = isStatement ? "Action" : "Func";

				TypeSpec te = null;
				Namespace type_ns = module.GlobalRootNamespace.GetNamespace ("System", true);
				if (type_ns != null) {
					te = type_ns.LookupType (module, d_name, dyn_args_count + default_args, LookupMode.Normal, loc);
				}

				if (te != null) {
					if (!isStatement) {
						var t = ret_type;
						if (t.Kind == MemberKind.InternalCompilerType)
							t = ec.BuiltinTypes.Object;

						if (targs_for_instance != null)
							targs_for_instance[targs_for_instance.Length - 1] = new TypeExpression (t, loc);

						if (mutator != null)
							t = t.Mutate (mutator);

						targs[targs.Length - 1] = new TypeExpression (t, loc);
					}

					del_type = new GenericTypeExpr (te, new TypeArguments (targs), loc);
					if (targs_for_instance != null)
						del_type_instance_access = new GenericTypeExpr (te, new TypeArguments (targs_for_instance), loc);
					else
						del_type_instance_access = del_type;
				}
			}

			//
			// Create custom delegate when no appropriate predefined delegate has been found
			//
			Delegate d;
			if (del_type == null) {
				TypeSpec rt = isStatement ? ec.BuiltinTypes.Void : ret_type;
				Parameter[] p = new Parameter[dyn_args_count + 1];
				p[0] = new Parameter (targs[0], "p0", Parameter.Modifier.NONE, null, loc);

				var site = ec.CreateDynamicSite ();
				int index = site.Containers == null ? 0 : site.Containers.Count;

				if (mutator != null)
					rt = mutator.Mutate (rt);

				for (int i = 1; i < dyn_args_count + 1; ++i) {
					p[i] = new Parameter (targs[i], "p" + i.ToString ("X"), arguments[i - 1].Modifier, null, loc);
				}

				d = new Delegate (site, new TypeExpression (rt, loc),
					Modifiers.INTERNAL | Modifiers.COMPILER_GENERATED,
					new MemberName ("Container" + index.ToString ("X")),
					new ParametersCompiled (p), null);

				d.CreateContainer ();
				d.DefineContainer ();
				d.Define ();
				d.PrepareEmit ();

				site.AddTypeContainer (d);

				//
				// Add new container to inflated site container when the
				// member cache already exists
				//
				if (site.CurrentType is InflatedTypeSpec && index > 0)
					site.CurrentType.MemberCache.AddMember (d.CurrentType);

				del_type = new TypeExpression (d.CurrentType, loc);
				if (targs_for_instance != null) {
					del_type_instance_access = null;
				} else {
					del_type_instance_access = del_type;
				}
			} else {
				d = null;
			}

			var site_type_decl = new GenericTypeExpr (callSiteGeneric, new TypeArguments (del_type), loc);
			var field = site_container.CreateCallSiteField (site_type_decl, loc);
			if (field == null)
				return;

			if (del_type_instance_access == null) {
				var dt = d.CurrentType.DeclaringType.MakeGenericType (module, context_mvars.Types);
				del_type_instance_access = new TypeExpression (MemberCache.GetMember (dt, d.CurrentType), loc);
			}

			var instanceAccessExprType = new GenericTypeExpr (callSiteGeneric, new TypeArguments (del_type_instance_access), loc);

			if (instanceAccessExprType.ResolveAsType (ec.MemberContext) == null)
				return;

			bool inflate_using_mvar = context_mvars != null && ec.IsAnonymousStoreyMutateRequired;

			TypeSpec gt;
			if (inflate_using_mvar || context_mvars == null) {
				gt = site_container.CurrentType;
			} else {
				gt = site_container.CurrentType.MakeGenericType (module, context_mvars.Types);
			}

			// When site container already exists the inflated version has to be
			// updated manually to contain newly created field
			if (gt is InflatedTypeSpec && site_container.AnonymousMethodsCounter > 1) {
				var tparams = gt.MemberDefinition.TypeParametersCount > 0 ? gt.MemberDefinition.TypeParameters : TypeParameterSpec.EmptyTypes;
				var inflator = new TypeParameterInflator (module, gt, tparams, gt.TypeArguments);
				gt.MemberCache.AddMember (field.InflateMember (inflator));
			}

			FieldExpr site_field_expr = new FieldExpr (MemberCache.GetMember (gt, field), loc);

			var block = ec.MemberContext is MethodCore ? 
				((MethodCore)ec.MemberContext).Block : 
					((ec.MemberContext is AbstractPropertyEventMethod) ? ((AbstractPropertyEventMethod)ec.MemberContext).Block : null);
			if (block == null)
				throw new InvalidOperationException ("Must have block when creating block context!");
			BlockContext bc = new BlockContext (ec.MemberContext, block, ec.BuiltinTypes.Void);

			Arguments args = new Arguments (1);
			args.Add (new Argument (binder));
			StatementExpression s = new StatementExpression (new SimpleAssign (site_field_expr, new Invocation (new MemberAccess (instanceAccessExprType, "Create"), args)));

			using (ec.With (BuilderContext.Options.OmitDebugInfo, true)) {
				if (s.Resolve (bc)) {
					Statement init = new If (new Binary (Binary.Operator.Equality, site_field_expr, new NullLiteral (loc)), s, loc);
					init.Emit (ec);
				}

				args = new Arguments (1 + dyn_args_count);
				args.Add (new Argument (site_field_expr));
				if (arguments != null) {
					int arg_pos = 1;
					foreach (Argument a in arguments) {
						if (a is NamedArgument) {
							// Name is not valid in this context
							args.Add (new Argument (a.Expr, a.ArgType));
						} else {
							args.Add (a);
						}

						if (inflate_using_mvar && a.Type != targs[arg_pos].Type)
							a.Expr.Type = targs[arg_pos].Type;

						++arg_pos;
					}
				}

				Expression target;
				if (isPlayScriptAotMode && !isStatement && type != ret_type) {
					// PlayScript: If doing an invoke, we have to cast the return type to the type expected by the expression..
					target = new Cast(new TypeExpression(type, loc), new DelegateInvocation (new MemberAccess (site_field_expr, "Target", loc).Resolve (bc), args, false, loc), loc).Resolve (bc);
				} else {
					//target = new DelegateInvocation (new MemberAccess (site_field_expr, "Target", loc).Resolve (bc), args, loc).Resolve (bc);
					target = new DelegateInvocation (new MemberAccess (site_field_expr, "Target", loc).Resolve (bc), args, false, loc).Resolve (bc);
				}

				if (target != null)
					target.Emit (ec);
			}
		}

		public override void FlowAnalysis (FlowAnalysisContext fc)
		{
			arguments.FlowAnalysis (fc);
		}

		public static MemberAccess GetBinderNamespace (ResolveContext rc, Location loc)
		{
			if (rc.Module.PredefinedTypes.IsPlayScriptDynamicMode) {
				return new MemberAccess (
					new QualifiedAliasMember (QualifiedAliasMember.GlobalAlias, "PlayScript", loc), "RuntimeBinder", loc);
			} else {
				return new MemberAccess (new MemberAccess (
					new QualifiedAliasMember (QualifiedAliasMember.GlobalAlias, "Microsoft", loc), "CSharp", loc), "RuntimeBinder", loc);
			}
		}

		protected MemberAccess GetBinder (string name, Location loc)
		{
			return new MemberAccess (new TypeExpression (binder_type, loc), name, loc);
		}
	}

	//
	// Dynamic member access compound assignment for events
	//
	class DynamicEventCompoundAssign : ExpressionStatement
	{
		class IsEvent : DynamicExpressionStatement, IDynamicBinder
		{
			string name;

			public IsEvent (string name, Arguments args, Location loc)
				: base (null, args, loc)
			{
				this.name = name;
				binder = this;
			}

			public Expression CreateCallSiteBinder (ResolveContext ec, Arguments args)
			{
				Statement.DynamicOps |= DynamicOperation.IsEvent;

				type = ec.BuiltinTypes.Bool;

				Arguments binder_args = new Arguments (3);

				binder_args.Add (new Argument (new BinderFlags (0, this)));
				binder_args.Add (new Argument (new StringLiteral (ec.BuiltinTypes, name, loc)));
				binder_args.Add (new Argument (new TypeOf (ec.CurrentType, loc)));

				return new Invocation (GetBinder ("IsEvent", loc), binder_args);
			}
		}

		Expression condition;
		ExpressionStatement invoke, assign;

		public DynamicEventCompoundAssign (string name, Arguments args, ExpressionStatement assignment, ExpressionStatement invoke, Location loc)
		{
			condition = new IsEvent (name, args, loc);
			this.invoke = invoke;
			this.assign = assignment;
			this.loc = loc;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			return condition.CreateExpressionTree (ec);
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			type = rc.BuiltinTypes.Dynamic;
			eclass = ExprClass.Value;
			condition = condition.Resolve (rc);
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			var rc = new ResolveContext (ec.MemberContext);
			var expr = new Conditional (new BooleanExpression (condition), invoke, assign, loc).Resolve (rc);
			expr.Emit (ec);
		}

		public override void EmitStatement (EmitContext ec)
		{
			var stmt = new If (condition, new StatementExpression (invoke), new StatementExpression (assign), loc);
			using (ec.With (BuilderContext.Options.OmitDebugInfo, true)) {
				stmt.Emit (ec);
			}
		}

		public override void FlowAnalysis (FlowAnalysisContext fc)
		{
			invoke.FlowAnalysis (fc);
		}
	}

	class DynamicConversion : DynamicExpressionStatement, IDynamicBinder
	{
		public DynamicConversion (TypeSpec targetType, CSharpBinderFlags flags, Arguments args, Location loc)
			: base (null, args, loc)
		{
			type = targetType;
			base.flags = flags;
			base.binder = this;
		}


		protected override Expression DoResolve(ResolveContext rc)
		{
			// get expresion we're converting
			var expr = this.Arguments[0].Expr;

			if (rc.Module.PredefinedTypes.IsPlayScriptAotMode && rc.Module.Compiler.Settings.NewDynamicRuntime_ConvertReturnType) {
				// encourage dynamic expression to resolve to our type to avoid this conversion
				expr = expr.ResolveWithTypeHint(rc, this.Type);
				if (expr.Type == this.type) {
					// skip dynamic conversions
					return expr;
				}
			}

			if (rc.Module.PredefinedTypes.IsPlayScriptAotMode && rc.Module.Compiler.Settings.NewDynamicRuntime_Convert) {
				var conversion = CreateDynamicConversion (rc, expr.Resolve (rc), this.Type);
				if (conversion != null)
					return conversion.Resolve (rc);
			}

			return base.DoResolve(rc);
		}

		#region IDynamicCallSite implementation

		public static Expression CreateDynamicConversion(ResolveContext rc, Expression expr, TypeSpec target_type)
		{
			var expr_type = expr.Type;

			// object can hold any value
			if (target_type.BuiltinType == BuiltinTypeSpec.Type.Object)
				return EmptyCast.Create(expr, target_type, rc);

			// casts between Object (Dynamic) and * (AsUntyped), and vice versa
			if ((expr_type.IsDynamic || TypeManager.IsAsUndefined (expr_type, rc)) && target_type.IsDynamic) {
				if (expr_type == target_type)
					return expr; // nothing to do

				// in C#, allow dynamic to hold undefined
				if (rc.FileType != SourceFileType.PlayScript)
					return EmptyCast.Create (expr, target_type, rc).Resolve (rc);

				// cast from * (AsUntyped) to Object (Dynamic)
				if ((expr.Type.IsAsUntyped || TypeManager.IsAsUndefined (expr.Type, rc)) && !target_type.IsAsUntyped) {
					var args = new Arguments (1);
					args.Add (new Argument (EmptyCast.RemoveDynamic (rc, expr)));
					var function = new MemberAccess (new TypeExpression (rc.Module.PredefinedTypes.PsConverter.Resolve (), expr.Location), "ConvertToObj", expr.Location);
					return new Invocation (function, args);
				}

				// cast from Object (Dynamic) to * (AsUntyped)
				return EmptyCast.Create (expr, target_type, rc).Resolve (rc);
			}

			// other class types must be type checked - fall back to the slow path
			if ((target_type.IsClass || target_type.IsInterface) && target_type.BuiltinType != BuiltinTypeSpec.Type.String)
				return null;

			TypeSpec converter = rc.Module.PredefinedTypes.PsConverter.Resolve();

			// perform numeric or other type conversion
			string converterMethod = null;

			switch (target_type.BuiltinType) {
				case BuiltinTypeSpec.Type.UInt:
					converterMethod = "ConvertToUInt";
					break;
				case BuiltinTypeSpec.Type.Double:
					converterMethod = "ConvertToDouble";
					break;
				case BuiltinTypeSpec.Type.Float:
					converterMethod = "ConvertToFloat";
					break;
				case BuiltinTypeSpec.Type.Int:
					converterMethod = "ConvertToInt";
					break;
				case BuiltinTypeSpec.Type.String:
					converterMethod = "ConvertToString";
					break;
				case BuiltinTypeSpec.Type.Bool:
					converterMethod = "ConvertToBool";
					break;
				default:
//					throw new InvalidOperationException("Unhandled convert to: " + target_type.GetSignatureForError());
					return EmptyCast.Create(expr, target_type, rc);
//					converterMethod = "ConvertTo" + target_type.Name.ToString();
//					break;
			}

			var cast_args = new Arguments(1);
			cast_args.Add(new Argument(EmptyCast.RemoveDynamic(rc, expr)));
			return new Invocation(new MemberAccess(new TypeExpression(converter, expr.Location), converterMethod, expr.Location), cast_args);
		}

		#endregion

		public Expression CreateCallSiteBinder (ResolveContext ec, Arguments args)
		{
			Statement.DynamicOps |= DynamicOperation.Convert;

			Arguments binder_args = new Arguments (3);

			flags |= ec.HasSet (ResolveContext.Options.CheckedScope) ? CSharpBinderFlags.CheckedContext : 0;

			binder_args.Add (new Argument (new BinderFlags (flags, this)));
			binder_args.Add (new Argument (new TypeOf (type, loc)));
			binder_args.Add (new Argument (new TypeOf (ec.CurrentType, loc)));
			return new Invocation (GetBinder ("Convert", loc), binder_args);
		}
	}

	class DynamicConstructorBinder : DynamicExpressionStatement, IDynamicBinder
	{
		private Expression typeExpr;

		public DynamicConstructorBinder (TypeSpec type, Arguments args, Location loc)
			: base (null, args, loc)
		{
			this.type = type;
			this.typeExpr = null;
			base.binder = this;
		}

		public DynamicConstructorBinder (Expression typeExpr, Arguments args, Location loc)
			: base (null, args, loc)
		{
			this.type = null;
			this.typeExpr = typeExpr;
			base.binder = this;
		}

		protected override Expression DoResolve(ResolveContext rc)
		{
			if (rc.Module.PredefinedTypes.IsPlayScriptAotMode && rc.Module.Compiler.Settings.NewDynamicRuntime_Constructor) {
				if ((this.typeExpr == null) && (this.type != null)) {
					var ctors = MemberCache.FindMembers (type, Constructor.ConstructorName, true);
					if (ctors != null) {
						// if there is one and only one ctor then use it
						if (ctors.Count == 1) {
							var first = Arguments[0];
							Arguments.RemoveAt(0);
							bool hasDynamic;
							Arguments.Resolve(rc, out hasDynamic);
							if (Arguments.AsTryResolveDynamicArgs(rc, ctors[0])) {
								// use normal new
								return new New(new TypeExpression(type, loc), Arguments, loc).Resolve(rc);
							}

							OverloadResolver.Error_ConstructorMismatch (rc, type, Arguments.Count, loc);
							return null;
						}
					}
				}

				// fall through to normal resolve -- 
			}

			return base.DoResolve(rc);
		}

		public Expression CreateCallSiteBinder (ResolveContext ec, Arguments args)
		{
			Statement.DynamicOps |= DynamicOperation.InvokeConstructor;

			Arguments binder_args = new Arguments (3);

			binder_args.Add (new Argument (new BinderFlags (0, this)));
			if (typeExpr != null) {
				binder_args.Add (new Argument (typeExpr));
			} else {
				binder_args.Add (new Argument (new TypeOf (ec.CurrentType, loc)));
			}
			binder_args.Add (new Argument (new ImplicitlyTypedArrayCreation (args.CreateDynamicBinderArguments (ec), loc)));

			return new Invocation (GetBinder ("InvokeConstructor", loc), binder_args);
		}
	}

	class DynamicIndexBinder : DynamicMemberAssignable
	{
		bool can_be_mutator;

		public DynamicIndexBinder (Arguments args, Location loc)
			: base (args, loc)
		{
		}

		public DynamicIndexBinder (CSharpBinderFlags flags, Arguments args, Location loc)
			: this (args, loc)
		{
			base.flags = flags;
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			can_be_mutator = true;
			return base.DoResolve (ec);
		}

		public override bool UseCallSite(ResolveContext ec, Arguments args)
		{
			return ec.Module.Compiler.Settings.NewDynamicRuntime_GetSetIndex;
		}

		public override Expression CreateCallSite(ResolveContext rc, Arguments args, bool isSet)
		{
			TypeExpression type;
			isSet |= (flags & CSharpBinderFlags.ValueFromCompoundAssignment) != 0;
			if (!isSet) {
				type = new TypeExpression(rc.Module.PredefinedTypes.PsGetIndex.Resolve(), loc);
			} else {
				type = new TypeExpression(rc.Module.PredefinedTypes.PsSetIndex.Resolve(), loc);
			}

			var site_args = new Arguments(0);
			return new New(
				type,
				site_args, 
				loc
			);
		}

		public override Expression InvokeCallSite(ResolveContext rc, Expression site, Arguments args, TypeSpec returnType, bool isStatement)
		{
			// get object and index
			var obj = args[0].Expr;
			var index = args[1].Expr;

			bool isSet = IsSetter(site);
			isSet |= (flags & CSharpBinderFlags.ValueFromCompoundAssignment) != 0;
			if (!isSet) {
				if (NeedsCastToObject(returnType)) {
					var site_args = new Arguments(2);
					site_args.Add(new Argument(obj));
					site_args.Add(new Argument(index));

					// get as an object
					if (returnType != null && returnType.IsAsUntyped)
						return new Invocation(new MemberAccess(site, "GetIndexAsUntyped"), site_args);
					else
						return new Invocation(new MemberAccess(site, "GetIndexAsObject"), site_args);
				} else {
					var site_args = new Arguments(2);
					site_args.Add(new Argument(obj));
					site_args.Add(new Argument(index));

					// get as a T
					var type_args = new TypeArguments();
					type_args.Add(new TypeExpression(returnType, loc));
					return new Invocation(new MemberAccess(site, "GetIndexAs", type_args, loc), site_args);
				}
			} else {
				var setVal = args[2].Expr;

				if (NeedsCastToObject(setVal.Type)) {
					var site_args = new Arguments(3);
					site_args.Add(new Argument(obj));
					site_args.Add(new Argument(index));
					site_args.Add(new Argument(new Cast(new TypeExpression(rc.BuiltinTypes.Object, loc), setVal, loc)));
						
					// set as an object
					var type_args = new TypeArguments();
					type_args.Add(new TypeExpression(rc.BuiltinTypes.Object, loc));
					// TODO: AsUntyped parameters aren't yet supported, so there is no SetIndexAsUntyped
					return new Invocation(new MemberAccess(site, "SetIndexAs", type_args, loc), site_args);
				} else {
					var site_args = new Arguments(3);
					site_args.Add(new Argument(obj));
					site_args.Add(new Argument(index));
					site_args.Add(new Argument(setVal));

					// set as a T
					var type_args = new TypeArguments();
					type_args.Add(new TypeExpression(setVal.Type, loc));
					return new Invocation(new MemberAccess(site, "SetIndexAs", type_args, loc), site_args);
				}
			}
		}

		protected override Expression CreateCallSiteBinder (ResolveContext ec, Arguments args, bool isSet)
		{
			Arguments binder_args = new Arguments (3);

			binder_args.Add (new Argument (new BinderFlags (flags, this)));
			binder_args.Add (new Argument (new TypeOf (ec.CurrentType, loc)));
			binder_args.Add (new Argument (new ImplicitlyTypedArrayCreation (args.CreateDynamicBinderArguments (ec), loc)));

			isSet |= (flags & CSharpBinderFlags.ValueFromCompoundAssignment) != 0;

			if (isSet) { 
				Statement.DynamicOps |= DynamicOperation.SetIndex;
			} else {
				Statement.DynamicOps |= DynamicOperation.GetIndex;
			}

			return new Invocation (GetBinder (isSet ? "SetIndex" : "GetIndex", loc), binder_args);
		}

		protected override Arguments CreateSetterArguments (ResolveContext rc, Expression rhs)
		{
			//
			// Indexer has arguments which complicates things as the setter and getter
			// are called in two steps when unary mutator is used. We have to make a
			// copy of all variable arguments to not duplicate any side effect.
			//
			// ++d[++arg, Foo ()]
			//

			if (!can_be_mutator)
				return base.CreateSetterArguments (rc, rhs);

			var setter_args = new Arguments (Arguments.Count + 1);
			for (int i = 0; i < Arguments.Count; ++i) {
				var expr = Arguments[i].Expr;

				if (expr is Constant || expr is VariableReference || expr is This) {
					setter_args.Add (Arguments [i]);
					continue;
				}

				LocalVariable temp = LocalVariable.CreateCompilerGenerated (expr.Type, rc.CurrentBlock, loc);
				expr = new SimpleAssign (temp.CreateReferenceExpression (rc, expr.Location), expr).Resolve (rc);
				Arguments[i].Expr = temp.CreateReferenceExpression (rc, expr.Location).Resolve (rc);
				setter_args.Add (Arguments [i].Clone (expr));
			}

			setter_args.Add (new Argument (rhs));
			return setter_args;
		}
	}

	class DynamicInvocation : DynamicExpressionStatement, IDynamicBinder, IDynamicCallSite
	{
		readonly ATypeNameExpression member;

		private bool IsMemberAccess;

		public DynamicInvocation (ATypeNameExpression member, Arguments args, Location loc)
			: base (null, args, loc)
		{
			base.binder = this;
			this.member = member;
			this.IsMemberAccess = (member is MemberAccess) || (member is SimpleName);
		}

		public static DynamicInvocation CreateSpecialNameInvoke (ATypeNameExpression member, Arguments args, Location loc)
		{
			return new DynamicInvocation (member, args, loc) {
				flags = CSharpBinderFlags.InvokeSpecialName
			};
		}

		protected override Expression DoResolve(ResolveContext rc)
		{
			if (rc.Module.PredefinedTypes.IsPlayScriptAotMode && rc.Module.Compiler.Settings.NewDynamicRuntime_HasOwnProperty) {
				// special case handling of hasOwnProperty
				if (this.member != null && this.Arguments != null && this.member.Name == "hasOwnProperty" && Arguments.Count == 2) {
					var ma = new MemberAccess(new MemberAccess(
						new QualifiedAliasMember(QualifiedAliasMember.GlobalAlias, "PlayScript", loc), "Dynamic", loc), "hasOwnProperty");

					var site_args = new Arguments(2);
					site_args.Add(new Argument(EmptyCast.RemoveDynamic(rc, Arguments[0].Expr.Resolve(rc))));
					site_args.Add(Arguments[1]);
					return new Invocation(ma, site_args).Resolve(rc);
				}
			}

			if (rc.Module.PredefinedTypes.IsPlayScriptAotMode && rc.Module.Compiler.Settings.NewDynamicRuntime_ToString) {
				// special case handling of toString
				if (this.member != null && this.Arguments != null && (this.member.Name == "toString" || this.member.Name == "ToString") && Arguments.Count == 1) {
					TypeSpec converter = rc.Module.PredefinedTypes.PsConverter.Resolve();
					var site_args = new Arguments(1);
					site_args.Add(new Argument(EmptyCast.RemoveDynamic(rc, Arguments[0].Expr.Resolve(rc))));
					return new Invocation(new MemberAccess(new TypeExpression(converter, Location), "ConvertToString", Location), site_args).Resolve(rc);
				}
			}

			return base.DoResolve(rc);
		}

		protected override Expression DoResolveWithTypeHint(ResolveContext rc, TypeSpec t) {
			if (IsMemberAccess)
			{
				this.Type = t;
			}
			return this.Resolve(rc);
		}

		#region IDynamicCallSite implementation

		public bool UseCallSite(ResolveContext rc, Arguments args)
		{
			return (IsMemberAccess && rc.Module.Compiler.Settings.NewDynamicRuntime_InvokeMember) ||
				(!IsMemberAccess && rc.Module.Compiler.Settings.NewDynamicRuntime_Invoke);
		}

		public Expression CreateCallSite(ResolveContext rc, Arguments args, bool isSet)
		{
			if (IsMemberAccess) {
				// construct new PsInvokeMember(name, argCount)
				var site_args = new Arguments(2);
				site_args.Add(new Argument(new StringLiteral(rc.BuiltinTypes, member.Name, member.Location)));
				site_args.Add(new Argument(new IntLiteral(rc.BuiltinTypes, (args.Count - 1), loc)));
				return new New(
					new TypeExpression(rc.Module.PredefinedTypes.PsInvokeMember.Resolve(), loc),
					site_args, 
					loc
				);
			} else {
				// construct new PsInvoke(argCount)
				var site_args = new Arguments(1);
				site_args.Add(new Argument(new IntLiteral(rc.BuiltinTypes, (args.Count - 1), loc)));
				return new New(
					new TypeExpression(rc.Module.PredefinedTypes.PsInvoke.Resolve(), loc),
					site_args, 
					loc
					);
			}
		}

		public TypeExpression CreateReducedTypeExpression(ResolveContext ec, TypeSpec t)
		{
			// Convert any internal type like dynamic or null to object
			if (t.Kind == MemberKind.InternalCompilerType)
				t = ec.BuiltinTypes.Object;


			return new TypeExpression(t, loc);
		}

		public Expression InvokeCallSite(ResolveContext rc, Expression site, Arguments args, TypeSpec returnType, bool isStatement)
		{
			string memberName = "Invoke";
			memberName += isStatement ? "Action" : "Func"; 
			memberName += (args.Count - 1);

			var ta = new TypeArguments();
			for (int i = 1; i < args.Count; ++i) {
				ta.Add(CreateReducedTypeExpression(rc, args[i].Type));
			}

			if (!isStatement) {
				ta.Add(CreateReducedTypeExpression(rc, returnType));
			}

			return new Invocation(new MemberAccess(site, memberName, ta, loc), args);
		}

		#endregion

		public Expression CreateCallSiteBinder (ResolveContext ec, Arguments args)
		{
			Statement.DynamicOps |= DynamicOperation.Invoke;

			Arguments binder_args = new Arguments (member != null ? 5 : 3);
			bool is_member_access = member is MemberAccess;

			CSharpBinderFlags call_flags;
			if (!is_member_access && member is SimpleName) {
				call_flags = CSharpBinderFlags.InvokeSimpleName;
				is_member_access = true;
			} else {
				call_flags = 0;
			}

			binder_args.Add (new Argument (new BinderFlags (call_flags, this)));

			if (is_member_access)
				binder_args.Add (new Argument (new StringLiteral (ec.BuiltinTypes, member.Name, member.Location)));

			if (member != null && member.HasTypeArguments) {
				TypeArguments ta = member.TypeArguments;
				if (ta.Resolve (ec, false)) {
					var targs = new ArrayInitializer (ta.Count, loc);
					foreach (TypeSpec t in ta.Arguments)
						targs.Add (new TypeOf (t, loc));

					binder_args.Add (new Argument (new ImplicitlyTypedArrayCreation (targs, loc)));
				}
			} else if (is_member_access) {
				binder_args.Add (new Argument (new NullLiteral (loc)));
			}

			binder_args.Add (new Argument (new TypeOf (ec.CurrentType, loc)));

			Expression real_args;
			if (args == null) {
				// Cannot be null because .NET trips over
				real_args = new ArrayCreation (
					new MemberAccess (GetBinderNamespace (ec, loc), "CSharpArgumentInfo", loc),
					new ArrayInitializer (0, loc), loc);
			} else {
				real_args = new ImplicitlyTypedArrayCreation (args.CreateDynamicBinderArguments (ec), loc);
			}

			binder_args.Add (new Argument (real_args));

			return new Invocation (GetBinder (is_member_access ? "InvokeMember" : "Invoke", loc), binder_args);
		}

		public override void EmitStatement (EmitContext ec)
		{
			flags |= CSharpBinderFlags.ResultDiscarded;
			base.EmitStatement (ec);
		}
	}

	class DynamicMemberBinder : DynamicMemberAssignable
	{
		readonly string name;

		public DynamicMemberBinder (string name, Arguments args, Location loc)
			: base (args, loc)
		{
			this.name = name;
		}

		public DynamicMemberBinder (string name, CSharpBinderFlags flags, Arguments args, Location loc)
			: this (name, args, loc)
		{
			base.flags = flags;
		}

		#region IDynamicCallSite implementation

		public override bool UseCallSite(ResolveContext ec, Arguments args)
		{
			return ec.Module.Compiler.Settings.NewDynamicRuntime_GetSetMember;
		}

		public override Expression CreateCallSite(ResolveContext rc, Arguments args, bool isSet)
		{
			TypeExpression type;
			isSet |= (flags & CSharpBinderFlags.ValueFromCompoundAssignment) != 0;
			if (!isSet) {
				type = new TypeExpression(rc.Module.PredefinedTypes.PsGetMember.Resolve(), loc);
			} else {
				type = new TypeExpression(rc.Module.PredefinedTypes.PsSetMember.Resolve(), loc);
			}

			var site_args = new Arguments(1);
			site_args.Add(new Argument(new StringLiteral(rc.BuiltinTypes, this.name, loc)));
			return new New(
				type,
				site_args, 
				loc
			);
		}

		public override Expression InvokeCallSite(ResolveContext rc, Expression site, Arguments args, TypeSpec returnType, bool isStatement)
		{
			var obj = args[0].Expr;

			bool isSet = IsSetter(site);
			isSet |= (flags & CSharpBinderFlags.ValueFromCompoundAssignment) != 0;
			if (!isSet) {
				if (NeedsCastToObject(returnType)) {
					var site_args = new Arguments(1);
					site_args.Add(new Argument(obj));
					if (returnType != null && returnType.IsAsUntyped)
						return new Invocation(new MemberAccess(site, "GetMemberAsUntyped"), site_args);
					else
						return new Invocation(new MemberAccess(site, "GetMemberAsObject"), site_args);
				} else {
					var site_args = new Arguments(1);
					site_args.Add(new Argument(obj));

					var type_args = new TypeArguments();
					type_args.Add(new TypeExpression(returnType, loc));
					return new Invocation(new MemberAccess(site, "GetMember", type_args, loc), site_args);
				}
			} else {
				var setVal = args[1].Expr;

				if (NeedsCastToObject(setVal.Type)) {
					var site_args = new Arguments(3);
					site_args.Add(new Argument(obj));
					site_args.Add(new Argument(new Cast(new TypeExpression(rc.BuiltinTypes.Object, loc), setVal, loc)));
					site_args.Add(new Argument(new BoolLiteral(rc.BuiltinTypes, false, loc)));
					// TODO: AsUntyped parameters aren't yet supported, so there is no SetMemberAsUntyped
					return new Invocation(new MemberAccess(site, "SetMemberAsObject"), site_args);
				} else {
					var site_args = new Arguments(2);
					site_args.Add(new Argument(obj));
					site_args.Add(new Argument(setVal));

					var type_args = new TypeArguments();
					type_args.Add(new TypeExpression(setVal.Type, loc));
					return new Invocation(new MemberAccess(site, "SetMember", type_args, loc), site_args);
				}
			}
		}

		#endregion

		protected override Expression CreateCallSiteBinder (ResolveContext ec, Arguments args, bool isSet)
		{
			Arguments binder_args = new Arguments (4);

			binder_args.Add (new Argument (new BinderFlags (flags, this)));
			binder_args.Add (new Argument (new StringLiteral (ec.BuiltinTypes, name, loc)));
			binder_args.Add (new Argument (new TypeOf (ec.CurrentType, loc)));
			binder_args.Add (new Argument (new ImplicitlyTypedArrayCreation (args.CreateDynamicBinderArguments (ec), loc)));

			isSet |= (flags & CSharpBinderFlags.ValueFromCompoundAssignment) != 0;

			if (isSet) {
				Statement.DynamicOps |= DynamicOperation.SetMember;
			} else {
				Statement.DynamicOps |= DynamicOperation.GetMember;
			}

			return new Invocation (GetBinder (isSet ? "SetMember" : "GetMember", loc), binder_args);
		}

	}

	//
	// Any member binder which can be source and target of assignment
	//
	abstract class DynamicMemberAssignable : DynamicExpressionStatement, IDynamicBinder, IDynamicCallSite, IAssignMethod
	{
		Expression setter;
		Arguments setter_args;

		protected DynamicMemberAssignable (Arguments args, Location loc)
			: base (null, args, loc)
		{
			base.binder = this;
		}

		protected override Expression DoResolveWithTypeHint(ResolveContext rc, TypeSpec t) {
			this.Type = t;
			return this.Resolve(rc);
		}

		public Expression CreateCallSiteBinder (ResolveContext ec, Arguments args)
		{
			//
			// DoResolve always uses getter
			//
			return CreateCallSiteBinder (ec, args, false);
		}

		protected bool IsSetter(Expression expr)
		{
			return expr.Type.Name.Contains("Set") && !expr.Type.Name.Contains("Get");
		}

		protected bool NeedsCastToObject(TypeSpec t)
		{
			return (t == null) || (t.Kind == MemberKind.InternalCompilerType) || (t.BuiltinType == BuiltinTypeSpec.Type.Dynamic);
		}

		protected abstract Expression CreateCallSiteBinder (ResolveContext ec, Arguments args, bool isSet);

		#region IDynamicCallSite implementation
		public abstract bool       UseCallSite(ResolveContext rc, Arguments args);
		public abstract Expression CreateCallSite(ResolveContext rc, Arguments args, bool isSet);
		public abstract Expression InvokeCallSite(ResolveContext ec, Expression site, Arguments args, TypeSpec returnType, bool isStatement);
		#endregion

		protected virtual Arguments CreateSetterArguments (ResolveContext rc, Expression rhs)
		{
			var setter_args = new Arguments (Arguments.Count + 1);
			setter_args.AddRange (Arguments);
			setter_args.Add (new Argument (rhs));
			return setter_args;
		}

		public override Expression DoResolveLValue (ResolveContext rc, Expression right_side)
		{
			if (right_side == EmptyExpression.OutAccess) {
				right_side.DoResolveLValue (rc, this);
				return null;
			}

			var res_right_side = right_side.Resolve (rc);

			if (DoResolveCore (rc) && res_right_side != null) {
				setter_args = CreateSetterArguments (rc, res_right_side);

				// create setter callsite
				var dc = (binder as IDynamicCallSite);
				if (rc.Module.PredefinedTypes.IsPlayScriptAotMode && (dc != null) && dc.UseCallSite(rc, setter_args)) {
					this.useDelegateInvoke = false;
					setter_args.CreateDynamicBinderArguments(rc);
					setter = CreateCallSite(rc, setter_args, true);
				} else {
					this.useDelegateInvoke = true;
					setter = CreateCallSiteBinder (rc, setter_args, true);
				}
			}

			eclass = ExprClass.Variable;
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			// It's null for ResolveLValue used without assignment
			if (binder_expr == null)
				EmitCall (ec, setter, Arguments, false);
			else
				base.Emit (ec);
		}

		public override void EmitStatement (EmitContext ec)
		{
			// It's null for ResolveLValue used without assignment
			if (binder_expr == null)
				EmitCall (ec, setter, Arguments, true);
			else
				base.EmitStatement (ec);
		}

		#region IAssignMethod Members

		public void Emit (EmitContext ec, bool leave_copy)
		{
			throw new NotImplementedException ();
		}

		public void EmitAssign (EmitContext ec, Expression source, bool leave_copy, bool isCompound)
		{
			EmitCall (ec, setter, setter_args, !leave_copy);
		}

		#endregion
	}

	class DynamicUnaryConversion : DynamicExpressionStatement, IDynamicBinder
	{
		readonly string name;

		public DynamicUnaryConversion (string name, Arguments args, Location loc)
			: base (null, args, loc)
		{
			this.name = name;
			base.binder = this;
		}

		private static string GetDynamicUnaryTypeName(TypeSpec type)
		{
			switch (type.BuiltinType){
				case BuiltinTypeSpec.Type.Bool:
					return "Bool";
				case BuiltinTypeSpec.Type.Int:
					return "Int";
				case BuiltinTypeSpec.Type.Double:
					return "Double";
				case BuiltinTypeSpec.Type.String:
					return "String";
				case BuiltinTypeSpec.Type.UInt:
					return "UInt";
				default:
					return "Object";
			}
		}

		protected override Expression DoResolve(ResolveContext rc)
		{
			if (rc.Module.PredefinedTypes.IsPlayScriptAotMode && rc.Module.Compiler.Settings.NewDynamicRuntime_UnaryOps) {
				return CreateDynamicUnaryOperation(rc);
			}

			return base.DoResolve(rc);
		}

		private Expression CreateDynamicUnaryOperation(ResolveContext rc)
		{
			// if type is "*", we return "*"; otherwise, dynamic
			var isAsUntyped = Arguments[0].Type.IsAsUntyped;

			// strip dynamic from argument
			Arguments.CastDynamicArgs(rc);

			TypeSpec unary = rc.Module.PredefinedTypes.PsUnaryOperation.Resolve();
			string type = GetDynamicUnaryTypeName(Arguments[0].Type);

			// create unary method name
			string unaryMethod = this.name + type;

			var ret = new Invocation (new MemberAccess (new TypeExpression (unary, loc), unaryMethod, loc), Arguments).Resolve (rc);
			if (ret.Type == rc.BuiltinTypes.Object) {
				// cast object to untyped/dynamic for return types
				ret = new Cast (new TypeExpression (isAsUntyped ? rc.BuiltinTypes.AsUntyped : rc.BuiltinTypes.Dynamic, loc), ret, loc).Resolve (rc);
			}
			return ret;
		}


		public static DynamicUnaryConversion CreateIsTrue (ResolveContext rc, Arguments args, Location loc)
		{
			return new DynamicUnaryConversion ("IsTrue", args, loc) { type = rc.BuiltinTypes.Bool };
		}

		public static DynamicUnaryConversion CreateIsFalse (ResolveContext rc, Arguments args, Location loc)
		{
			return new DynamicUnaryConversion ("IsFalse", args, loc) { type = rc.BuiltinTypes.Bool };
		}

		public Expression CreateCallSiteBinder (ResolveContext ec, Arguments args)
		{
			Statement.DynamicOps |= DynamicOperation.Unary;

			Arguments binder_args = new Arguments (4);

			MemberAccess ns;
			if (ec.Module.PredefinedTypes.IsPlayScriptAotMode) {
				ns = new QualifiedAliasMember (QualifiedAliasMember.GlobalAlias, "PlayScript", loc);
			} else {
				ns = new MemberAccess (new MemberAccess (
					new QualifiedAliasMember (QualifiedAliasMember.GlobalAlias, "System", loc), "Linq", loc), "Expressions", loc);
			}

			var flags = ec.HasSet (ResolveContext.Options.CheckedScope) ? CSharpBinderFlags.CheckedContext : 0;

			binder_args.Add (new Argument (new BinderFlags (flags, this)));
			binder_args.Add (new Argument (new MemberAccess (new MemberAccess (ns, "ExpressionType", loc), name, loc)));
			binder_args.Add (new Argument (new TypeOf (ec.CurrentType, loc)));
			binder_args.Add (new Argument (new ImplicitlyTypedArrayCreation (args.CreateDynamicBinderArguments (ec), loc)));

			return new Invocation (GetBinder ("UnaryOperation", loc), binder_args);
		}
	}

	class DynamicBinaryExpression : DynamicExpressionStatement, IDynamicBinder
	{
		readonly string          name;
		readonly Binary.Operator oper;

		public DynamicBinaryExpression (Binary.Operator oper, string name, Arguments args, Location loc)
			: base (null, args, loc)
		{
			this.oper = oper;
			this.name = name;
			base.binder = this;
		}

		protected override Expression DoResolve(ResolveContext rc)
		{
			if (rc.Module.PredefinedTypes.IsPlayScriptAotMode && rc.Module.Compiler.Settings.NewDynamicRuntime_BinaryOps) {
				return this.CreateDynamicBinaryOperation(rc);
			}
			return base.DoResolve(rc);
		}

		private static string GetDynamicBinaryTypeName(TypeSpec type)
		{
			switch (type.BuiltinType){
				case BuiltinTypeSpec.Type.Bool:
					return "Bool";
				case BuiltinTypeSpec.Type.Int:
					return "Int";
				case BuiltinTypeSpec.Type.Double:
					return "Double";
				case BuiltinTypeSpec.Type.String:
					return "String";
				case BuiltinTypeSpec.Type.UInt:
					return "UInt";
				default:
					return "Obj";
			}
		}

		private Expression CreateDynamicBinaryOperation(ResolveContext rc)
		{
			// if either type is "*", we return "*"; otherwise, dynamic
			var isAsUntyped = (Arguments [0].Type.IsAsUntyped || Arguments [1].Type.IsAsUntyped);

			// strip dynamic from all arguments
			Arguments.CastDynamicArgs(rc);

			TypeSpec binary = rc.Module.PredefinedTypes.PsBinaryOperation.Resolve();

			// perform numeric or other type conversion
			string binaryMethod = null;
			switch (oper)
			{
				case Binary.Operator.Multiply:
					binaryMethod = "Multiply";
					break;
				case Binary.Operator.Division:
					binaryMethod = "Division";
					break;
				case Binary.Operator.Modulus:
					binaryMethod = "Modulus";
					break;
				case Binary.Operator.Addition:
					binaryMethod = "Addition";
					break;
				case Binary.Operator.Subtraction:
					binaryMethod = "Subtraction";
					break;
				case Binary.Operator.LeftShift:
					binaryMethod = "LeftShift";
					break;
				case Binary.Operator.RightShift:
					binaryMethod = "RightShift";
					break;
				case Binary.Operator.AsURightShift:
					binaryMethod = "AsURightShift";
					break;
				case Binary.Operator.LessThan:
					binaryMethod = "LessThan";
					break;
				case Binary.Operator.GreaterThan:
					binaryMethod = "GreaterThan";
					break;
				case Binary.Operator.LessThanOrEqual:
					binaryMethod = "LessThanOrEqual";
					break;
				case Binary.Operator.GreaterThanOrEqual:
					binaryMethod = "GreaterThanOrEqual";
					break;
				case Binary.Operator.Equality:
					binaryMethod = "Equality";
					break;
				case Binary.Operator.Inequality:
					binaryMethod = "Inequality";
					break;
				case Binary.Operator.AsStrictEquality:
					binaryMethod = "AsStrictEquality";
					break;
				case Binary.Operator.AsStrictInequality:
					binaryMethod = "AsStrictInequality";
					break;
				case Binary.Operator.BitwiseAnd:
					binaryMethod = "BitwiseAnd";
					break;
				case Binary.Operator.ExclusiveOr:
					binaryMethod = "ExclusiveOr";
					break;
				case Binary.Operator.BitwiseOr:
					binaryMethod = "BitwiseOr";
					break;
					// we should never support these
//				case Binary.Operator.LogicalAnd:
//					binaryMethod = "LogicalAnd";
//					break;
//				case Binary.Operator.LogicalOr:
//					binaryMethod = "LogicalOr";
//					break;
				case Binary.Operator.AsE4xChild:
					binaryMethod = "AsE4xChild";
					break;
				case Binary.Operator.AsE4xDescendant:
					binaryMethod = "AsE4xDescendant";
					break;
				case Binary.Operator.AsE4xChildAttribute:
					binaryMethod = "AsE4xChildAttribute";
					break;
				case Binary.Operator.AsE4xDescendantAttribute:
					binaryMethod = "AsE4xDescendantAttribute";
					break;
				default:
					throw new InvalidOperationException("Unknown binary operation: " + oper);
			}

			string leftType = GetDynamicBinaryTypeName(Arguments[0].Type);
			string rightType = GetDynamicBinaryTypeName(Arguments[1].Type);

			// for strict equality checks, just use a single method and check
			// types at runtime
			if (oper == Binary.Operator.AsStrictEquality ||
				oper == Binary.Operator.AsStrictInequality) {
				leftType = rightType = "Obj";
			}

			// append to binary method instead of using overloads
			binaryMethod += leftType + rightType;

			var ret = new Invocation (new MemberAccess (new TypeExpression (binary, loc), binaryMethod, loc), Arguments).Resolve (rc);
			if (ret.Type == rc.BuiltinTypes.Object) {
				// cast object to untyped/dynamic for return types
				ret = new Cast (new TypeExpression (isAsUntyped ? rc.BuiltinTypes.AsUntyped : rc.BuiltinTypes.Dynamic, loc), ret, loc).Resolve (rc);
			}
			return ret;
		}


		public Expression CreateCallSiteBinder (ResolveContext ec, Arguments args)
		{
			Arguments binder_args = new Arguments (4);

			MemberAccess ns;
			if (ec.Module.PredefinedTypes.IsPlayScriptAotMode) {
				ns = new QualifiedAliasMember (QualifiedAliasMember.GlobalAlias, "PlayScript", loc);
			} else {
				ns = new MemberAccess (new MemberAccess (
					new QualifiedAliasMember (QualifiedAliasMember.GlobalAlias, "System", loc), "Linq", loc), "Expressions", loc);
			}

			CSharpBinderFlags flags = 0;
			if (ec.HasSet (ResolveContext.Options.CheckedScope))
				flags = CSharpBinderFlags.CheckedContext;

			if ((oper & Binary.Operator.LogicalMask) != 0)
				flags |= CSharpBinderFlags.BinaryOperationLogical;

			binder_args.Add (new Argument (new EnumConstant (new IntLiteral (ec.BuiltinTypes, (int) flags, loc), ec.Module.PredefinedTypes.GetBinderFlags(ec).Resolve ())));
			binder_args.Add (new Argument (new MemberAccess (new MemberAccess (ns, "ExpressionType", loc), this.name, loc)));
			binder_args.Add (new Argument (new TypeOf (ec.CurrentType, loc)));									
			binder_args.Add (new Argument (new ImplicitlyTypedArrayCreation (args.CreateDynamicBinderArguments (ec), loc)));

			return new Invocation (new MemberAccess (new TypeExpression (ec.Module.PredefinedTypes.GetBinder(ec).TypeSpec, loc), "BinaryOperation", loc), binder_args);
		}
	}






	sealed class DynamicSiteClass : HoistedStoreyClass
	{
		public DynamicSiteClass (TypeDefinition parent, MemberBase host, TypeParameters tparams)
			: base (parent, MakeMemberName (host, "DynamicSite", parent.DynamicSitesCounter, tparams, Location.Null), tparams, Modifiers.STATIC, MemberKind.Class)
		{
			parent.DynamicSitesCounter++;
		}

		public FieldSpec CreateCallSiteField (FullNamedExpression type, Location loc)
		{
			int index = AnonymousMethodsCounter++;
			Field f = new HoistedField (this, type, Modifiers.PUBLIC | Modifiers.STATIC, "Site" + index.ToString ("X"), null, loc);
			f.Define ();

			AddField (f);
			return f.Spec;
		}
	}
}
