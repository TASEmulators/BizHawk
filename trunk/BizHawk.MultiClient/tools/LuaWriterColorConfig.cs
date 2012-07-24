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
	public partial class LuaWriterColorConfig : Form
	{
        //Get existing global Lua color settings
        int KeyWordColor = Global.Config.LuaKeyWordColor;
        int CommentColor = Global.Config.LuaCommentColor;
        int StringColor = Global.Config.LuaStringColor;
        int SymbolColor = Global.Config.LuaSymbolColor;
        int LibraryColor = Global.Config.LuaLibraryColor;

		public LuaWriterColorConfig()
		{
			InitializeComponent();
		}

        private void LuaWriterColorConfig_Load(object sender, EventArgs e)
        {
            //Set the initial colors into the panels
            KeyWordColorDialog.Color = Color.FromArgb(KeyWordColor);
            CommentColorDialog.Color = Color.FromArgb(CommentColor);
            StringColorDialog.Color = Color.FromArgb(StringColor);
            SymbolColorDialog.Color = Color.FromArgb(SymbolColor);
            LibraryColorDialog.Color = Color.FromArgb(LibraryColor);
        }

        private void panelKeyWord_DoubleClick(object sender, EventArgs e)
        {
            if (KeyWordColorDialog.ShowDialog() == DialogResult.OK)
            {
                KeyWordColor = KeyWordColorDialog.Color.ToArgb();  //Set new color
                panelKeyWord.BackColor = KeyWordColorDialog.Color;  //Update panel color with selection
            }
        }

        private void panelComment_DoubleClick(object sender, EventArgs e)
        {
            if (CommentColorDialog.ShowDialog() == DialogResult.OK)
            {
                CommentColor = CommentColorDialog.Color.ToArgb();  //Set new color
                panelComment.BackColor = CommentColorDialog.Color;  //Update panel color with selection
            }
        }

        private void panelString_DoubleClick(object sender, EventArgs e)
        {
            if (StringColorDialog.ShowDialog() == DialogResult.OK)
            {
                StringColor = StringColorDialog.Color.ToArgb();  //Set new color
                panelString.BackColor = StringColorDialog.Color;  //Update panel color with selection
            }
        }
 
        private void panelSymbol_DoubleClick(object sender, EventArgs e)
        {
            if (SymbolColorDialog.ShowDialog() == DialogResult.OK)
            {
                SymbolColor = SymbolColorDialog.Color.ToArgb();  //Set new color
                panelSymbol.BackColor = SymbolColorDialog.Color;  //Update panel color with selection
            }
        }

        private void panelLibrary_DoubleClick(object sender, EventArgs e)
        {
            if (LibraryColorDialog.ShowDialog() == DialogResult.OK)
            {
                LibraryColor = LibraryColorDialog.Color.ToArgb();  //Set new color
                panelLibrary.BackColor = LibraryColorDialog.Color;  //Update panel color with selection
            }
        }

		private void OK_Click(object sender, EventArgs e)
		{

		}
    }
}
