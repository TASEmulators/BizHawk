using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class FileExtensionPreferencesPicker : UserControl
	{
		public FileExtensionPreferencesPicker()
		{
			InitializeComponent();
		}

		public string FileExtension { get; set; }
		public string OriginalPreference { get; set; }

		public string CurrentlySelectedSystemId
		{
			get
			{
				if (PlatformDropdown.SelectedIndex > 0)
				{
					return GlobalWin.MainForm.SupportedPlatforms
						.FirstOrDefault(x => x.Value == PlatformDropdown.SelectedItem.ToString()).Key;
				}

				return string.Empty;
			}
		}

		private void PopulatePlatforms()
		{
			PlatformDropdown.Items.Add("Ask me on load");
			foreach (var platform in GlobalWin.MainForm.SupportedPlatforms)
			{
				PlatformDropdown.Items.Add(platform.Value);
			}
		}

		private IEnumerable<string> DropdownSystemIds
		{
			get
			{
				var dispVals = PlatformDropdown.Items.OfType<string>();

				foreach (var val in dispVals)
				{
					yield return GlobalWin.MainForm.SupportedPlatforms.FirstOrDefault(x => x.Value == val).Key ?? string.Empty;
				}
			}
		}

		private void FileExtensionPreferencesPicker_Load(object sender, EventArgs e)
		{
			PopulatePlatforms();

			var selectedSystemId = Global.Config.PreferredPlatformsForExtensions[FileExtension];
			if (!string.IsNullOrEmpty(selectedSystemId))
			{
				var selectedDispString = GlobalWin.MainForm.SupportedPlatforms[selectedSystemId];

				var selectedItem = PlatformDropdown.Items
					.OfType<string>()
					.FirstOrDefault(item => item == selectedDispString);

				if (selectedItem != null)
				{
					PlatformDropdown.SelectedItem = selectedItem;
				}
				else
				{
					PlatformDropdown.SelectedIndex = 0;
				}
			}
			else
			{
				PlatformDropdown.SelectedIndex = 0;
			}

			FileExtensionLabel.Text = FileExtension;
		}
	}
}
