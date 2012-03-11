using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Emulation.Consoles.Sega;

namespace BizHawk.MultiClient
{
	public partial class SMSGraphicsConfig : Form
	{
		SMS sms;

		public SMSGraphicsConfig()
		{
			InitializeComponent();
		}

		private void SMSGraphicsConfig_Load(object sender, EventArgs e)
		{
			sms = Global.Emulator as SMS;
			DispOBJ.Checked = Global.Config.SMSDispOBJ;
			DispBG.Checked = Global.Config.SMSDispBG;
		}

		private void OK_Click(object sender, EventArgs e)
		{
			Global.Config.SMSDispOBJ = DispOBJ.Checked;
			Global.Config.SMSDispBG = DispBG.Checked;

			this.Close();
		}
	}
}
