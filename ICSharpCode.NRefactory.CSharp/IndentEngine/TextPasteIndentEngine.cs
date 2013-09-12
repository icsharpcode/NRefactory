//
// TextPasteIndentEngine.cs
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
using ICSharpCode.NRefactory.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	///     Represents a decorator of an IStateMachineIndentEngine instance
	///     that provides logic for text paste events.
	/// </summary>
	public class TextPasteIndentEngine : IStateMachineIndentEngine, ITextPasteHandler
	{
		#region Properties

		/// <summary>
		///     An instance of IStateMachineIndentEngine which handles
		///     the indentation logic.
		/// </summary>
		internal readonly IStateMachineIndentEngine engine;

		/// <summary>
		///     Text editor options.
		/// </summary>
		internal readonly TextEditorOptions textEditorOptions;

		/// <summary>
		///     C# formatting options.
		/// </summary>
		internal readonly CSharpFormattingOptions formattingOptions;

		#endregion

		#region Constructors

		/// <summary>
		///     Creates a new TextPasteIndentEngine instance.
		/// </summary>
		/// <param name="decoratedEngine">
		///     An instance of <see cref="IStateMachineIndentEngine"/> to which the
		///     logic for indentation will be delegated.
		/// </param>
		/// <param name="textEditorOptions">
		///    Text editor options for indentation.
		/// </param>
		/// <param name="formattingOptions">
		///     C# formatting options.
		/// </param>
		public TextPasteIndentEngine(IStateMachineIndentEngine decoratedEngine, TextEditorOptions textEditorOptions, CSharpFormattingOptions formattingOptions)
		{
			this.engine = decoratedEngine;
			this.textEditorOptions = textEditorOptions;
			this.formattingOptions = formattingOptions;
		}

		#endregion

		#region ITextPasteHandler

		/// <inheritdoc />
		string ITextPasteHandler.FormatPlainText(int offset, string text, byte[] copyData)
		{
			if (copyData != null && copyData.Length == 1)
			{
				var strategy = TextPasteUtils.Strategies[(PasteStrategy)copyData[0]];
				text = strategy.Decode(text);
			}

			var result = new StringBuilder();

			engine.Update(offset);
			if (engine.IsInsideStringLiteral)
			{
				result.Append(TextPasteUtils.StringLiteralStrategy.Encode(text));
			}
			else if (engine.IsInsideVerbatimString)
			{
				result.Append(TextPasteUtils.VerbatimStringStrategy.Encode(text));
			}
			else
			{
				var clone = new CSharpIndentEngine(new StringBuilderDocument(engine.Document.Text), textEditorOptions, formattingOptions)
				{
					EnableCustomIndentLevels = engine.EnableCustomIndentLevels
				};

				var insertedTextEndAnchor = clone.Document.CreateAnchor(offset);
				insertedTextEndAnchor.MovementType = AnchorMovementType.AfterInsertion;

				clone.Document.Insert(offset, text);

				var line = clone.Document.GetLineByOffset(offset);
				if (insertedTextEndAnchor.Offset > line.EndOffset)
				{
					// first line
					result.Append(calculateLineText(clone, line, offset, line.EndOffset, useDeltaIndent: true) + textEditorOptions.EolMarker);

					// central lines
					for (line = line.NextLine; line.EndOffset < insertedTextEndAnchor.Offset; line = line.NextLine)
					{
						result.Append(calculateLineText(clone, line, line.Offset, line.EndOffset) + textEditorOptions.EolMarker);
					}

					// last line
					result.Append(calculateLineText(clone, line, line.Offset, insertedTextEndAnchor.Offset, useDeltaIndent: true));
				}
				else
				{
					// single line
					result.Append(calculateLineText(clone, line, offset, insertedTextEndAnchor.Offset, useDeltaIndent: true));
				}
			}

			return result.ToString();
		}

		/// <summary>
		///     Uses the given engine to calculate the indent level missing from the
		///     correct indentation of the given instance of IDocumentLine.
		/// </summary>
		/// <param name="engine">
		///     An instance of CSharpIndentEngine.
		/// </param>
		/// <param name="line">
		///	    Line in the engine's document for which the delta indent is calculated.
		/// </param>
		/// <returns>
		///     The delta indent missing from the correct indentation of the given line.
		/// </returns>
		internal string calculateDeltaIndent(CSharpIndentEngine engine, IDocumentLine line)
		{
			if (engine == null)
			{
				return string.Empty;
			}

			engine.Update(line.EndOffset);

			var deltaIndent = new Indent(textEditorOptions);
			var correctIndent = engine.currentState.ThisLineIndent;
			var currentIndent = Indent.ConvertFrom(
				string.Concat(engine.Document.GetText(line).TakeWhile(c => char.IsWhiteSpace(c))), 
				engine.currentState.ThisLineIndent, 
				textEditorOptions);

			if (currentIndent.CurIndent <= correctIndent.CurIndent)
			{
				while (currentIndent.CurIndent + textEditorOptions.ContinuationIndent <= correctIndent.CurIndent)
				{
					currentIndent.Push(IndentType.Continuation);
					deltaIndent.Push(IndentType.Continuation);
				}

				deltaIndent.ExtraSpaces = correctIndent.ExtraSpaces - currentIndent.ExtraSpaces;
			}
			else
			{
				// TODO: too much indent already on this line, try to delete it with '\b'?
			}

			return deltaIndent.IndentString;
		}

		/// <summary>
		///     Uses the given engine to calculate the correct string representation
		///     of a segment in the given instance of IDocumentLine.
		/// </summary>
		/// <param name="engine">
		///     An instance of IStateMachineIndentEngine.
		/// </param>
		/// <param name="line">
		///     Line in the engine's document that contains the given segment.
		/// </param>
		/// <param name="start">
		///     Segment's starting offset.
		///     Should be less or equal than end and between [line.Offset, line.EndOffset].
		/// </param>
		/// <param name="end">
		///     Segment's ending offset.
		///     Should be greater or equal than start and between [line.Offset, line.EndOffset].
		/// </param>
		/// <param name="useDeltaIndent">
		///     If this is true, the current indentation on this line's beginning will be used to
		///     calculate the delta indent level, which will become the indentation of the segment.
		///     <see cref="calculateDeltaIndent"/>
		/// </param>
		/// <returns>
		///     The correct string representation of the segment on the given line.
		/// </returns>
		internal string calculateLineText(IStateMachineIndentEngine engine, IDocumentLine line, int start, int end, bool useDeltaIndent = false)
		{
			if (!(start <= end && (start >= line.Offset && start <= line.EndOffset) && (end >= line.Offset && end <= line.EndOffset)))
			{
				throw new ArgumentOutOfRangeException("start <= end && start, end € [line.Offset, line.EndOffset]");
			}

			// OPTION: CSharpFormattingOptions.EmptyLineFormatting
			if (line.Length == 0 && formattingOptions.EmptyLineFormatting == EmptyLineFormatting.DoNotIndent)
			{
				return string.Empty;
			}

			var text = engine.Document.GetText(start, end - start);

			engine.Update(start);
			if (!(engine.IsInsideMultiLineComment || engine.IsInsideVerbatimString || engine.IsInsidePreprocessorComment))
			{
				engine.Update(line.EndOffset);

				if (!useDeltaIndent)
				{
					return engine.ThisLineIndent + text.TrimStart(' ', '\t');
				}

				return calculateDeltaIndent(engine as CSharpIndentEngine, line) + text;
			}

			return text;
		}

		/// <inheritdoc />
		byte[] ITextPasteHandler.GetCopyData(ISegment segment)
		{
			engine.Update(segment.Offset);

			if (engine.IsInsideStringLiteral)
			{
				return new[] { (byte)PasteStrategy.StringLiteral };
			}
			else if (engine.IsInsideVerbatimString)
			{
				return new[] { (byte)PasteStrategy.VerbatimString };
			}

			return null;
		}

		#endregion

		#region IDocumentIndentEngine

		/// <inheritdoc />
		public IDocument Document
		{
			get { return engine.Document; }
		}

		/// <inheritdoc />
		public string ThisLineIndent
		{
			get { return engine.ThisLineIndent; }
		}

		/// <inheritdoc />
		public string NextLineIndent
		{
			get { return engine.NextLineIndent; }
		}

		/// <inheritdoc />
		public string CurrentIndent
		{
			get { return engine.CurrentIndent; }
		}

		/// <inheritdoc />
		public bool NeedsReindent
		{
			get { return engine.NeedsReindent; }
		}

		/// <inheritdoc />
		public int Offset
		{
			get { return engine.Offset; }
		}

		/// <inheritdoc />
		public TextLocation Location
		{
			get { return engine.Location; }
		}

		/// <inheritdoc />
		public bool EnableCustomIndentLevels 
		{
			get { return engine.EnableCustomIndentLevels; }
			set { engine.EnableCustomIndentLevels = value; } 
		}

		/// <inheritdoc />
		public void Push(char ch)
		{
			engine.Push(ch);
		}

		/// <inheritdoc />
		public void Reset()
		{
			engine.Reset();
		}

		/// <inheritdoc />
		public void Update(int offset)
		{
			engine.Update(offset);
		}

		#endregion

		#region IStateMachineIndentEngine

		public bool IsInsidePreprocessorDirective
		{
			get { return engine.IsInsidePreprocessorDirective; }
		}

		public bool IsInsidePreprocessorComment
		{
			get { return engine.IsInsidePreprocessorComment; }
		}

		public bool IsInsideStringLiteral
		{
			get { return engine.IsInsideStringLiteral; }
		}

		public bool IsInsideVerbatimString
		{
			get { return engine.IsInsideVerbatimString; }
		}

		public bool IsInsideCharacter
		{
			get { return engine.IsInsideCharacter; }
		}

		public bool IsInsideString
		{
			get { return engine.IsInsideString; }
		}

		public bool IsInsideLineComment
		{
			get { return engine.IsInsideLineComment; }
		}

		public bool IsInsideMultiLineComment
		{
			get { return engine.IsInsideMultiLineComment; }
		}

		public bool IsInsideDocLineComment
		{
			get { return engine.IsInsideDocLineComment; }
		}

		public bool IsInsideComment
		{
			get { return engine.IsInsideComment; }
		}

		public bool IsInsideOrdinaryComment
		{
			get { return engine.IsInsideOrdinaryComment; }
		}

		public bool IsInsideOrdinaryCommentOrString
		{
			get { return engine.IsInsideOrdinaryCommentOrString; }
		}

		public bool LineBeganInsideVerbatimString
		{
			get { return engine.LineBeganInsideVerbatimString; }
		}

		public bool LineBeganInsideMultiLineComment
		{
			get { return engine.LineBeganInsideMultiLineComment; }
		}

		#endregion

		#region IClonable

		public IStateMachineIndentEngine Clone()
		{
			return new TextPasteIndentEngine(engine, textEditorOptions, formattingOptions);
		}

		IDocumentIndentEngine IDocumentIndentEngine.Clone()
		{
			return Clone();
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		#endregion
	}

	/// <summary>
	///     Types of text-paste strategies.
	/// </summary>
	public enum PasteStrategy : byte
	{
		PlainText = 0,
		StringLiteral = 1,
		VerbatimString = 2
	}

	/// <summary>
	///     Defines some helper methods for dealing with text-paste events.
	/// </summary>
	public static class TextPasteUtils
	{
		/// <summary>
		///     Collection of text-paste strategies.
		/// </summary>
		public static TextPasteStrategies Strategies = new TextPasteStrategies();

		/// <summary>
		///     The interface for a text-paste strategy.
		/// </summary>
		public interface IPasteStrategy
		{
			/// <summary>
			///     Formats the given text according with this strategy rules.
			/// </summary>
			/// <param name="text">
			///    The text to format.
			/// </param>
			/// <returns>
			///     Formatted text.
			/// </returns>
			string Encode(string text);

			/// <summary>
			///     Converts text formatted according with this strategy rules
			///     to its original form.
			/// </summary>
			/// <param name="text">
			///     Formatted text to convert.
			/// </param>
			/// <returns>
			///     Original form of the given formatted text.
			/// </returns>
			string Decode(string text);

			/// <summary>
			///     Type of this strategy.
			/// </summary>
			PasteStrategy Type { get; }
		}

		/// <summary>
		///     Wrapper that discovers all defined text-paste strategies and defines a way
		///     to easily access them through their <see cref="PasteStrategy"/> type.
		/// </summary>
		public sealed class TextPasteStrategies
		{
			/// <summary>
			///     Collection of discovered text-paste strategies.
			/// </summary>
			IDictionary<PasteStrategy, IPasteStrategy> strategies;

			/// <summary>
			///     Uses reflection to find all types derived from <see cref="IPasteStrategy"/>
			///     and adds an instance of each strategy to <see cref="strategies"/>.
			/// </summary>
			public TextPasteStrategies()
			{
				strategies = Assembly
					.GetExecutingAssembly()
					.GetTypes()
					.Where(t => typeof(IPasteStrategy).IsAssignableFrom(t) && t.IsClass)
					.Select(t => (IPasteStrategy)t.GetProperty("Instance").GetValue(null, null))
					.ToDictionary(s => s.Type);
			}

			/// <summary>
			///     Checks if there is a strategy of the given type and returns it.
			/// </summary>
			/// <param name="strategy">
			///     Type of the strategy instance.
			/// </param>
			/// <returns>
			///     A strategy instance of the requested type,
			///     or <see cref="DefaultStrategy"/> if it wasn't found.
			/// </returns>
			public IPasteStrategy this[PasteStrategy strategy]
			{
				get
				{
					if (strategies.ContainsKey(strategy))
					{
						return strategies[strategy];
					}

					return DefaultStrategy;
				}
			}
		}

		/// <summary>
		///     Doesn't do any formatting. Serves as the default strategy.
		/// </summary>
		public class PlainTextPasteStrategy : IPasteStrategy
		{
			#region Singleton

			public static IPasteStrategy Instance
			{
				get
				{
					return instance ?? (instance = new PlainTextPasteStrategy());
				}
			}

			static PlainTextPasteStrategy instance;

			protected PlainTextPasteStrategy()
			{
			}

			#endregion

			/// <inheritdoc />
			public string Encode(string text)
			{
				return text;
			}

			/// <inheritdoc />
			public string Decode(string text)
			{
				return text;
			}

			/// <inheritdoc />
			public PasteStrategy Type
			{
				get { return PasteStrategy.PlainText; }
			}
		}

		/// <summary>
		///     Escapes chars in the given text so that they don't
		///     break a valid string literal.
		/// </summary>
		public class StringLiteralPasteStrategy : IPasteStrategy
		{
			#region Singleton

			public static IPasteStrategy Instance
			{
				get
				{
					return instance ?? (instance = new StringLiteralPasteStrategy());
				}
			}

			static StringLiteralPasteStrategy instance;

			protected StringLiteralPasteStrategy()
			{
			}

			#endregion

			Dictionary<char, IEnumerable<char>> encodeReplace = new Dictionary<char, IEnumerable<char>> {
				{ '\"', "\\\"" },
				{ '\\', "\\\\" },
				{ '\n', "\\n" },
				{ '\r', "\\r" },
				{ '\t', "\\t" },
			};

			Dictionary<char, char> decodeReplace = new Dictionary<char, char> {
				{ '"', '"' },
				{ '\\', '\\' },
				{ 'n', '\n' },
				{ 'r', '\r' },
				{ 't', '\t' },
			};

			/// <inheritdoc />
			public string Encode(string text)
			{
				return string.Concat(text.SelectMany(c => encodeReplace.ContainsKey(c) ? encodeReplace[c] : new[] { c }));
			}

			/// <inheritdoc />
			public string Decode(string text)
			{
				var result = new StringBuilder();
				bool isEscaped = false;

				foreach (var ch in text)
				{
					if (isEscaped)
					{
						if (decodeReplace.ContainsKey(ch))
						{
							result.Append(decodeReplace[ch]);
						}
						else
						{
							result.Append('\\', ch);
						}
					}
					else if (ch != '\\')
					{
						result.Append(ch);
					}

					isEscaped = !isEscaped && ch == '\\';
				}

				return result.ToString();
			}

			/// <inheritdoc />
			public PasteStrategy Type
			{
				get { return PasteStrategy.StringLiteral; }
			}
		}

		/// <summary>
		///     Escapes chars in the given text so that they don't
		///     break a valid verbatim string.
		/// </summary>
		public class VerbatimStringPasteStrategy : IPasteStrategy
		{
			#region Singleton

			public static IPasteStrategy Instance
			{
				get
				{
					return instance ?? (instance = new VerbatimStringPasteStrategy());
				}
			}

			static VerbatimStringPasteStrategy instance;

			protected VerbatimStringPasteStrategy()
			{
			}

			#endregion

			Dictionary<char, IEnumerable<char>> encodeReplace = new Dictionary<char, IEnumerable<char>> {
				{ '\"', "\"\"" },
			};

			/// <inheritdoc />
			public string Encode(string text)
			{
				return string.Concat(text.SelectMany(c => encodeReplace.ContainsKey(c) ? encodeReplace[c] : new[] { c }));
			}

			/// <inheritdoc />
			public string Decode(string text)
			{
				bool isEscaped = false;
				return string.Concat(text.Where(c => !(isEscaped = !isEscaped && c == '"')));
			}

			/// <inheritdoc />
			public PasteStrategy Type
			{
				get { return PasteStrategy.VerbatimString; }
			}
		}

		/// <summary>
		///     The default text-paste strategy.
		/// </summary>
		public static IPasteStrategy DefaultStrategy = PlainTextPasteStrategy.Instance;

		/// <summary>
		///     String literal text-paste strategy.
		/// </summary>
		public static IPasteStrategy StringLiteralStrategy = StringLiteralPasteStrategy.Instance;

		/// <summary>
		///     Verbatim string text-paste strategy.
		/// </summary>
		public static IPasteStrategy VerbatimStringStrategy = VerbatimStringPasteStrategy.Instance;
	}
}
