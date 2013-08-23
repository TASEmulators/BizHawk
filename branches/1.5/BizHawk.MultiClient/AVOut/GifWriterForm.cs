using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient.AVOut
{
	public partial class GifWriterForm : Form
	{
		public GifWriterForm()
		{
			InitializeComponent();
		}

		public static GifWriter.GifToken DoTokenForm(IWin32Window parent)
		{
			using (var dlg = new GifWriterForm())
			{
				dlg.numericUpDown1.Value = Global.Config.GifWriterFrameskip;
				dlg.numericUpDown2.Value = Global.Config.GifWriterDelay;
				dlg.numericUpDown2_ValueChanged(null, null);

				var result = dlg.ShowDialog(parent);
				if (result == DialogResult.OK)
				{
					Global.Config.GifWriterFrameskip = (int)dlg.numericUpDown1.Value;
					Global.Config.GifWriterDelay = (int)dlg.numericUpDown2.Value;
					return GifWriter.GifToken.LoadFromConfig();
				}
				else
					return null;
			}
		}

		private void numericUpDown2_ValueChanged(object sender, EventArgs e)
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
				label3.Text = string.Format("{0} FPS", (int)((100 + numericUpDown2.Value / 2) / numericUpDown2.Value));
			}
		}
	}
}
