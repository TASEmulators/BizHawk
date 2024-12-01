using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class FileExtensionPreferences : Form
	{
		private readonly IDictionary<string, string> _preferredPlatformsForExtensions;

		public FileExtensionPreferences(IDictionary<string, string> preferredPlatformsForExtensions)
		{
			_preferredPlatformsForExtensions = preferredPlatformsForExtensions;
			InitializeComponent();
		}

		private void FileExtensionPreferences_Load(object sender, EventArgs e)
		{
			int spacing = UIHelper.ScaleY(30);
			int count = 0;
			foreach (var (fileExt, sysID) in _preferredPlatformsForExtensions)
			{
				var picker = new FileExtensionPreferencesPicker(_preferredPlatformsForExtensions)
				{
					FileExtension = fileExt,
					OriginalPreference = sysID,
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
				_preferredPlatformsForExtensions[picker.FileExtension] = picker.CurrentlySelectedSystemId;
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
