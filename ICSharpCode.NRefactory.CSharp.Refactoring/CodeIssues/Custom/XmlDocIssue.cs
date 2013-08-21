//
// XmlDocIssue.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.Refactoring;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using ICSharpCode.NRefactory.Documentation;
using ICSharpCode.NRefactory.Xml;
using ICSharpCode.NRefactory.Editor;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("Validate Xml documentation",
	                  Description = "Validate Xml docs",
	                  Category = IssueCategories.CompilerWarnings,
	                  Severity = Severity.Warning)]
	public class XmlDocIssue : GatherVisitorCodeIssueProvider
	{
		protected override IGatherVisitor CreateVisitor(BaseRefactoringContext context)
		{
			return new GatherVisitor(context);
		}

		class GatherVisitor : GatherVisitorBase<XmlDocIssue>
		{
			readonly List<Comment> storedXmlComment = new List<Comment>();

			public GatherVisitor(BaseRefactoringContext ctx)
				: base (ctx)
			{
			}

			void InvalideXmlComments()
			{
				if (storedXmlComment.Count == 0)
					return;
				var from = storedXmlComment.First().StartLocation;
				var to = storedXmlComment.Last().EndLocation;
				AddIssue(
					from,
					to,
					ctx.TranslateString("Xml comment is not placed before a valid language element"),
					ctx.TranslateString("Remove comment"),
					script => {
					var startOffset = script.GetCurrentOffset(from);
					var endOffset = script.GetCurrentOffset(to);
					endOffset += ctx.GetLineByOffset(endOffset).DelimiterLength;
					script.RemoveText(startOffset, endOffset - startOffset);
				});


				storedXmlComment.Clear();
			}

			public override void VisitComment(Comment comment)
			{
				if (comment.CommentType == CommentType.Documentation)
					storedXmlComment.Add(comment);
			}

			public override void VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
			{
				InvalideXmlComments();
				base.VisitNamespaceDeclaration(namespaceDeclaration);
			}

			public override void VisitUsingDeclaration(UsingDeclaration usingDeclaration)
			{
				InvalideXmlComments();
				base.VisitUsingDeclaration(usingDeclaration);
			}

			public override void VisitUsingAliasDeclaration(UsingAliasDeclaration usingDeclaration)
			{
				InvalideXmlComments();
				base.VisitUsingAliasDeclaration(usingDeclaration);
			}

			public override void VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration)
			{
				InvalideXmlComments();
				base.VisitExternAliasDeclaration(externAliasDeclaration);
			}

			TextLocation TranslateOffset (int offset)
			{
				int line = storedXmlComment.First ().StartLocation.Line;
				foreach (var cmt in storedXmlComment) {
					var next = offset - cmt.Content.Length;
					if (next <= 0)
						return new TextLocation(line, cmt.StartLocation.Column + 3 - next);
				}
				return new TextLocation(line, 1);
			}

			void AddXmlIssue(int offset, int length, string str)
			{
				AddIssue(TranslateOffset(offset),
				         TranslateOffset(offset + length),
				         str);
			}

			int SearchAttributeColumn(int x, int line)
			{
				var comment = storedXmlComment [Math.Max(0, Math.Min(storedXmlComment.Count - 1, line))];
				var idx = comment.Content.IndexOfAny(new char[] { '"', '\'' }, x);
				return idx < 0 ? x : idx + 1;
			}

			void CheckXmlDoc(AstNode node)
			{
				ResolveResult resolveResult = ctx.Resolve(node);
				IEntity member = null;
				if (resolveResult is TypeResolveResult)
					member = resolveResult.Type.GetDefinition();
				if (resolveResult is MemberResolveResult)
					member = ((MemberResolveResult)resolveResult).Member;
				var xml = new StringBuilder();
				var firstline = "<root>\n";
				xml.Append (firstline);
				foreach (var cmt in storedXmlComment)
					xml.Append(cmt.Content + "\n");
				xml.Append("</root>\n");

				var doc = new AXmlParser().Parse(new StringTextSource(xml.ToString ()));

				Stack<AXmlObject> stack = new Stack<AXmlObject> ();
				stack.Push (doc);
				foreach (var err in doc.SyntaxErrors)
					AddXmlIssue(err.StartOffset - firstline.Length, err.EndOffset - firstline.Length, err.Description);

				while (stack.Count > 0) {
					var cur = stack.Pop ();
					if (cur is AXmlElement) {
						var reader = cur as AXmlElement;
						switch (reader.Name) {
						case "typeparam":
						case "typeparamref":
							var name = reader.Attributes.FirstOrDefault (attr => attr.Name == "name");
							if (name == null)
								break;
							if (member.SymbolKind == SymbolKind.TypeDefinition) {
								var type = (ITypeDefinition)member;
								if (!type.TypeArguments.Any(arg => arg.Name == name.Value)) {
									AddXmlIssue(name.StartOffset - firstline.Length, name.Value.Length, string.Format(ctx.TranslateString("Type parameter '{0}' not found"), name));
								}
							}
							break;
						case "param":
						case "paramref":
							name = reader.Attributes.FirstOrDefault (attr => attr.Name == "name");
							if (name == null)
								break;
							var m = member as IParameterizedMember;
							if (m != null && m.Parameters.Any(p => p.Name == name.Value))
								break;
							AddXmlIssue(name.StartOffset - firstline.Length, name.Value.Length, string.Format(ctx.TranslateString("Parameter '{0}' not found"), name.Value));
							break;
						case "exception":
						case "seealso":
						case "see":
							var cref = reader.Attributes.FirstOrDefault (attr => attr.Name == "cref");
							if (cref == null)
								break;
							try {
								var trctx = ctx.Resolver.TypeResolveContext;

								if (member is IMember)
									trctx = trctx.WithCurrentTypeDefinition(member.DeclaringTypeDefinition).WithCurrentMember((IMember)member);
								if (member is ITypeDefinition)
									trctx = trctx.WithCurrentTypeDefinition((ITypeDefinition)member);
								var entity = IdStringProvider.FindEntity(cref.Value, trctx);
								if (entity == null) {
									AddXmlIssue(cref.StartOffset - firstline.Length, cref.Length, string.Format(ctx.TranslateString("Cannot find reference '{0}'"), cref));
								}
							} catch (Exception e) {
								AddXmlIssue(cref.StartOffset - firstline.Length, cref.Length, string.Format(ctx.TranslateString("Reference parsing error '{0}'."), e.Message));
							}
							break;

						}
					}
					foreach (var child in cur.Children)
						stack.Push (child);
				}
				storedXmlComment.Clear();
			}

			AstNode GetParameterHighlightNode(AstNode node, int i)
			{
				if (node is MethodDeclaration)
					return ((MethodDeclaration)node).Parameters.ElementAt(i).NameToken;
				if (node is ConstructorDeclaration)
					return ((ConstructorDeclaration)node).Parameters.ElementAt(i).NameToken;
				if (node is OperatorDeclaration)
					return ((OperatorDeclaration)node).Parameters.ElementAt(i).NameToken;
				if (node is IndexerDeclaration)
					return ((IndexerDeclaration)node).Parameters.ElementAt(i).NameToken;
				throw new InvalidOperationException("invalid parameterized node:" + node);
			}

			protected virtual void VisitXmlChildren(AstNode node)
			{
				AstNode next;
				var child = node.FirstChild;
				while (child != null && (child is Comment || child.Role == Roles.NewLine)) {
					next = child.NextSibling;
					child.AcceptVisitor(this);
					child = next;
				}

				CheckXmlDoc(node);

				for (; child != null; child = next) {
					// Store next to allow the loop to continue
					// if the visitor removes/replaces child.
					next = child.NextSibling;
					child.AcceptVisitor(this);
				}
				InvalideXmlComments();
			}

			public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
			{
				VisitXmlChildren(typeDeclaration);
			}

			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
			{
				var rr = ctx.Resolve(methodDeclaration) as MemberResolveResult;
				VisitXmlChildren(methodDeclaration);
			}

			public override void VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration)
			{
				VisitXmlChildren(delegateDeclaration);
			}

			public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
			{
				VisitXmlChildren(constructorDeclaration);
			}

			public override void VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration)
			{
				VisitXmlChildren(eventDeclaration);
			}

			public override void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
			{
				VisitXmlChildren(destructorDeclaration);
			}

			public override void VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration)
			{
				VisitXmlChildren(enumMemberDeclaration);
			}

			public override void VisitEventDeclaration(EventDeclaration eventDeclaration)
			{
				VisitXmlChildren(eventDeclaration);
			}

			public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
			{
				VisitXmlChildren(fieldDeclaration);
			}

			public override void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
			{
				VisitXmlChildren(indexerDeclaration);
			}

			public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
			{
				VisitXmlChildren(propertyDeclaration);
			}

			public override void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
			{
				VisitXmlChildren(operatorDeclaration);
			}
		}
	}
}
