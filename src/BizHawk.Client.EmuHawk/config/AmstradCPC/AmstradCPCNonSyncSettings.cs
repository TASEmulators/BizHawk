using System;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.Computers.AmstradCPC;

using EnumsNET;

namespace BizHawk.Client.EmuHawk
{
	public partial class AmstradCpcNonSyncSettings : Form
	{
		private readonly IMainFormForConfig _mainForm;
		private readonly AmstradCPC.AmstradCPCSettings _settings;

		public AmstradCpcNonSyncSettings(
			IMainFormForConfig mainForm,
			AmstradCPC.AmstradCPCSettings settings)
		{
			_mainForm = mainForm;
			_settings = settings;
			InitializeComponent();
			Icon = Properties.Resources.GameControllerIcon;
		}

		private void IntvControllerSettings_Load(object sender, EventArgs e)
		{
			// OSD Message Verbosity
			var osdTypes = Enums.GetNames<AmstradCPC.OSDVerbosity>();
			foreach (var val in osdTypes)
			{
				osdMessageVerbositycomboBox1.Items.Add(val);
			}
			osdMessageVerbositycomboBox1.SelectedItem = _settings.OSDMessageVerbosity.ToString();
			UpdateOSDNotes(Enums.Parse<AmstradCPC.OSDVerbosity>(osdMessageVerbositycomboBox1.SelectedItem.ToString()));
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			bool changed =
				_settings.OSDMessageVerbosity.ToString() != osdMessageVerbositycomboBox1.SelectedItem.ToString();

			if (changed)
			{
				_settings.OSDMessageVerbosity = Enums.Parse<AmstradCPC.OSDVerbosity>(osdMessageVerbositycomboBox1.SelectedItem.ToString());

				_mainForm.PutCoreSettings(_settings);

				DialogResult = DialogResult.OK;
				Close();
			}
			else
			{
				DialogResult = DialogResult.OK;
				Close();
			}
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			_mainForm.AddOnScreenMessage("Misc settings aborted");
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void UpdateOSDNotes(AmstradCPC.OSDVerbosity type)
		{
			switch (type)
			{
				case AmstradCPC.OSDVerbosity.Full:
					lblOSDVerbinfo.Text = "Show all OSD messages";
					break;
				case AmstradCPC.OSDVerbosity.Medium:
					lblOSDVerbinfo.Text = "Only show machine/device generated messages";
					break;
				case AmstradCPC.OSDVerbosity.None:
					lblOSDVerbinfo.Text = "No core-driven OSD messages";
					break;
			}
		}

		private void OSDComboBox_SelectionChangeCommitted(object sender, EventArgs e)
		{
			var cb = (ComboBox)sender;
			UpdateOSDNotes(Enums.Parse<AmstradCPC.OSDVerbosity>(cb.SelectedItem.ToString()));
		}
	}
}
