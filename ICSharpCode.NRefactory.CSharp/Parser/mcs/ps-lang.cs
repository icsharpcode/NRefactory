// Copyright 2013 Zynga Inc.
//	
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//		
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.

using System;
using System.Collections.Generic;

#if STATIC
using IKVM.Reflection;
using IKVM.Reflection.Emit;
#else
using System.Reflection;
using System.Reflection.Emit;
#endif
using ICSharpCode.NRefactory.MonoCSharp;

namespace Mono.PlayScript
{
	//
	// Constants
	//

	public static class PsConsts 
	{
		//
		// The namespace used for the root package.
		//
		public const string PsRootNamespace = "_root";
	}

	//
	// Expressions
	//
	
	public class UntypedTypeExpression : TypeExpr
	{
		public UntypedTypeExpression (Location loc)
		{
			this.loc = loc;
		}

		public override TypeSpec ResolveAsType (IMemberContext mc, bool allowUnboundTypeArguments)
		{
			return mc.Module.Compiler.BuiltinTypes.AsUntyped;
		}
	}

	public class UntypedBlockVariable : BlockVariable
	{
		public UntypedBlockVariable (LocalVariable li)
			: base (li)
		{
		}

		public new FullNamedExpression TypeExpression {
			get {
				return type_expr;
			}
			set {
				type_expr = value;
			}
		}

		public override bool Resolve (BlockContext bc)
		{
			if (type_expr == null) {
				if (Initializer == null)
					type_expr = new UntypedTypeExpression (loc);
				else
					type_expr = new VarExpr (loc);
			}

			return base.Resolve (bc);
		}
	}

	public class UntypedExceptionExpression : UntypedTypeExpression
	{
		public UntypedExceptionExpression (Location loc)
			: base(loc)
		{
		}

		public override TypeSpec ResolveAsType (IMemberContext mc, bool allowUnboundTypeArguments)
		{
			return mc.Module.Compiler.BuiltinTypes.Exception;
		}
	}

	//
	// ActionScript: Object initializers implement standard JSON style object
	// initializer syntax in the form { ident : expr [ , ... ] } or { "literal" : expr [, ... ]}
	// Like the array initializer, type is inferred from assignment type, parameter type, or
	// field, var initializer type, or of no type can be inferred it is of type Dictionary<String,Object>.
	//
	public partial class AsObjectInitializer : Expression
	{
		List<Expression> elements;
		BlockVariable variable;
		Assign assign;

		public AsObjectInitializer (List<Expression> init, Location loc)
		{
			elements = init;
			this.loc = loc;
		}

		public AsObjectInitializer (int count, Location loc)
			: this (new List<Expression> (count), loc)
		{
		}

		public AsObjectInitializer (Location loc)
			: this (4, loc)
		{
		}

		#region Properties

		public int Count {
			get { return elements.Count; }
		}

		public List<Expression> Elements {
			get {
				return elements;
			}
		}

		public Expression this [int index] {
			get {
				return elements [index];
			}
		}

		public BlockVariable VariableDeclaration {
			get {
				return variable;
			}
			set {
				variable = value;
			}
		}

		public Assign Assign {
			get {
				return assign;
			}
			set {
				assign = value;
			}
		}

		#endregion

		public void Add (Expression expr)
		{
			elements.Add (expr);
		}

		public override bool ContainsEmitWithAwait ()
		{
			throw new NotSupportedException ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			throw new NotSupportedException ("ET");
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			var target = (AsObjectInitializer) t;

			target.elements = new List<Expression> (elements.Count);
			foreach (var element in elements)
				target.elements.Add (element.Clone (clonectx));
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			// ActionScript - Always use dynamic "expando" object.
			TypeExpression type = new TypeExpression (rc.Module.PredefinedTypes.AsExpandoObject.Resolve(), Location);

			return new NewInitialize (type, null, 
				new CollectionOrObjectInitializers(elements, Location), Location).Resolve (rc);
		}

		public override void Emit (EmitContext ec)
		{
			throw new InternalErrorException ("Missing Resolve call");
		}

		public override object Accept (StructuralVisitor visitor)
		{
			var ret = visitor.Visit (this);

			if (visitor.AutoVisit) {
				if (visitor.Skip) {
					visitor.Skip = false;
					return ret;
				}
				if (visitor.Continue && elements != null) {
					foreach (var elem in elements) {
						if (visitor.Continue)
							elem.Accept (visitor);
					}
				}
			}

			return ret;
		}
	}

	//
	// ActionScript: Array initializer expression is a standard expression
	// allowed anywhere an expression is valid.  The type is inferred from
	// assignment type, parameter type, or field/variable initializer type.
	// If no type is inferred, the type is Vector.<Object>.
	//
	public partial class AsArrayInitializer : ArrayInitializer
	{
		Assign assign;
		FullNamedExpression vectorType;

		public AsArrayInitializer (List<Expression> init, Location loc)
			: base(init, loc)
		{
		}

		public AsArrayInitializer (int count, Location loc)
			: this (new List<Expression> (count), loc)
		{
		}

		public AsArrayInitializer (Location loc)
			: this (4, loc)
		{
		}

		#region Properties

		public Assign Assign {
			get {
				return assign;
			}
			set {
				assign = value;
			}
		}

		public FullNamedExpression VectorType {
			get {
				return vectorType;
			}
			set {
				vectorType = value;
			}
		}

		#endregion

		protected override Expression DoResolve (ResolveContext rc)
		{
			// Attempt to build simple const initializer
			bool is_const_init = false;
			TypeSpec const_type = null;
			if (elements.Count > 0) {
				is_const_init = true;
				const_type = vectorType != null ? vectorType.ResolveAsType (rc) : null;
				foreach (var elem in elements) {
					if (elem == null) {
						is_const_init = false;
						break;
					}
					if (!(elem is Constant) && !(elem is Unary && ((Unary)elem).Expr is Constant)) {
						is_const_init = false;
						break;
					}
					TypeSpec elemType = elem.Type;
					if (vectorType == null) {
						if (elemType == null) {
							is_const_init = false;
							break;
						}
						if (const_type == null)
							const_type = BuiltinTypeSpec.IsPrimitiveType (elemType) ? elemType : rc.BuiltinTypes.Object;
						if (const_type != elemType) {
							if (((const_type == rc.BuiltinTypes.Int || const_type == rc.BuiltinTypes.UInt) && elemType == rc.BuiltinTypes.Double) ||
								(const_type == rc.BuiltinTypes.Double && (elemType == rc.BuiltinTypes.Int || elemType == rc.BuiltinTypes.UInt))) {
								const_type = rc.BuiltinTypes.Double;
							} else {
								const_type = rc.BuiltinTypes.Object;
							}
						}
					}
				}
			}

			TypeExpression type;
			if (vectorType != null) { // For new <Type> [ initializer ] expressions..
				var elemTypeSpec = vectorType.ResolveAsType(rc);
				if (elemTypeSpec != null) {
					type = new TypeExpression(
						rc.Module.PredefinedTypes.AsVector.Resolve().MakeGenericType (rc, new [] { elemTypeSpec }), Location);
				} else {
					type = new TypeExpression (rc.Module.PredefinedTypes.AsArray.Resolve(), Location);
				}
			} else {
				type = new TypeExpression (rc.Module.PredefinedTypes.AsArray.Resolve(), Location);
			}

			TypeSpec typeSpec = type.ResolveAsType(rc.MemberContext);
			if (typeSpec.IsArray) {
				ArrayCreation arrayCreate = (ArrayCreation)new ArrayCreation (type, this).Resolve (rc);
				return arrayCreate;
			} else if (is_const_init) {
				// If all elements in the initializer list are simple constants, we just pass the elements in a .NET array to the
				// PS Array initializer.
				var newArgs = new Arguments (1);
				newArgs.Add (new Argument (new ArrayCreation (new TypeExpression(const_type, loc), this, loc)));
				return new New (type, newArgs, loc).Resolve (rc);
			} else {
				var initElems = new List<Expression>();
				foreach (var e in elements) {
					initElems.Add (new CollectionElementInitializer(e));
				}
				return new NewInitialize (type, null, 
					new CollectionOrObjectInitializers(initElems, Location), Location).Resolve (rc);
			}
		}

		public override object Accept (StructuralVisitor visitor)
		{
			var ret = visitor.Visit (this);

			if (visitor.AutoVisit) {
				if (visitor.Skip) {
					visitor.Skip = false;
					return ret;
				}
				if (visitor.Continue && elements != null) {
					foreach (var elem in elements) {
						if (visitor.Continue)
							elem.Accept (visitor);
					}
				}
			}

			return ret;
		}
	}

	//
	// ActionScript: Implements the ActionScript delete expression.
	// This expression is used to implement the delete expression as
	// well as the delete statement.  Handles both the element access
	// form or the member access form.
	//
	public partial class AsDelete : ExpressionStatement {

		public Expression Expr;
		private Invocation removeExpr;
		
		public AsDelete (Expression expr, Location l)
		{
			this.Expr = expr;
			loc = l;
		}

		public override bool IsSideEffectFree {
			get {
				return removeExpr.IsSideEffectFree;
			}
		}

		public override bool ContainsEmitWithAwait ()
		{
			return removeExpr.ContainsEmitWithAwait ();
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			if (Expr is ElementAccess) {

				var elem_access = Expr as ElementAccess;

				if (elem_access.Arguments.Count != 1) {
					ec.Report.Error (7021, loc, "delete statement must have only one index argument.");
					return null;
				}

				var expr = elem_access.Expr.Resolve (ec);
				if (expr.Type == null) {
					return null;
				}

				if (expr.Type.IsArray) {
					ec.Report.Error (7021, loc, "delete statement not allowed on arrays.");
					return null;
				}

				if (!expr.Type.IsAsDynamicClass && (expr.Type.BuiltinType != BuiltinTypeSpec.Type.Dynamic))
				{
					ec.Report.Error (7021, loc, "delete statement only allowed on dynamic types or dynamic classes");
					return null;
				}

				// cast expression to IDynamicClass and invoke __DeleteDynamicValue
				var dynClass = new Cast(new MemberAccess(new SimpleName("PlayScript", loc), "IDynamicClass", loc), expr, loc);
				removeExpr = new Invocation (new MemberAccess (dynClass, "__DeleteDynamicValue", loc), elem_access.Arguments);
				return removeExpr.Resolve (ec);

			} else if (Expr is MemberAccess) {

				var memb_access = Expr as MemberAccess;

				var expr = memb_access.LeftExpression.Resolve (ec);
				if (expr.Type == null) {
					return null;
				}

				if (!expr.Type.IsAsDynamicClass && (expr.Type.BuiltinType != BuiltinTypeSpec.Type.Dynamic))
				{
					ec.Report.Error (7021, loc, "delete statement only allowed on dynamic types or dynamic classes");
					return null;
				}

				// cast expression to IDynamicClass and invoke __DeleteDynamicValue
				var dynClass = new Cast(new MemberAccess(new SimpleName("PlayScript", loc), "IDynamicClass", loc), expr, loc);
				var args = new Arguments(1);
				args.Add (new Argument(new StringLiteral(ec.BuiltinTypes, memb_access.Name, loc)));
				removeExpr = new Invocation (new MemberAccess (dynClass, "__DeleteDynamicValue", loc), args);
				return removeExpr.Resolve (ec);

			} else {
				// Error is reported elsewhere.
				return null;
			}
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			var target = (AsDelete) t;

			target.Expr = Expr.Clone (clonectx);
		}

		public override void Emit (EmitContext ec)
		{
			throw new System.NotImplementedException ();
		}

		public override void EmitStatement (EmitContext ec)
		{
			throw new System.NotImplementedException ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			return removeExpr.CreateExpressionTree(ec);
		}

		public override object Accept (StructuralVisitor visitor)
		{
			var ret = visitor.Visit (this);

			if (visitor.AutoVisit) {
				if (visitor.Skip) {
					visitor.Skip = false;
					return ret;
				}
				if (visitor.Continue && this.Expr != null)
					this.Expr.Accept (visitor);
			}

			return ret;
		}
	}

	//
	// ActionScript: Implements the ActionScript new expression.
	// This expression is used to implement the as new expression 
	// which takes either a type expression, an AsArrayInitializer,
	// or an invocation expression of some form.
	//
	public partial class AsNew : ExpressionStatement {
		
		public Expression Expr;
		private Expression newExpr;

		public AsNew (Expression expr, Location l)
		{
			this.Expr = expr;
			loc = l;
		}
		
		public override bool IsSideEffectFree {
			get {
				return newExpr.IsSideEffectFree;
			}
		}
		
		public override bool ContainsEmitWithAwait ()
		{
			return newExpr.ContainsEmitWithAwait ();
		}

		private bool IsPlayScriptScalarClass (string className)
		{
			switch (className) {
			case "String":
			case "Number":
			case "Boolean":
				return true;
			default:
				return false;
			}
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			if (Expr is AsArrayInitializer)
				return Expr.Resolve (ec);

			New newExpr = null;

			if (Expr is Invocation) {
				var inv = Expr as Invocation;

				//
				// Special case for PlayScript scalar types with 1 argument - 
				// just do an assignment. This is required for cosntructs like
				//
				//	var num:Number = new Number(1.0);
				//
				// since the underlying C# types are primitives and don't have
				// constructors which take arugments.
				//
				var sn = inv.Exp as SimpleName;
				if (sn != null && IsPlayScriptScalarClass (sn.Name) && inv.Arguments != null && inv.Arguments.Count == 1) {
					Argument arg = inv.Arguments [0].Clone (new CloneContext ());
					arg.Resolve (ec);
					if (arg.Expr.Type != null) {
						if (BuiltinTypeSpec.IsPrimitiveType (arg.Expr.Type) || arg.Expr.Type.BuiltinType == BuiltinTypeSpec.Type.String)
							return arg.Expr;
					}
					// TODO: ActionScript does actually allow this, but its runtime
					// rules are hard to implement at compile time, and this should
					// be a rare use case, so I am leaving it as a compiler error for
					// now.
					ec.Report.Error (7112, loc, "The type `{0}' does not contain a constructor that takes non-scalar arguments", sn.Name);
					return null;
				}

				newExpr = new New(inv.Exp, inv.Arguments, loc);
			} else if (Expr is ElementAccess) {
				if (loc.SourceFile != null && !loc.SourceFile.PsExtended) {
					ec.Report.Error (7103, loc, "Native arrays are only suppored in ASX.'");
					return null;
				}
				var elemAcc = Expr as ElementAccess;
				var exprList = new List<Expression>();
				foreach (var arg in elemAcc.Arguments) {
					exprList.Add (arg.Expr);
				}
				// TODO: Handle jagged arrays
				var arrayCreate = new ArrayCreation ((FullNamedExpression) elemAcc.Expr, exprList, 
				                new ComposedTypeSpecifier (exprList.Count, loc), null, loc);
				return arrayCreate.Resolve (ec);
			} else {
				var resolveExpr = Expr.Resolve (ec);
				if (resolveExpr == null)
					return null;
				if (resolveExpr is TypeOf) {
					newExpr = new New (((TypeOf)resolveExpr).TypeExpression, new Arguments (0), loc);
				} else {
					newExpr = new New (resolveExpr, new Arguments (0), loc);
				}
			}

			return newExpr.Resolve (ec);
		}
		
		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			var target = (AsNew) t;

			target.Expr = Expr.Clone (clonectx);
		}
		
		public override void Emit (EmitContext ec)
		{
			throw new System.NotImplementedException ();
		}
		
		public override void EmitStatement (EmitContext ec)
		{
			throw new System.NotImplementedException ();
		}
		
		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			return newExpr.CreateExpressionTree(ec);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			var ret = visitor.Visit (this);

			if (visitor.AutoVisit) {
				if (visitor.Skip) {
					visitor.Skip = false;
					return ret;
				}
				if (visitor.Continue && this.Expr != null)
					this.Expr.Accept (visitor);
				if (visitor.Continue && this.newExpr != null)
					this.newExpr.Accept (visitor);
			}

			return ret;
		}
	}

	//
	// ActionScript: Implements the ActionScript typeof expression.
	// This expression is for backwards compatibility with javascript
	// and is not supported in ASX.
	//
	public partial class AsTypeOf : ExpressionStatement {
		
		public Expression Expr;
		
		public AsTypeOf (Expression expr, Location l)
		{
			this.Expr = expr;
			loc = l;
		}
		
		public override bool IsSideEffectFree {
			get {
				return Expr.IsSideEffectFree;
			}
		}
		
		public override bool ContainsEmitWithAwait ()
		{
			return Expr.ContainsEmitWithAwait ();
		}
		
		protected override Expression DoResolve (ResolveContext ec)
		{
			if (loc.SourceFile != null && loc.SourceFile.PsExtended) {
				ec.Report.Error (7101, loc, "'typeof' operator not supported in ASX.'");
				return null;
			}

			var args = new Arguments(1);
			args.Add (new Argument(Expr));

			return new Invocation(new MemberAccess(new MemberAccess(
				new SimpleName(PsConsts.PsRootNamespace, loc), "_typeof_fn", loc), "_typeof", loc), args).Resolve (ec);
		}
		
		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			var target = (AsDelete) t;
			
			target.Expr = Expr.Clone (clonectx);
		}
		
		public override void Emit (EmitContext ec)
		{
			throw new System.NotImplementedException ();
		}
		
		public override void EmitStatement (EmitContext ec)
		{
			throw new System.NotImplementedException ();
		}
		
		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			return Expr.CreateExpressionTree(ec);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			var ret = visitor.Visit (this);

			if (visitor.AutoVisit) {
				if (visitor.Skip) {
					visitor.Skip = false;
					return ret;
				}
				if (visitor.Continue && this.Expr != null)
					this.Expr.Accept (visitor);
			}

			return ret;
		}
	}


	public partial class RegexLiteral : Constant, ILiteralConstant
	{
		readonly public string Regex;
		readonly public string Options;

		public RegexLiteral (BuiltinTypes types, string regex, string options, Location loc)
			: base (loc)
		{
			Regex = regex;
			Options = options ?? "";
		}

		public override bool IsLiteral {
			get { return true; }
		}

		public override object GetValue ()
		{
			return "/" + Regex + "/" + Options;
		}
		
		public override string GetValueAsLiteral ()
		{
			return GetValue () as String;
		}
		
		public override long GetValueAsLong ()
		{
			throw new NotSupportedException ();
		}

		public override void EncodeAttributeValue (IMemberContext rc, AttributeEncoder enc, TypeSpec targetType, TypeSpec parameterType)
		{
			throw new NotSupportedException ();
		}
		
		public override bool IsDefaultValue {
			get {
				return Regex == null && Options == "";
			}
		}
		
		public override bool IsNegative {
			get {
				return false;
			}
		}
		
		public override bool IsNull {
			get {
				return IsDefaultValue;
			}
		}
		
		public override Constant ConvertExplicitly (bool in_checked_context, TypeSpec target_type, ResolveContext opt_ec)
		{
			return null;
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			var args = new Arguments(2);
			args.Add (new Argument(new StringLiteral(rc.BuiltinTypes, Regex, this.Location)));
			args.Add (new Argument(new StringLiteral(rc.BuiltinTypes, Options, this.Location)));

			return new New(new TypeExpression(rc.Module.PredefinedTypes.AsRegExp.Resolve(), this.Location), 
			               args, this.Location).Resolve (rc);
		}

		public override void Emit (EmitContext ec)
		{
			throw new NotSupportedException ();
		}

#if FULL_AST
		public char[] ParsedValue { get; set; }
#endif

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public partial class XmlLiteral : Constant, ILiteralConstant
	{
		readonly public string Xml;

		public XmlLiteral (BuiltinTypes types, string xml, Location loc)
			: base (loc)
		{
			Xml = xml;
		}
		
		public override bool IsLiteral {
			get { return true; }
		}
		
		public override object GetValue ()
		{
			return Xml;
		}
		
		public override string GetValueAsLiteral ()
		{
			return GetValue () as String;
		}
		
		public override long GetValueAsLong ()
		{
			throw new NotSupportedException ();
		}
		
		public override void EncodeAttributeValue (IMemberContext rc, AttributeEncoder enc, TypeSpec targetType, TypeSpec parameterType)
		{
			throw new NotSupportedException ();
		}
		
		public override bool IsDefaultValue {
			get {
				return Xml == null;
			}
		}
		
		public override bool IsNegative {
			get {
				return false;
			}
		}
		
		public override bool IsNull {
			get {
				return IsDefaultValue;
			}
		}
		
		public override Constant ConvertExplicitly (bool in_checked_context, TypeSpec target_type, ResolveContext opt_ec)
		{
			return null;
		}
		
		protected override Expression DoResolve (ResolveContext rc)
		{
			var args = new Arguments(1);
			args.Add (new Argument(new StringLiteral(rc.BuiltinTypes, Xml, this.Location)));

			return new New(new TypeExpression(rc.Module.PredefinedTypes.AsXml.Resolve(), this.Location), 
			               args, this.Location).Resolve (rc);
		}
		
		public override void Emit (EmitContext ec)
		{
			throw new NotSupportedException ();
		}
		
#if FULL_AST
		public char[] ParsedValue { get; set; }
#endif
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	/// <summary>
	///   Implementation of the ActionScript `in' operator.
	/// </summary>
	public partial class AsIn : Expression
	{
		protected Expression expr;
		protected Expression objExpr;

		public AsIn (Expression expr, Expression obj_expr, Location l)
		{
			this.expr = expr;
			this.objExpr = obj_expr;
			loc = l;
		}

		public Expression Expr {
			get {
				return expr;
			}
		}

		public Expression ObjectExpression {
			get {
				return objExpr;
			}
		}

		public override bool ContainsEmitWithAwait ()
		{
			throw new NotSupportedException ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			throw new NotSupportedException ("ET");
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			var objExpRes = objExpr.Resolve (ec);

			var args = new Arguments (1);
			args.Add (new Argument (expr));

			if (objExpRes.Type.IsDynamic) {
				var inArgs = new Arguments (2);
				inArgs.Add (new Argument (objExpr));
				inArgs.Add (new Argument (expr));
				return new Invocation (new MemberAccess (new MemberAccess (new SimpleName ("PlayScript", loc), "Support", loc), "DynamicIn", loc), inArgs).Resolve (ec);
			} else {
				string containsMethodName = "Contains";
	
				if (objExpRes.Type != null && objExpRes.Type.ImplementsInterface (ec.Module.PredefinedTypes.IDictionary.Resolve(), true)) {
					containsMethodName = "ContainsKey";
				}

				return new Invocation (new MemberAccess (objExpr, containsMethodName, loc), args).Resolve (ec);
			}
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			AsIn target = (AsIn) t;

			target.expr = expr.Clone (clonectx);
			target.objExpr = objExpr.Clone (clonectx);
		}

		public override void Emit (EmitContext ec)
		{
			throw new InternalErrorException ("Missing Resolve call");
		}

		public override object Accept (StructuralVisitor visitor)
		{
			var ret = visitor.Visit (this);

			if (visitor.AutoVisit) {
				if (visitor.Skip) {
					visitor.Skip = false;
					return ret;
				}
				if (visitor.Continue && this.expr != null)
					this.expr.Accept (visitor);
				if (visitor.Continue && this.objExpr != null)
					this.objExpr.Accept (visitor);
			}

			return ret;
		}

	}

	/// <summary>
	///   Implementation of the ActionScript `undefined' object constant.
	/// </summary>
	public partial class AsUndefinedLiteral : Expression
	{
		public AsUndefinedLiteral (Location l)
		{
			loc = l;
		}

		public override string ToString ()
		{
			return this.GetType ().Name + " (undefined)";
		}

		public override bool ContainsEmitWithAwait ()
		{
			throw new NotSupportedException ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			throw new NotSupportedException ("ET");
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			return new MemberAccess(new TypeExpression(ec.Module.PredefinedTypes.AsUndefined.Resolve(), loc), 
			                        "_undefined", loc).Resolve (ec);
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
		}

		public override void Emit (EmitContext ec)
		{
			throw new InternalErrorException ("Missing Resolve call");
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}

	}

	//
	// ActionScript: Implements the ActionScript delete expression.
	// This expression is used to implement the delete expression as
	// well as the delete statement.  Handles both the element access
	// form or the member access form.
	//
	public partial class AsLocalFunction : Statement {
		
		public string Name;
		public AnonymousMethodExpression MethodExpr;
		public BlockVariable VarDecl;

		public AsLocalFunction (Location loc, string name, AnonymousMethodExpression methodExpr, BlockVariable varDecl)
		{
			this.loc = loc;
			this.Name = name;
			this.MethodExpr = methodExpr;
			this.VarDecl = varDecl;
		}

		public override bool Resolve (BlockContext bc)
		{
			return true;
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			var target = (AsLocalFunction) t;

			target.Name = Name;
			target.MethodExpr = MethodExpr.Clone (clonectx) as AnonymousMethodExpression;
			target.VarDecl = VarDecl.Clone (clonectx) as BlockVariable;
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			throw new NotImplementedException ();
		}
			
		protected override void DoEmit (EmitContext ec)
		{
		}


		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			throw new System.NotSupportedException ();
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			var ret = visitor.Visit (this);

			if (visitor.AutoVisit) {
				if (visitor.Skip) {
					visitor.Skip = false;
					return ret;
				}
				if (visitor.Continue && this.MethodExpr != null)
					this.MethodExpr.Accept (visitor);
			}

			return ret;
		}
	}

	// Use namespace statement
	public partial class AsUseNamespaceStatement : Statement {

		public string NS;

		public AsUseNamespaceStatement(string ns, Location loc)
		{
			this.loc = loc;
			NS = ns;
		}

		public override bool Resolve (BlockContext ec)
		{
			return true;
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			throw new NotImplementedException ();
		}

//		public override bool ResolveUnreachable (BlockContext ec, bool warn)
//		{
//			return true;
//		}
		
		public override void Emit (EmitContext ec)
		{
		}
		
		protected override void DoEmit (EmitContext ec)
		{
			throw new NotSupportedException ();
		}
		
		protected override void CloneTo (CloneContext clonectx, Statement target)
		{
			// nothing needed.
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}

	}

	public partial class AsNonAssignStatementExpression : Statement
	{
		public Expression expr;
		
		public AsNonAssignStatementExpression (Expression expr)
		{
			this.expr = expr;
		}
		
		public Expression Expr {
			get {
				return expr;
			}
		}

		public override bool Resolve (BlockContext bc)
		{
			if (!base.Resolve (bc))
				return false;

			expr = expr.Resolve (bc);

			return expr != null;
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			// TODO: Is it ok to short-circuit the flow analysis for a getter that has no assignment?...
			return true;
		}

		protected override void DoEmit (EmitContext ec)
		{
			if (!expr.IsSideEffectFree) {
				expr.EmitSideEffect (ec);
			}
		}

		protected override void CloneTo (CloneContext clonectx, Statement target)
		{
			var t = target as AsNonAssignStatementExpression;
			t.expr = expr.Clone (clonectx);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			var ret = visitor.Visit (this);

			if (visitor.AutoVisit) {
				if (visitor.Skip) {
					visitor.Skip = false;
					return ret;
				}
				if (visitor.Continue && this.expr != null)
					this.expr.Accept (visitor);
			}

			return ret;
		}
	}

	/// <summary>
	///   Implementation of the ActionScript E4X xml query.
	/// </summary>
	public partial class AsXmlQueryExpression : Expression
	{
		protected Expression expr;
		protected Expression query;
		
		public AsXmlQueryExpression (Expression expr, Expression query, Location l)
		{
			this.expr = expr;
			this.query = query;
			loc = l;
		}
		
		public Expression Expr {
			get {
				return expr;
			}
		}

		public Expression Query {
			get {
				return query;
			}
		}

		public override bool ContainsEmitWithAwait ()
		{
			throw new NotSupportedException ();
		}
		
		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			throw new NotSupportedException ("ET");
		}
		
		protected override Expression DoResolve (ResolveContext ec)
		{
			// TODO: Implement XML query expression.
			return null;
		}
		
		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			AsXmlQueryExpression target = (AsXmlQueryExpression) t;
			
			target.expr = expr.Clone (clonectx);
			target.query = query.Clone (clonectx);
		}
		
		public override void Emit (EmitContext ec)
		{
			throw new InternalErrorException ("Missing Resolve call");
		}

		public override object Accept (StructuralVisitor visitor)
		{
			var ret = visitor.Visit (this);

			if (visitor.AutoVisit) {
				if (visitor.Skip) {
					visitor.Skip = false;
					return ret;
				}
				if (visitor.Continue && this.expr != null)
					this.expr.Accept (visitor);
				if (visitor.Continue && this.query != null)
					this.query.Accept (visitor);
			}

			return ret;
		}
		
	}

	public class AsMethod : Method
	{
		public AsMethod (TypeDefinition parent, FullNamedExpression returnType, Modifiers mod, MemberName name, ParametersCompiled parameters, Attributes attrs)
			: base (parent, returnType, mod, name, parameters, attrs)
		{
		}

		public static new Method Create (TypeDefinition parent, FullNamedExpression returnType, Modifiers mod,
		                                 MemberName name, ParametersCompiled parameters, Attributes attrs)
		{
			var rt = returnType ?? new UntypedTypeExpression (name.Location);

			var m = Method.Create (parent, rt, mod, name, parameters, attrs);

			if (returnType == null)
				m.HasNoReturnType = true;

			return m;
		}

		protected override void Error_OverrideWithoutBase (MemberSpec candidate)
		{
			if (candidate == null) {
				Report.Error (1020, Location, "`{0}': Method marked override must override another method", GetSignatureForError ());
				return;
			}

			base.Error_OverrideWithoutBase (candidate);
		}
	}
}
