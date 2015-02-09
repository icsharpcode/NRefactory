using System;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	public struct CompletionTriggerInfo
	{
		public CompletionTriggerReason CompletionTriggerReason { get; private set; }
		public char? TriggerCharacter { get; private set; }

		public CompletionTriggerInfo (CompletionTriggerReason completionTriggerReason, char? triggerCharacter = null)
		{
			this.CompletionTriggerReason = completionTriggerReason;
			this.TriggerCharacter = triggerCharacter;
		}
	}
}

