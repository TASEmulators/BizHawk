using System.Windows.Forms;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class RomStatusPicker : Form
	{
		public RomStatusPicker()
		{
			InitializeComponent();
		}

		public RomStatus PickedStatus { get; private set; } = RomStatus.Unknown;

		private void RomStatusPicker_Load(object sender, EventArgs e)
		{
			GoodRadio.Select();
			pictureBox1.Image = pictureBox1.InitialImage = Resources.GreenCheck;
			pictureBox2.Image = pictureBox2.InitialImage = Resources.HomeBrew;
			pictureBox3.Image = pictureBox3.InitialImage = Resources.Hack;
			pictureBox4.Image = pictureBox4.InitialImage = Resources.Translation;
			pictureBox5.Image = pictureBox5.InitialImage = Resources.ExclamationRed;
			pictureBox6.Image = pictureBox6.InitialImage = Resources.ExclamationRed;
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
