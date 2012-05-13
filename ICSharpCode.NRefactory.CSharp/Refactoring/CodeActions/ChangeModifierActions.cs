using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("To public", Description = "Changes the access modifier to public.")]
	public class ToPublicAction : ChangeModifierBaseAction, ICodeActionProvider
	{
		protected override Modifiers Modifier {get {return Modifiers.Public;}}
		protected override CodeAction GetReplaceAction (RefactoringContext context)
		{
			return new CodeAction(context.TranslateString("To public"), script => {
				if (IsProtectedInternal()){
					RemoveSpaceAfterModifier (script);
					script.Remove(modifierNode.GetNextNode());
				}
				script.Replace(modifierNode, new CSharpModifierToken (modifierNode.StartLocation, Modifier));
			});
		}

		protected override bool tryGetImplicitPrivateAction(RefactoringContext context, out CodeAction privateAction)
		{
			privateAction = null;
			var classNode = context.GetNode<CSharpTokenNode>();
			if (classNode == null) return false;
			if (classNode.Role != Roles.ClassKeyword) return false;
			if (classNode.GetPrevNode() is CSharpModifierToken) return false;
			privateAction = new CodeAction(context.TranslateString("To public"), script => {
				script.InsertBefore(classNode, new CSharpModifierToken (classNode.StartLocation, Modifier));
			});
			return true;
		}

		void RemoveSpaceAfterModifier (Script script)
		{
			script.RemoveText(script.GetCurrentOffset(modifierNode.GetNextNode().EndLocation), 1);
		}
	}
	
	[ContextAction("To private", Description = "Changes the access modifier to public.")]
	public class ToPrivateAction : ChangeModifierBaseAction, ICodeActionProvider
	{
		protected override Modifiers Modifier {get {return Modifiers.Private;}}
		protected override bool tryGetImplicitPrivateAction(RefactoringContext context, out CodeAction privateAction) {
			privateAction = null;
			return false;
		}
		protected override CodeAction GetReplaceAction (RefactoringContext context)
		{
			return new CodeAction(context.TranslateString("To private"), script => {
				if (IsProtectedInternal()){
					RemoveSpaceAfterModifier (script);
					script.Remove(modifierNode.GetNextNode());
				}
				script.Replace(modifierNode, new CSharpModifierToken (modifierNode.StartLocation, Modifier));
			});
		}

		void RemoveSpaceAfterModifier (Script script)
		{
			script.RemoveText(script.GetCurrentOffset(modifierNode.GetNextNode().EndLocation), 1);
		}
	}
	
	[ContextAction("To protected", Description = "Changes the access modifier to public.")]
	public class ToProtectedAction : ChangeModifierBaseAction, ICodeActionProvider
	{
		protected override Modifiers Modifier {get {return Modifiers.Protected;}}
		protected override CodeAction GetReplaceAction (RefactoringContext context)
		{
			return new CodeAction(context.TranslateString("To protected"), script => {
				script.Replace(modifierNode, new CSharpModifierToken (modifierNode.StartLocation, Modifier));
			});
		}

		protected override bool tryGetImplicitPrivateAction(RefactoringContext context, out CodeAction privateAction)
		{
			privateAction = null;
			var classNode = context.GetNode<CSharpTokenNode>();
			if (classNode.Role != Roles.ClassKeyword) return false;
			if (classNode.GetPrevNode() is CSharpModifierToken) return false;
			privateAction = new CodeAction(context.TranslateString("To protected"), script => {
				script.InsertBefore(classNode, new CSharpModifierToken (classNode.StartLocation, Modifier));
			});
			return true;
		}
	}
}