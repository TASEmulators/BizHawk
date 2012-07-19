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
			foreach (Match keyWordMatch in keyWords.Matches(LuaText.Text))
            {
				LuaText.Select(keyWordMatch.Index, keyWordMatch.Length);
				LuaText.SelectionColor = Color.Blue;
				LuaText.SelectionStart = selPos;
				LuaText.SelectionColor = Color.Black;
            }

		}

    }
}
