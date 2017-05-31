using System;
using System.Windows.Forms;

using BizHawk.Common.StringExtensions;
using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class NewN64PluginSettings : Form
	{
		private N64Settings _s;
		private N64SyncSettings _ss;

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
			if (VideoResolutionComboBox.Text != "Custom")
			{
				var videoSettings = VideoResolutionComboBox.SelectedItem.ToString();
				var strArr = videoSettings.Split('x');
				_s.VideoSizeX = int.Parse(strArr[0].Trim());
				_s.VideoSizeY = int.Parse(strArr[1].Trim());
			}
			else
			{
				_s.VideoSizeX =
					VideoResolutionXTextBox.Text.IsUnsigned() ?
					int.Parse(VideoResolutionXTextBox.Text) : 320;

				_s.VideoSizeY =
					VideoResolutionYTextBox.Text.IsUnsigned() ?
					int.Parse(VideoResolutionYTextBox.Text) : 240;
			}

			_ss.Core = CoreTypeDropdown.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.CoreType>();

			_ss.Rsp = RspTypeDropdown.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.RspType>();

			_ss.VideoPlugin = PluginComboBox.SelectedItem
				.ToString()
				.GetEnumFromDescription<PluginType>();

			PutSettings(_s);
			PutSyncSettings(_ss);

			DialogResult = DialogResult.OK;
			Close();
		}

		private void NewN64PluginSettings_Load(object sender, EventArgs e)
		{
			_s = GetSettings();
			_ss = GetSyncSettings();

			CoreTypeDropdown.PopulateFromEnum<N64SyncSettings.CoreType>(_ss.Core);
			RspTypeDropdown.PopulateFromEnum<N64SyncSettings.RspType>(_ss.Rsp);
			PluginComboBox.PopulateFromEnum<PluginType>(_ss.VideoPlugin);

			VideoResolutionXTextBox.Text = _s.VideoSizeX.ToString();
			VideoResolutionYTextBox.Text = _s.VideoSizeY.ToString();

			var videoSetting = _s.VideoSizeX
				+ " x "
				+ _s.VideoSizeY;

			var index = VideoResolutionComboBox.Items.IndexOf(videoSetting);
			if (index >= 0)
			{
				VideoResolutionComboBox.SelectedIndex = index;
			}
			else if (PluginComboBox.SelectedIndex != 4)
			{
				VideoResolutionComboBox.SelectedIndex = 13;
				ShowCustomVideoResolutionControls();
			}

			RicePropertyGrid.SelectedObject = _ss.RicePlugin;
			Glidemk2PropertyGrid.SelectedObject = _ss.Glide64mk2Plugin;
			GlidePropertyGrid.SelectedObject = _ss.GlidePlugin;
			JaboPropertyGrid.SelectedObject = _ss.JaboPlugin;
		}

		#region Setting Get/Set

		private static N64SyncSettings GetSyncSettings()
		{
			if (Global.Emulator is N64)
			{
				return ((N64)Global.Emulator).GetSyncSettings();
			}

			return (N64SyncSettings)Global.Config.GetCoreSyncSettings<N64>()
					?? new N64SyncSettings();
		}

		private static N64Settings GetSettings()
		{
			if (Global.Emulator is N64)
			{
				return ((N64)Global.Emulator).GetSettings();
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

		private void VideoResolutionComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (VideoResolutionComboBox.Text == "Custom")
			{
				ShowCustomVideoResolutionControls();
			}
			else
			{
				HideCustomVideoResolutionControls();
				var newResolution = VideoResolutionComboBox.SelectedItem.ToString();
				var strArr = newResolution.Split('x');
				VideoResolutionXTextBox.Text = strArr[0].Trim();
				VideoResolutionYTextBox.Text = strArr[1].Trim();
			}
		}

		private void ShowCustomVideoResolutionControls()
		{
			LabelVideoResolutionX.Visible = true;
			LabelVideoResolutionY.Visible = true;
			VideoResolutionXTextBox.Visible = true;
			VideoResolutionYTextBox.Visible = true;
		}

		private void HideCustomVideoResolutionControls()
		{
			LabelVideoResolutionX.Visible = false;
			LabelVideoResolutionY.Visible = false;
			VideoResolutionXTextBox.Visible = false;
			VideoResolutionYTextBox.Visible = false;
		}
	}
}
