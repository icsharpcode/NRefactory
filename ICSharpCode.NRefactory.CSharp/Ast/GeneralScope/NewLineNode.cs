using System;
namespace ICSharpCode.NRefactory.CSharp
{

	/// <summary>
	/// A New line node represents a line break in the text.
	/// </summary>
	public abstract class NewLineNode : AstNode
	{
		public override NodeType NodeType {
			get {
				return NodeType.Whitespace;
			}
		}

		public abstract UnicodeNewline NewLineType {
			get;
		}

		TextLocation startLocation;
		public override TextLocation StartLocation {
			get { 
				return startLocation;
			}
		}
		
		public override TextLocation EndLocation {
			get {
				return new TextLocation (startLocation.Line + 1, 1);
			}
		}

		public NewLineNode() : this (TextLocation.Empty)
		{
		}

		public NewLineNode(TextLocation startLocation)
		{
			this.startLocation = startLocation;
		}

		public sealed override string ToString(CSharpFormattingOptions formattingOptions)
		{
			return NewLine.GetString (NewLineType);
		}

		public override void AcceptVisitor(IAstVisitor visitor)
		{
			visitor.VisitNewLine (this);
		}
			
		public override T AcceptVisitor<T>(IAstVisitor<T> visitor)
		{
			return visitor.VisitNewLine (this);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitNewLine (this, data);
		}
	}

	class UnixNewLine : NewLineNode
	{
		public override UnicodeNewline NewLineType {
			get {
				return UnicodeNewline.LF;
			}
		}

		public UnixNewLine()
		{
		}

		public UnixNewLine(TextLocation startLocation) : base (startLocation)
		{
		}

		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			var o = other as UnixNewLine;
			return o != null;
		}
	}

	class WindowsNewLine : NewLineNode
	{
		public override UnicodeNewline NewLineType {
			get {
				return UnicodeNewline.CRLF;
			}
		}

		public WindowsNewLine()
		{
		}

		public WindowsNewLine(TextLocation startLocation) : base (startLocation)
		{
		}

		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			var o = other as WindowsNewLine;
			return o != null;
		}
	}

	class MacNewLine : NewLineNode
	{
		public override UnicodeNewline NewLineType {
			get {
				return UnicodeNewline.CR;
			}
		}

		public MacNewLine()
		{
		}

		public MacNewLine(TextLocation startLocation) : base (startLocation)
		{
		}

		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			var o = other as MacNewLine;
			return o != null;
		}
	}
}

