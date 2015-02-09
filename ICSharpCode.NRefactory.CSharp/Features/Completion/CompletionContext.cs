using System;
using Microsoft.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

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

		public async Task<SemanticModel> GetSemanticModelAsync (CancellationToken cancellationToken = default (CancellationToken)) 
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

		public CompletionContext (Document document, int position, SemanticModel semanticModel  = null)
		{
			this.document = document;
			this.semanticModel = semanticModel;
			this.position = position;
		}
	}
}