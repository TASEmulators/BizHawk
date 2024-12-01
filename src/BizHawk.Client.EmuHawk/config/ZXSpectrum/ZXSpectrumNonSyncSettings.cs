using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;

namespace BizHawk.Client.EmuHawk
{
	public partial class ZxSpectrumNonSyncSettings : Form
	{
		private readonly ISettingsAdapter _settable;

		private readonly ZXSpectrum.ZXSpectrumSettings _settings;
		private int _bgColor;

		public ZxSpectrumNonSyncSettings(ISettingsAdapter settable)
		{
			_settable = settable;
			_settings = (ZXSpectrum.ZXSpectrumSettings) _settable.GetSettings();
			InitializeComponent();
			Icon = Properties.Resources.GameControllerIcon;
		}

		private void IntvControllerSettings_Load(object sender, EventArgs e)
		{
			_bgColor = _settings.BackgroundColor;

			SetBtnColor();

			checkBoxShowCoreBrdColor.Checked = _settings.UseCoreBorderForBackground;

			// OSD Message Verbosity
			var osdTypes = Enum.GetNames(typeof(ZXSpectrum.OSDVerbosity));
			foreach (var val in osdTypes)
			{
				osdMessageVerbositycomboBox1.Items.Add(val);
			}
			osdMessageVerbositycomboBox1.SelectedItem = _settings.OSDMessageVerbosity.ToString();
			UpdateOSDNotes((ZXSpectrum.OSDVerbosity)Enum.Parse(typeof(ZXSpectrum.OSDVerbosity), osdMessageVerbositycomboBox1.SelectedItem.ToString()));
		}

		private void SetBtnColor()
		{
			var c = System.Drawing.Color.FromArgb(_bgColor);
			buttonChooseBGColor.ForeColor = c;
			buttonChooseBGColor.BackColor = c;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			var changed = checkBoxShowCoreBrdColor.Checked != _settings.UseCoreBorderForBackground
				|| _bgColor != _settings.BackgroundColor
				|| osdMessageVerbositycomboBox1.SelectedItem.ToString() != _settings.OSDMessageVerbosity.ToString();

			if (changed)
			{
				_settings.OSDMessageVerbosity = (ZXSpectrum.OSDVerbosity)Enum.Parse(typeof(ZXSpectrum.OSDVerbosity), osdMessageVerbositycomboBox1.SelectedItem.ToString());
				_settings.BackgroundColor = _bgColor;
				_settings.UseCoreBorderForBackground = checkBoxShowCoreBrdColor.Checked;

				_settable.PutCoreSettings(_settings);

				DialogResult = DialogResult.OK;
				Close();
			}
			else
			{
				DialogResult = DialogResult.OK;
				Close();
			}
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void UpdateOSDNotes(ZXSpectrum.OSDVerbosity type)
		{
			switch (type)
			{
				case ZXSpectrum.OSDVerbosity.Full:
					lblOSDVerbinfo.Text = "Show all OSD messages";
					break;
				case ZXSpectrum.OSDVerbosity.Medium:
					lblOSDVerbinfo.Text = "Only show machine/device generated messages";
					break;
				case ZXSpectrum.OSDVerbosity.None:
					lblOSDVerbinfo.Text = "No core-driven OSD messages";
					break;
			}
		}

		private void OSDComboBox_SelectionChangeCommitted(object sender, EventArgs e)
		{
			var cb = (ComboBox)sender;
			UpdateOSDNotes((ZXSpectrum.OSDVerbosity)Enum.Parse(typeof(ZXSpectrum.OSDVerbosity), cb.SelectedItem.ToString()));
		}

		private void buttonChooseBGColor_Click(object sender, EventArgs e)
		{
			var currColor = _settings.BackgroundColor;
			System.Drawing.Color c = System.Drawing.Color.FromArgb(currColor);
			using var cd = new ColorDialog();

			System.Drawing.Color[] colors =
			{
				System.Drawing.Color.FromArgb(0x00, 0x00, 0x00),
				System.Drawing.Color.FromArgb(0x00, 0x00, 0xD7),
				System.Drawing.Color.FromArgb(0xD7, 0x00, 0xD7),
				System.Drawing.Color.FromArgb(0x00, 0xD7, 0x00),
				System.Drawing.Color.FromArgb(0x00, 0xD7, 0xD7),
				System.Drawing.Color.FromArgb(0xD7, 0xD7, 0x00),
				System.Drawing.Color.FromArgb(0xD7, 0xD7, 0xD7),
				System.Drawing.Color.FromArgb(0x00, 0x00, 0xFF),
				System.Drawing.Color.FromArgb(0x00, 0x00, 0x00),
				System.Drawing.Color.FromArgb(0x00, 0x00, 0xFF),
				System.Drawing.Color.FromArgb(0xFF, 0x00, 0x00),
				System.Drawing.Color.FromArgb(0xFF, 0x00, 0xFF),
				System.Drawing.Color.FromArgb(0x00, 0xFF, 0x00),
				System.Drawing.Color.FromArgb(0x00, 0xFF, 0xFF),
				System.Drawing.Color.FromArgb(0xFF, 0xFF, 0x00),
				System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF)
			};

			cd.CustomColors = new[]
			{
				System.Drawing.ColorTranslator.ToOle(colors[0]),
				System.Drawing.ColorTranslator.ToOle(colors[1]),
				System.Drawing.ColorTranslator.ToOle(colors[2]),
				System.Drawing.ColorTranslator.ToOle(colors[3]),
				System.Drawing.ColorTranslator.ToOle(colors[4]),
				System.Drawing.ColorTranslator.ToOle(colors[5]),
				System.Drawing.ColorTranslator.ToOle(colors[6]),
				System.Drawing.ColorTranslator.ToOle(colors[7]),
				System.Drawing.ColorTranslator.ToOle(colors[8]),
				System.Drawing.ColorTranslator.ToOle(colors[9]),
				System.Drawing.ColorTranslator.ToOle(colors[10]),
				System.Drawing.ColorTranslator.ToOle(colors[11]),
				System.Drawing.ColorTranslator.ToOle(colors[12]),
				System.Drawing.ColorTranslator.ToOle(colors[13]),
				System.Drawing.ColorTranslator.ToOle(colors[14]),
				System.Drawing.ColorTranslator.ToOle(colors[15])
			};

			cd.Color = c;

			if (!cd.ShowDialog().IsOk()) return;

			_bgColor = cd.Color.ToArgb();
			SetBtnColor();
		}
	}
}
