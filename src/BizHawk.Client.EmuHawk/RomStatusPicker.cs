using System;
using System.Windows.Forms;

using BizHawk.API.ApiHawk;

namespace BizHawk.Client.EmuHawk
{
	public partial class RomStatusPicker : Form
	{
		public RomStatusPicker()
		{
			InitializeComponent();
			PickedStatus = RomStatus.Unknown;
		}

		public RomStatus PickedStatus { get; private set; }

		private void RomStatusPicker_Load(object sender, EventArgs e)
		{
			GoodRadio.Select();
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			if (GoodRadio.Checked)
			{
				PickedStatus = RomStatus.GoodDump;
			}
			else if (HomebrewRadio.Checked)
			{
				PickedStatus = RomStatus.Homebrew;
			}
			else if (HackRadio.Checked)
			{
				PickedStatus = RomStatus.Hack;
			}
			else if (HackRadio.Checked)
			{
				PickedStatus = RomStatus.TranslatedRom;
			}
			else if (BadRadio.Checked)
			{
				PickedStatus = RomStatus.BadDump;
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
