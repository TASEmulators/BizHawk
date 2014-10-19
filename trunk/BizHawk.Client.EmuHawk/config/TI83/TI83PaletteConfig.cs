using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Calculators;

namespace BizHawk.Client.EmuHawk
{
	public partial class TI83PaletteConfig : Form
	{
		public TI83PaletteConfig()
		{
			InitializeComponent();
		}

		private void TI83PaletteConfig_Load(object sender, EventArgs e)
		{
			var s = ((TI83)Global.Emulator).GetSettings();

			// Alpha hack because Winform is lame with colors
			BackgroundPanel.BackColor = Color.FromArgb(255, Color.FromArgb((int)s.BGColor));
			ForeGroundPanel.BackColor = Color.FromArgb(255, Color.FromArgb((int)s.ForeColor));
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			var ti83 = (TI83)Global.Emulator;
			var s = ti83.GetSettings();
			s.BGColor = (uint)BackgroundPanel.BackColor.ToArgb();
			s.ForeColor = (uint)ForeGroundPanel.BackColor.ToArgb();

			ti83.PutSettings(s);

			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void BackgroundPanel_Click(object sender, EventArgs e)
		{
			// custom colors are ints, not Color structs?
			// and they don't work right unless the alpha bits are set to 0
			// and the rgb order is switched
			int customColor = BackgroundPanel.BackColor.R | BackgroundPanel.BackColor.G << 8 | BackgroundPanel.BackColor.B << 16;

			var dlg = new ColorDialog
			{
				AllowFullOpen = true,
				AnyColor = true,
				Color = BackgroundPanel.BackColor,
				CustomColors = new int[] { customColor }
			};

			if (dlg.ShowDialog() == DialogResult.OK)
			{
				BackgroundPanel.BackColor = dlg.Color;
			}
		}

		private void ForeGroundPanel_Click(object sender, EventArgs e)
		{
			// custom colors are ints, not Color structs?
			// and they don't work right unless the alpha bits are set to 0
			// and the rgb order is switched
			int customColor = ForeGroundPanel.BackColor.R | ForeGroundPanel.BackColor.G << 8 | ForeGroundPanel.BackColor.B << 16;

			var dlg = new ColorDialog
			{
				AllowFullOpen = true,
				AnyColor = true,
				Color = ForeGroundPanel.BackColor,
				CustomColors = new int[] { customColor }
			};

			if (dlg.ShowDialog() == DialogResult.OK)
			{
				ForeGroundPanel.BackColor = dlg.Color;
			}
		}

		private void DefaultsBtn_Click(object sender, EventArgs e)
		{
			var s = new TI83.TI83Settings();
			BackgroundPanel.BackColor = Color.FromArgb((int)s.BGColor);
			ForeGroundPanel.BackColor = Color.FromArgb((int)s.ForeColor);
		}
	}
}
