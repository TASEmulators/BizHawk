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
        public Regex keyWords = new Regex("and|break|do|else|elseif|end|false|for|function|if|in|local|nil|not|or|repeat|return|then|true|until|while");
        public LuaWriter()
        {
            InitializeComponent();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            int selPos = LuaText.SelectionStart;
            int selChars = LuaText.SelectedText.Length;

            ColorReservedWords();

            ColorComments();

            ColorStrings();

            LuaText.Select(selPos, selChars);
        }

        private void ColorStrings()
        {
            int firstQuote = LuaText.Find("\"", 0);
            while (firstQuote > 0)
            {
                if (LuaText.SelectionColor != Color.Green)
                {
                    if (LuaText.Text[LuaText.SelectionStart - 1] != '\\')
                    {
                        int stringStart = LuaText.SelectionStart;
                        int endLine = LuaText.GetFirstCharIndexFromLine(LuaText.GetLineFromCharIndex(stringStart) + 1) - 1;
                        int stringEnd = LuaText.Find("\"", stringStart + 1, endLine, RichTextBoxFinds.MatchCase);

                        if (stringEnd > 0)
                        {
                            LuaText.Select(stringStart, stringEnd - stringStart + 1);
                            firstQuote = LuaText.Find("\"", stringEnd + 1, RichTextBoxFinds.MatchCase);
                        }
                        else
                        {
                            LuaText.Select(stringStart, endLine - stringStart);
                            firstQuote = LuaText.Find("\"", endLine + 1, RichTextBoxFinds.MatchCase);
                        }

                        LuaText.SelectionColor = Color.Gray;
                    }
                }
                else
                    firstQuote = LuaText.Find("\"", LuaText.SelectionStart + 1, RichTextBoxFinds.MatchCase);
            }
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
                    LuaText.SelectionColor = Color.Green;
                }
                else
                {
                    if (LuaText.GetLineFromCharIndex(CommentMatch.Index) + 1 == LuaText.Lines.Count())
                        endComment = LuaText.Text.Length - CommentMatch.Index;
                    else
                        endComment = LuaText.GetFirstCharIndexFromLine(LuaText.GetLineFromCharIndex(CommentMatch.Index) + 1) - CommentMatch.Index;

                    LuaText.Select(CommentMatch.Index, endComment);
                    LuaText.SelectionColor = Color.Green;
                }
            }
        }

        private void ColorReservedWords()
        {
            int curPos = 0;

            foreach (Match keyWordMatch in keyWords.Matches(LuaText.Text))
            {
                LuaText.Select(curPos, keyWordMatch.Index);
                if (LuaText.SelectionColor != Color.Gray)
                {
                    LuaText.SelectionColor = Color.Black;
                    LuaText.Select(keyWordMatch.Index, keyWordMatch.Length);
                    LuaText.SelectionColor = Color.Blue;
                }
                curPos = keyWordMatch.Index + keyWordMatch.Length;
            }

            LuaText.Select(curPos, LuaText.Text.Length);
            LuaText.SelectionColor = Color.Black;
        }

    }
}