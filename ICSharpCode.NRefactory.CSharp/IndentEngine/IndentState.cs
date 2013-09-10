//
// IndentState.cs
//
// Author:
//       Matej Miklečić <matej.miklecic@gmail.com>
//
// Copyright (c) 2013 Matej Miklečić (matej.miklecic@gmail.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ICSharpCode.NRefactory.CSharp
{
	#region IndentState

	/// <summary>
	///     The base class for all indentation states. 
	///     Each state defines the logic for indentation based on chars that
	///     are pushed to it.
	/// </summary>
	public abstract class IndentState : ICloneable
	{
		#region Properties

		/// <summary>
		///     The indentation engine using this state.
		/// </summary>
		public CSharpIndentEngine Engine;

		/// <summary>
		///     The parent state. 
		///     This state can use the indentation levels of its parent.
		///     When this state exits, the engine returns to the parent.
		/// </summary>
		public IndentState Parent;

		/// <summary>
		///     The indentation of the current line.
		///     This is set when the state is created and will be changed to
		///     <see cref="NextLineIndent"/> when the <see cref="CSharpIndentEngine.newLineChar"/> 
		///     is pushed.
		/// </summary>
		public Indent ThisLineIndent;

		/// <summary>
		///     The indentation of the next line.
		///     This is set when the state is created and can change depending
		///     on the pushed chars.
		/// </summary>
		public Indent NextLineIndent;

		#endregion

		#region Constructors

		/// <summary>
		///     Creates a new indentation state.
		/// </summary>
		/// <param name="engine">
		///     The indentation engine that uses this state.
		/// </param>
		/// <param name="parent">
		///     The parent state, or null if this state doesn't have one ->
		///         e.g. the state represents the global space.
		/// </param>
		protected IndentState(CSharpIndentEngine engine, IndentState parent = null)
		{
			Parent = parent;
			Engine = engine;

			InitializeState();
		}

		/// <summary>
		///     Creates a new indentation state that is a copy of the given
		///     prototype.
		/// </summary>
		/// <param name="prototype">
		///     The prototype state.
		/// </param>
		/// <param name="engine">
		///     The engine of the new state.
		/// </param>
		protected IndentState(IndentState prototype, CSharpIndentEngine engine)
		{
			Engine = engine;
			Parent = prototype.Parent != null ? prototype.Parent.Clone(engine) : null;

			ThisLineIndent = prototype.ThisLineIndent.Clone();
			NextLineIndent = prototype.NextLineIndent.Clone();
		}

		#endregion

		#region IClonable

		object ICloneable.Clone()
		{
			return Clone(Engine);
		}

		public abstract IndentState Clone(CSharpIndentEngine engine);

		#endregion

		#region Methods

		/// <summary>
		///     Initializes the state:
		///       - sets the default indentation levels.
		/// </summary>
		/// <remarks>
		///     Each state can override this method if it needs a different
		///     logic for setting up the default indentations.
		/// </remarks>
		public virtual void InitializeState()
		{
			ThisLineIndent = new Indent(Engine.textEditorOptions);
			NextLineIndent = ThisLineIndent.Clone();
		}

		/// <summary>
		///     Actions performed when this state exits.
		/// </summary>
		public virtual void OnExit()
		{
			if (Parent == null)
				return;
			// if a state exits on the newline character, it has to push
			// it back to its parent (and so on recursively if the parent 
			// state also exits). Otherwise, the parent state wouldn't
			// know that the engine isn't on the same line anymore.
			if (Engine.currentChar == Engine.newLineChar)
			{
				Parent.Push(Engine.newLineChar);
			}
            
			// when a state exits the engine stays on the same line and this
			// state has to adjust the current indent of its parent so that 
			// it's equal to this line indent.
			Parent.ThisLineIndent = ThisLineIndent.Clone();
		}

		/// <summary>
		///     Changes the current state of the <see cref="CSharpIndentEngine"/> using the current
		///     state as the parent for the new one.
		/// </summary>
		/// <typeparam name="T">
		///     The type of the new state. Must be assignable from <see cref="IndentState"/>.
		/// </typeparam>
		public void ChangeState<T>()
			where T : IndentState
		{
			Engine.currentState = IndentStateFactory.Create<T>(Engine.currentState);
		}

		/// <summary>
		///     Exits this state by setting the current state of the
		///     <see cref="CSharpIndentEngine"/> to this state's parent.
		/// </summary>
		public void ExitState()
		{
			OnExit();
			Engine.currentState = Engine.currentState.Parent ?? Engine.currentState;
		}

		/// <summary>
		///     Common logic behind the push method.
		///     Each state can override this method and implement its own logic.
		/// </summary>
		/// <param name="ch">
		///     The current character that's being pushed.
		/// </param>
		public virtual void Push(char ch)
		{
			// replace ThisLineIndent with NextLineIndent if the newLineChar is pushed
			if (ch == Engine.newLineChar)
			{
				ThisLineIndent = NextLineIndent.Clone();
			}
		}

		/// <summary>
		///     When derived, checks if the given sequence of chars form
		///     a valid keyword or variable name, depending on the state.
		/// </summary>
		/// <param name="keyword">
		///     A possible keyword.
		/// </param>
		public virtual void CheckKeyword(string keyword)
		{ }

		#endregion
	}

	#endregion

	#region IndentStateFactory

	/// <summary>
	///     Indentation state factory.
	/// </summary>
	public static class IndentStateFactory
	{
		/// <summary>
		///     Creates a new state.
		/// </summary>
		/// <param name="stateType">
		///     Type of the state. Must be assignable from <see cref="IndentState"/>.
		/// </param>
		/// <param name="engine">
		///     Indentation engine for the state.
		/// </param>
		/// <param name="parent">
		///     Parent state.
		/// </param>
		/// <returns>
		///     A new state of type <paramref name="stateType"/>.
		/// </returns>
		static IndentState Create(Type stateType, CSharpIndentEngine engine, IndentState parent = null)
		{
			return (IndentState)Activator.CreateInstance(stateType, engine, parent);
		}

		/// <summary>
		///     Creates a new state.
		/// </summary>
		/// <typeparam name="T">
		///     Type of the state. Must be assignable from <see cref="IndentState"/>.
		/// </typeparam>
		/// <param name="engine">
		///     Indentation engine for the state.
		/// </param>
		/// <param name="parent">
		///     Parent state.
		/// </param>
		/// <returns>
		///     A new state of type <typeparamref name="T"/>.
		/// </returns>
		public static IndentState Create<T>(CSharpIndentEngine engine, IndentState parent = null)
			where T : IndentState
		{
			return Create(typeof(T), engine, parent);
		}

		/// <summary>
		///     Creates a new state.
		/// </summary>
		/// <typeparam name="T">
		///     Type of the state. Must be assignable from <see cref="IndentState"/>.
		/// </typeparam>
		/// <param name="prototype">
		///     Parent state. Also, the indentation engine of the prototype is
		///     used as the engine for the new state.
		/// </param>
		/// <returns>
		///     A new state of type <typeparamref name="T"/>.
		/// </returns>
		public static IndentState Create<T>(IndentState prototype)
			where T : IndentState
		{
			return Create(typeof(T), prototype.Engine, prototype);
		}

		/// <summary>
		///     The default state, used for the global space.
		/// </summary>
		public static Func<CSharpIndentEngine, IndentState> Default = engine => Create<GlobalBodyState>(engine);
	}

	#endregion

	#region Null state

	/// <summary>
	///     Null state.
	/// </summary>
	/// <remarks>
	///     Doesn't define any transitions to new states.
	/// </remarks>
	public class NullState : IndentState
	{
		public NullState(CSharpIndentEngine engine, IndentState parent = null)
			: base(engine, parent)
		{ }

		public NullState(NullState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{ }

		public override void Push(char ch)
		{ }

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new NullState(this, engine);
		}
	}

	#endregion

	#region Brackets body states

	#region Brackets body base

	/// <summary>
	///     The base for all brackets body states.
	/// </summary>
	/// <remarks>
	///     Represents a block of code between a pair of brackets.
	/// </remarks>
	public abstract class BracketsBodyBaseState : IndentState
	{
		/// <summary>
		///     Defines transitions for all types of open brackets.
		/// </summary>
		public static Dictionary<char, Action<IndentState>> OpenBrackets = 
			new Dictionary<char, Action<IndentState>>
		{ 
			{ '{', state => state.ChangeState<BracesBodyState>() }, 
			{ '(', state => state.ChangeState<ParenthesesBodyState>() }, 
			{ '[', state => state.ChangeState<SquareBracketsBodyState>() },
			// TODO: Since the '<' char is also used as the 'less-than' operator,
			// until the logic for distinguishing this two cases is implemented
			// this state must not define this next transition.
			// FIX: Ignore this bracket completely or maybe just check if we're not in a right hand expression? 
			// { '<', state => state.ChangeState<AngleBracketsBody>() }
		};

		/// <summary>
		///     When derived in a concrete bracket body state, represents
		///     the closed bracket character pair.
		/// </summary>
		public abstract char ClosedBracket { get; }

		/// <summary>
		///     Type of the current block body.
		/// </summary>
		public Body CurrentBody;

		/// <summary>
		///     Type of the next block body.
		///     Same as <see cref="CurrentBody"/> if none of the
		///     <see cref="Body"/> keywords have been read.
		/// </summary>
		public Body NextBody;

		/// <summary>
		///     Type of the current statement.
		/// </summary>
		public Statement CurrentStatement;

		/// <summary>
		///    Contains indent levels of nested if statements.
		/// </summary>
		public Stack<Indent> NestedIfStatementLevels = new Stack<Indent>();

		protected BracketsBodyBaseState(CSharpIndentEngine engine, IndentState parent = null)
			: base(engine, parent)
		{ }

		protected BracketsBodyBaseState(BracketsBodyBaseState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			CurrentBody = prototype.CurrentBody;
			NextBody = prototype.NextBody;
			CurrentStatement = prototype.CurrentStatement;
			NestedIfStatementLevels = new Stack<Indent>(prototype.NestedIfStatementLevels);
		}

		/// <summary>
		///     Extracts the <see cref="CurrentBody"/> from the given state.
		/// </summary>
		/// <returns>
		///     The correct <see cref="Body"/> type for this state.
		/// </returns>
		static Body extractBody(IndentState state)
		{
			if (state != null && state is BracketsBodyBaseState)
			{
				return ((BracketsBodyBaseState)state).NextBody;
			}

			return Body.None;
		}

		public override void Push(char ch)
		{
			base.Push(ch);

			if (ch == '#' && Engine.isLineStart)
			{
				ChangeState<PreProcessorState>();
			}
			else if (ch == '/' && Engine.previousChar == '/')
			{
				ChangeState<LineCommentState>();
			}
			else if (ch == '*' && Engine.previousChar == '/')
			{
				ChangeState<MultiLineCommentState>();
			}
			else if (ch == '"')
			{
				if (Engine.previousChar == '@')
				{
					ChangeState<VerbatimStringState>();
				}
				else
				{
					ChangeState<StringLiteralState>();
				}
			}
			else if (ch == '\'')
			{
				ChangeState<CharacterState>();
			}
			else if (OpenBrackets.ContainsKey(ch))
			{
				OpenBrackets[ch](this);
			}
			else if (ch == ClosedBracket)
			{
				if (Engine.isLineStart)
				{
					if (Parent != null) {
						ThisLineIndent = Parent.ThisLineIndent.Clone();
					} else {
						ThisLineIndent.Reset();
					}
				}

				ExitState();
			}
		}

		public override void InitializeState()
		{
			CurrentBody = BracketsBodyBaseState.extractBody(Parent);
			NextBody = Body.None;
			CurrentStatement = Statement.None;

			AddIndentation(CurrentBody);
		}

		#region Helpers

		/// <summary>
		///     Types of braces bodies.
		/// </summary>
		public enum Body
		{
			None,
			Namespace,
			Class,
			Struct,
			Interface,
			Enum,
			Switch,
			Case,
			Try,
			Catch,
			Finally
		}

		/// <summary>
		///     Types of statements.
		/// </summary>
		public enum Statement
		{
			None,
			If,
			Else,
			Do,
			While,
			For,
			Foreach,
			Lock,
			Using,
			Return
		}

		static readonly Dictionary<string, Body> blocks = new Dictionary<string, Body>
		{
			{ "namespace", Body.Namespace },
			{ "class", Body.Class },
			{ "struct", Body.Struct },
			{ "interface", Body.Interface },
			{ "enum", Body.Enum },
			{ "switch", Body.Switch },
			{ "try", Body.Try },
			{ "catch", Body.Catch },
			{ "finally", Body.Finally },
		};
		
		static readonly Dictionary<string, Statement> statements = new Dictionary<string, Statement>
		{
			{ "if", Statement.If },
			{ "else", Statement.Else },
			{ "do", Statement.Do },
			{ "while", Statement.While },
			{ "for", Statement.For },
			{ "foreach", Statement.Foreach },
			{ "lock", Statement.Lock },
			{ "using", Statement.Using },
			{ "return", Statement.Return },
		};

		readonly string[] caseDefaultKeywords = {
			"case",
			"default"
		};

		readonly string[] classStructKeywords = {
			"class",
			"struct"
		};

		/// <summary>
		///     Checks if the given string is a keyword and sets the
		///     <see cref="NextBody"/> and the <see cref="CurrentStatement"/>
		///     variables appropriately.
		/// </summary>
		/// <param name="keyword">
		///     A possible keyword.
		/// </param>
		public override void CheckKeyword(string keyword)
		{
			if (blocks.ContainsKey(keyword))
			{
				var isKeywordTemplateConstraint = classStructKeywords.Contains(keyword);
				if (!(isKeywordTemplateConstraint && (NextBody == Body.Class || NextBody == Body.Struct || NextBody == Body.Interface)))
				{
					NextBody = blocks[keyword];
				}
			}
			else if (caseDefaultKeywords.Contains(keyword) && CurrentBody == Body.Switch)
			{
				ChangeState<SwitchCaseState>();
			}
			else if (keyword == "where" && Engine.isLineStartBeforeWordToken)
			{
				ThisLineIndent.Push(IndentType.Continuation);
			}
			else if (statements.ContainsKey(keyword))
			{
				Statement previousStatement = CurrentStatement;
				CurrentStatement = statements[keyword];

				// check if the using is a using declaration
				if (CurrentStatement == Statement.Using &&
				   (this is GlobalBodyState || CurrentBody == Body.Namespace))
				{
					return;
				}

				if (ThisLineIndent.Count > 0 && ThisLineIndent.Peek() == IndentType.Continuation)
				{
					// OPTION: CSharpFormattingOptions.AlignEmbeddedIfStatements
					if (Engine.formattingOptions.AlignEmbeddedIfStatements &&
					    previousStatement == Statement.If &&
					    CurrentStatement == Statement.If &&
					    Engine.previousKeyword != "else")
					{
						ThisLineIndent.Pop();

						if (CurrentStatement == Statement.If)
						{
							NestedIfStatementLevels.Push(ThisLineIndent);
						}

						return;
					}

					// OPTION: CSharpFormattingOptions.AlignEmbeddedUsingStatements
					if (Engine.formattingOptions.AlignEmbeddedUsingStatements &&
					    previousStatement == Statement.Using &&
					    CurrentStatement == Statement.Using)
					{
						ThisLineIndent.Pop();
						return;
					}
				}

				// else statement is handled differently
				if (CurrentStatement == Statement.Else)
				{
					if (NestedIfStatementLevels.Count > 0)
					{
						ThisLineIndent = NestedIfStatementLevels.Pop().Clone();
						NextLineIndent = ThisLineIndent.Clone();
					}
					NextLineIndent.Push(IndentType.Continuation);
				}
				else
				{
					// check if the nested statements expression has been broken
					if (previousStatement == Statement.None)
					{
						NestedIfStatementLevels.Clear();
					}

					// only add continuation for 'else' in 'else if' statement.
					if (!(CurrentStatement == Statement.If && previousStatement == Statement.Else))
					{
						NextLineIndent.Push(IndentType.Continuation);
					}

					if (CurrentStatement == Statement.If)
					{
						NestedIfStatementLevels.Push(ThisLineIndent);
					}
				}
			}
		}

		/// <summary>
		///     Pushes a new level of indentation depending on the given
		///     <paramref name="braceStyle"/>.
		/// </summary>
		void AddIndentation(BraceStyle braceStyle)
		{
			switch (braceStyle)
			{
				case BraceStyle.DoNotChange:
				case BraceStyle.EndOfLine:
				case BraceStyle.EndOfLineWithoutSpace:
				case BraceStyle.NextLine:
				case BraceStyle.NextLineShifted:
				case BraceStyle.BannerStyle:
					NextLineIndent.Push(IndentType.Block);
					break;
				case BraceStyle.NextLineShifted2:
					NextLineIndent.Push(IndentType.DoubleBlock);
					break;
			}
		}

		/// <summary>
		///     Pushes a new level of indentation depending on the given
		///     <paramref name="body"/>.
		/// </summary>
		void AddIndentation(Body body)
		{
			switch (body)
			{
				case Body.None:
					if (Engine.formattingOptions.IndentBlocks)
						NextLineIndent.Push(IndentType.Block);
					else
						NextLineIndent.Push(IndentType.Empty);
					break;
				case Body.Namespace:
					if (Engine.formattingOptions.IndentNamespaceBody)
						AddIndentation(Engine.formattingOptions.NamespaceBraceStyle);
					else
						NextLineIndent.Push(IndentType.Empty);
					break;
				case Body.Class:
					if (Engine.formattingOptions.IndentClassBody)
						AddIndentation(Engine.formattingOptions.ClassBraceStyle);
					else
						NextLineIndent.Push(IndentType.Empty);
					break;
				case Body.Struct:
					if (Engine.formattingOptions.IndentStructBody)
						AddIndentation(Engine.formattingOptions.StructBraceStyle);
					else
						NextLineIndent.Push(IndentType.Empty);
					break;
				case Body.Interface:
					if (Engine.formattingOptions.IndentInterfaceBody)
						AddIndentation(Engine.formattingOptions.InterfaceBraceStyle);
					else
						NextLineIndent.Push(IndentType.Empty);
					break;
				case Body.Enum:
					if (Engine.formattingOptions.IndentEnumBody)
						AddIndentation(Engine.formattingOptions.EnumBraceStyle);
					else
						NextLineIndent.Push(IndentType.Empty);
					break;
				case Body.Switch:
					if (Engine.formattingOptions.IndentSwitchBody)
						AddIndentation(Engine.formattingOptions.StatementBraceStyle);
					else
						NextLineIndent.Push(IndentType.Empty);
					break;
				case Body.Try:
				case Body.Catch:
				case Body.Finally:
					AddIndentation(Engine.formattingOptions.StatementBraceStyle);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion
	}

	#endregion

	#region Global body state

	/// <summary>
	///     Global body state.
	/// </summary>
	/// <remarks>
	///     Represents the global space of the program.
	/// </remarks>
	public class GlobalBodyState : BracketsBodyBaseState
	{
		public override char ClosedBracket
		{
			get { return '\0'; }
		}

		public GlobalBodyState(CSharpIndentEngine engine, IndentState parent = null)
			: base(engine, parent)
		{ }

		public GlobalBodyState(GlobalBodyState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{ }

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new GlobalBodyState(this, engine);
		}

		public override void InitializeState()
		{
			ThisLineIndent = new Indent(Engine.textEditorOptions);
			NextLineIndent = ThisLineIndent.Clone();
		}
	}

	#endregion

	#region Braces body state

	/// <summary>
	///     Braces body state.
	/// </summary>
	/// <remarks>
	///     Represents a block of code between { and }.
	/// </remarks>
	public class BracesBodyState : BracketsBodyBaseState
	{
		/// <summary>
		///     True if the engine is on the right side of the equal operator '='.
		/// </summary>
		public bool IsRightHandExpression;

		/// <summary>
		///     True if the '=' char has been pushed and it's not
		///     a part of a relational operator (&gt;=, &lt;=, !=, ==).
		/// </summary>
		public bool IsEqualCharPushed;

		public override char ClosedBracket
		{
			get { return '}'; }
		}

		public BracesBodyState(CSharpIndentEngine engine, IndentState parent = null)
			: base(engine, parent)
		{ }

		public BracesBodyState(BracesBodyState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			IsRightHandExpression = prototype.IsRightHandExpression;
			IsEqualCharPushed = prototype.IsEqualCharPushed;
		}

		public override void Push(char ch)
		{
			// handle IsRightHandExpression property
			if (IsEqualCharPushed && ch != '=' && ch != '>')
			{
				IsRightHandExpression = true;
				NextLineIndent.ExtraSpaces = Math.Max(0, Engine.column - NextLineIndent.CurIndent);
			}
			IsEqualCharPushed = false;

			if (ch == ';')
			{
				while (NextLineIndent.Count > 0 && NextLineIndent.Peek() == IndentType.Continuation)
				{
					NextLineIndent.Pop();
				}
				NextLineIndent.ExtraSpaces = 0;
				IsRightHandExpression = false;

				if (CurrentStatement == Statement.None)
				{
					NestedIfStatementLevels.Clear();
				}
				CurrentStatement = Statement.None;
			}
			else if (ch == ',' && IsRightHandExpression)
			{
				NextLineIndent.ExtraSpaces = 0;
				IsRightHandExpression = false;
			}
			else if (ch == '=' && !IsRightHandExpression && !new[] { '=', '<', '>', '!' }.Contains(Engine.previousChar))
			{
				IsEqualCharPushed = true;
			}
			else if (ch == '.' && IsRightHandExpression)
			{
				if (Engine.previousChar == ')' && ThisLineIndent.ExtraSpaces > 0)
				{
					ThisLineIndent.ExtraSpaces = 0;
					ThisLineIndent.Push(IndentType.Continuation);
					// TODO: NextLineIndent = ThisLineIndent ???
				}
				else
				{
					// NextLineIndent.ExtraSpaces = Math.Max(0, Engine.column - NextLineIndent.CurIndent - 1);
				}
			}
			else if (ch == Engine.newLineChar && NextLineIndent.ExtraSpaces > 0 &&
					(Engine.previousChar == '=' || Engine.previousChar == '.'))
			{
				// the last significant pushed char was '=' or '.' and we added
				// extra spaces to align the next line, but the newline char was
				// pushed afterwards so it's better to replace the extra spaces
				// with one continuation indent.
				NextLineIndent.ExtraSpaces = 0;
				NextLineIndent.Push(IndentType.Continuation);
			}
			else if (ch == ':' && Engine.isLineStart && !IsRightHandExpression)
			{
				// try to capture ': base(...)', ': this(...)' and inherit statements when they are on a new line
				ThisLineIndent.Push(IndentType.Continuation);
			}

			base.Push(ch);
		}

		public override void InitializeState()
		{
			// remove all continuations and extra spaces from the previous state
			Parent.NextLineIndent.ExtraSpaces = 0;
			if (Parent.NextLineIndent.Count > 0 && Parent.NextLineIndent.Peek() == IndentType.Continuation)
			{
				Parent.NextLineIndent.Pop();
			}

			if (Engine.isLineStart)
			{
				Parent.ThisLineIndent = Parent.NextLineIndent.Clone();
			}

			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = Parent.NextLineIndent.Clone();

			base.InitializeState();
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new BracesBodyState(this, engine);
		}

		public override void OnExit()
		{
			// remove continuations and extra-spaces from the parent state 
			// if this block was a part of some statement
			var parent = Parent as BracketsBodyBaseState;
			if (parent != null)
			{
				if (parent.CurrentStatement == Statement.None)
				{
					parent.NestedIfStatementLevels.Clear();
				}
				else
				{
					parent.CurrentStatement = Statement.None;
					parent.NextLineIndent.ExtraSpaces = 0;
					while (parent.NextLineIndent.Count > 0 && parent.NextLineIndent.Peek() == IndentType.Continuation)
					{
						parent.NextLineIndent.Pop();
					}
				}
			}

			base.OnExit();
		}
	}

	#endregion

	#region Switch-case body state

	/// <summary>
	///     Switch-case statement state.
	/// </summary>
	/// <remarks>
	///     Represents the block of code in one switch case (including default).
	/// </remarks>
	public class SwitchCaseState : BracesBodyState
	{
		public override char ClosedBracket
		{
			get { return '}'; }
		}
		public SwitchCaseState(CSharpIndentEngine engine, IndentState parent = null)
			: base(engine, parent)
		{ }

		public SwitchCaseState(SwitchCaseState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{ }

		public override void Push(char ch)
		{
			if (ch == ClosedBracket)
			{
				ExitState();
			}

			base.Push(ch);
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();

			// remove all continuations and extra spaces from this line indent
			ThisLineIndent.ExtraSpaces = 0;
			if (ThisLineIndent.Count > 0 && ThisLineIndent.Peek() == IndentType.Continuation)
			{
				ThisLineIndent.Pop();
			}

			NextLineIndent = ThisLineIndent.Clone();
			if (Engine.formattingOptions.IndentCaseBody)
				NextLineIndent.Push(IndentType.Block);
			else
				NextLineIndent.Push(IndentType.Empty);
		}

		static readonly string[] caseDefaultKeywords = {
			"case",
			"default"
		};

		static readonly string[] breakContinueReturnKeywords = {
			"break",
			"continue",
			"return"
		};

		public override void CheckKeyword(string keyword)
		{
			if (caseDefaultKeywords.Contains(keyword))
			{
				ExitState();
				ChangeState<SwitchCaseState>();
			}
			else if (breakContinueReturnKeywords.Contains(keyword))
			{
				// OPTION: Engine.formattingOptions.IndentBreakStatements
				if (!Engine.formattingOptions.IndentBreakStatements)
				{
					ThisLineIndent = Parent.ThisLineIndent.Clone();
				}
			}
			else
			{
				base.CheckKeyword(keyword);
			}
		}

		public override void OnExit()
		{
			// override the base.OnExit() logic
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new SwitchCaseState(this, engine);
		}
	}

	#endregion

	#region Parentheses body state

	/// <summary>
	///     Parentheses body state.
	/// </summary>
	/// <remarks>
	///     Represents a block of code between ( and ).
	/// </remarks>
	public class ParenthesesBodyState : BracketsBodyBaseState
	{
		/// <summary>
		///     True if any char has been pushed.
		/// </summary>
		public bool IsSomethingPushed;

		public override char ClosedBracket
		{
			get { return ')'; }
		}

		public ParenthesesBodyState(CSharpIndentEngine engine, IndentState parent = null)
			: base(engine, parent)
		{ }

		public ParenthesesBodyState(ParenthesesBodyState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			IsSomethingPushed = prototype.IsSomethingPushed;
		}

		public override void Push(char ch)
		{
			// OPTION: CSharpFormattingOptions.AlignToFirstMethodCallArgument
			if (ch != Engine.newLineChar && !IsSomethingPushed && Engine.formattingOptions.AlignToFirstMethodCallArgument)
			{
				// align the next line at the beginning of the open bracket
				NextLineIndent.Pop();
				NextLineIndent.ExtraSpaces = Math.Max(0, Engine.column - NextLineIndent.CurIndent - 1);
			}

			base.Push(ch);
			IsSomethingPushed = true;
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = ThisLineIndent.Clone();
			NextLineIndent.Push(IndentType.Block);
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new ParenthesesBodyState(this, engine);
		}
	}

	#endregion

	#region Square brackets body state

	/// <summary>
	///     Square brackets body state.
	/// </summary>
	/// <remarks>
	///     Represents a block of code between [ and ].
	/// </remarks>
	public class SquareBracketsBodyState : BracketsBodyBaseState
	{
		/// <summary>
		///     True if any char has been pushed.
		/// </summary>
		public bool IsSomethingPushed;

		public override char ClosedBracket
		{
			get { return ']'; }
		}

		public SquareBracketsBodyState(CSharpIndentEngine engine, IndentState parent = null)
			: base(engine, parent)
		{ }

		public SquareBracketsBodyState(SquareBracketsBodyState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			IsSomethingPushed = prototype.IsSomethingPushed;
		}

		public override void Push(char ch)
		{
			// OPTION: CSharpFormattingOptions.AlignToFirstIndexerArgument
			if (ch != Engine.newLineChar && !IsSomethingPushed && Engine.formattingOptions.AlignToFirstIndexerArgument)
			{
				// align the next line at the beginning of the open bracket
				NextLineIndent.Pop();
				NextLineIndent.ExtraSpaces = Math.Max(0, Engine.column - NextLineIndent.CurIndent - 1);
			}

			base.Push(ch);
			IsSomethingPushed = true;
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = ThisLineIndent.Clone();
			NextLineIndent.Push(IndentType.Block);
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new SquareBracketsBodyState(this, engine);
		}
	}

	#endregion

	#region Angle brackets body state

	/// <summary>
	///     Angle brackets body state.
	/// </summary>
	/// <remarks>
	///     Represents a block of code between < and >.
	/// </remarks>
	public class AngleBracketsBodyState : BracketsBodyBaseState
	{
		/// <summary>
		///     True if any char has been pushed.
		/// </summary>
		public bool IsSomethingPushed;

		public override char ClosedBracket
		{
			get { return '>'; }
		}

		public AngleBracketsBodyState(CSharpIndentEngine engine, IndentState parent = null)
			: base(engine, parent)
		{ }

		public AngleBracketsBodyState(AngleBracketsBodyState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			IsSomethingPushed = prototype.IsSomethingPushed;
		}

		public override void Push(char ch)
		{
			if (ch != Engine.newLineChar && !IsSomethingPushed)
			{
				// align the next line at the beginning of the open bracket
				NextLineIndent.Pop();
				NextLineIndent.ExtraSpaces = Math.Max(0, Engine.column - NextLineIndent.CurIndent - 1);
			}

			base.Push(ch);
			IsSomethingPushed = true;
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = ThisLineIndent.Clone();
			NextLineIndent.Push(IndentType.Block);
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new AngleBracketsBodyState(this, engine);
		}
	}

	#endregion

	#endregion

	#region PreProcessor state

	/// <summary>
	///     PreProcessor directive state.
	/// </summary>
	/// <remarks>
	///     Activated when the '#' char is pushed and the 
	///     <see cref="CSharpIndentEngine.isLineStart"/> is true.
	/// </remarks>
	public class PreProcessorState : IndentState
	{
		/// <summary>
		///     The type of the preprocessor directive.
		/// </summary>
		public PreProcessorDirective DirectiveType;

		/// <summary>
		///     If <see cref="DirectiveType"/> is set (not equal to 'None'), this
		///     stores the expression of the directive.
		/// </summary>
		public StringBuilder DirectiveStatement;

		public PreProcessorState(CSharpIndentEngine engine, IndentState parent = null)
			: base(engine, parent)
		{
			DirectiveType = PreProcessorDirective.None;
			DirectiveStatement = new StringBuilder();
		}

		public PreProcessorState(PreProcessorState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			DirectiveType = prototype.DirectiveType;
			DirectiveStatement = new StringBuilder(prototype.DirectiveStatement.ToString());
		}

		public override void Push(char ch)
		{
			// HACK: if this change would be left for the CheckKeyword method, we will lose
			//       it if the next pushed char is newLineChar since ThisLineIndent will be 
			//       immediately replaced with NextLineIndent
			if (Engine.wordToken.Length == "endregion".Length && 
			    Engine.wordToken.ToString() == "endregion")
			{
				ThisLineIndent = Parent.NextLineIndent.Clone();
			}

			base.Push(ch);

			if (DirectiveType != PreProcessorDirective.None)
			{
				DirectiveStatement.Append(ch);
			}

			if (ch == Engine.newLineChar)
			{
				ExitState();

				switch (DirectiveType)
				{
					case PreProcessorDirective.If:
						if (!Engine.ifDirectiveEvalResult)
						{
							Engine.ifDirectiveEvalResult = eval(DirectiveStatement.ToString());
							if (Engine.ifDirectiveEvalResult)
							{
								// the if/elif directive is true -> continue with the previous state
							}
							else
							{
								// the if/elif directive is false -> change to a state that will 
								// ignore any chars until #endif or #elif
								ChangeState<PreProcessorCommentState>();
							}
						}
						else
						{
							// one of the if/elif directives in this block was true -> 
							// change to a state that will ignore any chars until #endif
							ChangeState<PreProcessorCommentState>();
						}
						break;
					case PreProcessorDirective.Else:
						if (Engine.ifDirectiveEvalResult)
						{
							// some if/elif directive was true -> change to a state that will 
							// ignore any chars until #endif
							ChangeState<PreProcessorCommentState>();
						}
						else
						{
							// none if/elif directives were true -> continue with the previous state
						}
						break;
					case PreProcessorDirective.Define:
						var defineSymbol = DirectiveStatement.ToString().Trim();
						if (!Engine.conditionalSymbols.Contains(defineSymbol))
						{
							Engine.conditionalSymbols.Add(defineSymbol);
						}
						break;
					case PreProcessorDirective.Undef:
						var undefineSymbol = DirectiveStatement.ToString().Trim();
						if (Engine.conditionalSymbols.Contains(undefineSymbol))
						{
							Engine.conditionalSymbols.Remove(undefineSymbol);
						}
						break;
					case PreProcessorDirective.Endif:
						// marks the end of this block
						Engine.ifDirectiveEvalResult = false;
						break;
					case PreProcessorDirective.Region:
					case PreProcessorDirective.Pragma:
					case PreProcessorDirective.Warning:
					case PreProcessorDirective.Error:
					case PreProcessorDirective.Line:
						// continue with the previous state
						break;
				}
			}
		}

		public override void InitializeState()
		{
			// OPTION: IndentPreprocessorStatements
			if (Engine.formattingOptions.IndentPreprocessorDirectives)
			{
				ThisLineIndent = Parent.ThisLineIndent.Clone();
			}
			else
			{
				ThisLineIndent = new Indent(Engine.textEditorOptions);
			}

			NextLineIndent = Parent.NextLineIndent.Clone();
		}

		static readonly Dictionary<string, PreProcessorDirective> preProcessorDirectives = new Dictionary<string, PreProcessorDirective>
		{
			{ "if", PreProcessorDirective.If },
			{ "elif", PreProcessorDirective.If },
			{ "else", PreProcessorDirective.Else },
			{ "endif", PreProcessorDirective.Endif },
			{ "region", PreProcessorDirective.Region },
			{ "endregion", PreProcessorDirective.Region },
			{ "pragma", PreProcessorDirective.Pragma },
			{ "warning", PreProcessorDirective.Warning },
			{ "error", PreProcessorDirective.Error },
			{ "line", PreProcessorDirective.Line },
			{ "define", PreProcessorDirective.Define },
			{ "undef", PreProcessorDirective.Undef }
		};

		public override void CheckKeyword(string keyword)
		{
			// check if the directive type has already been set
			if (DirectiveType != PreProcessorDirective.None)
			{
				return;
			}

			if (preProcessorDirectives.ContainsKey(keyword))
			{
				DirectiveType = preProcessorDirectives[keyword];

				// adjust the indentation for the region/endregion directives
				if (DirectiveType == PreProcessorDirective.Region)
				{
					ThisLineIndent = Parent.NextLineIndent.Clone();
				}
			}
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new PreProcessorState(this, engine);
		}

		/// <summary>
		///     Types of preprocessor directives.
		/// </summary>
		public enum PreProcessorDirective
		{
			None,
			If,
			// Elif, // use If instead
			Else,
			Endif,
			Region,
			// EndRegion, // use Region instead
			Pragma,
			Warning, 
			Error,
			Line,
			Define,
			Undef
		}

		#region Pre processor evaluation (from cs-tokenizer.cs)

		static bool is_identifier_start_character(int c)
		{
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || Char.IsLetter((char)c);
		}

		static bool is_identifier_part_character(char c)
		{
			if (c >= 'a' && c <= 'z')
				return true;

			if (c >= 'A' && c <= 'Z')
				return true;

			if (c == '_' || (c >= '0' && c <= '9'))
				return true;

			if (c < 0x80)
				return false;

			return Char.IsLetter(c) || Char.GetUnicodeCategory(c) == UnicodeCategory.ConnectorPunctuation;
		}

		bool eval_val(string s)
		{
			if (s == "true")
				return true;
			if (s == "false")
				return false;

			return Engine.conditionalSymbols != null && Engine.conditionalSymbols.Contains(s);
		}

		bool pp_primary(ref string s)
		{
			s = s.Trim();
			int len = s.Length;

			if (len > 0)
			{
				char c = s[0];

				if (c == '(')
				{
					s = s.Substring(1);
					bool val = pp_expr(ref s, false);
					if (s.Length > 0 && s[0] == ')')
					{
						s = s.Substring(1);
						return val;
					}
					return false;
				}

				if (is_identifier_start_character(c))
				{
					int j = 1;

					while (j < len)
					{
						c = s[j];

						if (is_identifier_part_character(c))
						{
							j++;
							continue;
						}
						bool v = eval_val(s.Substring(0, j));
						s = s.Substring(j);
						return v;
					}
					bool vv = eval_val(s);
					s = "";
					return vv;
				}
			}
			return false;
		}

		bool pp_unary(ref string s)
		{
			s = s.Trim();
			int len = s.Length;

			if (len > 0)
			{
				if (s[0] == '!')
				{
					if (len > 1 && s[1] == '=')
					{
						return false;
					}
					s = s.Substring(1);
					return !pp_primary(ref s);
				}
				else
					return pp_primary(ref s);
			}
			else
			{
				return false;
			}
		}

		bool pp_eq(ref string s)
		{
			bool va = pp_unary(ref s);

			s = s.Trim();
			int len = s.Length;
			if (len > 0)
			{
				if (s[0] == '=')
				{
					if (len > 2 && s[1] == '=')
					{
						s = s.Substring(2);
						return va == pp_unary(ref s);
					}
					else
					{
						return false;
					}
				}
				else if (s[0] == '!' && len > 1 && s[1] == '=')
				{
					s = s.Substring(2);

					return va != pp_unary(ref s);

				}
			}

			return va;

		}

		bool pp_and(ref string s)
		{
			bool va = pp_eq(ref s);

			s = s.Trim();
			int len = s.Length;
			if (len > 0)
			{
				if (s[0] == '&')
				{
					if (len > 2 && s[1] == '&')
					{
						s = s.Substring(2);
						return (va & pp_and(ref s));
					}
					else
					{
						return false;
					}
				}
			}
			return va;
		}

		//
		// Evaluates an expression for `#if' or `#elif'
		//
		bool pp_expr(ref string s, bool isTerm)
		{
			bool va = pp_and(ref s);
			s = s.Trim();
			int len = s.Length;
			if (len > 0)
			{
				char c = s[0];

				if (c == '|')
				{
					if (len > 2 && s[1] == '|')
					{
						s = s.Substring(2);
						return va | pp_expr(ref s, isTerm);
					}
					else
					{

						return false;
					}
				}
				if (isTerm)
				{
					return false;
				}
			}

			return va;
		}

		bool eval(string s)
		{
			bool v = pp_expr(ref s, true);
			s = s.Trim();
			if (s.Length != 0)
			{
				return false;
			}

			return v;
		}

		#endregion
	}

	#endregion

	#region PreProcessorComment state

	/// <summary>
	///     PreProcessor comment state.
	/// </summary>
	/// <remarks>
	///     Activates when the #if or #elif directive is false and ignores
	///     all pushed chars until the next '#'.
	/// </remarks>
	public class PreProcessorCommentState : IndentState
	{
		public PreProcessorCommentState(CSharpIndentEngine engine, IndentState parent = null)
			: base(engine, parent)
		{ }

		public PreProcessorCommentState(PreProcessorCommentState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{ }

		public override void Push(char ch)
		{
			base.Push(ch);

			if (ch == '#' && Engine.isLineStart)
			{
				// TODO: Return back only on #if/#elif/#else/#endif
				// Ignore any of the other directives (especially #define/#undef)
				ExitState();
				ChangeState<PreProcessorState>();
			}
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.NextLineIndent.Clone();
			NextLineIndent = ThisLineIndent.Clone();
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new PreProcessorCommentState(this, engine);
		}
	}

	#endregion

	#region LineComment state

	/// <summary>
	///     Single-line comment state.
	/// </summary>
	public class LineCommentState : IndentState
	{
		/// <summary>
		///     It's possible that this should be the DocComment state:
		///         check if the first next pushed char is equal to '/'.
		/// </summary>
		public bool CheckForDocComment = true;

		public LineCommentState(CSharpIndentEngine engine, IndentState parent = null)
			: base(engine, parent)
		{
			if (engine.formattingOptions.KeepCommentsAtFirstColumn && engine.column == 2)
				ThisLineIndent.Reset();
		}

		public LineCommentState(LineCommentState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			CheckForDocComment = prototype.CheckForDocComment;
		}

		public override void Push(char ch)
		{
			base.Push(ch);

			if (ch == Engine.newLineChar)
			{
				ExitState();
			}
			else if (ch == '/' && CheckForDocComment)
			{
				// wrong state, should be DocComment.
				ExitState();
				ChangeState<DocCommentState>();
			}

			CheckForDocComment = false;
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = Parent.NextLineIndent.Clone();
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new LineCommentState(this, engine);
		}
	}

	#endregion

	#region DocComment state

	/// <summary>
	///     XML documentation comment state.
	/// </summary>
	public class DocCommentState : IndentState
	{
		public DocCommentState(CSharpIndentEngine engine, IndentState parent = null)
			: base(engine, parent)
		{ }

		public DocCommentState(DocCommentState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{ }

		public override void Push(char ch)
		{
			base.Push(ch);

			if (ch == Engine.newLineChar)
			{
				ExitState();
			}
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = Parent.NextLineIndent.Clone();
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new DocCommentState(this, engine);
		}
	}

	#endregion

	#region MultiLineComment state

	/// <summary>
	///     Multi-line comment state.
	/// </summary>
	public class MultiLineCommentState : IndentState
	{
		/// <summary>
		///     True if any char has been pushed to this state.
		/// </summary>
		/// <remarks>
		///     Needed to resolve an issue when the first pushed char is '/'.
		///     The state would falsely exit on this sequence of chars '/*/',
		///     since it only checks if the last two chars are '/' and '*'.
		/// </remarks>
		public bool IsAnyCharPushed;

		public MultiLineCommentState(CSharpIndentEngine engine, IndentState parent = null)
			: base(engine, parent)
		{ }

		public MultiLineCommentState(MultiLineCommentState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			IsAnyCharPushed = prototype.IsAnyCharPushed;
		}

		public override void Push(char ch)
		{
			base.Push(ch);

			if (ch == '/' && Engine.previousChar == '*' && IsAnyCharPushed)
			{
				ExitState();
			}

			IsAnyCharPushed = true;
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = ThisLineIndent.Clone();
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new MultiLineCommentState(this, engine);
		}
	}

	#endregion

	#region StringLiteral state

	/// <summary>
	///     StringLiteral state.
	/// </summary>
	public class StringLiteralState : IndentState
	{
		/// <summary>
		///     True if the next char is escaped with '\'.
		/// </summary>
		public bool IsEscaped;

		public StringLiteralState(CSharpIndentEngine engine, IndentState parent = null)
			: base(engine, parent)
		{ }

		public StringLiteralState(StringLiteralState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			IsEscaped = prototype.IsEscaped;
		}

		public override void Push(char ch)
		{
			base.Push(ch);

			if (ch == Engine.newLineChar)
			{
				ExitState();
			}
			else if (!IsEscaped && ch == '"')
			{
				ExitState();
			}
            
			IsEscaped = ch == '\\' && !IsEscaped;
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = Parent.NextLineIndent.Clone();
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new StringLiteralState(this, engine);
		}
	}

	#endregion

	#region Verbatim string state

	/// <summary>
	///     Verbatim string state.
	/// </summary>
	public class VerbatimStringState : IndentState
	{
		/// <summary>
		///     True if there is an odd number of '"' in a row.
		/// </summary>
		public bool IsEscaped;

		public VerbatimStringState(CSharpIndentEngine engine, IndentState parent = null)
			: base(engine, parent)
		{ }

		public VerbatimStringState(VerbatimStringState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			IsEscaped = prototype.IsEscaped;
		}

		public override void Push(char ch)
		{
			base.Push(ch);

			if (IsEscaped && ch != '"')
			{
				ExitState();
				// the char has been pushed to the wrong state, push it back
				Engine.currentState.Push(ch);
			}

			IsEscaped = ch == '"' && !IsEscaped;
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = new Indent(Engine.textEditorOptions);
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new VerbatimStringState(this, engine);
		}
	}

	#endregion

	#region Character state

	/// <summary>
	///     Character state.
	/// </summary>
	public class CharacterState : IndentState
	{
		/// <summary>
		///     True if the next char is escaped with '\'.
		/// </summary>
		public bool IsEscaped;

		public CharacterState(CSharpIndentEngine engine, IndentState parent = null)
			: base(engine, parent)
		{ }

		public CharacterState(CharacterState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			IsEscaped = prototype.IsEscaped;
		}

		public override void Push(char ch)
		{
			base.Push(ch);

			if (ch == Engine.newLineChar)
			{
				ExitState();
			}
			else if (!IsEscaped && ch == '\'')
			{
				ExitState();
			}

			IsEscaped = ch == '\\' && !IsEscaped;
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = Parent.NextLineIndent.Clone();
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new CharacterState(this, engine);
		}
	}

	#endregion
}
