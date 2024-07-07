using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class PlatformChooser : Form
	{
		private readonly Config _config;
		private readonly List<SystemLookup.SystemInfo> _availableSystems = new SystemLookup().AllSystems.ToList();
		

		public PlatformChooser(Config config)
		{
			_config = config;
			InitializeComponent();
		}

		public RomGame RomGame { get; set; }
		public string PlatformChoice { get; set; }

		private RadioButton SelectedRadio => PlatformsGroupBox.Controls.OfType<RadioButton>().FirstOrDefault(x => x.Checked);

		private void PlatformChooser_Load(object sender, EventArgs e)
		{
			RomSizeLabel.Text = RomGame.RomData.Length > 10 * 1024 * 1024
				? $"{RomGame.RomData.Length / 1024 / 1024:n0}mb"
				: $"{RomGame.RomData.Length / 1024:n0}kb";

			ExtensionLabel.Text = RomGame.Extension.ToLowerInvariant();
			HashBox.Text = RomGame.GameInfo.Hash;
			int count = 0;
			int spacing = 25;
			foreach (var platform in _availableSystems)
			{
				var radio = new RadioButton
				{
					Text = platform.FullName,
					Location = UIHelper.Scale(new Point(15, 15 + (count * spacing))),
					Size = UIHelper.Scale(new Size(200, 23))
				};

				PlatformsGroupBox.Controls.Add(radio);
				count++;
			}

			PlatformsGroupBox.Controls
				.OfType<RadioButton>()
				.First()
				.Select();
		}

		private void CancelButton_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			var selectedValue = SelectedRadio != null ? SelectedRadio.Text : "";
			PlatformChoice = _availableSystems.First(x => x.FullName == selectedValue).SystemId;

			if (AlwaysCheckbox.Checked)
			{
				_config.PreferredPlatformsForExtensions[RomGame.Extension.ToLowerInvariant()] = PlatformChoice;
			}

			Close();
		}

		private void label4_Click(object sender, EventArgs e)
			=> AlwaysCheckbox.Checked = !AlwaysCheckbox.Checked;
	}
}
