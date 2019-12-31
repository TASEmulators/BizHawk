using System;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class GifWriterForm : Form
	{
		public GifWriterForm()
		{
			InitializeComponent();
		}

		public static GifWriter.GifToken DoTokenForm(IWin32Window parent)
		{
			using var dlg = new GifWriterForm
			{
				numericUpDown1 = { Value = Global.Config.GifWriterFrameskip },
				numericUpDown2 = { Value = Global.Config.GifWriterDelay }
			};
			dlg.NumericUpDown2_ValueChanged(null, null);

			var result = dlg.ShowDialog(parent);
			if (result.IsOk())
			{
				Global.Config.GifWriterFrameskip = (int)dlg.numericUpDown1.Value;
				Global.Config.GifWriterDelay = (int)dlg.numericUpDown2.Value;

				return GifWriter.GifToken.LoadFromConfig();
			}

			return null;
		}

		private void NumericUpDown2_ValueChanged(object sender, EventArgs e)
		{
			if (numericUpDown2.Value == -1)
			{
				label3.Text = "Auto";
			}
			else if (numericUpDown2.Value == 0)
			{
				label3.Text = "Fastest";
			}
			else
			{
				label3.Text = $"{(int)((100 + numericUpDown2.Value / 2) / numericUpDown2.Value)} FPS";
			}
		}
	}
}
