using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class FileExtensionPreferences : Form
	{
		public FileExtensionPreferences()
		{
			InitializeComponent();
		}

		private void FileExtensionPreferences_Load(object sender, EventArgs e)
		{
			int spacing = UIHelper.ScaleY(30);
			int count = 0;
			foreach (var kvp in Global.Config.PreferredPlatformsForExtensions)
			{
				FileExtensionPreferencesPicker picker = new FileExtensionPreferencesPicker
				{
					FileExtension = kvp.Key,
					OriginalPreference = kvp.Value,
					Location = new Point(UIHelper.ScaleX(15), UIHelper.ScaleY(15) + (spacing * count))
				};

				count++;
				PrefPanel.Controls.Add(picker);
			}
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{

			foreach (var picker in PrefPanel.Controls.OfType<FileExtensionPreferencesPicker>())
			{
				Global.Config.PreferredPlatformsForExtensions[picker.FileExtension] = picker.CurrentlySelectedSystemId;
			}

			GlobalWin.OSD.AddMessage("Rom Extension Preferences changed");
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			GlobalWin.OSD.AddMessage("Rom Extension Preferences cancelled");
			Close();
		}
	}
}
