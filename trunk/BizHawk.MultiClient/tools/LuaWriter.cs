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
            int curPos = 0;

			foreach (Match keyWordMatch in keyWords.Matches(LuaText.Text))
            {
                LuaText.Select(curPos, keyWordMatch.Index);
                LuaText.SelectionColor = Color.Black;
				LuaText.Select(keyWordMatch.Index, keyWordMatch.Length);
				LuaText.SelectionColor = Color.Blue;
                curPos = keyWordMatch.Index + keyWordMatch.Length;
            }

            LuaText.Select(curPos, selPos);
            LuaText.SelectionColor = Color.Black;

            foreach (Match CommentMatch in new Regex("--").Matches(LuaText.Text))
            {
                int endComment;
                
                if (LuaText.GetLineFromCharIndex(CommentMatch.Index) + 1 == LuaText.Lines.Count())
                    endComment = LuaText.Text.Length - CommentMatch.Index;
                else
                    endComment = LuaText.GetFirstCharIndexFromLine(LuaText.GetLineFromCharIndex(CommentMatch.Index) + 1) - CommentMatch.Index;
                
                LuaText.Select(CommentMatch.Index, endComment);
                LuaText.SelectionColor = Color.Green;
            }

            LuaText.Select(selPos, selChars);
		}

    }
}
