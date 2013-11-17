using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class LuaWriterColorConfig : Form
	{
		//Get existing global Lua color settings
		int _textColor = Global.Config.LuaDefaultTextColor;
		int _keyWordColor = Global.Config.LuaKeyWordColor;
		int _commentColor = Global.Config.LuaCommentColor;
		int _stringColor = Global.Config.LuaStringColor;
		int _symbolColor = Global.Config.LuaSymbolColor;
		int _libraryColor = Global.Config.LuaLibraryColor;

		public LuaWriterColorConfig()
		{
			InitializeComponent();
		}

		private void LuaWriterColorConfig_Load(object sender, EventArgs e)
		{
			//Set the initial colors into the panels
			SetTextColor(_textColor);
			SetKeyWordColor(_keyWordColor);
			SetCommentColor(_commentColor);
			SetStringColor(_stringColor);
			SetSymbolColor(_symbolColor);
			SetLibraryColor(_libraryColor);

			BoldText.Checked = Global.Config.LuaDefaultTextBold;
			BoldKeyWords.Checked = Global.Config.LuaKeyWordBold;
			BoldComments.Checked = Global.Config.LuaCommentBold;
			BoldStrings.Checked = Global.Config.LuaStringBold;
			BoldSymbols.Checked = Global.Config.LuaSymbolBold;
			BoldLibraries.Checked = Global.Config.LuaLibraryBold;
		}

		private void SetTextColor(int color)
		{
			_textColor = color;   //Set new color
			panelText.BackColor = Color.FromArgb(color);   //Update panel color with new selection
		}

		private void SetKeyWordColor(int color)
		{
			_keyWordColor = color;   //Set new color
			panelKeyWord.BackColor = Color.FromArgb(color);   //Update panel color with new selection
		}

		private void SetCommentColor(int color)
		{
			_commentColor = color;   //Set new color
			panelComment.BackColor = Color.FromArgb(color);   //Update panel color with new selection
		}

		private void SetStringColor(int color)
		{
			_stringColor = color;   //Set new color
			panelString.BackColor = Color.FromArgb(color);   //Update panel color with new selection
		}

		private void SetSymbolColor(int color)
		{
			_symbolColor = color;   //Set new color
			panelSymbol.BackColor = Color.FromArgb(color);   //Update panel color with new selection
		}

		private void SetLibraryColor(int color)
		{
			_libraryColor = color;   //Set new color
			panelLibrary.BackColor = Color.FromArgb(color);   //Update panel color with new selection
		}

		//Pop up color dialog when double-clicked
		private void panelText_DoubleClick(object sender, EventArgs e)
		{
			if (TextColorDialog.ShowDialog() == DialogResult.OK)
			{
				SetTextColor(TextColorDialog.Color.ToArgb());
			}
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
			DialogResult = DialogResult.OK;
			Close();
		}

		private void SaveData()
		{
			//Colors
			Global.Config.LuaDefaultTextColor = _textColor;
			Global.Config.LuaKeyWordColor = _keyWordColor;
			Global.Config.LuaCommentColor = _commentColor;
			Global.Config.LuaStringColor = _stringColor;
			Global.Config.LuaSymbolColor = _symbolColor;
			Global.Config.LuaLibraryColor = _libraryColor;

			//Bold
			Global.Config.LuaDefaultTextBold = BoldText.Checked;
			Global.Config.LuaKeyWordBold = BoldKeyWords.Checked;
			Global.Config.LuaCommentBold = BoldComments.Checked;
			Global.Config.LuaStringBold = BoldStrings.Checked;
			Global.Config.LuaSymbolBold = BoldSymbols.Checked;
			Global.Config.LuaLibraryBold = BoldLibraries.Checked;
		}

		private void buttonDefaults_Click(object sender, EventArgs e)
		{
			SetTextColor(-16777216);
			SetKeyWordColor(-16776961);
			SetCommentColor(-16744448);
			SetStringColor(-8355712);
			SetSymbolColor(-16777216);
			SetLibraryColor(-16711681);

			BoldText.Checked = false;
			BoldKeyWords.Checked = false;
			BoldComments.Checked = false;
			BoldStrings.Checked = false;
			BoldSymbols.Checked = false;
			BoldLibraries.Checked = false;
		}
	}
}
