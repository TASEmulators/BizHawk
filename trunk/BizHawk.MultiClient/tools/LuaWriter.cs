using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient.tools
{
    public partial class LuaWriter : Form
    {
        string[] reserv = {"and", "break", "do", "else", "elseif", "end", "false", "for", "function", "if", "in", "local", "nil", "not", "or", "repeat", "return", "then", "true", "until", "while"};
        public LuaWriter()
        {
            InitializeComponent();
        }

        private void LuaText_TextChanged(object sender, EventArgs e)
        {
            int pos = LuaText.SelectionStart;
            foreach (string str in reserv)
            {
                int temppos = 0;
                do
                {
                    LuaText.Find(str, temppos, RichTextBoxFinds.WholeWord | RichTextBoxFinds.MatchCase);
                    if (LuaText.SelectedText.Count() > 0)
                    {
                        LuaText.SelectionColor = Color.Blue;
                    }
                    LuaText.Select(LuaText.SelectionStart + LuaText.SelectedText.Count() + 1, 0);
                    temppos = LuaText.SelectionStart;

                } while (temppos < LuaText.Text.Count());
            }
            LuaText.Select(pos, 0);
            LuaText.SelectionColor = Color.Black;
        }

    }
}
