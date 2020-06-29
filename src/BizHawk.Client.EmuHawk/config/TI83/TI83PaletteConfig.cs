using System;
using System.Drawing;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.Calculators;

namespace BizHawk.Client.EmuHawk
{
	public partial class TI83PaletteConfig : Form
	{
		private readonly IMainFormForConfig _mainForm;
		private readonly TI83.TI83Settings _settings;

		public TI83PaletteConfig(
			IMainFormForConfig mainForm,
			TI83.TI83Settings settings)
		{
			_mainForm = mainForm;
			_settings = settings;
			InitializeComponent();
			Icon = Properties.Resources.calculator_MultiSize;
		}

		private void TI83PaletteConfig_Load(object sender, EventArgs e)
		{
			// Alpha hack because Winform is lame with colors
			BackgroundPanel.BackColor = Color.FromArgb(255, Color.FromArgb((int)_settings.BGColor));
			ForeGroundPanel.BackColor = Color.FromArgb(255, Color.FromArgb((int)_settings.ForeColor));
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			_settings.BGColor = (uint)BackgroundPanel.BackColor.ToArgb();
			_settings.ForeColor = (uint)ForeGroundPanel.BackColor.ToArgb();

			_mainForm.PutCoreSettings(_settings);

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

			using var dlg = new ColorDialog
			{
				AllowFullOpen = true,
				AnyColor = true,
				Color = BackgroundPanel.BackColor,
				CustomColors = new[] { customColor }
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

			using var dlg = new ColorDialog
			{
				AllowFullOpen = true,
				AnyColor = true,
				Color = ForeGroundPanel.BackColor,
				CustomColors = new[] { customColor }
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
