using System;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class SMSGraphicsConfig : Form
	{

		public SMSGraphicsConfig()
		{
			InitializeComponent();
		}

		private void SMSGraphicsConfig_Load(object sender, EventArgs e)
		{
			DispOBJ.Checked = Global.Config.SMSDispOBJ;
			DispBG.Checked = Global.Config.SMSDispBG;
		}

		private void OK_Click(object sender, EventArgs e)
		{
			Global.Config.SMSDispOBJ = DispOBJ.Checked;
			Global.Config.SMSDispBG = DispBG.Checked;

			Close();
		}
	}
}
