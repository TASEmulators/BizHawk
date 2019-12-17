using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class FileExtensionPreferences : Form
	{
		private readonly Config _config;

		public FileExtensionPreferences(Config config)
		{
			_config = config;
			InitializeComponent();
		}

		private void FileExtensionPreferences_Load(object sender, EventArgs e)
		{
			int spacing = UIHelper.ScaleY(30);
			int count = 0;
			foreach (var kvp in _config.PreferredPlatformsForExtensions)
			{
				var picker = new FileExtensionPreferencesPicker
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
				_config.PreferredPlatformsForExtensions[picker.FileExtension] = picker.CurrentlySelectedSystemId;
			}

			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
