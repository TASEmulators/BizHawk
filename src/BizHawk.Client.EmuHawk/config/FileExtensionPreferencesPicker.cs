using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class FileExtensionPreferencesPicker : UserControl
	{
		private readonly IDictionary<string, string> _preferredPlatformsForExtensions;

		public FileExtensionPreferencesPicker(IDictionary<string, string> preferredPlatformsForExtensions)
		{
			_preferredPlatformsForExtensions = preferredPlatformsForExtensions;
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
					return EmulatorExtensions.SystemIDDisplayNames
						.First(x => x.Value == PlatformDropdown.SelectedItem.ToString()).Key;
				}

				return "";
			}
		}

		private void PopulatePlatforms()
		{
			PlatformDropdown.Items.Add("Ask me on load");
			foreach (var (systemId, fullName) in EmulatorExtensions.SystemIDDisplayNames)
			{
				PlatformDropdown.Items.Add(fullName);
			}
		}

		private void FileExtensionPreferencesPicker_Load(object sender, EventArgs e)
		{
			PopulatePlatforms();

			var selectedSystemId = _preferredPlatformsForExtensions[FileExtension];
			if (!string.IsNullOrEmpty(selectedSystemId))
			{
				if (EmulatorExtensions.SystemIDDisplayNames.TryGetValue(selectedSystemId, out string selectedSystem)
					&& PlatformDropdown.Items.Contains(selectedSystem))
				{
					PlatformDropdown.SelectedItem = selectedSystem;
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
