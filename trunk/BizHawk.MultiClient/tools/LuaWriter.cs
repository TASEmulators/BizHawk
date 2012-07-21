using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace BizHawk.MultiClient
{
    public partial class LuaWriter : Form
    {
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

			ColorCharacters();

            LuaText.Select(selPos, selChars);
        }

		private void ColorCharacters()
		{
			Color color = Color.Gray;
			Color commentColor = Color.Green;

			int firstApostrophe = LuaText.Find("'", 0);
			while (firstApostrophe >= 0)
			{
				if (LuaText.SelectionColor != commentColor)
				{
					int opening = firstApostrophe;
					int endLine;

					if (LuaText.Lines[LuaText.GetLineFromCharIndex(LuaText.GetFirstCharIndexOfCurrentLine())] == LuaText.Lines[LuaText.Lines.Length - 1])
						endLine = LuaText.Text.Length;
					else
						endLine = LuaText.GetFirstCharIndexFromLine(LuaText.GetLineFromCharIndex(opening) + 1) - 1;

					int ending = LuaText.Find("'", opening + 1, endLine, RichTextBoxFinds.None);
					if (ending > 0)
						while (ending > 0)
						{
							if (LuaText.Text[ending - 1] == '\\')
								ending++;
							else
								break;

							if (ending > endLine)
							{
								ending = opening;
								break;
							}

							ending = LuaText.Find("'", ending, endLine, RichTextBoxFinds.None);
						}
					else
						ending = opening;

					if (opening != ending)
					{
						LuaText.Select(opening, ending - opening + 1);
						LuaText.SelectionColor = color;
						if (ending != LuaText.Text.Length - 1)
							firstApostrophe = LuaText.Find("'", ending + 1, RichTextBoxFinds.None);
						else
							break;
					}
					else
						if (endLine != LuaText.Text.Length)
							firstApostrophe = LuaText.Find("'", endLine + 1, RichTextBoxFinds.None);
						else
							break;
				}
				else
				{
					if (LuaText.SelectionStart == LuaText.Text.Length - 1)
						break;
					else
						firstApostrophe = LuaText.Find("'", LuaText.SelectionStart + 1, RichTextBoxFinds.None);
				}
			}
		}

        private void ColorStrings()
        {
			Color color = Color.Gray;
			Color commentColor = Color.Green;

			int firstQuotation = LuaText.Find("\"", 0);
			while (firstQuotation >= 0)
			{
				if (LuaText.SelectionColor != commentColor)
				{
					int opening = firstQuotation;
					int endLine;

					if (LuaText.Lines[LuaText.GetLineFromCharIndex(LuaText.GetFirstCharIndexOfCurrentLine())] == LuaText.Lines[LuaText.Lines.Length - 1])
						endLine = LuaText.Text.Length;
					else
						endLine = LuaText.GetFirstCharIndexFromLine(LuaText.GetLineFromCharIndex(opening) + 1) - 1;

					int ending = LuaText.Find("\"", opening + 1, endLine, RichTextBoxFinds.None);
					if (ending > 0)
						while (ending > 0)
						{
							if (LuaText.Text[ending - 1] == '\\')
								ending++;
							else
								break;

							if (ending > endLine)
							{
								ending = opening;
								break;
							}

							ending = LuaText.Find("\"", ending, endLine, RichTextBoxFinds.None);
						}
					else
						ending = opening;

					if (opening != ending)
					{
						LuaText.Select(opening, ending - opening + 1);
						LuaText.SelectionColor = color;
						if (ending != LuaText.Text.Length - 1)
							firstQuotation = LuaText.Find("\"", ending + 1, RichTextBoxFinds.None);
						else
							break;
					}
					else
						if (endLine != LuaText.Text.Length)
							firstQuotation = LuaText.Find("\"", endLine + 1, RichTextBoxFinds.None);
						else
							break;
				}
				else
				{
					if (LuaText.SelectionStart == LuaText.Text.Length - 1)
						break;
					else
						firstQuotation = LuaText.Find("\"", LuaText.SelectionStart + 1, RichTextBoxFinds.None);
				}
			}
        }

        private void ColorComments()
        {
			Color color = Color.Green;

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
					LuaText.SelectionColor = color;
                }
                else
                {
                    if (LuaText.GetLineFromCharIndex(CommentMatch.Index) + 1 == LuaText.Lines.Count())
                        endComment = LuaText.Text.Length - CommentMatch.Index;
                    else
                        endComment = LuaText.GetFirstCharIndexFromLine(LuaText.GetLineFromCharIndex(CommentMatch.Index) + 1) - CommentMatch.Index;

                    LuaText.Select(CommentMatch.Index, endComment);
					LuaText.SelectionColor = color;
                }
            }
        }

        private void ColorReservedWords()
        {
			Color color = Color.Blue;

            foreach (Match keyWordMatch in keyWords.Matches(LuaText.Text))
            {
                char before = ' ', after = ' ';

                if (keyWordMatch.Index > 0)
                    before = LuaText.Text[keyWordMatch.Index - 1];

                if (keyWordMatch.Index + keyWordMatch.Length != LuaText.Text.Length)
                    after = LuaText.Text[keyWordMatch.Index + keyWordMatch.Length];

                if (!char.IsLetterOrDigit(before) && !char.IsLetterOrDigit(after))
                {
                    LuaText.Select(keyWordMatch.Index, keyWordMatch.Length);
					LuaText.SelectionColor = color;
                }
            }
        }

    }
}