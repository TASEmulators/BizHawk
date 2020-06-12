using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class FileExtensionPreferencesPicker : UserControl
	{
		public FileExtensionPreferencesPicker()
		{
			InitializeComponent();
			_availableSystems = new SystemLookup().AllSystems.ToList();
		}

		private readonly List<SystemLookup.SystemInfo> _availableSystems;

		public string FileExtension { get; set; }
		public string OriginalPreference { get; set; }

		public string CurrentlySelectedSystemId
		{
			get
			{
				if (PlatformDropdown.SelectedIndex > 0)
				{
					return _availableSystems
						.First(x => x.FullName == PlatformDropdown.SelectedItem.ToString()).SystemId;
				}

				return "";
			}
		}

		private void PopulatePlatforms()
		{
			PlatformDropdown.Items.Add("Ask me on load");
			foreach (var platform in _availableSystems)
			{
				PlatformDropdown.Items.Add(platform.FullName);
			}
		}

		private void FileExtensionPreferencesPicker_Load(object sender, EventArgs e)
		{
			PopulatePlatforms();

			var selectedSystemId = GlobalWin.Config.PreferredPlatformsForExtensions[FileExtension];
			if (!string.IsNullOrEmpty(selectedSystemId))
			{
				var selectedSystem = _availableSystems.FirstOrDefault(s => s.SystemId == selectedSystemId);

				var selectedItem = PlatformDropdown.Items
					.OfType<string>()
					.FirstOrDefault(item => item == (selectedSystem != null ? selectedSystem.FullName : ""));

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
