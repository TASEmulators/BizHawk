using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class N64VideoConfig : Form
	{
		public N64VideoConfig()
		{
			InitializeComponent();
		}

		private void N64VideoConfig_Load(object sender, EventArgs e)
		{
			string video_setting = Global.Config.N64VideoSizeX.ToString()
									+ " x "
									+ Global.Config.N64VideoSizeY.ToString();

			int index = VideoResolutionComboBox.Items.IndexOf(video_setting);
			if (index > 0)
			{
				VideoResolutionComboBox.SelectedIndex = index;
			}
		}

		private void OK_Click(object sender, EventArgs e)
		{
			SaveSettings();
			Global.OSD.AddMessage("Video settings saved.");
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Global.OSD.AddMessage("Video config cancelled.");
			Close();
		}

		private void SaveSettings()
		{
			string video_settings = VideoResolutionComboBox.SelectedItem.ToString();
			string[] strArr = video_settings.Split('x');
			Global.Config.N64VideoSizeX = Int32.Parse(strArr[0].Trim());
			Global.Config.N64VideoSizeY = Int32.Parse(strArr[1].Trim());
			
			Global.MainForm.FlagNeedsReboot(); //TODO: this won't always be necessary, keep that in mind
		}
	}
}
