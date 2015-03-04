using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class FileExtensionPreferencesPicker : UserControl
	{
		public FileExtensionPreferencesPicker()
		{
			InitializeComponent();
			AvailableSystems = new SystemLookup().AllSystems.ToList();
		}

		private readonly List<SystemLookup.SystemInfo> AvailableSystems;

		public string FileExtension { get; set; }
		public string OriginalPreference { get; set; }

		public string CurrentlySelectedSystemId
		{
			get
			{
				if (PlatformDropdown.SelectedIndex > 0)
				{
					return AvailableSystems
						.FirstOrDefault(x => x.SystemId == PlatformDropdown.SelectedItem.ToString()).FullName;
				}

				return string.Empty;
			}
		}

		private void PopulatePlatforms()
		{
			PlatformDropdown.Items.Add("Ask me on load");
			foreach (var platform in AvailableSystems)
			{
				PlatformDropdown.Items.Add(platform.FullName);
			}
		}

		private IEnumerable<string> DropdownSystemIds
		{
			get
			{
				var dispVals = PlatformDropdown.Items.OfType<string>();

				foreach (var val in dispVals)
				{
					yield return AvailableSystems.FirstOrDefault(x => x.FullName == val).SystemId ?? string.Empty;
				}
			}
		}

		private void FileExtensionPreferencesPicker_Load(object sender, EventArgs e)
		{
			PopulatePlatforms();

			var selectedSystemId = Global.Config.PreferredPlatformsForExtensions[FileExtension];
			if (!string.IsNullOrEmpty(selectedSystemId))
			{
				var selectedSystem = AvailableSystems.FirstOrDefault(s => s.SystemId == selectedSystemId);

				var selectedItem = PlatformDropdown.Items
					.OfType<string>()
					.FirstOrDefault(item => item == (selectedSystem != null ? selectedSystem.FullName : string.Empty));

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
