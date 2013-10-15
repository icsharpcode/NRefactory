using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("Some of enum values was not handled in switch statement",
		Description = "Some of enum values was not handled in switch statement.",
		Category = IssueCategories.CodeQualityIssues,
		Severity = Severity.Warning)]
	public class SomeOfEnumValuesWasNotHandledInSwitchStatementIssue: GatherVisitorCodeIssueProvider
	{
		internal static readonly string EnableCheckComment = "Check exhaustiveness";

		internal static bool IsExhaustivenessCheckEnabled(SwitchStatement switchStatement)
		{
			var comment = switchStatement.Parent.GetChildByRole(Roles.Comment);
			return comment != null && comment.Content.Trim() == EnableCheckComment;
		}

		internal static SwitchData BuildSwitchData(SwitchStatement switchStatement, BaseRefactoringContext context)
		{
			return new SwitchDataBuilder(switchStatement, context).Build();
		}

		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
		}

		internal class SwitchData
		{
			public readonly IType EnumType;
			public readonly IEnumerable<MemberReferenceExpression> LabelsExpressions;

			public SwitchData(IType enumType, IEnumerable<MemberReferenceExpression> labelsExpressions)
			{
				EnumType = enumType;
				LabelsExpressions = labelsExpressions.ToArray();
			}
		}

		class SwitchDataBuilder
		{
			readonly SwitchStatement _switchStatement;
			readonly BaseRefactoringContext _context;

			public SwitchDataBuilder(SwitchStatement switchStatement, BaseRefactoringContext context)
			{
				_switchStatement = switchStatement;
				_context = context;
			}

			public SwitchData Build()
			{
				var enumType = GetEnumType();
				if (enumType == null)
					return null;

				var labelsExpressions = GatherCaseLabelsExpressions();
				if (!AreAllExpressionsHasEnumType(labelsExpressions, enumType))
					return null;

				return new SwitchData(enumType, labelsExpressions.Cast<MemberReferenceExpression>());
			}

			IType GetEnumType()
			{
				var resolveResult = _context.Resolve(_switchStatement.Expression);
				return resolveResult.Type.Kind == TypeKind.Enum ? resolveResult.Type : null;
			}

			IEnumerable<Expression> GatherCaseLabelsExpressions()
			{
				var labels = _switchStatement.SwitchSections.SelectMany(_ => _.CaseLabels);
				var nonDefaultLabels = labels.Where(_ => !_.Expression.IsNull);

				return nonDefaultLabels.Select(_ => _.Expression);
			}

			bool AreAllExpressionsHasEnumType(IEnumerable<Expression> expressions, IType type)
			{
				var resolveResults = expressions.Select(_ => _context.Resolve(_)).ToArray();
				return resolveResults.Any() && resolveResults.All(_ => _.Type == type);
			}
		}

		class GatherVisitor: GatherVisitorBase<SomeOfEnumValuesWasNotHandledInSwitchStatementIssue>
		{
			public GatherVisitor(BaseRefactoringContext ctx) : base(ctx)
			{
			}

			public override void VisitSwitchStatement(SwitchStatement switchStatement)
			{
				base.VisitSwitchStatement(switchStatement);

				if (IsExhaustivenessCheckEnabled(switchStatement)) {
					var switchData = BuildSwitchData(switchStatement, ctx);

					if (switchData != null) {
						var missingValues = GetMissingEnumValues(switchData).ToArray();

						if (missingValues.Any())
							AddIssue(new CodeIssue(
								switchStatement.SwitchToken,
								"Some of enum values was not handled",
								"Handle missing values",
								script => GenerateMissingCasesForMissingValues(script, switchStatement, missingValues)
							));
					}
				}
			}

			static IEnumerable<IField> GetMissingEnumValues(SwitchData switchData)
			{
				var handledValues = switchData.LabelsExpressions.Select(_ => _.MemberName).ToArray();
				var allValues = switchData.EnumType.GetFields(_ => _.IsConst && _.IsPublic);

				return allValues.Where(_ => !handledValues.Contains(_.Name));
			}

			static void GenerateMissingCasesForMissingValues(Script script, SwitchStatement switchStatement, IEnumerable<IField> values)
			{
				var astType = new SimpleType(values.First().Type.Name);
				var newSwitchStatement = (SwitchStatement)switchStatement.Clone();

				var previousSection = GetDefaultSection(newSwitchStatement); 
				foreach (var value in values.Reverse()) {
					var newSection = new SwitchSection {
						CaseLabels = {
							new CaseLabel(new MemberReferenceExpression(astType.Clone(), value.Name))
						},
						Statements = {
							new ThrowStatement(new ObjectCreateExpression(new SimpleType("System.NotImplementedException")))
						}
					};

					if (previousSection != null)
						newSwitchStatement.SwitchSections.InsertBefore(previousSection, newSection);
					else
						newSwitchStatement.SwitchSections.Add(newSection);

					previousSection = newSection;
				}

				script.Replace(switchStatement, newSwitchStatement);
			}

			static SwitchSection GetDefaultSection(SwitchStatement switchStatement)
			{
				var sections = switchStatement.SwitchSections;
				return sections.FirstOrDefault(s => s.CaseLabels.Any(l => l.Expression.IsNull));
			}
		}
	}
}
