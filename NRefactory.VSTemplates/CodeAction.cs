using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	[NRefactoryCodeRefactoringProvider(Description = "<DESCRIPTION>")]
	[ExportCodeRefactoringProvider("<NAME>", LanguageNames.CSharp)]
	public class $safeitemrootname$ : CodeRefactoringProvider
	{
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			
			// TODO Find the SyntaxNode which the refactoring will affect

			context.RegisterRefactoring(
				CodeActionFactory.Create(
					span,
					DiagnosticSeverity.Info,
					"<DESCRIPTION>",
					t2 => {
						// TODO Implement refactoring here:
						// var newRoot = root.ReplaceNode((SyntaxNode)node, ...);
						var newRoot = root;	// TODO Remove this

						return Task.FromResult(document.WithSyntaxRoot(newRoot));
					}
				)
			);
		}
	}
}
