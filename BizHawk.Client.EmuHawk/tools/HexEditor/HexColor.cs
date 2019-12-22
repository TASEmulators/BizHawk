using System;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class HexColorsForm : Form
	{
		private readonly HexEditor _hexEditor;
		private readonly Config _config;

		public HexColorsForm(HexEditor hexEditor, Config config)
		{
			_hexEditor = hexEditor;
			_config = config;
			InitializeComponent();
		}

		private void HexColors_Form_Load(object sender, EventArgs e)
		{
			HexBackgrnd.BackColor = _config.HexBackgrndColor;
			HexForegrnd.BackColor = _config.HexForegrndColor;
			HexMenubar.BackColor = _config.HexMenubarColor;
			HexFreeze.BackColor = _config.HexFreezeColor;
			HexFreezeHL.BackColor = _config.HexHighlightFreezeColor;
			HexHighlight.BackColor = _config.HexHighlightColor;
		}

		private void HexBackground_Click(object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog() == DialogResult.OK)
			{
				_config.HexBackgrndColor = colorDialog1.Color;
				_hexEditor.Header.BackColor = colorDialog1.Color;
				_hexEditor.MemoryViewerBox.BackColor = _config.HexBackgrndColor;
				HexBackgrnd.BackColor = colorDialog1.Color;
			}
		}

		private void HexForeground_Click(object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog() == DialogResult.OK)
			{
				_config.HexForegrndColor = colorDialog1.Color;
				_hexEditor.Header.ForeColor = colorDialog1.Color;
				_hexEditor.MemoryViewerBox.ForeColor = _config.HexForegrndColor;
				HexForegrnd.BackColor = colorDialog1.Color;
			}
		}

		private void HexMenuBar_Click(object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog() == DialogResult.OK)
			{
				_config.HexMenubarColor = colorDialog1.Color;
				_hexEditor.HexMenuStrip.BackColor = _config.HexMenubarColor;
				HexMenubar.BackColor = colorDialog1.Color;
			}
		}

		private void HexHighlight_Click(object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog().IsOk())
			{
				_config.HexHighlightColor = colorDialog1.Color;
				HexHighlight.BackColor = colorDialog1.Color;
			}
		}

		private void HexFreeze_Click(object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog().IsOk())
			{
				_config.HexFreezeColor = colorDialog1.Color;
				HexFreeze.BackColor = colorDialog1.Color;
			}
		}

		private void HexFreezeHL_Click(object sender, MouseEventArgs e)
		{
			if (colorDialog1.ShowDialog().IsOk())
			{
				_config.HexHighlightFreezeColor = colorDialog1.Color;
				HexFreezeHL.BackColor = colorDialog1.Color;
			}
		}
	}
}
