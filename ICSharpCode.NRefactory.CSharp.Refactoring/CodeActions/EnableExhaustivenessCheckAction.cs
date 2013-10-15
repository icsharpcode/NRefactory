namespace ICSharpCode.NRefactory.CSharp.Refactoring.CodeActions
{
	[ContextAction(
		"Enable exhaustiveness check",
		Description = "Enable exhaustiveness check for this switch")]
	public class EnableExhaustivenessCheckAction: SpecializedCodeAction<SwitchStatement>
	{
		protected override CodeAction GetAction(RefactoringContext context, SwitchStatement node)
		{
			if (!node.SwitchToken.Contains(context.Location))
				return null;

			if (SomeOfEnumValuesWasNotHandledInSwitchStatementIssue.IsExhaustivenessCheckEnabled(node))
				return null;

			var switchData = SomeOfEnumValuesWasNotHandledInSwitchStatementIssue.BuildSwitchData(node, context);
			if (switchData == null)
				return null; 

			return new CodeAction(
				context.TranslateString("Enabled exhaustiveness check"),
				script => EnableCheck(script, node),
				node
			);
		}

		static void EnableCheck(Script script, SwitchStatement node)
		{
			var comment = new Comment(" " + SomeOfEnumValuesWasNotHandledInSwitchStatementIssue.EnableCheckComment);
			script.InsertBefore(node, comment);
		}
	}
}
