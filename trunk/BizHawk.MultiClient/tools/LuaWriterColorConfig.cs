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
            SetKeyWordColor(KeyWordColor);
            SetCommentColor(CommentColor);
            SetStringColor(StringColor);
            SetSymbolColor(SymbolColor);
            SetLibraryColor(LibraryColor);
        }

        private void SetKeyWordColor(int color)
        {
            KeyWordColor = color;   //Set new color
            panelKeyWord.BackColor = Color.FromArgb(color);   //Update panel color with new selection
        }

        private void SetCommentColor(int color)
        {
            CommentColor = color;   //Set new color
            panelComment.BackColor = Color.FromArgb(color);   //Update panel color with new selection
        }

        private void SetStringColor(int color)
        {
            StringColor = color;   //Set new color
            panelString.BackColor = Color.FromArgb(color);   //Update panel color with new selection
        }

        private void SetSymbolColor(int color)
        {
            SymbolColor = color;   //Set new color
            panelSymbol.BackColor = Color.FromArgb(color);   //Update panel color with new selection
        }

        private void SetLibraryColor(int color)
        {
            LibraryColor = color;   //Set new color
			panelLibrary.BackColor = Color.FromArgb(color);   //Update panel color with new selection
			//MessageBox.Show(color.ToString());
        }

        //Pop up color dialog when double-clicked
        private void panelKeyWord_DoubleClick(object sender, EventArgs e)
        {
            if (KeyWordColorDialog.ShowDialog() == DialogResult.OK)
            {
                SetKeyWordColor(KeyWordColorDialog.Color.ToArgb());
            }
        }

        //Pop up color dialog when double-clicked
        private void panelComment_DoubleClick(object sender, EventArgs e)
        {
            if (CommentColorDialog.ShowDialog() == DialogResult.OK)
            {
                SetCommentColor(CommentColorDialog.Color.ToArgb());
            }
        }

        //Pop up color dialog when double-clicked
        private void panelString_DoubleClick(object sender, EventArgs e)
        {
            if (StringColorDialog.ShowDialog() == DialogResult.OK)
            {
                SetStringColor(StringColorDialog.Color.ToArgb());
            }
        }

        //Pop up color dialog when double-clicked
        private void panelSymbol_DoubleClick(object sender, EventArgs e)
        {
            if (SymbolColorDialog.ShowDialog() == DialogResult.OK)
            {
                SetSymbolColor(SymbolColorDialog.Color.ToArgb());
            }
        }

        //Pop up color dialog when double-clicked
        private void panelLibrary_DoubleClick(object sender, EventArgs e)
        {
            if (LibraryColorDialog.ShowDialog() == DialogResult.OK)
            {
                SetLibraryColor(LibraryColorDialog.Color.ToArgb());
                LibraryColorDialog.Color = Color.FromArgb(10349567);
            }
        }

        private void OK_Click(object sender, EventArgs e)
		{
            SaveData();   //Save the chosen settings
            this.DialogResult = DialogResult.OK;
            this.Close();
		}

        private void SaveData()
        {
            Global.Config.LuaKeyWordColor = KeyWordColor;
            Global.Config.LuaCommentColor = CommentColor;
            Global.Config.LuaStringColor = StringColor;
            Global.Config.LuaSymbolColor = SymbolColor;
            Global.Config.LuaLibraryColor = LibraryColor;
        }

        private void buttonDefaults_Click(object sender, EventArgs e)
        {
            SetKeyWordColor(-16776961);
            SetCommentColor(-16744448);
            SetStringColor(-8355712);
            SetSymbolColor(-16777216);
			SetLibraryColor(-16711681);
        }
    }
}
