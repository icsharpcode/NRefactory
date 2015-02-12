using System;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	/// <summary>
	/// Provides information about what triggered completion.
	/// </summary>
	public struct CompletionTriggerInfo
	{
		/// <summary>
		/// Provides the reason that completion was triggered.
		/// </summary>
		public CompletionTriggerReason CompletionTriggerReason { get; private set; }

		/// <summary>
		/// If the <see cref="CompletionTriggerReason"/> was <see
		/// cref="CompletionTriggerReason.CharTyped"/> then this was the character that was
		/// typed or deleted by backspace.  Otherwise it is null.
		/// </summary>
		public char? TriggerCharacter { get; private set; }

		/// <summary>
		/// Returns true if the reason completion was triggered was to augment an existing list of
		/// completion items.
		/// </summary>
		public bool IsAugment { get; private set; }

		/// <summary>
		///  Returns true if completion was triggered by the debugger.
		/// </summary>
		public bool IsDebugger { get; private set; }

		/// <summary>
		/// Return true if completion is running in the Immediate Window.
		/// </summary>
		public bool IsImmediateWindow { get; private set; }

		public CompletionTriggerInfo (CompletionTriggerReason completionTriggerReason, char? triggerCharacter = null, bool isAugment = false, bool isDebugger = false, bool isImmediateWindow = false) : this()
		{
			this.CompletionTriggerReason = completionTriggerReason;
			this.TriggerCharacter = triggerCharacter;
			this.IsAugment = isAugment;
			this.IsDebugger = isDebugger;
			this.IsImmediateWindow = isImmediateWindow;
		}

		public CompletionTriggerInfo WithIsAugment(bool isAugment)
		{
			return this.IsAugment == isAugment
				? this
				: new CompletionTriggerInfo(this.CompletionTriggerReason, this.TriggerCharacter, isAugment, this.IsDebugger, this.IsImmediateWindow);
		}

		public CompletionTriggerInfo WithIsDebugger(bool isDebugger)
		{
			return this.IsDebugger == isDebugger
				? this
				: new CompletionTriggerInfo(this.CompletionTriggerReason, this.TriggerCharacter, this.IsAugment, isDebugger, this.IsImmediateWindow);
		}

		public CompletionTriggerInfo WithIsImmediateWindow(bool isImmediateWIndow)
		{
			return this.IsImmediateWindow == isImmediateWIndow
				? this
				: new CompletionTriggerInfo(this.CompletionTriggerReason, this.TriggerCharacter, this.IsAugment, this.IsDebugger, isImmediateWIndow);
		}
	}
}

