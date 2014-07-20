using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.ControlExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class NewN64PluginSettings : Form
	{
		N64Settings s;
		N64SyncSettings ss;

		public NewN64PluginSettings()
		{
			InitializeComponent();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void SaveBtn_Click(object sender, EventArgs e)
		{
			var video_settings = VideoResolutionComboBox.SelectedItem.ToString();
			var strArr = video_settings.Split('x');
			s.VideoSizeX = int.Parse(strArr[0].Trim());
			s.VideoSizeY = int.Parse(strArr[1].Trim());

			ss.Core = CoreTypeDropdown.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.CoreType>();

			ss.Rsp = RspTypeDropdown.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.RspType>();

			ss.VideoPlugin = PluginComboBox.SelectedItem
				.ToString()
				.GetEnumFromDescription<PluginType>();

			PutSettings(s);
			PutSyncSettings(ss);

			DialogResult = DialogResult.OK;
			Close();
		}

		private void NewN64PluginSettings_Load(object sender, EventArgs e)
		{
			s = GetSettings();
			ss = GetSyncSettings();

			CoreTypeDropdown.PopulateFromEnum<N64SyncSettings.CoreType>(ss.Core);
			RspTypeDropdown.PopulateFromEnum<N64SyncSettings.RspType>(ss.Rsp);
			PluginComboBox.PopulateFromEnum<PluginType>(ss.VideoPlugin);

			var video_setting = s.VideoSizeX
						+ " x "
						+ s.VideoSizeY;

			var index = VideoResolutionComboBox.Items.IndexOf(video_setting);
			if (index >= 0)
			{
				VideoResolutionComboBox.SelectedIndex = index;
			}

			RicePropertyGrid.SelectedObject = ss.RicePlugin;
			Glidemk2PropertyGrid.SelectedObject = ss.Glide64mk2Plugin;
			GlidePropertyGrid.SelectedObject = ss.GlidePlugin;
			JaboPropertyGrid.SelectedObject = ss.JaboPlugin;
		}

		#region Setting Get/Set

		private static N64SyncSettings GetSyncSettings()
		{
			if (Global.Emulator is N64)
			{
				return (N64SyncSettings)Global.Emulator.GetSyncSettings();
			}

			return (N64SyncSettings)Global.Config.GetCoreSyncSettings<N64>()
					?? new N64SyncSettings();
		}

		private static N64Settings GetSettings()
		{
			if (Global.Emulator is N64)
			{
				return (N64Settings)Global.Emulator.GetSettings();
			}

			return (N64Settings)Global.Config.GetCoreSettings<N64>()
					?? new N64Settings();
		}

		private static void PutSyncSettings(N64SyncSettings s)
		{
			if (Global.Emulator is N64)
			{
				GlobalWin.MainForm.PutCoreSyncSettings(s);
			}
			else
			{
				Global.Config.PutCoreSyncSettings<N64>(s);
			}
		}

		private static void PutSettings(N64Settings s)
		{
			if (Global.Emulator is N64)
			{
				GlobalWin.MainForm.PutCoreSettings(s);
			}
			else
			{
				Global.Config.PutCoreSettings<N64>(s);
			}
		}

		#endregion
	}
}
