using System;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class HexColorsForm : Form
	{
		public HexColorsForm()
		{
			InitializeComponent();
		}

		private void HexColors_Form_Load(object sender, EventArgs e)
		{
			HexBackgrnd.BackColor = Global.Config.HexBackgrndColor;
			HexForegrnd.BackColor = Global.Config.HexForegrndColor;
			HexMenubar.BackColor = Global.Config.HexMenubarColor;
			HexFreeze.BackColor = Global.Config.HexFreezeColor;
			HexFreezeHL.BackColor = Global.Config.HexHighlightFreezeColor;
			HexHighlight.BackColor = Global.Config.HexHighlightColor;
		}

		private void HexBackgrnd_Click(object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog() == DialogResult.OK)
			{
				Global.Config.HexBackgrndColor = colorDialog1.Color;
				GlobalWin.Tools.HexEditor.Header.BackColor = colorDialog1.Color;
				GlobalWin.Tools.HexEditor.MemoryViewerBox.BackColor = Global.Config.HexBackgrndColor;
				HexBackgrnd.BackColor = colorDialog1.Color;
			}
		}

		private void HexForegrnd_Click(object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog() == DialogResult.OK)
			{
				Global.Config.HexForegrndColor = colorDialog1.Color;
				GlobalWin.Tools.HexEditor.Header.ForeColor = colorDialog1.Color;
				GlobalWin.Tools.HexEditor.MemoryViewerBox.ForeColor = Global.Config.HexForegrndColor;
				HexForegrnd.BackColor = colorDialog1.Color;
			}
		}

		private void HexMenubar_Click(object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog() == DialogResult.OK)
			{
				Global.Config.HexMenubarColor = colorDialog1.Color;
				GlobalWin.Tools.HexEditor.HexMenuStrip.BackColor = Global.Config.HexMenubarColor;
				HexMenubar.BackColor = colorDialog1.Color;
			}
		}

		private void HexHighlight_Click(object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog() == DialogResult.OK)
			{
				Global.Config.HexHighlightColor = colorDialog1.Color;
				HexHighlight.BackColor = colorDialog1.Color;
			}
		}

		private void HexFreeze_Click(object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog() == DialogResult.OK)
			{
				Global.Config.HexFreezeColor = colorDialog1.Color;
				HexFreeze.BackColor = colorDialog1.Color;
			}
		}

		private void HexFreezeHL_Click(object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog() == DialogResult.OK)
			{
				Global.Config.HexHighlightFreezeColor = colorDialog1.Color;
				HexFreezeHL.BackColor = colorDialog1.Color;
			}
		}
	}
}
