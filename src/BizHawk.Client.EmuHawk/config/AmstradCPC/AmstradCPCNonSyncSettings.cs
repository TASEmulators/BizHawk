using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Computers.AmstradCPC;

namespace BizHawk.Client.EmuHawk
{
	public partial class AmstradCpcNonSyncSettings : Form
	{
		private readonly ISettingsAdapter _settable;

		private readonly AmstradCPC.AmstradCPCSettings _settings;

		public AmstradCpcNonSyncSettings(ISettingsAdapter settable)
		{
			_settable = settable;
			_settings = (AmstradCPC.AmstradCPCSettings) _settable.GetSettings();
			InitializeComponent();
			Icon = Properties.Resources.GameControllerIcon;
		}

		private void IntvControllerSettings_Load(object sender, EventArgs e)
		{
			// OSD Message Verbosity
			var osdTypes = Enum.GetNames(typeof(AmstradCPC.OSDVerbosity));
			foreach (var val in osdTypes)
			{
				osdMessageVerbositycomboBox1.Items.Add(val);
			}
			osdMessageVerbositycomboBox1.SelectedItem = _settings.OSDMessageVerbosity.ToString();
			UpdateOSDNotes((AmstradCPC.OSDVerbosity)Enum.Parse(typeof(AmstradCPC.OSDVerbosity), osdMessageVerbositycomboBox1.SelectedItem.ToString()));
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			bool changed =
				_settings.OSDMessageVerbosity.ToString() != osdMessageVerbositycomboBox1.SelectedItem.ToString();

			if (changed)
			{
				_settings.OSDMessageVerbosity = (AmstradCPC.OSDVerbosity)Enum.Parse(typeof(AmstradCPC.OSDVerbosity), osdMessageVerbositycomboBox1.SelectedItem.ToString());

				_settable.PutCoreSettings(_settings);

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
			UpdateOSDNotes((AmstradCPC.OSDVerbosity)Enum.Parse(typeof(AmstradCPC.OSDVerbosity), cb.SelectedItem.ToString()));
		}
	}
}
