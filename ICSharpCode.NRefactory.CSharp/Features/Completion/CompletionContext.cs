using System;
using Microsoft.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using ICSharpCode.NRefactory6.CSharp.Completion;
using System.Linq;

namespace ICSharpCode.NRefactory6.CSharp
{
	public class CompletionContext
	{
		readonly Document document;

		public Document Document {
			get {
				return document;
			}
		}

		SemanticModel semanticModel;

		internal async Task<SemanticModel> GetSemanticModelAsync (CancellationToken cancellationToken = default (CancellationToken)) 
		{
			if (semanticModel == null)
				semanticModel = await document.GetSemanticModelAsync (cancellationToken);
			return semanticModel;
		}

		readonly int position;
		public int Position {
			get {
				return position;
			}
		}

		SyntaxContext syntaxContext;
		internal Task<SyntaxContext> GetSyntaxContextAsync  (Workspace workspace, CancellationToken cancellationToken = default (CancellationToken)) 
		{
			if (syntaxContext == null)
				syntaxContext = SyntaxContext.Create(workspace, document, semanticModel, position, cancellationToken);
			return Task.FromResult (syntaxContext);
		}

		IEnumerable<CompletionContextHandler> additionalContextHandlers;

		public IEnumerable<CompletionContextHandler> AdditionalContextHandlers {
			get {
				return additionalContextHandlers ?? Enumerable.Empty<CompletionContextHandler> ();
			}
			set {
				additionalContextHandlers = value;
			}
		}

		public CompletionContext (Document document, int position, SemanticModel semanticModel  = null)
		{
			this.document = document;
			this.semanticModel = semanticModel;
			this.position = position;
		}
	}
}