using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class HexColors_Form : Form
	{
		public HexColors_Form()
		{
			InitializeComponent();
		}

		private void HexColors_Form_Load(object sender, EventArgs e)
		{
			HexBackgrnd.BackColor = Global.Config.hexbackgrnd;
			HexForegrnd.BackColor = Global.Config.hexforegrnd;
			HexMenubar.BackColor = Global.Config.hexmenubar;
		}

		private void HexBackgrnd_Click(Object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				Global.Config.hexbackgrnd = colorDialog1.Color;
				Global.MainForm.HexEditor1.MemoryViewerBox.BackColor = Global.Config.hexbackgrnd;
				this.HexBackgrnd.BackColor = colorDialog1.Color;
			}
		}

		private void HexForegrnd_Click(Object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				Global.Config.hexforegrnd = colorDialog1.Color;
				Global.MainForm.HexEditor1.MemoryViewerBox.ForeColor = Global.Config.hexforegrnd;
				this.HexForegrnd.BackColor = colorDialog1.Color;

			}
		}

		private void HexMenubar_Click(Object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				Global.Config.hexmenubar = colorDialog1.Color;
				Global.MainForm.HexEditor1.menuStrip1.BackColor = Global.Config.hexmenubar;
				this.HexMenubar.BackColor = colorDialog1.Color;
			}
		}
	}
}
