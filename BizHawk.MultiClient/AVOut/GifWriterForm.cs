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

				var result = dlg.ShowDialog(parent);
				if (result == DialogResult.OK)
				{
					Global.Config.GifWriterFrameskip = (int)dlg.numericUpDown1.Value;
					return GifWriter.GifToken.LoadFromConfig();
				}
				else
					return null;
			}
		}
	}
}
