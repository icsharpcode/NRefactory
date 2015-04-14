using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings
{
	/// <summary>
	/// A specialized code action creates a code action assoziated with one special type of ast nodes.
	/// </summary>
	public abstract class SpecializedCodeRefactoringProvider<T> : CodeRefactoringProvider where T : SyntaxNode
	{
		/// <summary>
		/// Gets the action for the specified ast node.
		/// </summary>
		/// <returns>
		/// The code action. May return <c>null</c>, if no action can be provided.
		/// </returns>
		/// <param name='document'>
		/// The document.
		/// </param>
		/// <param name='semanticModel'>
		/// The semantic model.
		/// </param>
		/// <param name='span'>
		/// The span.
		/// </param>
		/// <param name = "root">The root node.</param>
		/// <param name = "cancellationToken"></param>
		/// <param name='node'>
		/// The AstNode it's ensured that the node is always != null, if called.
		/// </param>
		protected abstract IEnumerable<CodeAction> GetActions(Document document, SemanticModel semanticModel, SyntaxNode root, TextSpan span, T node, CancellationToken cancellationToken);

		#region ICodeActionProvider implementation
		public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		{
			var document = context.Document;
			if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
				return;
			var span = context.Span;
			if (!span.IsEmpty)
				return;
			var cancellationToken = context.CancellationToken;
			if (cancellationToken.IsCancellationRequested)
				return;
			
			var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			if (model.IsFromGeneratedCode())
				return;
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
			var node = root.FindNode(span, false, true);
			var foundNode = (T)node.AncestorsAndSelf().FirstOrDefault(n => n is T);
			if (foundNode == null)
				return;
			foreach (var action in GetActions(document, model, root, span, foundNode, cancellationToken))
				context.RegisterRefactoring(action);
		}
		#endregion
	}
}