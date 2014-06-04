using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.NRefactory6.CSharp.Refactoring
{
	/// <summary>
	/// A specialized code action creates a code action assoziated with one special type of ast nodes.
	/// </summary>
	public abstract class SpecializedCodeAction<T> : ICodeRefactoringProvider where T : SyntaxNode
	{
		/// <summary>
		/// Gets the action for the specified ast node.
		/// </summary>
		/// <returns>
		/// The code action. May return <c>null</c>, if no action can be provided.
		/// </returns>
		/// <param name='semanticModel'>
		/// The semantic model.
		/// </param>
		/// <param name = "root">The root node.</param>
		/// <param name='node'>
		/// The AstNode it's ensured that the node is always != null, if called.
		/// </param>
		protected abstract IEnumerable<CodeAction> GetActions(SemanticModel semanticModel, SyntaxNode root, TextSpan span, T node, CancellationToken cancellationToken);

		#region ICodeActionProvider implementation
		public async Task<IEnumerable<CodeAction>> GetRefactoringsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
		{
			var model = await document.GetSemanticModelAsync(cancellationToken);
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			var node = root.FindNode(span);
			var foundNode = (T)node.AncestorsAndSelf().FirstOrDefault(n => n is T);
			if (foundNode == null)
				return null;
			return GetActions(model, root, span, foundNode, cancellationToken);
		}
		#endregion
	}
}