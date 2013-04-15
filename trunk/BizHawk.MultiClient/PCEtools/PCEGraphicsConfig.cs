using System;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class PCEGraphicsConfig : Form
	{
		public PCEGraphicsConfig()
		{
			InitializeComponent();
		}

		private void PCEGraphicsConfig_Load(object sender, EventArgs e)
		{
			DispOBJ1.Checked = Global.Config.PCEDispOBJ1;
			DispBG1.Checked = Global.Config.PCEDispBG1;
			DispOBJ2.Checked = Global.Config.PCEDispOBJ2;
			DispBG2.Checked = Global.Config.PCEDispBG2;
		}

		private void OK_Click(object sender, EventArgs e)
		{
			Global.Config.PCEDispOBJ1 = DispOBJ1.Checked;
			Global.Config.PCEDispBG1 = DispBG1.Checked;
			Global.Config.PCEDispOBJ2 = DispOBJ2.Checked;
			Global.Config.PCEDispBG2 = DispBG2.Checked;

			Close();
		}
	}
}
