using System;
using System.Windows.Forms;
using BizHawk.Client.Common;

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
				numericUpDown1 = { Value = GlobalWin.Config.GifWriterFrameskip },
				numericUpDown2 = { Value = GlobalWin.Config.GifWriterDelay }
			};
			dlg.NumericUpDown2_ValueChanged(null, null);

			var result = dlg.ShowDialog(parent);
			if (result.IsOk())
			{
				GlobalWin.Config.GifWriterFrameskip = (int)dlg.numericUpDown1.Value;
				GlobalWin.Config.GifWriterDelay = (int)dlg.numericUpDown2.Value;

				return GifWriter.GifToken.LoadFromConfig();
			}

			return null;
		}

		private void NumericUpDown2_ValueChanged(object sender, EventArgs e)
		{
			label3.Text = numericUpDown2.Value switch
			{
				-1 => "Auto",
				0 => "Fastest",
				_ => $"{(int) ((100 + numericUpDown2.Value / 2) / numericUpDown2.Value)} FPS"
			};
		}
	}
}
