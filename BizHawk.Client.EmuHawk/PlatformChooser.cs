using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class PlatformChooser : Form
	{
		public RomGame RomGame { get; set; }
		public string PlatformChoice { get; set; }

		private RadioButton SelectedRadio
		{
			get
			{
				return PlatformsGroupBox.Controls.OfType<RadioButton>().FirstOrDefault(x => x.Checked);
			}
		}

		public PlatformChooser()
		{
			InitializeComponent();
		}

		private void PlatformChooser_Load(object sender, EventArgs e)
		{
			if (RomGame.RomData.Length > 10 * 1024 * 1024) // If 10mb, show in megabytes
			{
				RomSizeLabel.Text = string.Format("{0:n0}", (RomGame.RomData.Length / 1024 / 1024)) + "mb";
			}
			else
			{
				RomSizeLabel.Text = string.Format("{0:n0}", (RomGame.RomData.Length / 1024)) + "kb";
			}

			ExtensionLabel.Text = RomGame.Extension.ToLower();

			int count = 0;
			int spacing = 25;
			foreach (var platform in GlobalWin.MainForm.SupportedPlatforms)
			{
				var radio = new RadioButton
				{
					Text = platform.Value,
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
			var selectedValue = SelectedRadio != null ? SelectedRadio.Text : string.Empty;
			PlatformChoice = GlobalWin.MainForm.SupportedPlatforms.FirstOrDefault(x => x.Value == selectedValue).Key;

			if (AlwaysCheckbox.Checked)
			{
				Global.Config.PreferredPlatformsForExtensions[RomGame.Extension.ToLower()] = PlatformChoice;
			}

			Close();
		}

		private void label4_Click(object sender, EventArgs e)
		{
			AlwaysCheckbox.Checked ^= true;
		}
	}
}
