using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;

namespace BizHawk.MultiClient
{
	public partial class LuaWriter : Form
	{
		public string CurrentFile = "";
		public Regex keyWords = new Regex("and|break|do|else|if|end|false|for|function|in|local|nil|not|or|repeat|return|then|true|until|while|elseif");
		public LuaWriter()
		{
			InitializeComponent();
		}

		private void timer_Tick(object sender, EventArgs e)
		{
			int selPos = LuaText.SelectionStart;
			int selChars = LuaText.SelectedText.Length;

			LuaText.SelectAll();
			LuaText.SelectionColor = Color.Black;

			ColorReservedWords();

			ColorComments();

			ColorStrings();

			LuaText.Select(selPos, selChars);
		}

		private void ColorStrings()
		{
			int firstMark, opening, ending, endLine;

			char[] chars = { '"', '\'' };
			foreach (char mark in chars)
			{
				firstMark = LuaText.Find(mark.ToString());
				while (firstMark > 0)
				{
					opening = firstMark;
					if (LuaText.GetLineFromCharIndex(opening) + 1 == LuaText.Lines.Count())
						endLine = LuaText.Text.Length - 1;
					else
						endLine = LuaText.GetFirstCharIndexFromLine(LuaText.GetLineFromCharIndex(opening) + 1) - 1;

					ending = 0;

					if (opening != LuaText.Text.Length - 1)
					{
						if (opening + 1 != endLine)
						{
							ending = LuaText.Find(mark.ToString(), opening + 1, endLine, RichTextBoxFinds.MatchCase);
							if (ending > 0)
							{
								while (ending > 0)
								{
									if (!IsThisPartOfTheString(LuaText.Text.Substring(opening, ending - opening + 1)))
										break;
									else
										ending++;

									ending = LuaText.Find(mark.ToString(), ending, endLine, RichTextBoxFinds.MatchCase);
								}
							}
							else
								ending = endLine;
						}
						else
							ending = endLine;
					}
					else
						ending = endLine;

					if (opening != LuaText.Text.Length)
					{
						LuaText.Select(opening, ending - opening + 1);
						LuaText.SelectionColor = Color.FromArgb(Global.Config.LuaStringColor);
						if (ending >= LuaText.Text.Length)
							ending++;
						else
							break;

						firstMark = LuaText.Find(mark.ToString(), ending + 1, LuaText.Text.Length, RichTextBoxFinds.MatchCase);
					}
					else
						break;
				}
			}
		}

		private bool IsThisPartOfTheString(string wholestring)
		{
			int ammount = 0;
			for (int x = wholestring.Length - 2; x > -1; x--)
			{
				if (wholestring[x] == '\\')
					ammount++;
				else
					break;
			}

			return !(ammount % 2 == 0);
		}

		private void ColorComments()
		{
			foreach (Match CommentMatch in new Regex("--").Matches(LuaText.Text))
			{
				int endComment;

				if (LuaText.Text.Substring(CommentMatch.Index, 4) == "--[[")
				{
					if (LuaText.Find("]]", RichTextBoxFinds.MatchCase) > 0)
						endComment = LuaText.SelectionStart - CommentMatch.Index + 2;
					else
						endComment = LuaText.Text.Length;

					LuaText.Select(CommentMatch.Index, endComment);
					LuaText.SelectionColor = Color.FromArgb(Global.Config.LuaCommentColor);
				}
				else
				{
					if (LuaText.GetLineFromCharIndex(CommentMatch.Index) + 1 == LuaText.Lines.Count())
						endComment = LuaText.Text.Length - CommentMatch.Index;
					else
						endComment = LuaText.GetFirstCharIndexFromLine(LuaText.GetLineFromCharIndex(CommentMatch.Index) + 1) - CommentMatch.Index;

					LuaText.Select(CommentMatch.Index, endComment);
					LuaText.SelectionColor = Color.FromArgb(Global.Config.LuaCommentColor);
				}
			}
		}

		private void ColorReservedWords()
		{
			foreach (Match keyWordMatch in keyWords.Matches(LuaText.Text))
			{
				char before = ' ', after = ' ';

				if (keyWordMatch.Index > 0)
					if (keyWordMatch.Index > 5 && keyWordMatch.Value != "if" && LuaText.Text.Substring(keyWordMatch.Index - 4, 4) != "else")
						before = LuaText.Text[keyWordMatch.Index - 1];

				if (keyWordMatch.Index + keyWordMatch.Length != LuaText.Text.Length)
					if (keyWordMatch.Value != "else" && LuaText.Text.Substring(keyWordMatch.Index, 2) != "if")
						after = LuaText.Text[keyWordMatch.Index + keyWordMatch.Length];

				if (!char.IsLetterOrDigit(before) && !char.IsLetterOrDigit(after))
				{
					LuaText.Select(keyWordMatch.Index, keyWordMatch.Length);
					LuaText.SelectionColor = Color.FromArgb(Global.Config.LuaKeyWordColor);
				}
			}
		}

		private void LuaWriter_Load(object sender, EventArgs e)
		{
			if (!String.IsNullOrWhiteSpace(CurrentFile))
			{
				LoadCurrentFile();
			}
		}

		private void LoadCurrentFile()
		{
			var file = new FileInfo(CurrentFile);
			if (file.Exists == false)
			{
				return;
			}

			using (StreamReader sr = file.OpenText())
			{
				StringBuilder luaText = new StringBuilder();
				string s = "";
				while ((s = sr.ReadLine()) != null)
				{
					luaText.Append(s);
					luaText.Append('\n');
				}

				if (luaText.Length > 0)
				{
					LuaText.Text = luaText.ToString();
				}
			}
		}

		private void LuaWriter_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
		}

		private void LuaWriter_DragDrop(object sender, DragEventArgs e)
		{
			string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (Path.GetExtension(filePaths[0]) == (".lua") || Path.GetExtension(filePaths[0]) == (".txt"))
			{
				//TODO: save changes
				CurrentFile = filePaths[0];
				LoadCurrentFile();
			}
		}
	}
}