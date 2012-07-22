using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp.Resolver {
	sealed class QueryExpressionLambda : LambdaResolveResult
	{
		internal class Parameter : IParameter {
			public string Name { get { return name; } }
			public DomRegion Region { get { return DomRegion.Empty; } }
			public IType Type { get { return type; } }
			public bool IsConst { get { return false; } }
			public object ConstantValue { get { return null; } }
			public IList<IAttribute> Attributes { get { return EmptyList<IAttribute>.Instance; } }
			public bool IsRef { get { return false; } }
			public bool IsOut { get { return false; } }
			public bool IsParams { get { return false; } }
			public bool IsOptional { get { return false; } }

			private string name;
			internal IType type;

			public Parameter(string name) : this(name, SpecialType.UnknownType) {
			}

			public Parameter(string name, IType type) {
				this.name = name;
				this.type = type;
			}

			public override string ToString() {
				return DefaultParameter.ToString(this);
			}
		}

		readonly Parameter[] parameters;
		readonly ResolveResult bodyExpression;
		internal IType[] inferredParameterTypes;
		IDictionary<IVariable, ResolveResult> rangeVariableMap;

		public IDictionary<IVariable, ResolveResult> RangeVariableMap { get { return rangeVariableMap; } }
			
		public QueryExpressionLambda(int parameterCount, ResolveResult bodyExpression)
		{
			this.parameters = new Parameter[parameterCount];
			for (int i = 0; i < parameterCount; i++) {
				parameters[i] = new Parameter("x" + i);
			}
			this.bodyExpression = bodyExpression;
		}

		internal QueryExpressionLambda(IList<Parameter> parameters, ResolveResult bodyExpression, IDictionary<IVariable, ResolveResult> rangeVariableMap)
		{
			this.parameters = new Parameter[parameters.Count];
			for (int i = 0; i < parameters.Count; i++) {
				this.parameters[i] = parameters[i];
			}
			this.bodyExpression = bodyExpression;
			this.rangeVariableMap = rangeVariableMap;
		}
			
		public override IList<IParameter> Parameters {
			get { return parameters; }
		}
			
		public override Conversion IsValid(IType[] parameterTypes, IType returnType, CSharpConversions conversions)
		{
			if (parameterTypes.Length == parameters.Length) {
				this.inferredParameterTypes = parameterTypes;
				return new QueryExpressionLambdaConversion(parameterTypes);
			} else {
				return Conversion.None;
			}
		}
			
		public override bool IsAsync {
			get { return false; }
		}
			
		public override bool IsImplicitlyTyped {
			get { return true; }
		}
			
		public override bool IsAnonymousMethod {
			get { return false; }
		}
			
		public override bool HasParameterList {
			get { return true; }
		}
			
		public override ResolveResult Body {
			get { return bodyExpression; }
		}
			
		public override IType GetInferredReturnType(IType[] parameterTypes)
		{
			return bodyExpression.Type;
		}
			
		public override string ToString()
		{
			return String.Format("[QueryExpressionLambda ({0}) => {1}]", String.Join(",", parameters.Select(p => p.Name)), bodyExpression);
		}

		internal void UpdateParametersFrom(QueryExpressionLambdaConversion conversion) {
			for (int i = 0; i < parameters.Length; i++) {
				parameters[i].type = conversion.ParameterTypes[i];
			}
		}

		internal sealed class QueryExpressionLambdaConversion : Conversion
		{
			internal readonly IType[] ParameterTypes;
			
			public QueryExpressionLambdaConversion(IType[] parameterTypes)
			{
				this.ParameterTypes = parameterTypes;
			}
			
			public override bool IsImplicit {
				get { return true; }
			}
			
			public override bool IsAnonymousFunctionConversion {
				get { return true; }
			}
		}
	}
}