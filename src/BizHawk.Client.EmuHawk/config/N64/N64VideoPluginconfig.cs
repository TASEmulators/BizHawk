using System;
using System.Windows.Forms;

using BizHawk.Common.StringExtensions;
using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class N64VideoPluginConfig : Form
	{
		private readonly MainForm _mainForm;
		private readonly Config _config;
		private readonly IEmulator _emulator;
		private readonly N64Settings _s;
		private readonly N64SyncSettings _ss;

		private const string CustomResItemName = "Custom";

		private static readonly string[] ValidResolutions =
		{
			"320 x 240",
			"400 x 300",
			"480 x 360",
			"512 x 384",
			"640 x 480",
			"800 x 600",
			"1024 x 768",
			"1152 x 864",
			"1280 x 960",
			"1400 x 1050",
			"1600 x 1200",
			"1920 x 1440",
			"2048 x 1536",
			"2880 x 2160",
			CustomResItemName
		};

		private bool _programmaticallyChangingPluginComboBox = false;

		public N64VideoPluginConfig(
			MainForm mainForm,
			Config config,
			IEmulator emulator)
		{
			_mainForm = mainForm;
			_config = config;
			_emulator = emulator;

			// because mupen is a pile of garbage, this all needs to work even when N64 is not loaded
			if (_emulator is N64 n64)
			{
				_s = n64.GetSettings();
				_ss = n64.GetSyncSettings();
			}
			else
			{
				_s = (N64Settings)_config.GetCoreSettings<N64>()
					?? new N64Settings();
				_ss = (N64SyncSettings)_config.GetCoreSyncSettings<N64>()
					?? new N64SyncSettings();
			}

			InitializeComponent();
			Icon = Properties.Resources.monitor_MultiSize;
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			// Add confirmation of cancelling change
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void Button1_Click(object sender, EventArgs e)
		{
			SaveSettings();
			DialogResult = DialogResult.OK;
			Close();
		}

		private void SaveSettings()
		{
			// Global
			if (VideoResolutionComboBox.Text != CustomResItemName)
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

			_ss.VideoPlugin = PluginComboBox.Text switch
			{
				"Rice" => PluginType.Rice,
				"Glide64" => PluginType.Glide,
				"Glide64mk2" => PluginType.GlideMk2,
				"GLideN64" => PluginType.GLideN64,
				_ => _ss.VideoPlugin
			};

			_ss.Core = CoreTypeDropdown.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.CoreType>();

			_ss.Rsp = RspTypeDropdown.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.RspType>();

			if (_emulator is N64)
			{
				_mainForm.PutCoreSettings(_s);
				_mainForm.PutCoreSyncSettings(_ss);
			}
			else
			{
				_config.PutCoreSettings<N64>(_s);
				_config.PutCoreSyncSettings<N64>(_ss);
			}
		} 

		private void N64VideoPluginConfig_Load(object sender, EventArgs e)
		{
			CoreTypeDropdown.PopulateFromEnum<N64SyncSettings.CoreType>(_ss.Core);
			RspTypeDropdown.PopulateFromEnum<N64SyncSettings.RspType>(_ss.Rsp);

			switch (_ss.VideoPlugin)
			{
				case PluginType.GlideMk2:
					PluginComboBox.Text = "Glide64mk2";
					break;
				case PluginType.Glide:
					PluginComboBox.Text = "Glide64";
					break;
				case PluginType.Rice:
					PluginComboBox.Text = "Rice";
					break;
				case PluginType.GLideN64:
					PluginComboBox.Text = "GLideN64";
					break;
			}

			VideoResolutionXTextBox.Text = _s.VideoSizeX.ToString();
			VideoResolutionYTextBox.Text = _s.VideoSizeY.ToString();

			var videoSetting = $"{_s.VideoSizeX} x {_s.VideoSizeY}";

			var index = VideoResolutionComboBox.Items.IndexOf(videoSetting);
			if (index >= 0)
			{
				VideoResolutionComboBox.SelectedIndex = index;
			}
			else if (PluginComboBox.SelectedIndex != 4) // wtf
			{
				VideoResolutionComboBox.SelectedIndex =
					VideoResolutionComboBox.Items.IndexOf(CustomResItemName);
				ShowCustomVideoResolutionControls();
			}

			RicePropertyGrid.SelectedObject = _ss.RicePlugin;
			GlidePropertyGrid.SelectedObject = _ss.GlidePlugin;
			Glide64Mk2PropertyGrid.SelectedObject = _ss.Glide64mk2Plugin;
			GlideN64PropertyGrid.SelectedObject = _ss.GLideN64Plugin;
		}

		private void PluginComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_programmaticallyChangingPluginComboBox)
			{
				return;
			}

			if (VideoResolutionComboBox.SelectedItem == null)
			{
				VideoResolutionComboBox.SelectedIndex = 0;
			}

			string[] strArr;
			int oldSizeX, oldSizeY;

			var oldResolution = VideoResolutionComboBox.SelectedItem?.ToString() ?? "";
			if (oldResolution != CustomResItemName)
			{
				strArr = oldResolution.Split('x');
				oldSizeX = int.Parse(strArr[0].Trim());
				oldSizeY = int.Parse(strArr[1].Trim());
			}
			else
			{
				oldSizeX = int.Parse(VideoResolutionXTextBox.Text);
				oldSizeY = int.Parse(VideoResolutionYTextBox.Text);
			}

			// Change resolution list to the rest
			VideoResolutionComboBox.Items.Clear();
			VideoResolutionComboBox.Items.AddRange(ValidResolutions);

			// If the given resolution is in the table, pick it.
			// Otherwise find a best fit
			var index = VideoResolutionComboBox.Items.IndexOf(oldResolution);
			if (index >= 0)
			{
				VideoResolutionComboBox.SelectedIndex = index;
			}
			else
			{
				int bestFit = -1;
				for (int i = 0; i < VideoResolutionComboBox.Items.Count; i++)
				{
					if ((string)VideoResolutionComboBox.Items[i] != CustomResItemName)
					{
						string option = (string)VideoResolutionComboBox.Items[i];
						strArr = option.Split('x');
						int newSizeX = int.Parse(strArr[0].Trim());
						int newSizeY = int.Parse(strArr[1].Trim());
						if (oldSizeX < newSizeX || oldSizeX == newSizeX && oldSizeY < newSizeY)
						{
							if (i == 0)
							{
								bestFit = 0;
								break;
							}
							
							bestFit = i - 1;
							break;
						}
					}
				}

				if (bestFit < 0)
				{
					VideoResolutionComboBox.SelectedIndex = VideoResolutionComboBox.Items.Count - 1;
				}
				else
				{
					VideoResolutionComboBox.SelectedIndex = bestFit;
				}
			}
		}

		private void VideoResolutionComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (VideoResolutionComboBox.Text == CustomResItemName)
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
