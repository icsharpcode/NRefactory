using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public abstract class ChangeModifierBaseAction
	{
		protected abstract Modifiers Modifier {get;}
		protected abstract string Name {get;}
		protected abstract CodeAction GetReplaceAction (RefactoringContext context);
		protected abstract bool tryGetImplicitPrivateAction(RefactoringContext context, out CodeAction privateAction);
		protected CSharpModifierToken modifierNode;

		protected bool IsProtectedInternal(){
			var isProtected = modifierNode.Modifier == Modifiers.Protected;
			if (isProtected){
				var nextModifier = modifierNode.GetNextNode() as CSharpModifierToken;
				return nextModifier != null && nextModifier.Modifier == Modifiers.Internal;
			}
			return false;
		}
		
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			modifierNode = context.GetNode<CSharpModifierToken>();
			if (modifierNode == null) {
				CodeAction privateAction;
				if (tryGetImplicitPrivateAction(context, out privateAction))
					yield return privateAction;
				else yield break;
			}
			if (modifierNode.Modifier == Modifier) yield break;
			var isTypeDeclaration = modifierNode.Parent is TypeDeclaration;
			if (!isTypeDeclaration) yield break;
			yield return GetReplaceAction(context);
		}
	}
}

